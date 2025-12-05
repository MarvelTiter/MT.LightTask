using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask.Test;

[TestClass]
public class IntervalTaskTest
{
    class IntervalTask : ITask
    {
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"运行时间: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss:fff}");
            await Task.Delay(1200, cancellationToken);
        }
    }
    [TestMethod]
    public async Task IntervalTest()
    {
        // 配置任务
        var services = new ServiceCollection();
        services.AddLightTask();
        services.AddScoped<IntervalTask>();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var tc = provider.GetRequiredService<ITaskCenter>();
        var now = DateTimeOffset.Now;
        tc.AddTask<IntervalTask>("测试", b => b.WithInterval(TimeSpan.FromSeconds(1)));
        await Task.Delay(TimeSpan.FromSeconds(10));
    }
}
