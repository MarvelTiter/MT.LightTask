using MT.LightTask.Storage;
using MT.LightTask.Strategy;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MT.LightTask;

public sealed class StrategyBuilder : IStrategyBuilder
{
    public static StrategyBuilder Default => new();

    [JsonConverter(typeof(JsonStringEnumConverter<ScheduleType>))]
    public ScheduleType Type { get; set; }
    public DateTimeOffset? Start { get; set; }
    public string? Cron { get; set; }
    public int Retry { get; set; } = 0;
    public TimeSpan? Interval { get; set; }
    public int BaseInterval { get; set; }
    public TimeSpan? Timeout { get; set; }

    [JsonIgnore]
    public IRetryWaitStrategy Strategy { get; set; } = new DefaultRetryWaitDuration();
    public Dictionary<string, object?>? CustomRetryStrategy { get; set; }
    [JsonIgnore]
    public bool ShouldStroage { get; set; }

    public IScheduleStrategy Build()
    {
        return Type switch
        {
            ScheduleType.Once => new OnceScheduleStrategy()
            {
                StartTime = Start,
                RetryLimit = Retry,
                Timeout = Timeout,
                RetryWaitStrategy = Strategy,
                RetryIntervalBase = BaseInterval,
            },
            ScheduleType.Cron => new CronScheduleStrategy(Cron!)
            {
                RetryLimit = Retry,
                Timeout = Timeout,
                RetryWaitStrategy = Strategy,
                RetryIntervalBase = BaseInterval,
            },
            ScheduleType.Signal => new SignalScheduleStrategy()
            {
                RetryLimit = Retry,
                Timeout = Timeout,
                RetryWaitStrategy = Strategy,
                RetryIntervalBase = BaseInterval,
            },
            ScheduleType.Interval => new IntervalScheduleStrategy(Interval!.Value)
            {
                Timeout = Timeout,
                RetryWaitStrategy = Strategy,
                RetryIntervalBase = BaseInterval,
            },
            _ => throw new ArgumentException()
        };
    }

    public IStrategyBuilder Once(DateTimeOffset startTime)
    {
        Type = ScheduleType.Once;
        Start = startTime;
        return this;
    }

    public IStrategyBuilder WithCron(string cron)
    {
        Type = ScheduleType.Cron;
        this.Cron = cron;
        return this;
    }

    public IStrategyBuilder WithSignal()
    {
        Type = ScheduleType.Signal;
        return this;
    }

    public IStrategyBuilder WithInterval(TimeSpan interval)
    {
        Type = ScheduleType.Interval;
        this.Interval = interval;
        return this;
    }

    public IStrategyBuilder WithRetry(int times, int baseInterval = 1000)
    {
        Retry = times;
        this.BaseInterval = baseInterval;
        return this;
    }

    public IStrategyBuilder UseRetryStrategy<T>(int times)
        where T : IRetryWaitStrategy, new()
    {
        Retry = times;
        Strategy = new T();
        CustomRetryStrategy = Strategy.Serialize();
        CustomRetryStrategy["RetryWaitStrategyType"] = typeof(T).AssemblyQualifiedName;
        return this;
    }

    public IStrategyBuilder WithTimeout(TimeSpan timeout)
    {
        this.Timeout = timeout;
        return this;
    }
    public IStrategyBuilder Storage()
    {
        ShouldStroage = true;
        return this;
    }
    public IStrategyBuilder WithRetry(int times, Func<int, TimeSpan> durationProvider)
    {
        //retry = times;
        //this.durationProvider = durationProvider;
        //return this;
        throw new NotSupportedException();
    }


}
