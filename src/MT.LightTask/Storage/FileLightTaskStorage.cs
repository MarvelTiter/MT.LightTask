using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MT.LightTask.Storage;

public class FileLightTaskStorage(string filePath) : ILightTaskStorage
{
    private readonly string filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    private readonly object fileLock = new();

    public Task<bool> DeleteTaskConfigAsync(string name)
    {
        lock (fileLock)
        {
            var list = LoadAllFromFile();
            var removed = list.RemoveAll(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
            {
                SaveAllToFile(list);
            }
            return Task.FromResult(removed);
        }
    }

    public Task<TaskConfig?> LoadTaskConfigAsync(string name)
    {
        lock (fileLock)
        {
            var list = LoadAllFromFile();
            var cfg = list.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(cfg);
        }
    }

    public Task<IEnumerable<TaskConfig>> LoadAllTaskConfigsAsync()
    {
        lock (fileLock)
        {
            var list = LoadAllFromFile();
            return Task.FromResult<IEnumerable<TaskConfig>>(list);
        }
    }

    public Task SaveTaskConfigAsync(TaskConfig config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        lock (fileLock)
        {
            var list = LoadAllFromFile();
            var idx = list.FindIndex(x => string.Equals(x.Name, config.Name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                list[idx] = config;
            }
            else
            {
                list.Add(config);
            }
            SaveAllToFile(list);
            return Task.CompletedTask;
        }
    }

    private List<TaskConfig> LoadAllFromFile()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new List<TaskConfig>();
            }
            var txt = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(txt)) return new List<TaskConfig>();
            var list = JsonSerializer.Deserialize(txt, SourceGenerationContext.Default.ListTaskConfig);
            return list ?? [];
        }
        catch
        {
            return new List<TaskConfig>();
        }
    }

    private void SaveAllToFile(List<TaskConfig> list)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var txt = JsonSerializer.Serialize(list, SourceGenerationContext.Default.ListTaskConfig);
        File.WriteAllText(filePath, txt);
    }
}
