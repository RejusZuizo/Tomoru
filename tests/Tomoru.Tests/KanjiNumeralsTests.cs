using Tomoru.Services;
using Xunit;

namespace Tomoru.Tests;

/// <summary>Numbers as Japanese numerals for the kanji clock face. The range is
/// only 0–59, but the irregularities are the whole point: ten is 十 rather than
/// 一十, twenty is 二十 rather than 二十零, and the teens put 十 first while the
/// tens put it second.</summary>
public class KanjiNumeralsTests
{
    [Theory]
    [InlineData(0, "零")]
    [InlineData(1, "一")]
    [InlineData(9, "九")]
    public void Single_digits_are_one_character(int n, string expected)
        => Assert.Equal(expected, KanjiNumerals.From(n));

    [Fact]
    public void Ten_drops_its_leading_one()
    {
        // 一十 is the mistake everyone makes writing this by rule.
        Assert.Equal("十", KanjiNumerals.From(10));
    }

    [Theory]
    [InlineData(11, "十一")]
    [InlineData(18, "十八")]
    [InlineData(19, "十九")]
    public void The_teens_lead_with_ten(int n, string expected)
        => Assert.Equal(expected, KanjiNumerals.From(n));

    [Theory]
    [InlineData(20, "二十")]
    [InlineData(40, "四十")]
    [InlineData(50, "五十")]
    public void Round_tens_have_no_trailing_unit(int n, string expected)
        => Assert.Equal(expected, KanjiNumerals.From(n));

    [Theory]
    [InlineData(21, "二十一")]
    [InlineData(25, "二十五")]
    [InlineData(59, "五十九")]
    public void The_rest_are_tens_then_units(int n, string expected)
        => Assert.Equal(expected, KanjiNumerals.From(n));

    [Fact]
    public void Out_of_range_falls_back_to_digits_rather_than_throwing()
    {
        // A clock face is not worth crashing over.
        Assert.Equal("60", KanjiNumerals.From(60));
        Assert.Equal("-1", KanjiNumerals.From(-1));
    }

    // ---- the countdown itself ----

    [Fact]
    public void A_countdown_reads_minutes_then_seconds()
    {
        Assert.Equal("十八分四十秒", KanjiNumerals.Countdown(18, 40));
    }

    [Fact]
    public void Under_a_minute_drops_the_minutes_entirely()
    {
        // 零分四十秒 is technically right and reads like a machine.
        Assert.Equal("四十秒", KanjiNumerals.Countdown(0, 40));
    }

    [Fact]
    public void The_very_end_is_just_zero_seconds()
    {
        Assert.Equal("零秒", KanjiNumerals.Countdown(0, 0));
    }

    [Fact]
    public void Exactly_on_a_minute_drops_the_seconds()
    {
        // 二十五分零秒 is what a machine says. This sits on screen at the start
        // of every single block, so it's worth reading like a person.
        Assert.Equal("二十五分", KanjiNumerals.Countdown(25, 0));
    }

    [Fact]
    public void A_minute_and_a_bit_keeps_both_halves()
    {
        Assert.Equal("一分一秒", KanjiNumerals.Countdown(1, 1));
    }
}
