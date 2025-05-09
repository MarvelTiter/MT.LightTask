namespace MT.LightTask;

/// <summary>
/// 除非收到取消等待的信号，否则不会执行
/// </summary>
internal class SignalScheduleStrategy : DefaultScheduleStrategy
{
    public override bool WaitForExecute(CancellationToken cancellationToken)
    {
        var signal = !cancellationToken.WaitHandle.WaitOne();
        LastRuntime = DateTimeOffset.Now;
        return signal;
    }
}