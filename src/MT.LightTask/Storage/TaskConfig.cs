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

    public StrategyBuilder? Builder { get; set; }
}

public class TaskStatus
{
    [JsonConverter(typeof(JsonStringEnumConverter<TaskScheduleStatus>))]
    public TaskScheduleStatus ScheduleStatus { get; set; }
    public Dictionary<string, object?> Values { get; set; } = [];
}
