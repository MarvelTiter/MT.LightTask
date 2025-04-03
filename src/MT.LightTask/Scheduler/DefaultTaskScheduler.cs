using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MT.LightTask;

class DefaultTaskScheduler(string name) : ITaskScheduler, IDisposable
{
    private SchedulerRunner? runner;
    private CancellationTokenSource? schedulerTokenSource;
    private bool disposedValue;

    public string Name { get; } = name;
    [NotNull] public IScheduleStrategy? Strategy { get; set; }
    [NotNull] public ITask? Task { get; set; }
    [NotNull] public Action<string>? Log { get; set; }
    [NotNull] public ITaskAopNotify? Aop { get; set; }
    public Exception? Exception { get; set; }
    public TaskRunStatus TaskStatus { get; set; }
    public TaskScheduleStatus ScheduleStatus { get; set; }

    public void RunImmediately() => runner?.Run();

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
                TaskStatus = TaskRunStatus.Running;
                var start = Stopwatch.GetTimestamp();
                await Task.ExecuteAsync(token).ConfigureAwait(false);
                Strategy.LastRunElapsedTime = Stopwatch.GetElapsedTime(start);
                TaskStatus = TaskRunStatus.Success;
                Aop.NotifyOnCompletedSuccessfully(this);
                await Aop.NotifyOnCompletedSuccessfullyAsync(this);
            }
            catch (TaskCanceledException)
            {
                TaskStatus = TaskRunStatus.Canceled;
            }
            catch (Exception ex)
            {
                Exception = ex;
                TaskStatus = TaskRunStatus.OccurException;
                Log($"任务[{Name}] 异常: {ex.Message}");
                Aop.NotifyOnError(this);
                await Aop.NotifyOnErrorAsync(this);
            }
            finally
            {
                Aop.NotifyOnCompleted(this);
                await Aop.NotifyOnCompletedAsync(this);
                //if (token.IsCancellationRequested) TaskStatus = TaskRunStatus.Canceled;
                //if (Exception != null) TaskStatus = TaskRunStatus.OccurException;
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
        TaskStatus = TaskRunStatus.Running;
    }

    public void Stop()
    {
        schedulerTokenSource?.Cancel();
        ScheduleStatus = TaskScheduleStatus.Ready;
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

    class SchedulerRunner(Func<CancellationToken, Task> work, DefaultTaskScheduler scheduler) : IDisposable
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
                if (scheduler.Strategy.StartTime.HasValue)
                {
                    var wait = scheduler.Strategy.StartTime.Value - DateTimeOffset.Now;
                    if (wait > TimeSpan.Zero)
                    {
                        scheduler.Log($"任务[{scheduler.Name}]: 等待开始时间 => {wait}");
                        waitCancelTokenSource.Token.WaitHandle.WaitOne(wait);
                    }
                }
                while (!cancelTokenSource.IsCancellationRequested)
                {
                    if (!scheduler.Strategy.WaitForExecute(waitCancelTokenSource.Token))
                    {
                        if (waitCancelTokenSource.IsCancellationRequested)
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

                    await work(cancelTokenSource.Token).ConfigureAwait(false);
                    scheduler.Log($"任务[{scheduler.Name}] Elapsed: {scheduler.Strategy.LastRunElapsedTime} NextRuntime: {scheduler.Strategy.NextRuntime}");
                }
            });
        }
        public void Run() => waitCancelTokenSource?.Cancel();
        public void Stop() => runnerTokenSource?.Cancel();

        protected virtual void Dispose(bool disposing)
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
    }
}
