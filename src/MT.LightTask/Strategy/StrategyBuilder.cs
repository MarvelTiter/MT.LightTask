using MT.LightTask.Storage;

namespace MT.LightTask;

class StrategyBuilder : IStrategyBuilder
{
    ScheduleType type = 0;
    DateTimeOffset? start;
    string? cron;
    int retry = 0;
    TimeSpan? interval;
    int baseInterval;
    Func<int, TimeSpan>? durationProvider;
    public IScheduleStrategy Build()
    {
        return type switch
        {
            ScheduleType.Once => new DefaultScheduleStrategy()
            {
                StartTime = start,
                RetryLimit = retry,
                WaitDurationProvider = durationProvider,
                RetryIntervalBase = baseInterval
            },
            ScheduleType.Cron => new CronScheduleStrategy(cron!)
            {
                RetryLimit = retry,
                WaitDurationProvider = durationProvider,
                RetryIntervalBase = baseInterval
            },
            ScheduleType.Signal => new SignalScheduleStrategy()
            {
                RetryLimit = retry,
                WaitDurationProvider = durationProvider,
                RetryIntervalBase = baseInterval
            },
            ScheduleType.Interval => new IntervalScheduleStrategy(interval!.Value)
            {
                WaitDurationProvider = durationProvider,
                RetryIntervalBase = baseInterval
            },
            _ => throw new ArgumentException()
        };
    }

    public IStrategyBuilder Once(DateTimeOffset startTime)
    {
        type = ScheduleType.Cron;
        start = startTime;
        return this;
    }

    public IStrategyBuilder WithCron(string cron)
    {
        type = ScheduleType.Cron;
        this.cron = cron;
        return this;
    }

    public IStrategyBuilder WithSignal()
    {
        type = ScheduleType.Signal;
        return this;
    }

    public IStrategyBuilder WithInterval(TimeSpan interval)
    {
        type = ScheduleType.Interval;
        this.interval = interval;
        return this;
    }

    public IStrategyBuilder WithRetry(int times, int baseInterval = 1000)
    {
        retry = times;
        this.baseInterval = baseInterval;
        return this;
    }

    public IStrategyBuilder WithRetry(int times, Func<int, TimeSpan> durationProvider)
    {
        retry = times;
        this.durationProvider = durationProvider;
        return this;
    }

}
