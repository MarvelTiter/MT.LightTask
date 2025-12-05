using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MT.LightTask;

public class TaskCenter(IServiceProvider serviceProvider) : ITaskCenter//, ITaskAopNotify
{
    private readonly ConcurrentDictionary<string, ITaskScheduler> tasks = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    private readonly ILogger<TaskCenter> logger = serviceProvider.GetRequiredService<ILogger<TaskCenter>>();

    public event Action<TaskEventArgs>? OnTaskStatusChanged;
    public event Action<TaskEventArgs>? OnTaskScheduleChanged;
    public event Action<TaskEventArgs>? OnCompleted;

    public ITaskCenter AddTask(string name, ITask task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder)
    {
        var scheduler = (DefaultTaskScheduler)tasks.GetOrAdd(name, k =>
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

    public ITaskCenter AddTask<TContext>(string name, ITask<TContext> task, TContext context, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder)
    {
        var scheduler = (DefaultTaskSchedulerWithContext<TContext>)tasks.GetOrAdd(name, k =>
        {
            var scheduler = new DefaultTaskSchedulerWithContext<TContext>(k)
            {
                Log = Log,
                Aop = this
            };
            return scheduler;
        });
        var b = new StrategyBuilder();
        var strategy = strategyBuilder.Invoke(b);
        scheduler.InternalStart(task, context, strategy);
        return this;
    }
    public ITaskCenter AddTask(string name, ITask task, Action<IStrategyBuilder> strategyBuilder)
    {
        var scheduler = (DefaultTaskScheduler)tasks.GetOrAdd(name, k =>
        {
            var scheduler = new DefaultTaskScheduler(k)
            {
                Log = Log,
                Aop = this
            };
            return scheduler;
        });
        var b = new StrategyBuilder();
        strategyBuilder.Invoke(b);
        scheduler.InternalStart(task, b.Build());
        return this;
    }
    public ITaskCenter AddTask<TContext>(string name, ITask<TContext> task, TContext context, Action<IStrategyBuilder> strategyBuilder)
    {
        var scheduler = (DefaultTaskSchedulerWithContext<TContext>)tasks.GetOrAdd(name, k =>
        {
            var scheduler = new DefaultTaskSchedulerWithContext<TContext>(k)
            {
                Log = Log,
                Aop = this
            };
            return scheduler;
        });
        var b = new StrategyBuilder();
        strategyBuilder.Invoke(b);
        scheduler.InternalStart(task, context, b.Build());
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

    public void Log(string message)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{message}", message);
        }
    }

    public void NotifyTaskStatusChanged(ITaskScheduler scheduler) => OnTaskStatusChanged?.Invoke(new(scheduler, this));
    public void NotifyTaskScheduleChanged(ITaskScheduler scheduler) => OnTaskScheduleChanged?.Invoke(new(scheduler, this));

    public void NotifyTaskCompleted(ITaskScheduler scheduler) => OnCompleted?.Invoke(new(scheduler, this));


    private readonly AsyncHandlerManager<TaskEventArgs> taskStatusChanged = new();
    private readonly AsyncHandlerManager<TaskEventArgs> taskScheduleChanged = new();
    private readonly AsyncHandlerManager<TaskEventArgs> taskCompleted = new();

    public IDisposable RegisterTaskStatusChangedHandler(Func<TaskEventArgs, Task> handler)
        => taskStatusChanged.RegisterHandler(handler);

    public IDisposable RegisterTaskScheduleChangedHandler(Func<TaskEventArgs, Task> handler)
        => taskScheduleChanged.RegisterHandler(handler);

    public IDisposable RegisterTaskCompletedHandler(Func<TaskEventArgs, Task> handler)
        => taskCompleted.RegisterHandler(handler);

    public Task NotifyTaskStatusChangedAsync(ITaskScheduler scheduler)
       => taskStatusChanged.NotifyInvokeHandlers(new(scheduler, this));
    public Task NotifyTaskScheduleChangedAsync(ITaskScheduler scheduler)
        => taskScheduleChanged.NotifyInvokeHandlers(new(scheduler, this));
    public Task NotifyTaskCompletedAsync(ITaskScheduler scheduler)
        => taskCompleted.NotifyInvokeHandlers(new(scheduler, this));
}
