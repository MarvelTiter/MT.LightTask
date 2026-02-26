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
            try
            {
                await UpdateTaskStatusAsync(Strategy.RetryTimes > 0 ? TaskRunStatus.Retry : TaskRunStatus.Running);
                var start = Stopwatch.GetTimestamp();
                await this.task.ExecuteAsync(token).ConfigureAwait(false);
                Strategy.LastRunElapsedTime = Stopwatch.GetElapsedTime(start);
                await UpdateTaskStatusAsync(TaskRunStatus.Success);
                // reset
            }
            catch (TaskCanceledException)
            {
                await UpdateTaskStatusAsync(TaskRunStatus.Canceled);
            }
            catch (Exception ex)
            {
                Exception = ex;
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
}