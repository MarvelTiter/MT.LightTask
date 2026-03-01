namespace MT.LightTask.Test.Web.Tasks
{
    public class IntervalTask(ILogger<IntervalTask> logger) : ITask
    {
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var rw = Random.Shared.Next(0, 2);
            await Task.Delay(TimeSpan.FromSeconds(rw), cancellationToken);
            logger.LogInformation("IntervalTask执行");
        }
    }

    public class OnceTask(ILogger<OnceTask> logger) : ITask
    {
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var rw = Random.Shared.Next(0, 2);
            await Task.Delay(TimeSpan.FromSeconds(rw), cancellationToken);
            logger.LogInformation("OnceTask执行");
        }
    }

    public class SignalTask(ILogger<SignalTask> logger) : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            logger.LogInformation("SignalTask执行");
            return Task.CompletedTask;
        }
    }

    public class CronTask(ILogger<CronTask> logger) : ITask
    {
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var rw = Random.Shared.Next(0, 4);
            await Task.Delay(TimeSpan.FromSeconds(rw), cancellationToken);
            logger.LogInformation("CronTask执行");
        }
    }
}
