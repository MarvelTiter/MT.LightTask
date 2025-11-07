using System.Diagnostics;

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
    public int RetryIntervalBase { get; set; }
    public Func<int, TimeSpan>? WaitDurationProvider { get; set; }

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
        //var reciveCancelSignal = cancellationToken.WaitHandle.WaitOne(wait);
        var reciveCancelSignal = Wait(wait, cancellationToken);
        LastRuntime = DateTimeOffset.Now;
        NextRuntime = null;
        return !reciveCancelSignal;
    }

    protected static readonly TimeSpan MAX_WAIT_TIMESPAN = TimeSpan.FromMilliseconds(int.MaxValue - 1);
    protected static bool Wait(TimeSpan timeout, CancellationToken token)
    {
        if (timeout < TimeSpan.Zero)
        {
            timeout = TimeSpan.Zero;
        }
        Debug.WriteLine($"等待时间: {timeout}");
        if (timeout <= MAX_WAIT_TIMESPAN)
        {
            return token.WaitHandle.WaitOne(timeout);
        }
        else
        {
            while (timeout > MAX_WAIT_TIMESPAN)
            {
                var access = token.WaitHandle.WaitOne(MAX_WAIT_TIMESPAN);
                if (access)
                {
                    return true;
                }
                timeout -= MAX_WAIT_TIMESPAN;
            }
            return token.WaitHandle.WaitOne(timeout);
        }
    }
}
