namespace MT.LightTask;

public interface IStrategyBuilder
{
    IScheduleStrategy WithCron(string cron);
}
