using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MT.LightTask.Extensions;
using MT.LightTask.Storage;
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

    /// <summary>
    /// 从存储中加载任务
    /// </summary>
    /// <returns></returns>
    public async Task LoadTasksFromStorageAsync(ILightTaskStorage storage)
    {
        try
        {
            var configs = await storage.LoadAllTaskConfigsAsync();
            foreach (var config in configs)
            {
                // 尝试从服务容器中获取任务实例
#pragma warning disable IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                var taskType = Type.GetType(config.TypeName);
#pragma warning restore IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                if (taskType == null)
                {
                    Log($"未找到任务类型 {config.Name}，跳过加载");
                    continue;
                }

                var taskInstance = ServiceProvider.GetService(taskType);
                if (taskInstance == null)
                {
                    Log($"未找到任务实例 {config.Name}，跳过加载");
                    continue;
                }

                if (taskInstance is ITask task)
                {
                    AddTaskFromConfig(config, task);
                }
                else
                {
                    Log($"任务 {config.Name} 未实现 ITask 接口，跳过加载");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"从存储加载任务失败: {ex.Message}");
        }
    }

    public async Task SaveTaskToStorageAsync(ILightTaskStorage storage, string taskName)
    {
        try
        {
            var sc = GetScheduler(taskName);
            if (sc is null) return;
            await storage.SaveTaskConfigAsync(sc.Config);
        }
        catch (Exception ex)
        {
            Log($"保存任务到存储失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据配置添加任务
    /// </summary>
    /// <param name="config"></param>
    /// <param name="task"></param>
    private void AddTaskFromConfig(TaskConfig config, ITask task)
    {
        AddTask(config.Name, task, builder =>
        {
            switch (config.Type)
            {
                case MT.LightTask.Storage.ScheduleType.Once:
                    if (config.StartTime.HasValue)
                    {
                        builder.Once(config.StartTime.Value);
                    }
                    break;
                case MT.LightTask.Storage.ScheduleType.Cron:
                    if (!string.IsNullOrEmpty(config.Cron))
                    {
                        builder.WithCron(config.Cron);
                    }
                    break;
                case MT.LightTask.Storage.ScheduleType.Interval:
                    if (config.Interval.HasValue)
                    {
                        builder.WithInterval(config.Interval.Value);
                    }
                    break;
                case MT.LightTask.Storage.ScheduleType.Signal:
                    builder.WithSignal();
                    break;
            }

            if (config.RetryLimit > 0)
            {
                builder.WithRetry(config.RetryLimit, config.RetryIntervalBase);
            }
        });

        Log($"从存储加载任务 {config.Name} 成功");
    }
}
