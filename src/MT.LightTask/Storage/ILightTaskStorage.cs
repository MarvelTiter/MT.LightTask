using System.Collections.Generic;
using System.Threading.Tasks;

namespace MT.LightTask.Storage;

public interface ILightTaskStorage
{
    Task LoadTasksAsync(ITaskCenter tc, CancellationToken cancellationToken);
    void SaveTaskConfig(TaskConfig config);

    Task<TaskStatus?> LoadTaskStatusAsync(string name, CancellationToken cancellationToken);
    void SaveTaskStatus(string name, TaskStatus config);

    void RemoveTaskStorage(string name);
}
