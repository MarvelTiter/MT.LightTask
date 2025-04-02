using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask;

class TaskHost(ITaskCenter center) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        //center.Start(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        center.Stop(cancellationToken);
        return Task.CompletedTask;
    }
}
