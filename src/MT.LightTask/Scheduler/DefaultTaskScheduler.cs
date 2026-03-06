using MT.LightTask.Storage;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MT.LightTask;

internal sealed class DefaultTaskScheduler : ITaskScheduler
{
    private ITask? task;
    private readonly Lazy<string> taskTypeName;
    private StrategyRunner? runner;
    private CancellationTokenSource? schedulerTokenSource;
    private bool disposedValue;
    public string Name { get; }
    [NotNull] public IScheduleStrategy? Strategy { get; set; }
    //[NotNull] public ITask? Task { get; set; }
    [NotNull] public Action<string>? Log { get; set; }
    [NotNull] public TaskCenter? Aop { get; set; }
    public ILightTaskStorage? Storage { get; set; }
    public Exception? Exception { get; set; }
    public TaskRunStatus TaskStatus { get => Strategy.RunStatus; set => Strategy.RunStatus = value; }
    public TaskScheduleStatus ScheduleStatus { get; set; }
    public DefaultTaskScheduler(string name)
    {
        Name = name;
        taskTypeName = new Lazy<string>(() => task?.GetType().AssemblyQualifiedName ?? throw new NullReferenceException("获取任务类型名称错误"));
    }
    public string TaskTypeName => taskTypeName.Value;

    internal async void StartTask(ITask task, IScheduleStrategy strategy)
    {
        this.task = task;
        Strategy = strategy;
        Log($"任务[{Name}]: 初始化");

        async Task work(CancellationToken token)
        {
            Exception = null;
            CancellationTokenSource? timeoutCts = null;
            CancellationTokenSource? linkedCts = null;

            try
            {
                await UpdateTaskStatusAsync(Strategy.RetryTimes > 0 ? TaskRunStatus.Retry : TaskRunStatus.Running);
                var start = Stopwatch.GetTimestamp();
                await ExecuteWithTimeout(this.task, token);
                Strategy.LastRunElapsedTime = Stopwatch.GetElapsedTime(start);
                await UpdateTaskStatusAsync(TaskRunStatus.Success);
            }
            catch (TaskCanceledException)
            {
                if (timeoutCts?.IsCancellationRequested == true)
                {
                    await UpdateTaskStatusAsync(TaskRunStatus.Timeout);
                    Log($"任务[{Name}] 超时({Strategy.Timeout})");
                }
                else
                {
                    await UpdateTaskStatusAsync(TaskRunStatus.Canceled);
                    Log($"任务[{Name}] 取消");
                }
                throw;
            }
            catch (Exception ex)
            {
                Exception = ex;
                await UpdateTaskStatusAsync(TaskRunStatus.OccurException);
                Log($"任务[{Name}] 异常: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                throw;
            }
            finally
            {
                timeoutCts?.Dispose();
                linkedCts?.Dispose();
                Aop.NotifyTaskCompleted(this);
                await Aop.NotifyTaskCompletedAsync(this);
            }

            async Task ExecuteWithTimeout(ITask t, CancellationToken token)
            {
                if (Strategy.Timeout.HasValue)
                {
                    timeoutCts = new CancellationTokenSource(Strategy.Timeout.Value);
                    linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
                    await t.ExecuteAsync(linkedCts.Token).ConfigureAwait(false);
                }
                else
                {
                    await t.ExecuteAsync(token).ConfigureAwait(false);
                }
            }
        }
        runner = new StrategyRunner(work, this);
        Log($"任务[{Name}]: 初始化完成");
        await LoadStatusAsync(CancellationToken.None);
        if (ScheduleStatus == TaskScheduleStatus.Running)
        {
            InternalStart();
        }
    }
    
    private async Task LoadStatusAsync(CancellationToken cancellationToken)
    {
        if (!TaskOptions.Instance.EnableStorage) return;
        if (Storage is null) return;
        var config = await Storage.LoadTaskStatusAsync(Name, cancellationToken);
        if (config is not null)
        {
            Strategy.LoadData(config.Values);
            ScheduleStatus = config.ScheduleStatus;
        }
    }
    private readonly object locker = new();
    private void StorageStatus()
    {
        if (!TaskOptions.Instance.EnableStorage) return;
        if (Storage is null) return;
        lock (locker)
        {
            var dic = Strategy.SaveData();
            var config = new Storage.TaskStatus()
            {
                ScheduleStatus = ScheduleStatus,
                Values = dic
            };
            Storage.SaveTaskStatus(Name, config);
        }
    }

    private class StrategyRunner(Func<CancellationToken, Task> work, DefaultTaskScheduler scheduler) : IDisposable
    {
        private CancellationToken schedulerCancelToken;
        // 调度取消
        private CancellationTokenSource? runnerTokenSource;
        // 调度+外部Token取消
        private CancellationTokenSource? cancelTokenSource;
        // 立即执行，取消等待，
        private CancellationTokenSource? waitCancelTokenSource;
        private bool disposedValue;

        public void Start(CancellationToken? cancellationToken)
        {
            schedulerCancelToken = cancellationToken ?? CancellationToken.None;
            runnerTokenSource = new CancellationTokenSource();
            cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(schedulerCancelToken, runnerTokenSource.Token);
            waitCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelTokenSource.Token);
            Task.Run(async () =>
            {
                scheduler.Log($"任务[{scheduler.Name}]: 开始运行");
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
                    scheduler.StorageStatus();
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
                    scheduler.Strategy.LastRuntime = DateTimeOffset.Now;
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
    private Task UpdateTaskStatusAsync(TaskRunStatus taskRunStatus)
    {
        TaskStatus = taskRunStatus;
        Aop.NotifyTaskStatusChanged(this);
        return Aop.NotifyTaskStatusChangedAsync(this);
    }
    private void InternalStart()
    {
        if (ScheduleStatus == TaskScheduleStatus.Running)
        {
            InternalStop();
        }

        schedulerTokenSource?.Dispose();
        schedulerTokenSource = new CancellationTokenSource();
        runner?.Start(schedulerTokenSource.Token);
        ScheduleStatus = TaskScheduleStatus.Running;
        Aop.NotifyTaskScheduleChanged(this);
        _ = Aop.NotifyTaskScheduleChangedAsync(this);
    }

    private void InternalStop()
    {
        schedulerTokenSource?.Cancel();
        ScheduleStatus = TaskScheduleStatus.Disabled;
        Aop.NotifyTaskScheduleChanged(this);
        _ = Aop.NotifyTaskScheduleChangedAsync(this);
    }

    public void Start()
    {
        InternalStart();
        StorageStatus();
    }

    public void Stop()
    {
        InternalStop();
        StorageStatus();
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                InternalStop();
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