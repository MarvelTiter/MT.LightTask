namespace MT.LightTask;

class StrategyBuilder : IStrategyBuilder
{
    int type = 0;
    DateTimeOffset? start;
    string? cron;
    int retry = 0;

    public IScheduleStrategy Build()
    {
        return type switch
        {
            1 => new DefaultScheduleStrategy() { StartTime = start, RetryLimit = retry },
            2 => new CronScheduleStrategy(cron!) { RetryLimit = retry },
            3 => new SignalScheduleStrategy() { RetryLimit = retry },
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

    public IStrategyBuilder WithRetry(int times)
    {
        retry = times;
        return this;
    }
}