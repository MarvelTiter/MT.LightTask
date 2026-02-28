using MT.LightTask.Storage;
using MT.LightTask.Strategy;

namespace MT.LightTask;

public interface IStrategyBuilder
{
    IStrategyBuilder Once(DateTimeOffset startTime);
    IStrategyBuilder WithInterval(TimeSpan interval);
    IStrategyBuilder WithCron(string cron);
    IStrategyBuilder WithSignal();
    IStrategyBuilder WithRetry(int times, int baseInterval = 1000);
    IStrategyBuilder WithTimeout(TimeSpan timeout);
    IStrategyBuilder UseRetryStrategy<T>(int times) where T : IRetryWaitStrategy, new();
    IStrategyBuilder Storage();

    [Obsolete("使用UseRetryStrategy", true)]
    /// <summary>
    /// 重试配置, 自定义退避策略
    /// </summary>
    /// <param name="times"></param>
    /// <param name="durationProvider"></param>
    /// <returns></returns>
    IStrategyBuilder WithRetry(int times, Func<int, TimeSpan> durationProvider);

    internal IScheduleStrategy Build();
}
