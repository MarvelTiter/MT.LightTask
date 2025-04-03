namespace MT.LightTask;

interface ITaskAopNotify
{
    void NotifyOnError(ITaskScheduler scheduler);
    Task NotifyOnErrorAsync(ITaskScheduler scheduler);
    void NotifyOnCompleted(ITaskScheduler scheduler);
    Task NotifyOnCompletedAsync(ITaskScheduler scheduler);
    void NotifyOnCompletedSuccessfully(ITaskScheduler scheduler);
    Task NotifyOnCompletedSuccessfullyAsync(ITaskScheduler scheduler);
}