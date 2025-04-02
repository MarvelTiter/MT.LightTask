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
