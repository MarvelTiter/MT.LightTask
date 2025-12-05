using Microsoft.Extensions.DependencyInjection;

namespace MT.LightTask;

public static class TaskCenterExtensions
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
    }
}
