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
    ITaskCenter AddTask(string name, ITask task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder);
    //void Start(CancellationToken cancellationToken);
    IEnumerable<ITaskScheduler> TaskSchedulers();
    ITaskScheduler? GetScheduler(string name);
    void Log(string message);
    bool Remove(string schedulerName);
    void Stop(CancellationToken cancellationToken);

    #region events
    //Action<ITaskScheduler>? OnError { get; set; }
    //Action<ITaskScheduler>? OnCompletedSuccessfully { get; set; }

    //Func<ITaskScheduler, Task>? OnErrorAsync { get; set; }
    //Func<ITaskScheduler, Task>? OnCompletedAsync { get; set; }
    //Func<ITaskScheduler, Task>? OnCompletedSuccessfullyAsync { get; set; }

    event Action<TaskEventArgs>? OnTaskStatusChanged;
    event Action<TaskEventArgs>? OnTaskScheduleChanged;
    event Action<TaskEventArgs>? OnCompleted;

    //Func<ITaskScheduler, Task>? OnTaskStatusChangedAsync { get; set; }
    IDisposable RegisterTaskStatusChangedHandler(Func<TaskEventArgs, Task> handler);
    IDisposable RegisterTaskScheduleChangedHandler(Func<TaskEventArgs, Task> handler);
    IDisposable RegisterTaskCompletedHandler(Func<TaskEventArgs, Task> handler);

    #endregion
}
