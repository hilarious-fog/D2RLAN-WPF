using System;
using System.IO;
using System.Runtime.InteropServices;

namespace D2RLAN.Models;

/// <summary>
/// Removes the Windows Mark of the Web (Zone.Identifier) so downloaded/copied executables
/// do not require manual "Unblock" in file properties.
/// </summary>
public static class FileUnblockHelper
{
    private const int ErrorFileNotFound = 2;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool DeleteFile(string lpFileName);

    public static bool TryUnblock(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return false;

        try
        {
            string zoneStreamPath = $"{filePath}:Zone.Identifier";
            if (DeleteFile(zoneStreamPath))
                return true;

            return Marshal.GetLastWin32Error() == ErrorFileNotFound;
        }
        catch
        {
            return false;
        }
    }
}
