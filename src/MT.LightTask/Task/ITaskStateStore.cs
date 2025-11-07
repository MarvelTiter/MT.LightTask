using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask;

//TODO 持久化任务状态
public interface ITaskStateStore
{
    Task SaveTaskStateAsync(string taskName, TaskState state);
    Task<TaskState?> GetTaskStateAsync(string taskName);
    Task RemoveTaskStateAsync(string taskName);
}
public class TaskState
{
    public TaskRunStatus Status { get; set; }
    public DateTimeOffset? LastRuntime { get; set; }
    public TimeSpan LastRunElapsedTime { get; set; }
    public int RetryTimes { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? NextRuntime { get; set; }
}