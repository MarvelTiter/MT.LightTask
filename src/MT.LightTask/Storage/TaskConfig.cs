using System;
using System.ComponentModel.DataAnnotations;

namespace MT.LightTask.Storage;

public enum ScheduleType
{
    Once,
    Cron,
    Interval,
    Signal
}

public class TaskConfig
{
    [Display(Name = "任务名称")]
    public string Name { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;

    public ScheduleType Type { get; set; }

    // For Once
    public DateTimeOffset? StartTime { get; set; }

    // For Cron
    public string? Cron { get; set; }

    // For Interval
    public TimeSpan? Interval { get; set; }

    // Retry settings
    public int RetryLimit { get; set; }
    public int RetryIntervalBase { get; set; }

    // Whether the task should be started automatically
    public bool Enabled { get; set; } = true;
}
