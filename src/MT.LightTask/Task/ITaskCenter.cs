using MT.LightTask.Storage;

namespace MT.LightTask;

public readonly struct TaskEventArgs(ITaskScheduler task, ITaskCenter center)
{
    public ITaskScheduler Scheduler { get; } = task;
    public ITaskCenter Center { get; } = center;

    public void RemoveFromCenter()
    {
        Center.Remove(Scheduler.Name);
    }
}

public interface ITaskCenter
{
    IServiceProvider ServiceProvider { get; }
    
    ITaskCenter AddTask(string name, ITask task, IScheduleStrategy? strategy = null);

    ITaskCenter AddTask(string name, ITask task, Action<IStrategyBuilder> builder);

    [Obsolete]
    ITaskCenter AddTask<TContext>(string name, ITask<TContext> task, TContext context, IScheduleStrategy? strategy = null);

    ITaskCenter UseStorage(ILightTaskStorage storage);
    IEnumerable<ITaskScheduler> TaskSchedulers();
    ITaskScheduler? GetScheduler(string name);
    void Log(string message);
    bool Remove(string schedulerName);
    void Stop(CancellationToken cancellationToken);

    #region events

    event Action<TaskEventArgs>? OnTaskStatusChanged;
    event Action<TaskEventArgs>? OnTaskScheduleChanged;
    event Action<TaskEventArgs>? OnCompleted;

    //Func<ITaskScheduler, Task>? OnTaskStatusChangedAsync { get; set; }
    IDisposable RegisterTaskStatusChangedHandler(Func<TaskEventArgs, Task> handler);
    IDisposable RegisterTaskScheduleChangedHandler(Func<TaskEventArgs, Task> handler);
    IDisposable RegisterTaskCompletedHandler(Func<TaskEventArgs, Task> handler);

    #endregion
}
