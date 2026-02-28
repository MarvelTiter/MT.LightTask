using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MT.LightTask.Storage;

public class LightTaskFileStorage(string filePath) : ILightTaskStorage
{
    public async Task LoadTasksAsync(ITaskCenter tc, CancellationToken cancellationToken)
    {
        var files = RetrieveSchedulers();
        foreach (var fileName in files)
        {
            var json = await File.ReadAllTextAsync(fileName, cancellationToken);
            var config = JsonSerializer.Deserialize(json, JsonContext.Default.TaskConfig);
            if (config is null) continue;

#pragma warning disable IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
            var taskType = Type.GetType(config.TaskTypeName);
#pragma warning restore IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
            if (taskType is not null)
            {
                var strategy = config.Builder?.Build();
                //strategy.LoadData(config.Values);
                var ti = (ITask)tc.ServiceProvider.GetRequiredService(taskType);
                tc.AddTask(config.Name, ti, strategy);
            }
        }
    }


    public void SaveTaskConfig(TaskConfig config)
    {
        try
        {
            //var dic = scheduler.Strategy.SaveData();
            var json = JsonSerializer.Serialize(config, JsonContext.Default.TaskConfig);
            var fileName = Path.Combine(filePath, $"{config.Name}.bin");
            File.WriteAllText(fileName, json);
        }
        catch (Exception)
        {
            throw;
        }

    }

    public async Task<TaskStatus?> LoadTaskStatusAsync(string name, CancellationToken cancellationToken)
    {
        var fileName = Path.Combine(filePath, $"{name}.sbin");
        if (File.Exists(fileName))
        {
            var config = await File.ReadAllTextAsync(fileName, cancellationToken);
            return JsonSerializer.Deserialize(config, JsonContext.Default.TaskStatus); ;
        }
        return null;
    }


    public void SaveTaskStatus(string name, TaskStatus config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, JsonContext.Default.TaskStatus);
            var fileName = Path.Combine(filePath, $"{name}.sbin");
            File.WriteAllText(fileName, json);
        }
        catch (Exception)
        {
            throw;
        }
    }

    protected IEnumerable<string> RetrieveSchedulers()
    {
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }
        return Directory.EnumerateFiles(filePath, "*.bin", SearchOption.TopDirectoryOnly);
    }

    public void RemoveTaskStorage(string name)
    {
        var file1 = Path.Combine(filePath, $"{name}.sbin");
        var file2 = Path.Combine(filePath, $"{name}.bin");
        if (File.Exists(file1)) File.Delete(file1);
        if (File.Exists(file2)) File.Delete(file2);
    }
}
