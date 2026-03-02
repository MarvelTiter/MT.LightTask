using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MT.LightTask.Storage;
using System.IO;

namespace MT.LightTask;

public class TaskOptions
{
    public string? StoragePath { get; set; }
    public bool EnableStorage { get; set; }

    internal static TaskOptions Instance = new();
}

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加轻量任务服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="option"></param>
    /// <returns></returns>
    public static IServiceCollection AddLightTask(this IServiceCollection services, Action<TaskOptions>? option = null)
    {
        // 默认存储路径：应用程序目录下的 tasks.json
        option?.Invoke(TaskOptions.Instance);
        services.TryAddSingleton<ITaskCenter, TaskCenter>();
        services.TryAddSingleton<ILightTaskStorage, LightTaskFileStorage>();
        services.AddHostedService<TaskHost>();
        return services;
    }

}
