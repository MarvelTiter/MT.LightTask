namespace MT.LightTask;

class StrategyBuilder : IStrategyBuilder
{
    public IScheduleStrategy WithCron(string cron) => new CronScheduleStrategy(cron);
}