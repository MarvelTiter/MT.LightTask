using MT.LightTask.Storage;
using System.Diagnostics;

namespace MT.LightTask;

[Obsolete]
internal sealed class DefaultTaskSchedulerWithContext<TContext> : DefaultTaskSchedulerBase<DefaultTaskSchedulerWithContext<TContext>>
{
    private ITask<TContext>? task;
    private TContext context = default!;
    private readonly Lazy<string> taskTypeName;
    public DefaultTaskSchedulerWithContext(string name) : base(name)
    {
        taskTypeName = new Lazy<string>(() => task?.GetType().AssemblyQualifiedName ?? throw new NullReferenceException("获取任务类型名称错误"));
    }
    public override object? Context => context;

    public override string TaskTypeName => taskTypeName.Value;

//    public override void StorageTask()
//    {
//        if (Storage is null) return;
//        lock (locker)
//        {
//            var dic = Strategy.SaveData();
//#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
//#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
//            var config = new TaskConfig()
//            {
//                Name = Name,
//                TaskTypeName = TaskTypeName,
//                Type = Strategy.ScheduleType,
//                Values = dic,
//            };
//#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
//#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
//            Storage.SaveTaskConfig(config);
//        }
//    }

    internal void InternalStart(ITask<TContext> task, TContext context, IScheduleStrategy strategy)
    {
        this.task = task;
        this.context = context;
        Strategy = strategy;
        Log($"任务[{Name}]: 初始化");

        async Task work(CancellationToken token)
        {
            Exception = null;
            try
            {
                await UpdateTaskStatusAsync(Strategy.RetryTimes > 0 ? TaskRunStatus.Retry : TaskRunStatus.Running);
                var start = Stopwatch.GetTimestamp();
                await this.task.ExecuteAsync(this.context, token).ConfigureAwait(false);
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
                throw;
            }
            finally
            {
                Aop.NotifyTaskCompleted(this);
                await Aop.NotifyTaskCompletedAsync(this);
            }
        }

        runner = new StrategyRunner(work, this);
        Log($"任务[{Name}]: 初始化完成");
        Start();
    }
}
