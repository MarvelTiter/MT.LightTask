using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MT.LightTask;

internal static class TaskSchedulerExtensions
{
    public static bool IsRunning(this DefaultTaskScheduler defaultSchedule)
    {
        return defaultSchedule.ScheduleStatus == TaskScheduleStatus.Running;
    }
}

internal class DefaultTaskScheduler(string name) : ITaskScheduler, IDisposable
{
    private SchedulerRunner? runner;
    private CancellationTokenSource? schedulerTokenSource;
    private bool disposedValue;

    public string Name { get; } = name;
    [NotNull] public IScheduleStrategy? Strategy { get; set; }
    [NotNull] public ITask? Task { get; set; }
    [NotNull] public Action<string>? Log { get; set; }
    [NotNull] public TaskCenter? Aop { get; set; }
    public Exception? Exception { get; set; }
    public TaskRunStatus TaskStatus { get; set; }
    public TaskScheduleStatus ScheduleStatus { get; set; }
    public bool CanRetry => Strategy.RetryLimit > 0 && Strategy.RetryTimes < Strategy.RetryLimit;

    private Task UpdateTaskStatusAsync(TaskRunStatus taskRunStatus)
    {
        TaskStatus = taskRunStatus;
        Aop.NotifyTaskStatusChanged(this);
        return Aop.NotifyTaskStatusChangedAsync(this);
    }

    public bool RunImmediately()
    {
        if (ScheduleStatus != TaskScheduleStatus.Running)
            return false;
        if (TaskStatus == TaskRunStatus.Running)
            return false;
        return runner?.Run() ?? false;
    }

