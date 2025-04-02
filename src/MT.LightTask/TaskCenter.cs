using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MT.LightTask;
public class TaskCenter(IServiceProvider serviceProvider) : ITaskCenter
{
    private readonly ConcurrentDictionary<string, DefaultTaskScheduler> tasks = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    private readonly ILogger<TaskCenter> logger = serviceProvider.GetRequiredService<ILogger<TaskCenter>>();
    public ITaskCenter AddTask(string name, ITask task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder)
    {
        var scheduler = tasks.GetOrAdd(name, k =>
         {
             var scheduler = new DefaultTaskScheduler(k)
             {
                 Log = Log
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
        }
    }

    public void Log(string message) => logger.LogInformation("{message}", message);

    
}
