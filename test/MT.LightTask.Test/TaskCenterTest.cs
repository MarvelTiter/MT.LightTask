using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace MT.LightTask.Test;

[TestClass]
public sealed class TaskCenterTest
{
    class TestTask(TestContext context) : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            context.Value = 100;
            return Task.CompletedTask;
        }
    }
    class TestContext
    {
        public int Value { get; set; }
    }
    [TestMethod]
    public async Task TaskClassTest()
    {
        var services = new ServiceCollection();
        services.AddLightTask();
        services.AddScoped<TestContext>();
        services.AddScoped<TestTask>();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var tc = provider.GetRequiredService<ITaskCenter>();
        var now = DateTimeOffset.Now;
        tc.AddTask<TestTask>("测试", b => b.Once(now.AddSeconds(2)).Build());
        tc.AddTask("测试2", (s, t) =>
        {
            var c = s.GetRequiredService<TestContext>();
            c.Value += 50;
            return Task.CompletedTask;
        }, b => b.Once(now.AddSeconds(3)).Build());
        await Task.Delay(TimeSpan.FromSeconds(4));
        var context = provider.GetRequiredService<TestContext>();
        Assert.IsTrue(context.Value == 150);
    }

    [TestMethod]
    public async Task TaskRunImmediatelyTest()
    {
        var services = new ServiceCollection();
        services.AddLightTask();
        services.AddScoped<TestContext>();
        services.AddScoped<TestTask>();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var tc = provider.GetRequiredService<ITaskCenter>();
        var now = DateTimeOffset.Now;
        tc.AddTask<TestTask>("测试", b => b.WithCron("*/5 * * * * ?").Build());
        var scheduler = tc.GetScheduler("测试");
        scheduler?.RunImmediately();
        await Task.Delay(TimeSpan.FromSeconds(32));
        var context = provider.GetRequiredService<TestContext>();
        Assert.IsTrue(context.Value == 100);
    }
}
