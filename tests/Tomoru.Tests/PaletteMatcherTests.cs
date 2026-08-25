using Tomoru.Services;
using Xunit;

namespace Tomoru.Tests;

/// <summary>The palette's ranking rules are the contract: prefix over
/// word-start over substring over subsequence over typo — and short queries
/// must not get typo-matched into noise.</summary>
public class PaletteMatcherTests
{
    [Fact]
    public void An_empty_query_matches_everything_equally()
    {
        Assert.Equal(0, PaletteMatcher.Score("", "dashboard"));
        Assert.Equal(0, PaletteMatcher.Score(null, "stats"));
        Assert.Equal(0, PaletteMatcher.Score("   ", "review"));
    }

    [Fact]
    public void The_ranking_ladder_holds()
    {
        var prefix = PaletteMatcher.Score("sta", "stats");
        var wordStart = PaletteMatcher.Score("sta", "focus stats");
        var substring = PaletteMatcher.Score("tat", "stats");
        var subsequence = PaletteMatcher.Score("stts", "stats");

        Assert.True(prefix > wordStart);
        Assert.True(wordStart > substring);
        Assert.True(substring > subsequence);
    }

    [Theory]
    [InlineData("reveiw", "review")]        // transposition
    [InlineData("algoritms", "algorithms")] // missing letter, long word
    [InlineData("timetabel", "timetable")]  // swapped tail
    public void Typos_still_find_the_row(string typo, string title)
    {
        Assert.NotNull(PaletteMatcher.Score(typo, title));
    }

    [Fact]
    public void Typo_tolerance_ranks_below_a_real_match()
    {
        var real = PaletteMatcher.Score("review", "review");
        var typo = PaletteMatcher.Score("reveiw", "review");

        Assert.True(real > typo);
    }

    [Fact]
    public void Short_queries_do_not_get_typo_matched()
    {
        // "st" is one edit from "at" — but two-letter queries matching half
        // the list would make the palette feel broken, not forgiving.
        Assert.Null(PaletteMatcher.Score("st", "at a glance"));
    }

    [Fact]
    public void Unrelated_text_does_not_match()
    {
        Assert.Null(PaletteMatcher.Score("zzzz", "dashboard"));
        Assert.Null(PaletteMatcher.Score("shop", "timetable"));
    }

    [Fact]
    public void Words_split_on_the_palettes_own_punctuation()
    {
        // "#12 · essay draft" — the ticket number and separators shouldn't
        // hide the word starts.
        Assert.NotNull(PaletteMatcher.Score("essay", "#12 · essay draft"));
        Assert.Equal(80, PaletteMatcher.Score("essay", "#12 · essay draft"));
    }

    [Fact]
    public void Case_never_matters()
    {
        Assert.Equal(
            PaletteMatcher.Score("MATH", "math201 problem set"),
            PaletteMatcher.Score("math", "MATH201 PROBLEM SET"));
    }

    // ---- swapped letters ----

    [Theory]
    [InlineData("rde", "red")]
    [InlineData("tset", "test")]
    [InlineData("kanij", "kanji")]
    public void A_swapped_pair_still_finds_the_word(string typo, string word)
    {
        // Transposition is the typo fingers actually make, and it used to be
        // forgiven only from four characters up — so "rde" found nothing at
        // all, which is the shortest possible way for search to look broken.
        Assert.NotNull(PaletteMatcher.Score(typo, word));
    }

    [Fact]
    public void A_swap_still_ranks_below_a_real_match()
    {
        // It's a rescue, not a preference: anything that genuinely contains the
        // query should still sort above it.
        var swapped = PaletteMatcher.Score("rde", "red");
        var real = PaletteMatcher.Score("red", "red");

        Assert.NotNull(swapped);
        Assert.True(real > swapped);
    }

    [Fact]
    public void A_substitution_on_a_short_query_is_still_refused()
    {
        // The reason short queries were excluded in the first place, and it
        // still holds — "st" and "at" differ by a substitution, not a swap.
        Assert.Null(PaletteMatcher.Score("st", "at"));
    }

    [Theory]
    [InlineData("abc", "acb")]   // swap at the end
    [InlineData("abc", "bac")]   // swap at the start
    public void The_swap_can_sit_anywhere_in_the_word(string q, string candidate)
    {
        Assert.NotNull(PaletteMatcher.Score(q, candidate));
    }

    [Fact]
    public void Two_separate_swaps_are_a_different_word()
    {
        // "badc" is "abcd" with two swaps. One typo is a slip; two is a guess.
        Assert.Null(PaletteMatcher.Score("badc", "abcd"));
    }

    [Fact]
    public void A_swap_needs_the_same_letters_not_just_the_same_shape()
    {
        Assert.Null(PaletteMatcher.Score("rdx", "red"));
    }
}
