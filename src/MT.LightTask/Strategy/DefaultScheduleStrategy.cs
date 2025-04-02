namespace MT.LightTask;

class DefaultScheduleStrategy : IScheduleStrategy
{
    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? LastRuntime { get; set; }

    public DateTimeOffset? NextRuntime { get; set; }

    public TimeSpan LastRunElapsedTime { get; set; }

    public TimeSpan Timeout { get; }

    public virtual bool WaitForExecute(Action<DateTimeOffset> handleNext, CancellationToken cancellationToken)
    {
        if (!LastRuntime.HasValue)
        {
            LastRuntime = DateTimeOffset.Now;
            return true;
        }
        return false;
    }
}
