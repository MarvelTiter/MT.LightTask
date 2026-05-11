#if NET8_0_OR_GREATER
namespace MT.LightTask.Storage;

[System.Text.Json.Serialization.JsonSerializable(typeof(TaskConfig))]
[System.Text.Json.Serialization.JsonSerializable(typeof(StrategyBuilder))]
[System.Text.Json.Serialization.JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true,
    PropertyNamingPolicy = System.Text.Json.Serialization.JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
)]
[System.Text.Json.Serialization.JsonSerializable(typeof(DateTimeOffset))]
[System.Text.Json.Serialization.JsonSerializable(typeof(DateTime))]
[System.Text.Json.Serialization.JsonSerializable(typeof(TimeSpan))]
[System.Text.Json.Serialization.JsonSerializable(typeof(int))]
[System.Text.Json.Serialization.JsonSerializable(typeof(TaskStatus))]
[System.Text.Json.Serialization.JsonSerializable(typeof(TaskScheduleStatus))]
[System.Text.Json.Serialization.JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class JsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
#endif