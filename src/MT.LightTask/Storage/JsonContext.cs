using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MT.LightTask.Storage;

[JsonSerializable(typeof(TaskConfig))]
[JsonSerializable(typeof(StrategyBuilder))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(TaskStatus))]
[JsonSerializable(typeof(TaskScheduleStatus))]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class JsonContext : JsonSerializerContext
{
}
