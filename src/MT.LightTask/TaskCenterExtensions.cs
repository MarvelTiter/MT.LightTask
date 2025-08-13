using Microsoft.Extensions.DependencyInjection;

namespace MT.LightTask;

public static class TaskCenterExtensions
{
    public static ITaskCenter AddTask<T>(this ITaskCenter center, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder) where T : ITask
    {
        var name = typeof(T).Name;
        return center.AddTask<T>(name, strategyBuilder);
    }
    public static ITaskCenter AddTask<T>(this ITaskCenter center, string name, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder) where T : ITask
    {
        var task = center.ServiceProvider.GetRequiredService<T>();
        return center.AddTask(name, task, strategyBuilder);
    }

    public static ITaskCenter AddTask<T>(this ITaskCenter center, string name, string cronExpression)
        where T : ITask
    {
        var task = center.ServiceProvider.GetRequiredService<T>();
        return center.AddTask(name, task,b => b.WithCron(cronExpression).Build());
    }

    public static ITaskCenter AddTask(this ITaskCenter center, string name, Func<IServiceProvider, CancellationToken, Task> task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder)
    {
        var defaultTask = new DefaultTask(task, center.ServiceProvider);
        return center.AddTask(name, defaultTask, strategyBuilder);
    }

    public static ITaskCenter AddTask(this ITaskCenter center, string name, string cronExpression, Func<IServiceProvider, CancellationToken, Task> task)
    {
        var defaultTask = new DefaultTask(task, center.ServiceProvider);
        return center.AddTask(name, defaultTask, b => b.WithCron(cronExpression).Build());
    }
}
