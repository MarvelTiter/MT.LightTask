using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask.Test;

[TestClass]
public class CancellationTokenSourceResetTest
{
    [TestMethod]
    public async Task TryReset()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await Task.Delay(TimeSpan.FromSeconds(3));
        Assert.IsTrue(cts.IsCancellationRequested);
        var b = cts.TryReset();
        Assert.IsFalse(cts.IsCancellationRequested);
    }
}
