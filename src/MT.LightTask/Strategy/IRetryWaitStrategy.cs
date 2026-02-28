using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask.Strategy;

/// <summary>
/// 重试等待策略接口
/// </summary>
public interface IRetryWaitStrategy
{
    /// <summary>
    /// 获取等待时间
    /// </summary>
    TimeSpan GetWaitDuration(IScheduleStrategy strategy);

    /// <summary>
    /// 序列化数据
    /// </summary>
    Dictionary<string, object?> Serialize();

    /// <summary>
    /// 反序列化
    /// </summary>
    void Deserialize(Dictionary<string, object?> data);
}
