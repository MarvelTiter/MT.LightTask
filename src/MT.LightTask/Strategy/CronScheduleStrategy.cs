namespace MT.LightTask;

internal class CronScheduleStrategy(string cron) : DefaultScheduleStrategy
{
    private readonly CronExpression Cron = CronExpression.Parse(cron);

    public override bool WaitForExecute(CancellationToken cancellationToken)
    {
        var next = Cron.GetNextOccurrence(DateTimeOffset.Now);
        var wait = next - DateTimeOffset.Now;
        NextRuntime = next;
        // 没收到取消信号，是false，说明到时间了
        //var reciveCancelSignal = cancellationToken.WaitHandle.WaitOne(wait);
        var reciveCancelSignal = Wait(wait, cancellationToken);
        LastRuntime = DateTimeOffset.Now;
        // 到时间执行的情况，计算下次运行时间，如果是立即执行，NextRuntime应该不变，不考虑边界情况
        if (!reciveCancelSignal)
        {
            NextRuntime = Cron.GetNextOccurrence(next);
        }
        return !reciveCancelSignal;
    }
}
