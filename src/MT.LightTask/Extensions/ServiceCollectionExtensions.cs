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
    public static IServiceCollection AddLightTask(this IServiceCollection services, Action<TaskOptions>? option = null)
    {
        // 默认存储路径：应用程序目录下的 tasks.json
        option?.Invoke(TaskOptions.Instance);
        var defaultStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lighttasks.json");
        services.TryAddSingleton<ITaskCenter, TaskCenter>();
        services.TryAddSingleton<ILightTaskStorage>(sp => new LightTaskFileStorage(TaskOptions.Instance.StoragePath ?? defaultStoragePath));
        services.TryAddSingleton<ILightTaskStorage, LightTaskFileStorage>();
        services.AddHostedService<TaskHost>();
        return services;
    }

    /// <summary>
    /// 添加轻量任务服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="storagePath">任务配置存储路径</param>
    /// <returns></returns>
    public static IServiceCollection AddLightTask(this IServiceCollection services, string storagePath)
    {
        services.TryAddSingleton<ITaskCenter, TaskCenter>();
        services.TryAddSingleton<ILightTaskStorage>(sp => new LightTaskFileStorage(storagePath));
        services.TryAddSingleton<ILightTaskStorage, LightTaskFileStorage>();
        services.AddHostedService<TaskHost>();
        return services;
    }
}
