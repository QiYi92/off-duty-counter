namespace OffDutyCounter;

internal sealed record Schedule(decimal Salary, DayOfWeek[] Rest, int[] Times, int FixedDays, int Payday)
{
    public static Schedule Default => new(8000, [DayOfWeek.Saturday, DayOfWeek.Sunday], [540,720,780,1080], 0, 0);

    public bool Valid => Salary > 0 && Rest is { Length: 1 or 2 } && Rest.Distinct().Count() == Rest.Length
        && Rest.All(day => Enum.IsDefined(day))
        && Times is { Length: 4 } && Times.All(minute => minute is >= 0 and < 1440)
        && Times[0] < Times[1] && Times[1] <= Times[2] && Times[2] < Times[3]
        && FixedDays is >= 0 and <= 31 && Payday is >= 0 and <= 31;

    public Snapshot At(DateTime now)
    {
        var day = now.Date;
        var start = new DateTime(now.Year, now.Month, 1);
        var workdates = Enumerable.Range(0, DateTime.DaysInMonth(now.Year, now.Month))
            .Select(i => start.AddDays(i)).Where(d => !Rest.Contains(d.DayOfWeek)).ToArray();
        var t = Times.Select(m => day.AddMinutes(m)).ToArray();
        bool rest = Rest.Contains(day.DayOfWeek);
        bool working = !rest && (now >= t[0] && now < t[1] || now >= t[2] && now < t[3]);
        double planned = (t[1] - t[0] + (t[3] - t[2])).TotalSeconds;
        double elapsed = rest ? 0 : Math.Clamp((now - t[0]).TotalSeconds, 0, (t[1] - t[0]).TotalSeconds)
            + Math.Clamp((now - t[2]).TotalSeconds, 0, (t[3] - t[2]).TotalSeconds);
        decimal daily = Salary / (FixedDays == 0 ? workdates.Length : FixedDays);
        decimal rate = daily / (decimal)planned;
        decimal amount = daily * ((decimal)Math.Floor(elapsed) / (decimal)planned);
        var (state, next, eventName) = rest ? ("休息日", (DateTime?)null, "")
            : now < t[0] ? ("未上班", t[0], "上班")
            : now < t[1] ? ("上班中", t[1], "午休")
            : now < t[2] ? ("午休中", t[2], "上班")
            : now < t[3] ? ("上班中", t[3], "下班")
            : ("已下班", null, "");
        int? untilPay = null;
        if (Payday != 0)
        {
            DateTime PayDate(DateTime month) => month.AddDays(Math.Min(Payday, DateTime.DaysInMonth(month.Year, month.Month)) - 1);
            var date = PayDate(start);
            if (date < day) date = PayDate(start.AddMonths(1));
            untilPay = (date - day).Days;
        }
        return new(state, next, eventName, amount, rest ? 0 : daily - amount, working ? rate : 0,
            rate, elapsed, planned, daily * workdates.Count(d => d < day) + amount, daily * workdates.Length, untilPay);
    }
}

internal sealed record Snapshot(string State, DateTime? Next, string Event, decimal Amount, decimal Remaining,
    decimal Rate, decimal BaseRate, double Elapsed, double Planned, decimal Month, decimal FullMonth, int? Payday);
