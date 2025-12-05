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
    [Obsolete("使用(Action<IStrategyBuilder> strategyBuilder)重载")]
    ITaskCenter AddTask(string name, ITask task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder);
    ITaskCenter AddTask(string name, ITask task, Action<IStrategyBuilder> strategyBuilder);
    //void Start(CancellationToken cancellationToken);
    [Obsolete("使用(Action<IStrategyBuilder> strategyBuilder)重载")]
    ITaskCenter AddTask<TContext>(string name, ITask<TContext> task, TContext context, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder);
    ITaskCenter AddTask<TContext>(string name, ITask<TContext> task, TContext context, Action<IStrategyBuilder> strategyBuilder);
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
