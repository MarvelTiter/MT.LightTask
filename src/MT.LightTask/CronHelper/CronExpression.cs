namespace MT.LightTask;

public partial class CronExpression
{
    public static CronExpression Parse(string cron) => new(cron);
    /// <summary>
    /// 秒数的位置索引
    /// </summary>
    internal const int POS_SEC = 0;

    /// <summary>
    /// 分钟的位置索引
    /// </summary>
    internal const int POS_MIN = 1;

    /// <summary>
    /// 小时的位置索引
    /// </summary>
    internal const int POS_HOUR = 2;

    /// <summary>
    /// 日期的位置索引
    /// </summary>
    internal const int POS_DOM = 3;

    /// <summary>
    /// 月份的位置索引
    /// </summary>
    internal const int POS_MON = 4;

    /// <summary>
    /// 星期的位置索引
    /// </summary>
    internal const int POS_DOW = 5;

    /// <summary>
    /// 月份的名称映射
    /// </summary>
    private static readonly Dictionary<string, int> MonthMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "JAN", 1 },
        { "FEB", 2 },
        { "MAR", 3 },
        { "APR", 4 },
        { "MAY", 5 },
        { "JUN", 6 },
        { "JUL", 7 },
        { "AUG", 8 },
        { "SEP", 9 },
        { "OCT", 10 },
        { "NOV", 11 },
        { "DEC", 12 }
    };

    /// <summary>
    /// 日期的名称映射<see cref="DayOfWeek"/>
    /// </summary>
    private static readonly Dictionary<string, int> DayOfWeekMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "SUN", 0 },
        { "MON", 1 },
        { "TUE", 2 },
        { "WED", 3 },
        { "THU", 4 },
        { "FRI", 5 },
        { "SAT", 6 }
    };

    private CronExpression(string cronExpression)
    {
        var parts = cronExpression.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 5)
        {
            ParseField("0", POS_SEC, 0, 59, ref second);
            ParseField(parts[0], POS_MIN, 0, 59, ref minute);
            ParseField(parts[1], POS_HOUR, 0, 23, ref hour);
            ParseField(parts[2], POS_DOM, 1, 31, ref dayOfMonth);
            ParseField(parts[3], POS_MON, 1, 12, ref month);
            ParseField(parts[4], POS_DOW, 1, 7, ref dayOfWeek);
        }
        else if (parts.Length == 6)
        {
            ParseField(parts[0], POS_SEC, 0, 59, ref second);
            ParseField(parts[1], POS_MIN, 0, 59, ref minute);
            ParseField(parts[2], POS_HOUR, 0, 23, ref hour);
            ParseField(parts[3], POS_DOM, 1, 31, ref dayOfMonth);
            ParseField(parts[4], POS_MON, 1, 12, ref month);
            ParseField(parts[5], POS_DOW, 1, 7, ref dayOfWeek);
        }
        else
        {
            throw new ArgumentException("Invalid cron expression format. Expected 5 or 6 fields.");
        }
    }

    private struct CronPartial(int pos)
    {
        public int Position { get; } = pos;
        public SortedSet<int> Targets { get; set; } = [];

        /// <summary>
        /// *
        /// </summary>
        public bool ALL_SPEC { get; set; }

        /// <summary>
        /// ?
        /// </summary>
        public bool NO_SPEC { get; set; }
    }

    private CronPartial second;
    private CronPartial minute;
    private CronPartial hour;
    private CronPartial dayOfMonth;
    private CronPartial month;
    private CronPartial dayOfWeek;


    public DateTimeOffset GetNextOccurrence(DateTimeOffset? after = null)
    {
        // 从下一秒开始检查
        after ??= DateTimeOffset.Now;
        after = after.Value.AddSeconds(1);
        DateTimeOffset dt = after.Value.WithoutMillis();
        bool gotOne = false;
        while (!gotOne)
        {
            HandleSecond(ref dt);
            if (!HandleMinute(ref dt))
            {
                continue;
            }

            if (!HandleHour(ref dt))
            {
                continue;
            }

            if (!HandleDay(ref dt))
            {
                continue;
            }

            if (!HandleMonth(ref dt))
            {
                continue;
            }

            gotOne = true;
        }

        return dt;

        void HandleSecond(ref DateTimeOffset d)
        {
            if (second.ALL_SPEC) return;
            int sec = d.Second;
            var st = second.Targets.GetViewBetween(sec, 9999999);
            if (st.Count > 0)
            {
                sec = st.First();
            }
            else
            {
                sec = second.Targets.First();
                d = d.AddMinutes(1);
            }

            d = d.WithSecond(sec);
        }

        bool HandleMinute(ref DateTimeOffset d)
        {
            if (minute.ALL_SPEC) return true;
            int min = d.Minute;
            int h = d.Hour;
            int t = -1;
            var st = minute.Targets.GetViewBetween(min, 9999999);
            if (st.Count > 0)
            {
                t = min;
                min = st.First();
            }
            else
            {
                min = minute.Targets.First();
                h++;
            }

            if (min != t)
            {
                d = new DateTimeOffset(d.Year, d.Month, d.Day, d.Hour, min, 0, d.Millisecond, d.Offset);
                d = d.SetCalendarHour(h);
                return false;
            }

            d = new(d.Year, d.Month, d.Day, d.Hour, min, d.Second, d.Millisecond, d.Offset);
            return true;
        }

        bool HandleHour(ref DateTimeOffset d)
        {
            if (hour.ALL_SPEC) return true;
            int h = d.Hour;
            int day = d.Day;
            int t = -1;
            var st = hour.Targets.GetViewBetween(h, 9999999);
            if (st.Count > 0)
            {
                t = h;
                h = st.First();
            }
            else
            {
                h = hour.Targets.First();
                day++;
            }

            if (h != t)
            {
                var daysInMonth = DateTime.DaysInMonth(d.Year, d.Month);
                if (day > daysInMonth)
                {
                    d = new DateTimeOffset(d.Year, d.Month, daysInMonth, d.Hour, 0, 0, d.Millisecond, d.Offset).AddDays(day - daysInMonth);
                }
                else
                {
                    d = new(d.Year, d.Month, day, d.Hour, 0, 0, d.Millisecond, d.Offset);
                }

                d = d.SetCalendarHour(h);
                return false;
            }

            d = new(d.Year, d.Month, d.Day, h, d.Minute, d.Second, d.Millisecond, d.Offset);
            return true;
        }

        bool HandleDay(ref DateTimeOffset d)
        {
            int day = d.Day;
            int mon = d.Month;
            int t = -1;
            int tmon = mon;
            // 日期位置是 '?' 
            bool dayOfMSpec = dayOfMonth.NO_SPEC;
            // 星期位置是 '?'
            bool dayOfWSpec = dayOfWeek.NO_SPEC;
            SortedSet<int> st;
            // 指定了日期，没指定星期
            if (!dayOfMSpec && dayOfWSpec)
            {
                if (dayOfMonth.ALL_SPEC) return true;
                st = dayOfMonth.Targets.GetViewBetween(day, 9999999);
                bool found = st.Count != 0;
                if (found)
                {
                    t = day;
                    day = st.First();
                    int lastDay = DateTime.DaysInMonth(d.Year, mon);
                    if (day > lastDay)
                    {
                        day = dayOfMonth.Targets.First();
                        mon++;
                    }
                }
                else
                {
                    day = dayOfMonth.Targets.First();
                    mon++;
                }

                if (day != t || mon != tmon)
                {
                    if (mon > 12)
                    {
                        d = new DateTimeOffset(d.Year, 12, day, 0, 0, 0, d.Offset).AddMonths(mon - 12);
                    }
                    else
                    {
                        int lDay = DateTime.DaysInMonth(d.Year, mon);

                        if (day <= lDay)
                        {
                            d = new(d.Year, mon, day, 0, 0, 0, d.Offset);
                        }
                        else
                        {
                            d = new DateTimeOffset(d.Year, mon, lDay, 0, 0, 0, d.Offset).AddDays(day - lDay);
                        }
                    }

                    return false;
                }
            }
            // 指定了星期，没指定日期
            else if (dayOfMSpec && !dayOfWSpec)
            {
                if (dayOfWeek.ALL_SPEC) return true;
                int cDow = (int)d.DayOfWeek;
                int dow = dayOfWeek.Targets.First();
                st = dayOfWeek.Targets.GetViewBetween(cDow, 9999999);
                if (st.Count > 0)
                {
                    dow = st.First();
                }

                int daysToAdd = 0;
                if (cDow < dow)
                {
                    daysToAdd = dow - cDow;
                }

                if (cDow > dow)
                {
                    daysToAdd = dow + (7 - cDow);
                }

                int lDay = DateTime.DaysInMonth(d.Year, mon);
                if (day + daysToAdd > lDay)
                {
                    if (mon == 12)
                    {
                        d = new DateTimeOffset(d.Year, mon - 11, 1, 0, 0, 0, d.Offset).AddYears(1);
                    }
                    else
                    {
                        d = new(d.Year, mon + 1, 1, 0, 0, 0, d.Offset);
                    }

                    return false;
                }

                if (daysToAdd > 0)
                {
                    d = new(d.Year, mon, day + daysToAdd, 0, 0, 0, d.Offset);
                    return false;
                }
            }
            else
            {
                throw new FormatException("不支持同时指定星期日和月日参数。");
            }

            return true;
        }

        bool HandleMonth(ref DateTimeOffset d)
        {
            if (month.ALL_SPEC) return true;
            int mon = d.Month;
            int year = d.Year;
            int t = -1;
            var st = month.Targets.GetViewBetween(mon, 9999999);
            if (st.Count > 0)
            {
                t = mon;
                mon = st.First();
            }
            else
            {
                mon = month.Targets.First();
                year++;
            }

            if (mon != t)
            {
                d = new(year, mon, 1, 0, 0, 0, d.Offset);
                return false;
            }

            d = new(d.Year, mon, d.Day, d.Hour, d.Minute, d.Second, d.Offset);
            return true;
        }
    }

    private void ParseField(string field, int type, int min, int max, ref CronPartial store)
    {
        if (field.Contains('L') || field.Contains('W') || field.Contains('#'))
        {
            throw new FormatException($"暂不支持的语法: {field}");
        }

        store = new CronPartial(type);
        var parts = field.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            ParseFieldPart(ref store, p, type, min, max);
        }
    }

    private void ParseFieldPart(ref CronPartial store, ReadOnlySpan<char> v, int type, int min, int max)
    {
        Span<char> mutilValue = stackalloc char[3];
        int posOffset = 0;
        int start = 0;
        int end = -1;
        int step = 1;
        bool useStep = false;
        bool useRange = false;
        for (int i = 0; i < v.Length; i++)
        {
            char c = v[i];
            //处理一般字符,MON,WEEK,Number
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                mutilValue[i - posOffset] = c;
                continue;
            }
            // 范围模式
            else if (c is '-')
            {
                posOffset = i + 1;
                useRange = true;
                HandleStringMap(mutilValue, type, ref start);
                mutilValue.Clear();
            }
            else if (c is '?')
            {
                if (v.Length > 1)
                {
                    throw new FormatException($"'?' 后存在其他字符{v}");
                }

                if (type != POS_DOM && type != POS_DOW)
                {
                    throw new FormatException($"'?'只能在日期和星期处使用");
                }

                if (type == POS_DOW && dayOfMonth.NO_SPEC)
                {
                    throw new FormatException("已在日期处指定'?'");
                }

                store.NO_SPEC = true;
            }
            else if (c is '*')
            {
                store.ALL_SPEC = true;
            }
            // Step
            else if (c is '/')
            {
                posOffset = i + 1;
                useStep = true;
                if (store.ALL_SPEC)
                {
                    start = min;
                    end = max;
                    store.ALL_SPEC = false;
                }
                if (useRange)
                {
                    HandleStringMap(mutilValue, type, ref end);
                }
                else
                {
                    HandleStringMap(mutilValue, type, ref start);
                }
                mutilValue.Clear();
            }
        }

        if (useStep)
        {
            var vt = HandleStringMap(mutilValue, type, ref step);
            if (vt == 0)
            {
                throw new FormatException($"'/'之后未解析到数字");
            }
        }
        else if (useRange)
        {
            HandleStringMap(mutilValue, type, ref end);
        }
        else
        {
            HandleStringMap(mutilValue, type, ref start);
        }
        SetTargets(ref store, start, end, min, max, useStep, step);
    }

    private static void SetTargets(ref CronPartial store, int start, int end, int min, int max, bool useStep, int step)
    {
        if (store.NO_SPEC) return;
        if (store.ALL_SPEC && !useStep) return;
        if (end == -1 && !store.ALL_SPEC)
        {
            store.Targets.Add(start);
            return;
        }
        if ((start < min || start > max || end > max))
        {
            throw new FormatException($"位置[{store.Position + 1}]的值非法");
        }

        //step = useStep ? end : 1;
        end = end == -1 ? max : end;
        for (int i = start; i <= end; i++)
        {
            if (i % step == 0)
            {
                store.Targets.Add(i);
            }
        }
    }

    /// <summary>
    /// 返回值跟<see cref="Ex.ContentType"/>一样
    /// </summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    private static int HandleStringMap(Span<char> value, int type, ref int index)
    {
        int valueType = value.ContentType();
        var str = new string(value);
        if (valueType == 1)
        {
            if (type == POS_DOM)
            {
                if (!MonthMap.TryGetValue(str, out index))
                {
                    throw new FormatException($"无效的月份值: {str}");
                }
            }
            else if (type == POS_DOW)
            {
                if (!DayOfWeekMap.TryGetValue(str, out index))
                {
                    throw new FormatException($"无效的星期值: {str}");
                }
            }
            else
            {
                throw new FormatException($"不支持在除日期和星期的地方使用字母");
            }
        }
        else if (valueType == 2)
        {
            index = int.Parse(str);
        }
        else if (valueType == -1)
        {
            throw new FormatException($"数值异常: {str}");
        }

        return valueType;
    }

}

