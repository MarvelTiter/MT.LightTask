namespace MT.LightTask.Test.Web.Tasks
{
    public class RetryTask : ITask
    {
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var rw = Random.Shared.Next(0, 2);
            await Task.Delay(TimeSpan.FromSeconds(rw), cancellationToken);
        }
    }

    public class Task2 : ITask
    {
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var rw = Random.Shared.Next(0, 2);
            await Task.Delay(TimeSpan.FromSeconds(rw), cancellationToken);
        }
    }
}
