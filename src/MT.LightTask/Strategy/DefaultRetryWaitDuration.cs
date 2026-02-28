using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask.Strategy;

internal class DefaultRetryWaitDuration : IRetryWaitStrategy
{
    public void Deserialize(Dictionary<string, object?> data)
    {

    }

    public TimeSpan GetWaitDuration(IScheduleStrategy strategy)
    {
        var times = Math.Pow(2, strategy.RetryTimes);
        return TimeSpan.FromMilliseconds(strategy.RetryIntervalBase * times);
    }

    public Dictionary<string, object?> Serialize()
    {
        return [];
    }
}
