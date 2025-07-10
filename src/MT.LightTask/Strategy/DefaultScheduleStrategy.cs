namespace MT.LightTask;

class DefaultScheduleStrategy : IScheduleStrategy
{
    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? LastRuntime { get; set; }

    public DateTimeOffset? NextRuntime { get; set; }

    public TimeSpan LastRunElapsedTime { get; set; }

    public TimeSpan Timeout { get => throw new NotImplementedException(); }
    public int RetryLimit { get; set; }
    public int RetryTimes { get; set; }

    public virtual bool WaitForExecute(CancellationToken cancellationToken)
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
        var reciveCancelSignal = cancellationToken.WaitHandle.WaitOne(wait);
        LastRuntime = DateTimeOffset.Now;
        NextRuntime = null;
        return !reciveCancelSignal;
    }
}
