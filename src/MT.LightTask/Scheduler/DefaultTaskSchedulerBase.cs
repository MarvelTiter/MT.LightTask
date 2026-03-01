using MT.LightTask.Storage;
using System.Diagnostics.CodeAnalysis;

namespace MT.LightTask;

internal abstract class DefaultTaskSchedulerBase<TScheduler>(string name) : ITaskScheduler
    where TScheduler : ITaskScheduler
{
    protected StrategyRunner? runner;
    protected CancellationTokenSource? schedulerTokenSource;
    protected bool disposedValue;
    public string Name { get; } = name;
    [NotNull] public IScheduleStrategy? Strategy { get; set; }
    //[NotNull] public ITask? Task { get; set; }
    [NotNull] public Action<string>? Log { get; set; }
    [NotNull] public TaskCenter? Aop { get; set; }
    public ILightTaskStorage? Storage { get; set; }
    public Exception? Exception { get; set; }
    public TaskRunStatus TaskStatus { get; set; }
    public TaskScheduleStatus ScheduleStatus { get; set; }
    public abstract object? Context { get; }
    public abstract string TaskTypeName { get; }

    protected class StrategyRunner(Func<CancellationToken, Task> work, TScheduler scheduler) : IDisposable
    {
        private CancellationToken schedulerCancelToken;
        // 调度取消
        private CancellationTokenSource? runnerTokenSource;
        // 调度+外部Token取消
        private CancellationTokenSource? cancelTokenSource;
        // 立即执行，取消等待，
        private CancellationTokenSource? waitCancelTokenSource;
        private bool disposedValue;
        private readonly object locker = new();

        private static async Task LoadStatusAsync(TScheduler scheduler, CancellationToken cancellationToken)
        {
            if (!TaskOptions.Instance.EnableStorage) return;
            if (scheduler.Storage is null) return;
            var config = await scheduler.Storage.LoadTaskStatusAsync(scheduler.Name, cancellationToken);
            if (config is not null)
            {
                scheduler.Strategy.LoadData(config.Values);
            }
        }

        private void StorageStatus(TScheduler scheduler)
        {
            if (!TaskOptions.Instance.EnableStorage) return;
            if (scheduler.Storage is null) return;
            lock (locker)
            {
                var dic = scheduler.Strategy.SaveData();
                var config = new Storage.TaskStatus()
                {
                    Values = dic
                };
                scheduler.Storage.SaveTaskStatus(scheduler.Name, config);
            }
        }

        public void Start(CancellationToken? cancellationToken)
        {
            schedulerCancelToken = cancellationToken ?? CancellationToken.None;
            runnerTokenSource = new CancellationTokenSource();
            cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(schedulerCancelToken, runnerTokenSource.Token);
            waitCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelTokenSource.Token);
            Task.Run(async () =>
            {
                scheduler.Log($"任务[{scheduler.Name}]: 开始运行");
                await LoadStatusAsync(scheduler, cancelTokenSource.Token);
                while (!cancelTokenSource.IsCancellationRequested)
                {
                    if (!scheduler.Strategy.WaitForExecute(waitCancelTokenSource.Token))
                    {
                        // 只有当waitCancelTokenSource取消时，才会立即执行，其他情况应该是break，结束任务
                        if (waitCancelTokenSource.IsCancellationRequested && !cancelTokenSource.IsCancellationRequested)
                        {
                            scheduler.Log($"任务[{scheduler.Name}] 立即执行");
                            waitCancelTokenSource.Dispose();
                            waitCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelTokenSource.Token);
                        }
                        else
                        {
                            break;
                        }
                    }

                    await ExecuteWithRetry(cancelTokenSource);
                    scheduler.Log($"任务[{scheduler.Name}] Elapsed: {scheduler.Strategy.LastRunElapsedTime} NextRuntime: {scheduler.Strategy.NextRuntime}");
                    StorageStatus(scheduler);
                }
            });
        }

        /// <summary>
        /// 处理重试
        /// </summary>
        private async Task ExecuteWithRetry(CancellationTokenSource cancelCts)
        {
            int retryCount = 0;
            bool success = false;

            while (!success && retryCount <= scheduler.Strategy.RetryLimit && !cancelCts.IsCancellationRequested)
            {
                try
                {
                    // 执行任务（包含超时控制）
                    //await ExecuteWithTimeout(cancelCts);
                    await work(cancelCts.Token).ConfigureAwait(false);
                    // 执行成功，退出重试循环
                    success = true;
                    // 重置重试计数
                    scheduler.Strategy.RetryTimes = 0;
                }
                catch (Exception ex) when (retryCount < scheduler.Strategy.RetryLimit)
                {
                    retryCount++;
                    scheduler.Strategy.RetryTimes = retryCount;
                    scheduler.Log($"任务[{scheduler.Name}] 执行失败，第 {retryCount} 次重试，错误: {ex.Message}");
                    // 重试等待
                    await DelayRetry(scheduler.Strategy);
                }
            }
        }

        /// <summary>
        /// 执行任务，支持超时控制
        /// </summary>
        private async Task ExecuteWithTimeout(CancellationTokenSource cancelCts)
        {
            if (scheduler.Strategy.Timeout.HasValue)
            {
                using var timeoutCts = new CancellationTokenSource(scheduler.Strategy.Timeout.Value);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancelCts.Token, timeoutCts.Token);
                await work(linkedCts.Token).ConfigureAwait(false);
            }
            else
            {
                await work(cancelCts.Token).ConfigureAwait(false);
            }
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
            var delay = strategy.RetryWaitStrategy.GetWaitDuration(strategy);
            return Task.Delay(delay);
        }
    }

    public bool RunImmediately()
    {
        if (ScheduleStatus != TaskScheduleStatus.Running)
            return false;
        if (TaskStatus == TaskRunStatus.Running)
            return false;
        return runner?.Run() ?? false;
    }
    protected Task UpdateTaskStatusAsync(TaskRunStatus taskRunStatus)
    {
        TaskStatus = taskRunStatus;
        Aop.NotifyTaskStatusChanged(this);
        return Aop.NotifyTaskStatusChangedAsync(this);
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
}
