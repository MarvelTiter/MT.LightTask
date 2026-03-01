using MT.LightTask.Storage;
using MT.LightTask.Strategy;

namespace MT.LightTask;

public interface IStrategyBuilder
{
    /// <summary>
    /// 只执行一次，与<see cref="WithInterval"/>、<see cref="WithCron"/>、<see cref="WithSignal"/>冲突
    /// </summary>
    /// <param name="startTime"></param>
    /// <returns></returns>
    IStrategyBuilder Once(DateTimeOffset startTime);
    /// <summary>
    /// 固定周期任务，与<see cref="Once"/>、<see cref="WithCron"/>、<see cref="WithSignal"/>冲突
    /// </summary>
    /// <param name="interval"></param>
    /// <returns></returns>
    IStrategyBuilder WithInterval(TimeSpan interval);
    /// <summary>
    /// 使用Cron表达式，与<see cref="WithInterval"/>、<see cref="Once"/>、<see cref="WithSignal"/>冲突
    /// </summary>
    /// <param name="cron"></param>
    /// <returns></returns>
    IStrategyBuilder WithCron(string cron);
    /// <summary>
    /// 接收信号量执行，与<see cref="WithInterval"/>、<see cref="Once"/>、<see cref="WithCron"/>冲突
    /// </summary>
    /// <returns></returns>
    IStrategyBuilder WithSignal();
    IStrategyBuilder WithRetry(int times, int baseInterval = 1000);
    IStrategyBuilder WithTimeout(TimeSpan timeout);
    /// <summary>
    /// 自定义重试退避策略，默认是指数退避
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="times"></param>
    /// <returns></returns>
    IStrategyBuilder UseRetryStrategy<T>(int times) where T : IRetryWaitStrategy, new();
    
    /// <summary>
    /// 是否将任务配置持久化，如果当前任务配置没有在程序启动路径中执行，又想下次启动时再次加载，就可以选择调用<see cref="Storage"/>
    /// </summary>
    /// <returns></returns>
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
