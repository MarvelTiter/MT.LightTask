using MT.LightTask.Strategy;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace MT.LightTask;

internal abstract class DefaultScheduleStrategy : IScheduleStrategy
{
    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? LastRuntime { get; set; }

    public DateTimeOffset? NextRuntime { get; set; }

    public TimeSpan LastRunElapsedTime { get; set; }
    public TimeSpan? Timeout { get; set; }
    public int RetryLimit { get; set; }
    public int RetryTimes { get; set; }
    public int RetryIntervalBase { get; set; }
    public Func<int, TimeSpan>? WaitDurationProvider { get; set; }
    [NotNull] public IRetryWaitStrategy? RetryWaitStrategy { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<TaskRunStatus>))]
    public TaskRunStatus RunStatus { get; set; }

    public abstract bool WaitForExecute(CancellationToken cancellationToken);

    public virtual Dictionary<string, object?> SaveData()
    {
        var data = new Dictionary<string, object?>
        {
            [nameof(StartTime)] = StartTime,
            [nameof(LastRuntime)] = LastRuntime,
            [nameof(LastRunElapsedTime)] = LastRunElapsedTime,
            [nameof(Timeout)] = Timeout,
            [nameof(RetryLimit)] = RetryLimit,
            [nameof(RetryIntervalBase)] = RetryIntervalBase,
            [nameof(RunStatus)] = (int)RunStatus,
        };
        return data;
    }

    public virtual void LoadData(Dictionary<string, object?> datas)
    {
        if (datas.TryGetValue(nameof(StartTime), out var st) && DateTimeOffset.TryParse(st?.ToString(), out var startTime))
        {
            StartTime = startTime;
        }
        if (datas.TryGetValue(nameof(LastRuntime), out var lrt) && DateTimeOffset.TryParse(lrt?.ToString(), out var last))
        {
            LastRuntime = last;
        }
        if (datas.TryGetValue(nameof(LastRunElapsedTime), out var lret) && TimeSpan.TryParse(lret?.ToString(), out var lastE))
        {
            LastRunElapsedTime = lastE;
        }
        if (datas.TryGetValue(nameof(Timeout), out var t) && TimeSpan.TryParse(t?.ToString(), out var timeout))
        {
            Timeout = timeout;
        }
        if (datas.TryGetValue(nameof(RetryLimit), out var rl) && int.TryParse(rl?.ToString(), out var limit))
        {
            RetryLimit = limit;
        }
        if (datas.TryGetValue(nameof(RetryIntervalBase), out var rib) && int.TryParse(rib?.ToString(), out var intervalBase))
        {
            RetryIntervalBase = intervalBase;
        }
        if (datas.TryGetValue(nameof(RunStatus), out var rs) && int.TryParse(rs?.ToString(), out var ei))
        {
            RunStatus = (TaskRunStatus)ei;
        }
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
