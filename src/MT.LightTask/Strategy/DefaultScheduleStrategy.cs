namespace MT.LightTask;

class DefaultScheduleStrategy : IScheduleStrategy
{
    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? LastRuntime { get; set; }

    public DateTimeOffset? NextRuntime { get; set; }

    public TimeSpan LastRunElapsedTime { get; set; }

    public TimeSpan Timeout { get => throw new NotImplementedException(); }

    public virtual bool WaitForExecute(CancellationToken cancellationToken)
    {
        if (!LastRuntime.HasValue)
        {
            LastRuntime = DateTimeOffset.Now;
            return true;
        }
        return false;
    }
}
