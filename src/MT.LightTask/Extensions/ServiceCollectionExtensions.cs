using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MT.LightTask;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLightTask(this IServiceCollection services)
    {
        services.TryAddSingleton<ITaskCenter, TaskCenter>();
        services.AddHostedService<TaskHost>();
        return services;
    }
}


