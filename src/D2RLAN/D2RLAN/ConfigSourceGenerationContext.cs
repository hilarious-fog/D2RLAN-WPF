using System.Text.Json.Serialization;
using static D2RLAN.ViewModels.ShellViewModel;

namespace MemoryEditor
{
    [JsonSerializable(typeof(Config))]
    [JsonSerializable(typeof(MemoryConfig))]
    internal partial class ConfigSourceGenerationContext : JsonSerializerContext { }
}
