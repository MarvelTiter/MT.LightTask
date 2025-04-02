using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MT.LightTask;

class DefaultTaskScheduler(string name) : ITaskScheduler
{
    private SchedulerRunner? runner;
    private CancellationTokenSource? schedulerTokenSource;
    public string Name { get; } = name;
    [NotNull] public IScheduleStrategy? Strategy { get; set; }
    [NotNull] public ITask? Task { get; set; }
    [NotNull] public Action<string>? Log { get; set; }
    public Exception? Exception { get; set; }
    public TaskRunStatus TaskStatus { get; set; }
    public TaskScheduleStatus ScheduleStatus { get; set; }
    public void Start(ITask task, IScheduleStrategy strategy)
    {
        Task = task;
        Strategy = strategy;
        InitTask();
        Start();
    }
    public void Start()
    {
        schedulerTokenSource = new CancellationTokenSource();
        runner?.Start(schedulerTokenSource.Token);
        ScheduleStatus = TaskScheduleStatus.Running;
    }

    public void Stop()
    {
        schedulerTokenSource?.Cancel();
        ScheduleStatus = TaskScheduleStatus.Ready;
    }


    private void InitTask()
    {
        Log($"任务[{Name}]: 初始化");
        async Task work(CancellationToken token)
        {
            Exception = null;
            try
            {
                TaskStatus = TaskRunStatus.Running;
                await Task.ExecuteAsync(token).ConfigureAwait(false);
                TaskStatus = TaskRunStatus.Success;
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Exception = ex;
                Log($"任务[{Name}] 异常: {ex.Message}");
            }
            if (token.IsCancellationRequested) TaskStatus = TaskRunStatus.Canceled;
            if (Exception != null) TaskStatus = TaskRunStatus.OccurException;
        }
        runner = new SchedulerRunner(work, this);
        Log($"任务[{Name}]: 初始化完成");
    }


    class SchedulerRunner(Func<CancellationToken, Task> work, DefaultTaskScheduler scheduler)
    {
        private CancellationToken schedulerCancelToken;
        private CancellationTokenSource? runnerTokenSource;
        private CancellationTokenSource? cancelTokenSource;

        public void Start(CancellationToken? cancellationToken)
        {
            schedulerCancelToken = cancellationToken ?? CancellationToken.None;
            runnerTokenSource = new CancellationTokenSource();
            cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(schedulerCancelToken, runnerTokenSource.Token);
            System.Threading.Tasks.Task.Run(async () =>
            {
                scheduler.Log($"任务[{scheduler.Name}]: 开始运行");
                if (scheduler.Strategy.StartTime.HasValue)
                {
                    var wait = scheduler.Strategy.StartTime.Value - DateTime.Now;
                    if (wait > TimeSpan.Zero)
                    {
                        scheduler.Log($"任务[{scheduler.Name}]: 等待开始时间 , 耗时: {wait}");
                        cancelTokenSource.Token.WaitHandle.WaitOne(wait);
                    }
                }
                while (!cancelTokenSource.IsCancellationRequested)
                {
                    if (!scheduler.Strategy.WaitForExecute(next => scheduler.Log($"任务[{scheduler.Name}] 下一次执行时间: {next}"),cancelTokenSource.Token)) break;

                    // 立刻运行一次
                    var sw = Stopwatch.StartNew();
                    await work(cancelTokenSource.Token).ConfigureAwait(false);
                    sw.Stop();
                    scheduler.Strategy.LastRunElapsedTime = sw.Elapsed;
                    scheduler.Log($"任务[{scheduler.Name}] Elapsed: {scheduler.Strategy.LastRunElapsedTime} NextRuntime: {scheduler.Strategy.NextRuntime}");
                }
            });
        }

        public void Stop()
        {
            runnerTokenSource?.Cancel();
        }
    }
}
