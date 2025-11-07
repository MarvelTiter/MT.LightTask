using Mono.Cecil.Rocks;
using System.Diagnostics;

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

    [TestMethod]
    public void 每天12点执行()
    {
        var expression = "0 12 * * ?";
        var cron = CronExpression.Parse(expression);
        var start = DateTimeOffset.Now;
        var next = cron.GetNextOccurrence();
        var shouldAddDays = start.Hour >= 12 ? 1 : 0;
        Assert.IsTrue(start.AddDays(shouldAddDays).Day == next.Day);
        Assert.IsTrue(next.Hour == 12);
        Assert.AreEqual(true, cron.DayOfMonths.AllSpec);
        Assert.AreEqual(true, cron.Months.AllSpec);
        Assert.AreEqual(true, cron.DayOfWeeks.NoSpec);
        Debug.WriteLine(cron);
    }

    [TestMethod]
    public void Should_Parse_Hourly_At_Zero_Minute()
    {
        var expression = "0 * ? * *";
        var cron = CronExpression.Parse(expression);
        var start = DateTimeOffset.Now;
        var next = cron.GetNextOccurrence();
        Assert.IsTrue(start.Hour + 1 == next.Hour);
        Assert.AreEqual(1, cron.Seconds.Targets.Length);
        Assert.AreEqual(0, cron.Seconds.Targets[0]);
        Assert.AreEqual(1, cron.Minutes.Targets.Length);
        Assert.AreEqual(0, cron.Minutes.Targets[0]);
        Assert.AreEqual(true, cron.Hours.AllSpec);
        Assert.AreEqual(true, cron.DayOfMonths.NoSpec);
        Assert.AreEqual(true, cron.Months.AllSpec);
        Assert.AreEqual(true, cron.DayOfWeeks.AllSpec);
        Debug.WriteLine(cron);
    }
}