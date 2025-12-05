using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MT.LightTask;

public class TaskInfo
{
    [Display(Name = "任务名称")]
    public string? Name { get; set; }
    [Display(Name = "调度状态")]
    public TaskScheduleStatus ScheduleStatus { get; set; }
    [Display(Name = "任务状态")]
    public TaskRunStatus TaskStatus { get; set; }
    [Display(Name = "最后运行时间")]
    public DateTimeOffset? LastRuntime { get; set; }
    [Display(Name = "下次运行时间")]
    public DateTimeOffset? NextRuntime { get; set; }
    [Display(Name = "最后运行耗时")]
    public TimeSpan? LastRunElapsedTime { get; set; }
    public Exception? Exception { get; set; }
}
