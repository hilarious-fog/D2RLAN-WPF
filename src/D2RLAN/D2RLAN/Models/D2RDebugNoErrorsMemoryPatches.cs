using System.Collections.Generic;
using System.Linq;

namespace D2RLAN.Models;

// Byte patches for D2R Debug Mode (No Errors).
public static class D2RDebugNoErrorsMemoryPatches
{
    public sealed class Patch
    {
        public Patch(
            string fileOffsetHex,
            string patchHex,
            string originalHex,
            bool isEnabled = true)
        {
            FileOffsetHex = fileOffsetHex;
            PatchHex = patchHex;
            OriginalHex = originalHex;
            IsEnabled = isEnabled;
        }

        public string FileOffsetHex { get; }
        public string PatchHex { get; }
        public string OriginalHex { get; }
        public bool IsEnabled { get; set; }
    }

    // Format per line: fileOffset: patchedByte originalByte
    public static IReadOnlyList<Patch> All { get; } = new Patch[]
    {
        new("0E6171", "90", "E9"),
        new("0E6172", "90", "CA"),
        new("0E6173", "90", "EB"),
        new("0E6174", "90", "1F"),
        new("0E6175", "90", "03"),
        //new("3330EB", "74", "75"),
        //new("33312B", "74", "75"),
        //new("33316B", "74", "75"),
        //new("333170", "00", "01"),
        //new("333171", "01", "00"),
        //new("339939", "90", "0F"),
        //new("33993A", "E9", "84"),
        new("43D74D", "75", "74"),
        new("43DA6B", "74", "75"),
        new("43DAEB", "74", "75"),
        new("43DB2B", "74", "75"),
        //new("CF9E2C", "74", "75"),
    };

    public static IEnumerable<Patch> GetEnabledPatches() =>
        All.Where(patch => patch.IsEnabled);
}
