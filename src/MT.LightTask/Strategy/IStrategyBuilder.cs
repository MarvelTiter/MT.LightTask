namespace MT.LightTask;

public interface IStrategyBuilder
{
    IScheduleStrategy Once(DateTimeOffset startTime);
    IScheduleStrategy WithCron(string cron);
}
