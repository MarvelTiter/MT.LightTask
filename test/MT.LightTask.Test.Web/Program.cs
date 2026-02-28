using MT.LightTask;
using MT.LightTask.Test.Web.Components;
using MT.LightTask.Test.Web.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLightTask(o =>
{
    o.EnableStorage = true;
});
builder.Services.AddTransient<RetryTask>();
builder.Services.AddTransient<Task2>();

var app = builder.Build();

app.UseLightTask(tc =>
{
    tc.AddTask<Task2>("测试3", "*/12 * * * * ?");
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
