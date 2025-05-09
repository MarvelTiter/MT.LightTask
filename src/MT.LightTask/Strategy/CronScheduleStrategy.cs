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
        var shouldExecute = !cancellationToken.WaitHandle.WaitOne(wait);
        LastRuntime = DateTimeOffset.Now;
        NextRuntime = Cron.GetNextOccurrence(next);
        return shouldExecute;
    }
}