/// <summary>
/// 测试用的
/// </summary>
public partial class CronExpression
{
    public readonly struct CronField(bool allSpec, bool noSpec, int[] targets)
    {
        /// <summary>
        /// 是否是'*'
        /// </summary>
        public bool AllSpec { get; } = allSpec;
        /// <summary>
        /// 是否是'?'
        /// </summary>
        public bool NoSpec { get; } = noSpec;
        /// <summary>
        /// 具体的数值
        /// </summary>
        public int[] Targets { get; } = targets;

        public override string ToString()
        {
            return $"""
                结果: {(AllSpec ? "*" : "")}{(NoSpec ? "?" : "")}{string.Join(", ", Targets)}
                """;
        }
    }

    public CronField Seconds
    {
        get => new(second.ALL_SPEC, second.NO_SPEC, [.. second.Targets]);
    }

    public CronField Minutes
    {
        get => new(minute.ALL_SPEC, minute.NO_SPEC, [.. minute.Targets]);
    }

    public CronField Hours
    {
        get => new(hour.ALL_SPEC, hour.NO_SPEC, [.. hour.Targets]);
    }

    public CronField DayOfMonths
    {
        get => new(dayOfMonth.ALL_SPEC, dayOfMonth.NO_SPEC, [.. dayOfMonth.Targets]);
    }

