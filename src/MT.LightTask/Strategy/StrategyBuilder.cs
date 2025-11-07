namespace MT.LightTask;

class StrategyBuilder : IStrategyBuilder
{
    int type = 0;
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
            1 => new DefaultScheduleStrategy()
            {
                StartTime = start,
                RetryLimit = retry,
                WaitDurationProvider = durationProvider,
                RetryIntervalBase = baseInterval
            },
            2 => new CronScheduleStrategy(cron!)
            {
                RetryLimit = retry,
                WaitDurationProvider = durationProvider,
                RetryIntervalBase = baseInterval
            },
            3 => new SignalScheduleStrategy()
            {
                RetryLimit = retry,
                WaitDurationProvider = durationProvider,
                RetryIntervalBase = baseInterval
            },
            4 => new IntervalScheduleStrategy(interval!.Value)
            {
                WaitDurationProvider = durationProvider,
                RetryIntervalBase = baseInterval
            },
            _ => throw new ArgumentException()
        };
    }

    public IStrategyBuilder Once(DateTimeOffset startTime)
    {
        type = 1;
        start = startTime;
        return this;
    }

    public IStrategyBuilder WithCron(string cron)
    {
        type = 2;
        this.cron = cron;
        return this;
    }

    public IStrategyBuilder WithSignal()
    {
        type = 3;
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

    public IStrategyBuilder WithInterval(TimeSpan interval)
    {
        type = 4;
        this.interval = interval;
        return this;
    }
}