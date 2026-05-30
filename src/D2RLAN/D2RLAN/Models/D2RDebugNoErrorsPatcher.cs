using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace D2RLAN.Models;

/// <summary>
/// Applies no-errors patches from <see cref="D2RDebugNoErrorsMemoryPatches"/>.
/// Offsets match <c>fc /b</c> output: byte positions in the PE file on disk, not loaded-module RVAs.
/// </summary>
public static class D2RDebugNoErrorsPatcher
{
    public const string SourceDebugExeFileName = "D2R_Debug.exe";
    public const string NoErrorsDebugExeFileName = "D2R_Debug_NoErrors.exe";

    public sealed class PatchResult
    {
        public int Total { get; init; }
        public int Succeeded { get; init; }
        public IReadOnlyList<string> Details { get; init; } = Array.Empty<string>();
    }

    public sealed class EnsureResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public string SourceExePath { get; init; } = string.Empty;
        public string LaunchExePath { get; init; } = string.Empty;
        public PatchResult? PatchResult { get; init; }
    }

    /// <summary>
    /// Copies <see cref="SourceDebugExeFileName"/> to <see cref="NoErrorsDebugExeFileName"/> and applies fc /b patches to the copy.
    /// </summary>
    public static EnsureResult EnsureNoErrorsExecutable(string gamePath)
    {
        string sourcePath = Path.Combine(gamePath, SourceDebugExeFileName);
        string launchPath = Path.Combine(gamePath, NoErrorsDebugExeFileName);

        if (!File.Exists(sourcePath))
        {
            return new EnsureResult
            {
                Success = false,
                ErrorMessage = $"{SourceDebugExeFileName} not found at: {sourcePath}",
                SourceExePath = sourcePath,
                LaunchExePath = launchPath
            };
        }

        FileUnblockHelper.TryUnblock(sourcePath);

        try
        {
            File.Copy(sourcePath, launchPath, overwrite: true);
            FileUnblockHelper.TryUnblock(launchPath);
        }
        catch (Exception ex)
        {
            return new EnsureResult
            {
                Success = false,
                ErrorMessage = $"Failed to copy {SourceDebugExeFileName} to {NoErrorsDebugExeFileName}: {ex.Message}",
                SourceExePath = sourcePath,
                LaunchExePath = launchPath
            };
        }

        PatchResult patchResult = ApplyToExecutableFile(launchPath);
        FileUnblockHelper.TryUnblock(launchPath);
        bool fileReady = File.Exists(launchPath);
        bool patchesOk = patchResult.Total > 0 && patchResult.Succeeded == patchResult.Total;
        bool success = fileReady && patchesOk;

        return new EnsureResult
        {
            Success = success,
            ErrorMessage = success
                ? null
                : !fileReady
                    ? $"{NoErrorsDebugExeFileName} was not created at: {launchPath}"
                    : $"Patching {NoErrorsDebugExeFileName} failed ({patchResult.Succeeded}/{patchResult.Total}).",
            SourceExePath = sourcePath,
            LaunchExePath = launchPath,
            PatchResult = patchResult
        };
    }

    public static string GetLaunchExecutablePath(string gamePath, bool noErrorsMode) =>
        noErrorsMode
            ? Path.Combine(gamePath, NoErrorsDebugExeFileName)
            : Path.Combine(gamePath, SourceDebugExeFileName);

    public static string GetDebugProcessName(bool noErrorsMode) =>
        noErrorsMode ? Path.GetFileNameWithoutExtension(NoErrorsDebugExeFileName) : "D2R_Debug";

    public static PatchResult ApplyToExecutableFile(string exePath)
    {
        if (!File.Exists(exePath))
            throw new FileNotFoundException("Debug executable not found.", exePath);

        var details = new List<string>();
        int succeeded = 0;
        int total = 0;

        using FileStream stream = new FileStream(
            exePath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.Read);

        foreach (D2RDebugNoErrorsMemoryPatches.Patch patch in D2RDebugNoErrorsMemoryPatches.GetEnabledPatches())
        {
            total++;
            if (TryPatchFileStream(stream, patch, out string detail))
            {
                succeeded++;
                details.Add($"OK — {detail}");
            }
            else
            {
                details.Add($"FAIL — {detail}");
            }
        }

        return new PatchResult { Total = total, Succeeded = succeeded, Details = details };
    }

    public static PatchResult ApplyToRunningProcess(
        string exePathOnDisk,
        IntPtr moduleBaseAddress,
        Func<IntPtr, byte?> tryReadByte,
        Func<IntPtr, byte, bool> tryWriteByte)
    {
        var details = new List<string>();
        int succeeded = 0;
        int total = 0;

        foreach (D2RDebugNoErrorsMemoryPatches.Patch patch in D2RDebugNoErrorsMemoryPatches.GetEnabledPatches())
        {
            total++;
            long fileOffset = ParseFileOffset(patch.FileOffsetHex);

            if (!PeFileOffsetMapper.TryFileOffsetToRva(exePathOnDisk, fileOffset, out long rva, out string mapDetail))
            {
                details.Add($"FAIL — file offset 0x{fileOffset:X}: {mapDetail}");
                continue;
            }

            IntPtr effectiveAddress = new IntPtr(moduleBaseAddress.ToInt64() + rva);
            byte patchByte = ParseSingleHexByte(patch.PatchHex);
            byte? expectedOriginal = string.IsNullOrWhiteSpace(patch.OriginalHex)
                ? null
                : ParseSingleHexByte(patch.OriginalHex);

            byte? current = tryReadByte(effectiveAddress);
            if (current == null)
            {
                details.Add(
                    $"FAIL — file 0x{fileOffset:X} -> RVA 0x{rva:X} (abs 0x{effectiveAddress.ToInt64():X}): unreadable");
                continue;
            }

            if (current == patchByte)
            {
                succeeded++;
                details.Add(
                    $"OK — file 0x{fileOffset:X} -> RVA 0x{rva:X}: already 0x{patchByte:X2}");
                continue;
            }

            if (expectedOriginal.HasValue && current != expectedOriginal.Value)
            {
                details.Add(
                    $"FAIL — file 0x{fileOffset:X} -> RVA 0x{rva:X}: found 0x{current:X2}, expected 0x{expectedOriginal:X2}");
                continue;
            }

            if (!tryWriteByte(effectiveAddress, patchByte))
            {
                details.Add($"FAIL — file 0x{fileOffset:X} -> RVA 0x{rva:X}: write failed");
                continue;
            }

            byte? verify = tryReadByte(effectiveAddress);
            if (verify != patchByte)
            {
                details.Add(
                    $"FAIL — file 0x{fileOffset:X} -> RVA 0x{rva:X}: verify read 0x{verify:X2}, wanted 0x{patchByte:X2}");
                continue;
            }

            succeeded++;
            details.Add(
                $"OK — file 0x{fileOffset:X} -> RVA 0x{rva:X}: 0x{current:X2} -> 0x{patchByte:X2}");
        }

        return new PatchResult { Total = total, Succeeded = succeeded, Details = details };
    }

    private static bool TryPatchFileStream(FileStream stream, D2RDebugNoErrorsMemoryPatches.Patch patch, out string detail)
    {
        detail = string.Empty;
        long offset = ParseFileOffset(patch.FileOffsetHex);
        byte patchByte = ParseSingleHexByte(patch.PatchHex);
        byte? expectedOriginal = string.IsNullOrWhiteSpace(patch.OriginalHex)
            ? null
            : ParseSingleHexByte(patch.OriginalHex);

        if (offset < 0 || offset >= stream.Length)
        {
            detail = $"file offset 0x{offset:X} is outside {Path.GetFileName(stream.Name)} (size 0x{stream.Length:X})";
            return false;
        }

        stream.Seek(offset, SeekOrigin.Begin);
        int read = stream.ReadByte();
        if (read < 0)
        {
            detail = $"file offset 0x{offset:X}: could not read";
            return false;
        }

        byte current = (byte)read;
        if (current == patchByte)
        {
            detail = $"file offset 0x{offset:X}: already 0x{patchByte:X2}";
            return true;
        }

        if (expectedOriginal.HasValue && current != expectedOriginal.Value)
        {
            detail = $"file offset 0x{offset:X}: found 0x{current:X2}, expected 0x{expectedOriginal.Value:X2}";
            return false;
        }

        stream.Seek(offset, SeekOrigin.Begin);
        stream.WriteByte(patchByte);
        stream.Flush();

        stream.Seek(offset, SeekOrigin.Begin);
        read = stream.ReadByte();
        if (read != patchByte)
        {
            detail = $"file offset 0x{offset:X}: verify failed (read 0x{read:X2}, wanted 0x{patchByte:X2})";
            return false;
        }

        detail = $"file offset 0x{offset:X}: 0x{current:X2} -> 0x{patchByte:X2}";
        return true;
    }

    private static long ParseFileOffset(string hex)
    {
        string normalized = hex.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[2..];

        return Convert.ToInt64(normalized, 16);
    }

    private static byte ParseSingleHexByte(string hex) =>
        Convert.FromHexString(hex.Trim())[0];
}

