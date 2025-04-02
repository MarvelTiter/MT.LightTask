namespace MT.LightTask;

class CronScheduleStrategy(string cron) : DefaultScheduleStrategy
{
    private readonly CronExpression Cron = CronExpression.Parse(cron);

    public override bool WaitForExecute(Action<DateTimeOffset> handleNext, CancellationToken cancellationToken)
    {
        var next = Cron.GetNextOccurrence(DateTimeOffset.Now);
        var wait = next - DateTimeOffset.Now;
        handleNext(next);
        var ret = !cancellationToken.WaitHandle.WaitOne(wait);
        LastRuntime = DateTimeOffset.Now;
        NextRuntime = Cron.GetNextOccurrence(next);
        return ret;
    }
}
