namespace MT.LightTask;

public interface IStrategyBuilder
{
    IStrategyBuilder Once(DateTimeOffset startTime);
    IStrategyBuilder WithInterval(TimeSpan interval);
    IStrategyBuilder WithCron(string cron);
    IStrategyBuilder WithSignal();
    IStrategyBuilder WithRetry(int times, int baseInterval = 1000);
    /// <summary>
    /// 重试配置, 自定义退避策略
    /// </summary>
    /// <param name="times"></param>
    /// <param name="durationProvider"></param>
    /// <returns></returns>
    IStrategyBuilder WithRetry(int times, Func<int, TimeSpan> durationProvider);
    IScheduleStrategy Build();
}
