namespace MT.LightTask;

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
    Action<ITaskScheduler>? OnError { get; set; }
    Action<ITaskScheduler>? OnCompleted { get; set; }
    Action<ITaskScheduler>? OnCompletedSuccessfully { get; set; }

    Func<ITaskScheduler, Task>? OnErrorAsync { get; set; }
    Func<ITaskScheduler, Task>? OnCompletedAsync { get; set; }
    Func<ITaskScheduler, Task>? OnCompletedSuccessfullyAsync { get; set; }
    #endregion
}
