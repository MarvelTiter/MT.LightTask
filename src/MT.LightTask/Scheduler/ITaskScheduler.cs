namespace MT.LightTask;

/// <summary>
/// 任务调度器接口
/// </summary>
public interface ITaskScheduler
{
    /// <summary>
    /// 调度器名称
    /// </summary>
    string Name { get; }
    /// <summary>
    /// 调度器的任务
    /// </summary>
    ITask? Task { get; set; }
    /// <summary>
    /// 调度任务发生的异常
    /// </summary>
    Exception? Exception { get; set; }
    /// <summary>
    /// 调度策略
    /// </summary>
    IScheduleStrategy? Strategy { get; set; }
    
    /// <summary>
    /// 当前任务状态
    /// </summary>
    TaskRunStatus TaskStatus { get; }

    /// <summary>
    /// 调度器状态
    /// </summary>
    TaskScheduleStatus ScheduleStatus { get; }

    /// <summary>
    /// 如果调度器正在运行，跳过等待时间，立即执行
    /// </summary>
    void RunImmediately();

    /// <summary>
    /// 启动调度器
    /// </summary>
    void Start();

    /// <summary>
    /// 停止调度器
    /// </summary>
    void Stop();
}