    public CronField Months
    {
        get => new(month.ALL_SPEC, month.NO_SPEC, [.. month.Targets]);
    }

    public CronField DayOfWeeks
    {
        get => new(dayOfWeek.ALL_SPEC, dayOfWeek.NO_SPEC, [.. dayOfWeek.Targets]);
    }

    public override string ToString()
    {
        return $"""
            Seconds: {Seconds}
            Minutes: {Minutes}
            Hours: {Hours}
            DayOfMonths: {DayOfMonths}
            Months: {Months}
            DayOfWeeks: {DayOfWeeks}
            """;
    }
}

file static class Ex
{
    public static DateTimeOffset WithoutMillis(this DateTimeOffset offset) => new DateTimeOffset(offset.Year, offset.Month, offset.Day, offset.Hour, offset.Minute, offset.Second, offset.Offset);
    public static DateTimeOffset WithSecond(this DateTimeOffset offset, int sec) => new DateTimeOffset(offset.Year, offset.Month, offset.Day, offset.Hour, offset.Minute, sec, offset.Millisecond, offset.Offset);

    public static DateTimeOffset SetCalendarHour(this DateTimeOffset date, int hour)
    {
        int hourToSet = hour;
        if (hourToSet == 24)
        {
            hourToSet = 0;
        }

        DateTimeOffset d = new(date.Year, date.Month, date.Day, hourToSet, date.Minute, date.Second, date.Millisecond, date.Offset);
        if (hour == 24)
        {
            d = d.AddDays(1);
        }

        return d;
    }

    /// <summary>
    /// <para>数组长度只能是3，并且存的3个字符的星期或者月份，或者是数字</para>
    /// <para>如果存的是星期或者月份，必须是占满3个空间，并且是A-Z之间的值</para>
    /// </summary>
    /// <param name="array"></param>
    /// <returns>-1 - 不知道什么东西, 0 - 空的数值, 1 - 星期或者月份, 2 - 数字</returns>
    public static int ContentType(this Span<char> array)
    {
        if (array.Length != 3) return -1;
        if (array[0] == char.MinValue) return 0;
        if (array[2] >= 'A' && array[2] <= 'Z') return 1;
        return 2;
    }
}