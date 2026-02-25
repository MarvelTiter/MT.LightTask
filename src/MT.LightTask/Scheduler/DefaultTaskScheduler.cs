using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace MT.LightTask;

internal sealed class DefaultTaskScheduler(string name) : DefaultTaskSchedulerBase<DefaultTaskScheduler>(name)
{
    private ITask? task;
    public override object? Context => null;
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