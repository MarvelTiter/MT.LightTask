using Microsoft.Extensions.DependencyInjection;

namespace MT.LightTask.Storage;

public class LightTaskFileStorage : ILightTaskStorage
{
    private readonly string filePath;

    public LightTaskFileStorage()
    {
        filePath = TaskOptions.Instance.StoragePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lighttasks.json");
    }

    public async Task LoadTasksAsync(ITaskCenter tc, CancellationToken cancellationToken)
    {
        var files = RetrieveSchedulers();
        foreach (var fileName in files)
        {
            try
            {
#if NET8_0_OR_GREATER
                var json = await File.ReadAllTextAsync(fileName, cancellationToken);
#else
                var json = File.ReadAllText(fileName);
#endif
#if NET8_0_OR_GREATER
                var config = System.Text.Json.JsonSerializer.Deserialize(json, JsonContext.Default.TaskConfig);
#else
                var config = Newtonsoft.Json.JsonConvert.DeserializeObject<TaskConfig>(json);
#endif
                if (config is null) continue;

#pragma warning disable IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                var taskType = Type.GetType(config.TaskTypeName);
#pragma warning restore IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                if (taskType is not null)
                {
                    var strategy = config.Builder?.Build();

                    if (taskType == typeof(DefaultTask))
                    {
                        var handler = RestoreDelegateTask(config.Name);
                        if (handler is not null)
                        {
                            tc.AddTask(config.Name, new DefaultTask(handler, tc.ServiceProvider), strategy);
                        }
                    }
                    else
                    {
                        //strategy.LoadData(config.Values);
                        var ti = (ITask)tc.ServiceProvider.GetRequiredService(taskType);
                        tc.AddTask(config.Name, ti, strategy);
                    }
                }
            }
#if NET8_0_OR_GREATER
            catch (System.Text.Json.JsonException)
#else 
            catch (Newtonsoft.Json.JsonException)
#endif
            {
                tc.Log($"{fileName}加载失败");
                continue;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    public virtual Func<IServiceProvider, CancellationToken, Task>? RestoreDelegateTask(string name) => null;

    public void SaveTaskConfig(TaskConfig config)
    {
        try
        {
            //var dic = scheduler.Strategy.SaveData();
#if NET8_0_OR_GREATER
            var json = System.Text.Json.JsonSerializer.Serialize(config, JsonContext.Default.TaskConfig);
#else
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
#endif
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
#if NET8_0_OR_GREATER
            var config = await File.ReadAllTextAsync(fileName, cancellationToken);
#else
            var config = File.ReadAllText(fileName);
#endif
            try
            {
#if NET8_0_OR_GREATER
                return System.Text.Json.JsonSerializer.Deserialize(config, JsonContext.Default.TaskStatus);
#else
#endif
            }
#if NET8_0_OR_GREATER
            catch (System.Text.Json.JsonException)
#else
            catch(Newtonsoft.Json.JsonException)
#endif
            {
                return null;
            }
            catch (Exception)
            {

                throw;
            }
        }
        return null;
    }


    public void SaveTaskStatus(string name, TaskStatus config)
    {
        var fileName = Path.Combine(filePath, $"{name}.sbin");
        var tempFile = Path.Combine(filePath, $"{name}.tmp");
        try
        {
#if NET8_0_OR_GREATER
            var json = System.Text.Json.JsonSerializer.Serialize(config, JsonContext.Default.TaskStatus);
#else
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
#endif
            File.WriteAllText(tempFile, json);
#if NET8_0_OR_GREATER
            File.Move(tempFile, fileName, true);
#else
            File.Move(tempFile, fileName);
#endif
        }
        catch (Exception ex)
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            throw new InvalidOperationException($"保存任务状态失败: {name}", ex);
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
