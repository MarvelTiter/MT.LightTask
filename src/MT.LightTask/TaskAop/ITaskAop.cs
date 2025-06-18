namespace MT.LightTask;

interface ITaskAopNotify
{
    void NotifyTaskStatusChanged(ITaskScheduler scheduler);
    void NotifyTaskScheduleChanged(ITaskScheduler scheduler);
    void NotifyTaskCompleted(ITaskScheduler scheduler);
}