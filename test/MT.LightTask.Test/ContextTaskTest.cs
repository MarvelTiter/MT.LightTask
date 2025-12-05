using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask.Test;

[TestClass]
public class ContextTaskTest
{
    private static readonly Dictionary<int, int> count = [];
    private static async Task TaskPayload(IServiceProvider serviceProvider, int type, CancellationToken cancellationToken)
    {
        var wait = new Random().Next(0, 500);
        await Task.Delay(wait, cancellationToken);
        Console.WriteLine($"任务type:{type}, 等待时间: {wait}ms");
        count[type] = count[type] + 1;
    }
    [TestMethod]
    public async Task Run()
    {
        var services = new ServiceCollection();
        services.AddLightTask();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var tc = provider.GetRequiredService<ITaskCenter>();
        count.Clear();
        for (int i = 0; i < 10; i++)
        {
            count[i + 1] = 0;
            tc.AddTaskWithContext($"任务{i + 1}", i + 1, TaskPayload, s => s.WithInterval(TimeSpan.FromSeconds(2)));
        }
        await Task.Delay(TimeSpan.FromSeconds(10));
        foreach (var item in count)
        {
            Console.WriteLine($"{item.Key} - {item.Value}");
        }
    }
}
