using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace MT.LightTask.Test;

[TestClass]
public sealed class SignalTaskTest
{
    class SignalTask : ITask
    {
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"SignalTask: ExecuteAsync Start {DateTime.Now}");
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            Debug.WriteLine($"SignalTask: ExecuteAsync Finished {DateTime.Now}");
        }
    }

    [TestMethod]
    public async Task SignalTest()
    {
        // 配置任务
        var services = new ServiceCollection();
        services.AddLightTask();
        services.AddScoped<SignalTask>();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var tc = provider.GetRequiredService<ITaskCenter>();
        var now = DateTimeOffset.Now;
        tc.AddTask<SignalTask>("测试", b => b.WithSignal().Build());
        // 测试任务
        var task = tc.GetScheduler("测试");
        var runSuccess = task.RunImmediately();
        Assert.IsTrue(runSuccess);
        runSuccess = task.RunImmediately();
        Assert.IsFalse(runSuccess);
        await Task.Delay(TimeSpan.FromSeconds(3));
        runSuccess = task.RunImmediately();
        Assert.IsTrue(runSuccess);
        await Task.Delay(TimeSpan.FromSeconds(3));
    }

    [TestMethod]
    public async Task SignalTest2()
    {
        // 配置任务
        var services = new ServiceCollection();
        services.AddLightTask();
        services.AddScoped<SignalTask>();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var tc = provider.GetRequiredService<ITaskCenter>();
        var now = DateTimeOffset.Now;
        tc.AddTask<SignalTask>("测试", b => b.WithSignal().Build());
        // 测试任务
        var task = tc.GetScheduler("测试");
        int successCount = 0;
        int failCount = 0;
        while (successCount < 3)
        {
            var success = task.RunImmediately();
            if (success)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }
        await Task.Delay(5000);
        Debug.WriteLine($"{successCount}/{failCount}");
    }
}