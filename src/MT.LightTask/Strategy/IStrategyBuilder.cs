namespace MT.LightTask;

public interface IStrategyBuilder
{
    IStrategyBuilder Once(DateTimeOffset startTime);
    IStrategyBuilder WithCron(string cron);
    IStrategyBuilder WithSignal();
    IStrategyBuilder WithRetry(int times);
    IScheduleStrategy Build();
}
