# MT.LightTask
 轻量定时任务

## 使用方式
```csharp
// 注册服务
builder.Services.AddLightTask();

// 注册任务
var app = builder.Build();
// Configure the HTTP request pipeline.
app.UseLightTask(c =>
{
    c.AddTask("测试1", (sp, token) =>
    {
        Console.WriteLine($"Task测试1: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        return Task.CompletedTask;
    }, b => b.WithCron("*/12 * * * * ?"));

    // TestTask需要实现ITask，并且注册到容器中
    c.AddTask<TestTask>("测试2", b => b.WithCron("*/5 * * * * ?").Build());
});

// TestTask
[AutoInject(ServiceType = typeof(TestTask))]
public class TestTask : ITask
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Task测试2: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        return Task.CompletedTask;
    }
}
```
