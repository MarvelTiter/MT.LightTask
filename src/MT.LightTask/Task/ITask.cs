namespace MT.LightTask;

/// <summary>
/// 定时任务接口
/// </summary>
public interface ITask
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
