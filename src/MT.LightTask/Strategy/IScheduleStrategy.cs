using System.ComponentModel.DataAnnotations;

namespace MT.LightTask;

/// <summary>
/// 调度策略接口
/// </summary>
public interface IScheduleStrategy
{
    /// <summary>
    /// 任务开始时间
    /// </summary>
    DateTimeOffset? StartTime { get; }

    /// <summary>
    /// 上次任务执行时间
    /// </summary>
    DateTimeOffset? LastRuntime { get; set; }

    /// <summary>
    /// 下一次运行时间
    /// </summary>
    /// <returns></returns>
    DateTimeOffset? NextRuntime { get; set; }

    /// <summary>
    /// 上一次运行任务耗时
    /// </summary>
    TimeSpan LastRunElapsedTime { get; set; }

    /// <summary>
    /// 任务超时时间
    /// </summary>
    TimeSpan Timeout { get; }
    bool WaitForExecute(Action<DateTimeOffset> handleNext, CancellationToken cancellationToken);
}

public enum TaskRunStatus
{
    [Display(Name = "运行中")]
    Running,
    [Display(Name = "成功")]
    Success,
    [Display(Name = "超时")]
    Timeout,
    [Display(Name = "取消")]
    Canceled,
    [Display(Name = "停用")]
    Disabled,
    [Display(Name = "异常")]
    OccurException,
}

public enum TaskScheduleStatus
{
    [Display(Name = "就绪")]
    Ready,
    [Display(Name = "运行中")]
    Running,
}