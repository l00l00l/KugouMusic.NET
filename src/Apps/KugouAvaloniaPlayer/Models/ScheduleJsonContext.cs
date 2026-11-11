using System.Text.Json.Serialization;

namespace KugouAvaloniaPlayer.Models;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(ScheduleConfig))]
internal partial class ScheduleJsonContext : JsonSerializerContext
{
}