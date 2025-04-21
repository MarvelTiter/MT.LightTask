using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MT.LightTask;
public class TaskCenter(IServiceProvider serviceProvider) : ITaskCenter, ITaskAopNotify
{
    private readonly ConcurrentDictionary<string, DefaultTaskScheduler> tasks = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    private readonly ILogger<TaskCenter> logger = serviceProvider.GetRequiredService<ILogger<TaskCenter>>();


    public Action<ITaskScheduler>? OnTaskStatusChanged { get; set; }
    public Action<ITaskScheduler>? OnTaskScheduleChanged { get; set; }

    public ITaskCenter AddTask(string name, ITask task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder)
    {
        var scheduler = tasks.GetOrAdd(name, k =>
         {
             var scheduler = new DefaultTaskScheduler(k)
             {
                 Log = Log,
                 Aop = this
             };
             return scheduler;
         });
        var b = new StrategyBuilder();
        var strategy = strategyBuilder.Invoke(b);
        scheduler.InternalStart(task, strategy);
        return this;
    }

    public bool Remove(string schedulerName)
    {
        var b = tasks.TryRemove(schedulerName, out var scheduler);
        scheduler?.Stop();
        scheduler?.Dispose();
        return b;
    }
    public ITaskScheduler? GetScheduler(string name)
    {
        tasks.TryGetValue(name, out var scheduler);
        return scheduler;
    }
    public IEnumerable<ITaskScheduler> TaskSchedulers() => tasks.Values;
    public void Stop(CancellationToken cancellationToken)
    {
        //taskCenterCancel?.Cancel();
        foreach (var item in tasks.Values)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            item.Stop();
            item.Dispose();
        }
    }

    public void Log(string message) => logger.LogInformation("{message}", message);
    public void NotifyTaskStatusChanged(ITaskScheduler scheduler) => OnTaskStatusChanged?.Invoke(scheduler);
    public void NotifyTaskScheduleChanged(ITaskScheduler scheduler) => OnTaskScheduleChanged?.Invoke(scheduler);


}
