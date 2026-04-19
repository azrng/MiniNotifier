using System.Text.Json.Serialization;
using MiniNotifier.Models.Entities;

namespace MiniNotifier.Models.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(HydrationSettingsDocument))]
internal sealed partial class HydrationSettingsJsonContext : JsonSerializerContext;
