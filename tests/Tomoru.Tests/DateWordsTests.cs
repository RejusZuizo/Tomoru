using System;
using Tomoru.Services;
using Xunit;

namespace Tomoru.Tests;

/// <summary>A countdown that reads "in -4 days" or "in 1 weeks" undoes the
/// point of having one.</summary>
public class DateWordsTests
{
    private static readonly DateOnly Today = new(2026, 8, 12);

    [Theory]
    [InlineData(0, "today")]
    [InlineData(1, "tomorrow")]
    [InlineData(2, "in 2 days")]
    [InlineData(13, "in 13 days")]
    public void Near_dates_are_counted_in_days(int offset, string expected)
        => Assert.Equal(expected, DateWords.Countdown(Today.AddDays(offset), Today));

    [Theory]
    [InlineData(14, "in 2 weeks")]
    [InlineData(21, "in 3 weeks")]
    [InlineData(59, "in 8 weeks")]
    public void Further_out_switches_to_weeks(int offset, string expected)
        => Assert.Equal(expected, DateWords.Countdown(Today.AddDays(offset), Today));

    [Fact]
    public void Weeks_round_rather_than_truncate()
    {
        // 17 days is nearer 2 weeks than 3, and never "2.43 weeks".
        Assert.Equal("in 2 weeks", DateWords.Countdown(Today.AddDays(17), Today));
        Assert.Equal("in 3 weeks", DateWords.Countdown(Today.AddDays(19), Today));
    }

    [Fact]
    public void A_long_way_off_is_months()
        => Assert.Equal("in 3 months", DateWords.Countdown(Today.AddDays(95), Today));

    [Theory]
    [InlineData(-1, "yesterday")]
    [InlineData(-4, "4 days overdue")]
    public void The_past_never_reads_as_a_countdown(int offset, string expected)
        => Assert.Equal(expected, DateWords.Countdown(Today.AddDays(offset), Today));

    [Fact]
    public void Soon_covers_the_next_week_and_anything_already_gone()
    {
        Assert.True(DateWords.IsSoon(Today.AddDays(7), Today));
        Assert.True(DateWords.IsSoon(Today.AddDays(-3), Today));
        Assert.False(DateWords.IsSoon(Today.AddDays(8), Today));
    }
}
