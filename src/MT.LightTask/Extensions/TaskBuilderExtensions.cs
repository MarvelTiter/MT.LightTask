using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask;

public static class TaskBuilderExtensions
{
    public static IScheduleStrategy EveryDay(this IStrategyBuilder _)
        => new CronScheduleStrategy("0 0 0 * * ?");
    public static IScheduleStrategy EveryDay(this IStrategyBuilder _, int hour) 
        => new CronScheduleStrategy($"0 0 {hour} * * ?");
    public static IScheduleStrategy EveryDay(this IStrategyBuilder _, int hour, int minute) 
        => new CronScheduleStrategy($"0 {minute} {hour} * * ?");

}