    internal void InternalStart(ITask task, IScheduleStrategy strategy)
    {
        Task = task;
        Strategy = strategy;
        Log($"任务[{Name}]: 初始化");

        async Task work(CancellationToken token)
        {
            Exception = null;
            try
            {
                await UpdateTaskStatusAsync(Strategy.RetryTimes > 0 ? TaskRunStatus.Retry : TaskRunStatus.Running);
                var start = Stopwatch.GetTimestamp();
                await Task.ExecuteAsync(token).ConfigureAwait(false);
                Strategy.LastRunElapsedTime = Stopwatch.GetElapsedTime(start);
                //TaskStatus = TaskRunStatus.Success;
                await UpdateTaskStatusAsync(TaskRunStatus.Success);
                // reset
            }
            catch (TaskCanceledException)
            {
                //TaskStatus = TaskRunStatus.Canceled;
                await UpdateTaskStatusAsync(TaskRunStatus.Canceled);
            }
            catch (Exception ex)
            {
                Exception = ex;
                //TaskStatus = TaskRunStatus.OccurException;
                await UpdateTaskStatusAsync(TaskRunStatus.OccurException);
                Log($"任务[{Name}] 异常: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                if (CanRetry)
                {
                    Strategy.RetryTimes++;
                }
            }
            finally
            {
                Aop.NotifyTaskCompleted(this);
                await Aop.NotifyTaskCompletedAsync(this);
            }
        }

        runner = new SchedulerRunner(work, this);
        Log($"任务[{Name}]: 初始化完成");
        Start();
    }

    public void Start()
    {
        if (ScheduleStatus == TaskScheduleStatus.Running)
        {
            Stop();
        }

        schedulerTokenSource?.Dispose();
        schedulerTokenSource = new CancellationTokenSource();
        runner?.Start(schedulerTokenSource.Token);
        ScheduleStatus = TaskScheduleStatus.Running;
        Aop.NotifyTaskScheduleChanged(this);
        _ = Aop.NotifyTaskScheduleChangedAsync(this);
    }

    public void Stop()
    {
        schedulerTokenSource?.Cancel();
        ScheduleStatus = TaskScheduleStatus.Ready;
        Aop.NotifyTaskScheduleChanged(this);
        _ = Aop.NotifyTaskScheduleChangedAsync(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                runner?.Dispose();
                schedulerTokenSource?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private class SchedulerRunner(Func<CancellationToken, Task> work, DefaultTaskScheduler scheduler) : IDisposable
    {
        private CancellationToken schedulerCancelToken;
        private CancellationTokenSource? runnerTokenSource;

        private CancellationTokenSource? cancelTokenSource;

        //用于取消等待，立即执行
        private CancellationTokenSource? waitCancelTokenSource;
        private bool disposedValue;

        public void Start(CancellationToken? cancellationToken)
        {
            schedulerCancelToken = cancellationToken ?? CancellationToken.None;
            runnerTokenSource = new CancellationTokenSource();
            cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(schedulerCancelToken, runnerTokenSource.Token);
            waitCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelTokenSource.Token);
            System.Threading.Tasks.Task.Run(async () =>
            {
                scheduler.Log($"任务[{scheduler.Name}]: 开始运行");
                //if (scheduler.Strategy.StartTime.HasValue)
                //{
                //    var wait = scheduler.Strategy.StartTime.Value - DateTimeOffset.Now;
                //    if (wait > TimeSpan.Zero)
                //    {
                //        scheduler.Log($"任务[{scheduler.Name}]: 等待开始时间 => {wait}");
                //        waitCancelTokenSource.Token.WaitHandle.WaitOne(wait);
                //    }
                //}

                while (!cancelTokenSource.IsCancellationRequested)
                {
                    if (!scheduler.Strategy.WaitForExecute(waitCancelTokenSource.Token))
                    {
                        // 只有当waitCancelTokenSource取消时，才会立即执行，其他情况应该是break，结束任务
                        if (waitCancelTokenSource.IsCancellationRequested && !cancelTokenSource.IsCancellationRequested)
                        {
                            scheduler.Log($"任务[{scheduler.Name}] 立即执行");
                            if (!waitCancelTokenSource.TryReset())
                            {
                                waitCancelTokenSource.Dispose();
                                waitCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelTokenSource.Token);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    // 重试处理
                    if (scheduler.Strategy.RetryLimit > 0)
                    {
                        //do
                        //{
                        //    await work(cancelTokenSource.Token).ConfigureAwait(false);
                        //}
                        //while (scheduler.CanRetry && scheduler.TaskStatus == TaskRunStatus.OccurException);
                        await work(cancelTokenSource.Token).ConfigureAwait(false);
                        while (scheduler.CanRetry && scheduler.TaskStatus == TaskRunStatus.OccurException)
                        {
                            //scheduler.Strategy.RetryTimes++;
                            scheduler.Log($"任务[{scheduler.Name}] 重试次数: {scheduler.Strategy.RetryTimes}");
                            await work(cancelTokenSource.Token).ConfigureAwait(false);
                            await DelayRetry(scheduler.Strategy);
                        }

                        scheduler.Strategy.RetryTimes = 0;
                    }
                    else
                    {
                        await work(cancelTokenSource.Token).ConfigureAwait(false);
                    }

                    scheduler.Log($"任务[{scheduler.Name}] Elapsed: {scheduler.Strategy.LastRunElapsedTime} NextRuntime: {scheduler.Strategy.NextRuntime}");
                }
            });
        }

        public bool Run()
        {
            // 已经取消等待了，不执行
            if (waitCancelTokenSource?.IsCancellationRequested == true)
                return false;
            waitCancelTokenSource?.Cancel();
            return true;
        }

        private void Stop() => runnerTokenSource?.Cancel();

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                    runnerTokenSource?.Dispose();
                    cancelTokenSource?.Dispose();
                    waitCancelTokenSource?.Dispose();
                }

                disposedValue = true;
            }
        }


        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private static Task DelayRetry(IScheduleStrategy strategy)
        {
            if (strategy.WaitDurationProvider is not null)
            {
                var delay = strategy.WaitDurationProvider.Invoke(strategy.RetryTimes);
                return System.Threading.Tasks.Task.Delay(delay);
            }
            else
            {
                // 指数退避策略：1s, 2s, 4s, 8s...
                var times = Math.Pow(2, strategy.RetryTimes);
                var delay = TimeSpan.FromMilliseconds(strategy.RetryIntervalBase * times);
                return System.Threading.Tasks.Task.Delay(delay);
            }
        }
    }
}