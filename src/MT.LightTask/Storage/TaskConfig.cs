using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MT.LightTask.Storage;

public enum ScheduleType
{
    None,
    Once,
    Cron,
    Interval,
    Signal
}

public class TaskConfig
{
    [Display(Name = "任务名称")]
    public string Name { get; set; } = string.Empty;
    public string TaskTypeName { get; set; } = string.Empty;

    //[JsonConverter(typeof(JsonStringEnumConverter<ScheduleType>))]
    //public ScheduleType Type { get; set; }
    //public string? Args { get; set; }
    public StrategyBuilder? Builder { get; set; }
}

public class TaskStatus
{
    public Dictionary<string, object?> Values { get; set; } = [];
}
