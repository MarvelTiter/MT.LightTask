using Microsoft.Extensions.Hosting;
using MT.LightTask.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask;

class TaskHost(ITaskCenter center, ILightTaskStorage storage) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await storage.LoadTasksAsync(center, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        center.Stop(cancellationToken);
        return Task.CompletedTask;
    }
}
