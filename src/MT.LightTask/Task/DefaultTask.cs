using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask;

class DefaultTask(Func<IServiceProvider, CancellationToken, Task> task, IServiceProvider serviceProvider) : ITask
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default) => task.Invoke(serviceProvider, cancellationToken);
}

class DefaultTask<TContext>(Func<IServiceProvider, TContext, CancellationToken, Task> task, IServiceProvider serviceProvider) : ITask<TContext>
{
    public Task ExecuteAsync(TContext context, CancellationToken cancellationToken = default) => task.Invoke(serviceProvider, context, cancellationToken);
}
