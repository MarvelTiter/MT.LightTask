namespace MT.LightTask;

public interface ITaskCenter
{
    IServiceProvider ServiceProvider { get; }
    ITaskCenter AddTask(string name, ITask task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder);
    //void Start(CancellationToken cancellationToken);
    IEnumerable<ITaskScheduler> TaskSchedulers();
    void Log(string message);
    bool Remove(string schedulerName);
    void Stop(CancellationToken cancellationToken);

}
