using Microsoft.Extensions.DependencyInjection;

namespace MT.LightTask;

[Obsolete("使用(Action<IStrategyBuilder> strategyBuilder)重载")]
public static class TaskCenterExtensionsOld
{
    extension(ITaskCenter center)
    {
        public ITaskCenter AddTask<T>(Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder) where T : ITask
        {
            var name = typeof(T).Name;
            return center.AddTask<T>(name, strategyBuilder);
        }
        public ITaskCenter AddTask<T>(string name, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder) where T : ITask
        {
            var task = center.ServiceProvider.GetRequiredService<T>();
            return center.AddTask(name, task, strategyBuilder);
        }

        public ITaskCenter AddTask<T>(string name, string cronExpression)
            where T : ITask
        {
            var task = center.ServiceProvider.GetRequiredService<T>();
            return center.AddTask(name, task, b => b.WithCron(cronExpression).Build());
        }

        public ITaskCenter AddTask(string name, Func<IServiceProvider, CancellationToken, Task> task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder)
        {
            var defaultTask = new DefaultTask(task, center.ServiceProvider);
            return center.AddTask(name, defaultTask, strategyBuilder);
        }

        public ITaskCenter AddTask(string name, string cronExpression, Func<IServiceProvider, CancellationToken, Task> task)
        {
            var defaultTask = new DefaultTask(task, center.ServiceProvider);
            return center.AddTask(name, defaultTask, b => b.WithCron(cronExpression).Build());
        }
        // 附带运行参数
        public ITaskCenter AddTaskWithContext<T, TContext>(TContext context, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder) where T : ITask<TContext>
        {
            var name = typeof(T).Name;
            return center.AddTaskWithContext<T, TContext>(name, context, strategyBuilder);
        }
        public ITaskCenter AddTaskWithContext<T, TContext>(string name, TContext context, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder) where T : ITask<TContext>
        {
            var task = center.ServiceProvider.GetRequiredService<T>();
            return center.AddTask(name, task, context, strategyBuilder);
        }

        public ITaskCenter AddTaskWithContext<T, TContext>(string name, TContext context, string cronExpression)
            where T : ITask<TContext>
        {
            var task = center.ServiceProvider.GetRequiredService<T>();
            return center.AddTask(name, task, context, b => b.WithCron(cronExpression).Build());
        }

        public ITaskCenter AddTaskWithContext<TContext>(string name, TContext context, Func<IServiceProvider, TContext, CancellationToken, Task> task, Func<IStrategyBuilder, IScheduleStrategy> strategyBuilder)
        {
            var defaultTask = new DefaultTask<TContext>(task, center.ServiceProvider);
            return center.AddTask(name, defaultTask, context, strategyBuilder);
        }

        public ITaskCenter AddTaskWithContext<TContext>(string name, TContext context, string cronExpression, Func<IServiceProvider, TContext, CancellationToken, Task> task)
        {
            var defaultTask = new DefaultTask<TContext>(task, center.ServiceProvider);
            return center.AddTask(name, defaultTask, context, b => b.WithCron(cronExpression).Build());
        }
    }
}

public static class TaskCenterExtensions
{
    extension(ITaskCenter center)
    {
        public ITaskCenter AddTask<T>(Action<IStrategyBuilder> strategyBuilder) where T : ITask
        {
            var name = typeof(T).Name;
            return center.AddTask<T>(name, strategyBuilder);
        }
        public ITaskCenter AddTask<T>(string name, Action<IStrategyBuilder> strategyBuilder) where T : ITask
        {
            var task = center.ServiceProvider.GetRequiredService<T>();
            return center.AddTask(name, task, strategyBuilder);
        }

        public ITaskCenter AddTask<T>(string name, string cronExpression)
            where T : ITask
        {
            var task = center.ServiceProvider.GetRequiredService<T>();
            return center.AddTask(name, task, b => b.WithCron(cronExpression));
        }

        public ITaskCenter AddTask(string name, Func<IServiceProvider, CancellationToken, Task> task, Action<IStrategyBuilder> strategyBuilder)
        {
            var defaultTask = new DefaultTask(task, center.ServiceProvider);
            return center.AddTask(name, defaultTask, strategyBuilder);
        }

        public ITaskCenter AddTask(string name, string cronExpression, Func<IServiceProvider, CancellationToken, Task> task)
        {
            var defaultTask = new DefaultTask(task, center.ServiceProvider);
            return center.AddTask(name, defaultTask, b => b.WithCron(cronExpression));
        }
        // 附带运行参数
        public ITaskCenter AddTaskWithContext<T, TContext>(TContext context, Action<IStrategyBuilder> strategyBuilder) where T : ITask<TContext>
        {
            var name = typeof(T).Name;
            return center.AddTaskWithContext<T, TContext>(name, context, strategyBuilder);
        }
        public ITaskCenter AddTaskWithContext<T, TContext>(string name, TContext context, Action<IStrategyBuilder> strategyBuilder) where T : ITask<TContext>
        {
            var task = center.ServiceProvider.GetRequiredService<T>();
            return center.AddTask(name, task, context, strategyBuilder);
        }

        public ITaskCenter AddTaskWithContext<T, TContext>(string name, TContext context, string cronExpression)
            where T : ITask<TContext>
        {
            var task = center.ServiceProvider.GetRequiredService<T>();
            return center.AddTask(name, task, context, b => b.WithCron(cronExpression));
        }

        public ITaskCenter AddTaskWithContext<TContext>(string name, TContext context, Func<IServiceProvider, TContext, CancellationToken, Task> task, Action<IStrategyBuilder> strategyBuilder)
        {
            var defaultTask = new DefaultTask<TContext>(task, center.ServiceProvider);
            return center.AddTask(name, defaultTask, context, strategyBuilder);
        }

        public ITaskCenter AddTaskWithContext<TContext>(string name, TContext context, string cronExpression, Func<IServiceProvider, TContext, CancellationToken, Task> task)
        {
            var defaultTask = new DefaultTask<TContext>(task, center.ServiceProvider);
            return center.AddTask(name, defaultTask, context, b => b.WithCron(cronExpression));
        }
    }
}
