using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask;
public static class TaskCenterExtensions
{
    public static ITaskCenter AddTask<T>(this ITaskCenter center, string name, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder) where T : ITask
    {
        var task = center.ServiceProvider.GetRequiredService<T>();
        return center.AddTask(name, task, strategyBuilder);
    }

    public static ITaskCenter AddTask(this ITaskCenter center, string name, Func<IServiceProvider, CancellationToken, Task> task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder)
    {
        var defaultTask = new DefaultTask(task, center.ServiceProvider);
        return center.AddTask(name, defaultTask, strategyBuilder);
    }
}
public class TaskCenter(IServiceProvider serviceProvider) : ITaskCenter
{
    private readonly ConcurrentDictionary<string, ITaskScheduler> tasks = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    private readonly ILogger<TaskCenter> logger = serviceProvider.GetRequiredService<ILogger<TaskCenter>>();
    public ITaskCenter AddTask(string name, ITask task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder)
    {
        var scheduler = tasks.GetOrAdd(name, k =>
         {
             var scheduler = new DefaultTaskScheduler(k);
             scheduler.Log = Log;
             return scheduler;
         });
        var b = new StrategyBuilder();
        var strategy = strategyBuilder.Invoke(b);
        scheduler.Start(task, strategy);
        return this;
    }

    public bool Remove(string schedulerName)
    {
        throw new NotImplementedException();
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
