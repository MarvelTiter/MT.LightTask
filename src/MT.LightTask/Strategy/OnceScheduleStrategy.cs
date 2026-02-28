using MT.LightTask.Storage;

namespace MT.LightTask;

internal class OnceScheduleStrategy : DefaultScheduleStrategy
{
    public override bool WaitForExecute(CancellationToken cancellationToken)
    {
        // 如果有上次运行时间，说明已经执行过了
        if (LastRuntime.HasValue)
        {
            return false;
        }
        if (StartTime.HasValue)
        {
            NextRuntime = StartTime.Value;
        }
        var wait = StartTime.HasValue ? StartTime.Value - DateTimeOffset.Now : TimeSpan.Zero;
        //var reciveCancelSignal = cancellationToken.WaitHandle.WaitOne(wait);
        var reciveCancelSignal = Wait(wait, cancellationToken);
        LastRuntime = DateTimeOffset.Now;
        NextRuntime = null;
        return !reciveCancelSignal;
    }
}
