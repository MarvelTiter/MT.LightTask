namespace MT.LightTask;


/// <summary>
/// 定时任务接口
/// </summary>
public interface ITask
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 带上下文的定时任务接口
/// </summary>
/// <typeparam name="TContext"></typeparam>
public interface ITask<TContext>
{
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
}