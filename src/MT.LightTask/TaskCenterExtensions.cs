using Microsoft.Extensions.DependencyInjection;
using MT.LightTask.Storage;

namespace MT.LightTask;

public static class TaskCenterExtensions
{
    extension(ITaskCenter center)
    {

        public IEnumerable<TaskInfo> GetTaskInfos()
        {
            return center.TaskSchedulers().Select(s => new TaskInfo()
            {
                Name = s.Name,
                Exception = s.Exception,
                LastRunElapsedTime = s.Strategy.LastRunElapsedTime,
                LastRuntime = s.Strategy.LastRuntime,
                NextRuntime = s.Strategy.NextRuntime,
                ScheduleStatus = s.ScheduleStatus,
                TaskStatus = s.TaskStatus
            });
        }

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
        [Obsolete]
        public ITaskCenter AddTaskWithContext<T, TContext>(TContext context, Action<IStrategyBuilder> strategyBuilder) where T : ITask<TContext>
        {
            var name = typeof(T).Name;
            return center.AddTaskWithContext<T, TContext>(name, context, strategyBuilder);
        }

        [Obsolete]
        public ITaskCenter AddTaskWithContext<T, TContext>(string name, TContext context, Action<IStrategyBuilder> strategyBuilder) where T : ITask<TContext>
        {
            var task = center.ServiceProvider.GetRequiredService<T>();
            var b = StrategyBuilder.Default;
            strategyBuilder.Invoke(b);
            return center.AddTask(name, task, context, b.Build());
        }

        [Obsolete]
        public ITaskCenter AddTaskWithContext<T, TContext>(string name, TContext context, string cronExpression)
            where T : ITask<TContext>
        {
            var task = center.ServiceProvider.GetRequiredService<T>();
            return center.AddTask(name, task, context, StrategyBuilder.Default.WithCron(cronExpression).Build());
        }

        [Obsolete]
        public ITaskCenter AddTaskWithContext<TContext>(string name, TContext context, Func<IServiceProvider, TContext, CancellationToken, Task> task, Action<IStrategyBuilder> strategyBuilder)
        {
            var defaultTask = new DefaultTask<TContext>(task, center.ServiceProvider);
            var b = StrategyBuilder.Default;
            strategyBuilder.Invoke(b);
            return center.AddTask(name, defaultTask, context, b.Build());
        }

        [Obsolete]
        public ITaskCenter AddTaskWithContext<TContext>(string name, TContext context, string cronExpression, Func<IServiceProvider, TContext, CancellationToken, Task> task)
        {
            var defaultTask = new DefaultTask<TContext>(task, center.ServiceProvider);
            return center.AddTask(name, defaultTask, context, StrategyBuilder.Default.WithCron(cronExpression).Build());
        }
    }
}
