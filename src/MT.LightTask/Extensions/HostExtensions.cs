using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MT.LightTask;

public static class HostExtensions
{
    public static T UseLightTask<T>(this T host, Action<ITaskCenter> config)
        where T : Microsoft.Extensions.Hosting.IHost
    {
        var center = host.Services.GetRequiredService<ITaskCenter>();
        config.Invoke(center);
        return host;
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLightTask(this IServiceCollection services)
    {
        services.TryAddSingleton<ITaskCenter, TaskCenter>();
        services.AddHostedService<TaskHost>();
        return services;
    }
}


