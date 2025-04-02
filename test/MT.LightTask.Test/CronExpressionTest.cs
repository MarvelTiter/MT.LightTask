namespace MT.LightTask.Test;

[TestClass]
public sealed class CronExpressionTest
{
    [TestMethod]
    public void CronTest1()
    {
        var cron = CronExpression.Parse("0 5-10 19 ? * *");
        var next = cron.GetNextOccurrence();
        next = cron.GetNextOccurrence(next);
        var tar = DateTime.Now.Date;
        // 19点05前是true, 否则是下一天
        Assert.IsTrue(next.Date == tar && next.Hour == 19 && next.Minute == 6 && next.Second == 0);
    }
}
