using System.Collections.Generic;
using System.Threading.Tasks;

namespace MT.LightTask.Storage;

public interface ILightTaskStorage
{
    Task SaveTaskConfigAsync(TaskConfig config);

    Task<TaskConfig?> LoadTaskConfigAsync(string name);

    Task<IEnumerable<TaskConfig>> LoadAllTaskConfigsAsync();

    Task<bool> DeleteTaskConfigAsync(string name);
}
