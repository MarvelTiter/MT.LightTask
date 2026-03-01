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
builder.Services.AddTransient<IntervalTask>();
builder.Services.AddTransient<OnceTask>();
builder.Services.AddTransient<SignalTask>();
builder.Services.AddTransient<CronTask>();

var app = builder.Build();

app.UseLightTask(tc =>
{
    tc.AddTask<OnceTask>("Once", b => b.Once(DateTimeOffset.Now.AddSeconds(10)));
    tc.AddTask<SignalTask>("Signal", b => b.WithSignal());
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
