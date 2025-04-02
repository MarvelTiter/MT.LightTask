namespace MT.LightTask;

/// <summary>
/// 任务调度器接口
/// </summary>
public interface ITaskScheduler
{
    string Name { get; }
    ITask? Task { get; set; }
    Exception? Exception { get; set; }
    IScheduleStrategy? Strategy { get; set; }
    TaskRunStatus TaskStatus { get; }
    TaskScheduleStatus ScheduleStatus { get; }
    //void Start(ITask task, IScheduleStrategy strategy);
    void RunImmediately();
    void Start();
    void Stop();
}