using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MT.LightTask.Storage;
using System.Collections.Concurrent;

namespace MT.LightTask;

public class TaskCenter : ITaskCenter//, ITaskAopNotify
{
    private readonly ConcurrentDictionary<string, ITaskScheduler> tasks = [];
    private ILightTaskStorage? storage;
    public IServiceProvider ServiceProvider { get; }
    private readonly ILogger<TaskCenter> logger;

    public TaskCenter(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        logger = serviceProvider.GetRequiredService<ILogger<TaskCenter>>();
        storage = TaskOptions.Instance.EnableStorage ? serviceProvider.GetService<ILightTaskStorage>() : null;
    }

    public event Action<TaskEventArgs>? OnTaskStatusChanged;
    public event Action<TaskEventArgs>? OnTaskScheduleChanged;
    public event Action<TaskEventArgs>? OnCompleted;

    public ITaskCenter UseStorage(ILightTaskStorage storage)
    {
        if (!TaskOptions.Instance.EnableStorage)
        {
            TaskOptions.Instance.EnableStorage = true;
        }
        this.storage = storage;
        return this;
    }

    public ITaskCenter AddTask(string name, ITask task, IScheduleStrategy? strategy = null)
    {
        var scheduler = (DefaultTaskScheduler)tasks.GetOrAdd(name, k =>
        {
            var scheduler = new DefaultTaskScheduler(k)
            {
                Log = Log,
                Storage = storage,
                Aop = this,
            };
            return scheduler;
        });
        strategy ??= StrategyBuilder.Default.Once(DateTimeOffset.Now.AddSeconds(1)).Build();
        scheduler.InternalStart(task, strategy);
        return this;
    }

    public ITaskCenter AddTask(string name, ITask task, Action<IStrategyBuilder> builder)
    {
        var scheduler = (DefaultTaskScheduler)tasks.GetOrAdd(name, k =>
        {
            var scheduler = new DefaultTaskScheduler(k)
            {
                Log = Log,
                Storage = storage,
                Aop = this,
            };
            return scheduler;
        });
        //strategy ??= StrategyBuilder.Default.Once(DateTimeOffset.Now.AddSeconds(1)).Build();
        var b = StrategyBuilder.Default;
        builder.Invoke(b);
        scheduler.InternalStart(task, b.Build());
        if (b.ShouldStroage && storage is not null && TaskOptions.Instance.EnableStorage)
        {
            var config = new TaskConfig()
            {
                Name = scheduler.Name,
                TaskTypeName = scheduler.TaskTypeName,
                Builder = b
            };
            storage.SaveTaskConfig(config);
        }
        return this;
    }

    [Obsolete]
    public ITaskCenter AddTask<TContext>(string name, ITask<TContext> task, TContext context, IScheduleStrategy? strategy = null)
    {
        var scheduler = (DefaultTaskSchedulerWithContext<TContext>)tasks.GetOrAdd(name, k =>
        {
            var scheduler = new DefaultTaskSchedulerWithContext<TContext>(k)
            {
                Log = Log,
                Storage = storage,
                Aop = this
            };
            return scheduler;
        });
        var b = new StrategyBuilder();
        strategy ??= StrategyBuilder.Default.Once(DateTimeOffset.Now.AddSeconds(1)).Build();
        scheduler.InternalStart(task, context, strategy);
        return this;
    }

    public bool Remove(string schedulerName)
    {
        var b = tasks.TryRemove(schedulerName, out var scheduler);
        scheduler?.Stop();
        scheduler?.Dispose();
        storage?.RemoveTaskStorage(schedulerName);
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
