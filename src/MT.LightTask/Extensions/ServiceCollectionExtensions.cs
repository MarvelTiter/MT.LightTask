using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MT.LightTask.Storage;
using System.IO;

namespace MT.LightTask;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLightTask(this IServiceCollection services)
    {
        // 默认存储路径：应用程序目录下的 tasks.json
        var defaultStoragePath = Path.Combine(AppContext.BaseDirectory, "tasks.json");
        return AddLightTask(services, defaultStoragePath);
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
        services.TryAddSingleton<ILightTaskStorage>(sp => new FileLightTaskStorage(storagePath));
        services.AddHostedService<TaskHost>();
        return services;
    }
}
