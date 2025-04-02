
namespace MT.LightTask;

class StrategyBuilder : IStrategyBuilder
{
    public IScheduleStrategy Once(DateTimeOffset startTime) => new DefaultScheduleStrategy() { StartTime = startTime };

    public IScheduleStrategy WithCron(string cron) => new CronScheduleStrategy(cron);
}