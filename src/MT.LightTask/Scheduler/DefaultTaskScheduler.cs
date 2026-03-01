using MT.LightTask.Storage;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace MT.LightTask;

internal sealed class DefaultTaskScheduler : DefaultTaskSchedulerBase<DefaultTaskScheduler>
{
    private ITask? task;
    private readonly Lazy<string> taskTypeName;
    public DefaultTaskScheduler(string name) : base(name)
    {
        taskTypeName = new Lazy<string>(() => task?.GetType().AssemblyQualifiedName ?? throw new NullReferenceException("获取任务类型名称错误"));
    }
    public override object? Context => null;
    public override string TaskTypeName => taskTypeName.Value;

    internal void InternalStart(ITask task, IScheduleStrategy strategy)
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
        Start();
    }
}