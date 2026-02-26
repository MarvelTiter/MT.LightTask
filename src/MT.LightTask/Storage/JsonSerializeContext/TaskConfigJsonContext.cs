using System.Text.Json.Serialization;

namespace MT.LightTask.Storage;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(TaskConfig))]
[JsonSerializable(typeof(List<TaskConfig>))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
