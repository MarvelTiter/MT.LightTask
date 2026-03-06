using MT.LightTask.Storage;

namespace MT.LightTask;

internal class IntervalScheduleStrategy(TimeSpan interval) : DefaultScheduleStrategy
{
    public TimeSpan Interval { get; set; } = interval;
    public override bool WaitForExecute(CancellationToken cancellationToken)
    {
        if (!LastRuntime.HasValue)
        {
            Set();
            return true;
        }
        if (!NextRuntime.HasValue || DateTimeOffset.Now > NextRuntime)
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
            //LastRuntime = DateTimeOffset.Now;
            NextRuntime = DateTimeOffset.Now + Interval;
        }
    }
}