/// <summary>Maps PE on-disk file offsets to RVAs for runtime patching of an already-loaded module.</summary>
internal static class PeFileOffsetMapper
{
    public static bool TryFileOffsetToRva(
        string pePath,
        long fileOffset,
        out long rva,
        out string detail)
    {
        rva = 0;
        detail = string.Empty;

        try
        {
            byte[] image = File.ReadAllBytes(pePath);
            if (image.Length < 0x40)
            {
                detail = "file too small for PE header";
                return false;
            }

            int peOffset = BitConverter.ToInt32(image, 0x3C);
            if (peOffset <= 0 || peOffset + 0x18 > image.Length)
            {
                detail = "invalid PE header offset";
                return false;
            }

            ushort numberOfSections = BitConverter.ToUInt16(image, peOffset + 6);
            ushort sizeOfOptionalHeader = BitConverter.ToUInt16(image, peOffset + 20);
            int sectionTableOffset = peOffset + 24 + sizeOfOptionalHeader;

            for (int i = 0; i < numberOfSections; i++)
            {
                int sectionOffset = sectionTableOffset + i * 40;
                if (sectionOffset + 40 > image.Length)
                    break;

                uint virtualAddress = BitConverter.ToUInt32(image, sectionOffset + 12);
                uint sizeOfRawData = BitConverter.ToUInt32(image, sectionOffset + 16);
                uint pointerToRawData = BitConverter.ToUInt32(image, sectionOffset + 20);

                if (sizeOfRawData == 0)
                    continue;

                long sectionEnd = (long)pointerToRawData + sizeOfRawData;
                if (fileOffset >= pointerToRawData && fileOffset < sectionEnd)
                {
                    rva = virtualAddress + (fileOffset - pointerToRawData);
                    detail = $"section {i}, RVA 0x{rva:X}";
                    return true;
                }
            }

            detail = "offset not in any PE section";
            return false;
        }
        catch (Exception ex)
        {
            detail = ex.Message;
            return false;
        }
    }
}
