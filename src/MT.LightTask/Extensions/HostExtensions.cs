using Microsoft.Extensions.DependencyInjection;

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


