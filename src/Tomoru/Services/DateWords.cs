using System;

namespace Tomoru.Services;

/// <summary>
/// Dates as a student reads them. "sept 6" is a fact you have to do arithmetic
/// on; "in 26 days" is the thing you actually wanted to know, and it's what
/// makes an exam list feel like pressure rather than a table.
/// </summary>
public static class DateWords
{
    /// <summary>How far off <paramref name="date"/> is — "today", "tomorrow",
    /// "in 5 days", "in 3 weeks". Past dates come back as "overdue", since
    /// nothing that reads a countdown wants "in -4 days".</summary>
    public static string Countdown(DateOnly date, DateOnly today)
    {
        var days = date.DayNumber - today.DayNumber;

        return days switch
        {
            < -1 => $"{-days} days overdue",
            -1 => "yesterday",
            0 => "today",
            1 => "tomorrow",
            < 14 => $"in {days} days",
            < 60 => $"in {WeeksWord(days)}",
            _ => $"in {days / 30} months"
        };
    }

    private static string WeeksWord(int days)
    {
        // Round to the nearest week rather than truncating: 13 days is "2
        // weeks" to anyone counting, not "1 week".
        var weeks = (int)Math.Round(days / 7.0);
        return weeks == 1 ? "1 week" : $"{weeks} weeks";
    }

    /// <summary>True when something deserves to look urgent — inside a week,
    /// or already gone.</summary>
    public static bool IsSoon(DateOnly date, DateOnly today, int withinDays = 7)
        => date.DayNumber - today.DayNumber <= withinDays;
}
