using MT.LightTask.Storage;

namespace MT.LightTask;

internal class IntervalScheduleStrategy(TimeSpan interval) : DefaultScheduleStrategy
{
    private readonly TimeSpan interval = interval;
    public override void SetConfig(TaskConfig config)
    {
        config.Type = ScheduleType.Interval;
        config.Interval = interval;
        config.RetryLimit = RetryLimit;
        config.RetryIntervalBase = RetryIntervalBase;
    }
    public override bool WaitForExecute(CancellationToken cancellationToken)
    {
        if (!LastRuntime.HasValue)
        {
            Set();
            return true;
        }
        if (DateTimeOffset.Now > NextRuntime)
        {
            Set();
            return true;
        }
        var wait = NextRuntime!.Value - DateTimeOffset.Now;
        var shouldExecute = !cancellationToken.WaitHandle.WaitOne(wait);
        Set();
        return shouldExecute;

        void Set()
        {
            LastRuntime = DateTimeOffset.Now;
            NextRuntime = DateTimeOffset.Now + interval;
        }
    }
}