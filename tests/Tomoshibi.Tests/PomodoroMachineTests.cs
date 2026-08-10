using Tomoshibi.Models;
using Tomoshibi.Services;
using Xunit;

namespace Tomoshibi.Tests;

/// <summary>The timer is the app's whole reason to exist, and until the rules
/// were lifted out of the view model nothing here could be driven from a test.
/// Now a whole study afternoon runs in a loop.</summary>
public class PomodoroMachineTests
{
    private static PomodoroSettings Settings(int focus = 25, int shortBreak = 5,
                                             int longBreak = 15, int rounds = 4) => new()
    {
        FocusMinutes = focus,
        ShortBreakMinutes = shortBreak,
        LongBreakMinutes = longBreak,
        RoundsBeforeLongBreak = rounds
    };

    private static PomodoroMachine Machine(PomodoroSettings? s = null)
    {
        var settings = s ?? Settings();
        return new PomodoroMachine(() => settings);
    }

    /// <summary>Run the phase to its last second and return the block it left.</summary>
    private static CompletedBlock RunOut(PomodoroMachine m)
    {
        CompletedBlock? done = null;
        while (done is null)
            done = m.Tick();
        return done.Value;
    }

    [Fact]
    public void Starts_on_a_full_focus_block()
    {
        var m = Machine();

        Assert.Equal(PomodoroPhase.Focus, m.Phase);
        Assert.Equal(1, m.Round);
        Assert.Equal(25 * 60, m.RemainingSeconds);
        Assert.Equal(25 * 60, m.PhaseTotalSeconds);
        Assert.Equal(1.0, m.Progress);
        Assert.False(m.IsMidPhase);
    }

    [Fact]
    public void Tick_drains_a_second_at_a_time()
    {
        var m = Machine();

        Assert.Null(m.Tick());
        Assert.Equal(25 * 60 - 1, m.RemainingSeconds);
        Assert.True(m.IsMidPhase);
    }

    [Fact]
    public void Focus_runs_into_a_short_break_and_reports_the_block()
    {
        var m = Machine();

        var done = RunOut(m);

        Assert.Equal(PomodoroPhase.Focus, done.Phase);
        Assert.Equal(25, done.FocusMinutes);
        Assert.Equal(PomodoroPhase.ShortBreak, m.Phase);
        Assert.Equal(5 * 60, m.RemainingSeconds);
        Assert.Equal(1, m.Round);
    }

    [Fact]
    public void A_finished_short_break_opens_the_next_round()
    {
        var m = Machine();

        RunOut(m);            // focus 1
        var done = RunOut(m); // short break

        Assert.Equal(PomodoroPhase.ShortBreak, done.Phase);
        Assert.Equal(PomodoroPhase.Focus, m.Phase);
        Assert.Equal(2, m.Round);
    }

    [Fact]
    public void The_fourth_focus_earns_the_long_break()
    {
        var m = Machine();

        // Three focus blocks, each followed by a short break.
        for (var i = 0; i < 3; i++)
        {
            RunOut(m);
            Assert.Equal(PomodoroPhase.ShortBreak, m.Phase);
            RunOut(m);
        }

        Assert.Equal(4, m.Round);
        Assert.Equal(PomodoroPhase.Focus, m.Phase);

        RunOut(m); // the fourth focus

        Assert.Equal(PomodoroPhase.LongBreak, m.Phase);
        Assert.Equal(15 * 60, m.RemainingSeconds);
    }

    [Fact]
    public void The_long_break_starts_a_fresh_set()
    {
        var m = Machine();

        for (var i = 0; i < 3; i++) { RunOut(m); RunOut(m); }
        RunOut(m); // fourth focus → long break

        var done = RunOut(m); // the long break itself

        Assert.Equal(PomodoroPhase.LongBreak, done.Phase);
        Assert.Equal(PomodoroPhase.Focus, m.Phase);
        Assert.Equal(1, m.Round);
    }

    [Fact]
    public void Rounds_before_long_break_is_honoured()
    {
        var m = Machine(Settings(rounds: 2));

        RunOut(m); // focus 1
        Assert.Equal(PomodoroPhase.ShortBreak, m.Phase);
        RunOut(m); // short break
        RunOut(m); // focus 2 — the set is done

        Assert.Equal(PomodoroPhase.LongBreak, m.Phase);
    }

    [Fact]
    public void Two_full_sets_run_the_same_way_the_second_time()
    {
        var m = Machine();
        var focusBlocks = 0;
        var longBreaks = 0;

        // 8 focus blocks = two complete sets.
        for (var i = 0; i < 16; i++)
        {
            var done = RunOut(m);
            if (done.Phase == PomodoroPhase.Focus) focusBlocks++;
            if (done.Phase == PomodoroPhase.LongBreak) longBreaks++;
        }

        Assert.Equal(8, focusBlocks);
        Assert.Equal(2, longBreaks);
        Assert.Equal(PomodoroPhase.Focus, m.Phase);
        Assert.Equal(1, m.Round);
    }

    [Fact]
    public void Advance_skips_the_block_and_still_names_it()
    {
        var m = Machine();
        m.Tick();

        var skipped = m.Advance();

        // The caller decides a skip isn't credited; the machine just reports.
        Assert.Equal(PomodoroPhase.Focus, skipped.Phase);
        Assert.Equal(PomodoroPhase.ShortBreak, m.Phase);
        Assert.Equal(5 * 60, m.RemainingSeconds);
    }

    [Fact]
    public void Reset_refills_the_phase_without_losing_the_round()
    {
        var m = Machine();
        RunOut(m); // focus 1 → short break
        RunOut(m); // short break → focus, round 2
        for (var i = 0; i < 60; i++) m.Tick();

        m.Reset();

        Assert.Equal(25 * 60, m.RemainingSeconds);
        Assert.Equal(2, m.Round);
        Assert.False(m.IsMidPhase);
    }

    [Fact]
    public void A_settings_change_leaves_the_running_block_alone()
    {
        var settings = Settings();
        var m = new PomodoroMachine(() => settings);
        m.Tick();

        settings.FocusMinutes = 50;

        // The block already running keeps the length it started with.
        Assert.Equal(25 * 60, m.PhaseTotalSeconds);
        Assert.Equal(25, m.PhaseFocusMinutes);
    }

    [Fact]
    public void The_next_block_picks_the_new_settings_up()
    {
        var settings = Settings();
        var m = new PomodoroMachine(() => settings);

        settings.ShortBreakMinutes = 9;
        RunOut(m);

        Assert.Equal(PomodoroPhase.ShortBreak, m.Phase);
        Assert.Equal(9 * 60, m.RemainingSeconds);
    }

    [Fact]
    public void A_completed_focus_is_credited_the_minutes_it_started_with()
    {
        var settings = Settings();
        var m = new PomodoroMachine(() => settings);

        // Lengthening focus mid-block must not inflate what the block banks.
        m.Tick();
        settings.FocusMinutes = 90;
        var done = RunOut(m);

        Assert.Equal(25, done.FocusMinutes);
    }

    [Fact]
    public void Refresh_adopts_new_settings_for_the_idle_phase()
    {
        var settings = Settings();
        var m = new PomodoroMachine(() => settings);

        settings.FocusMinutes = 50;
        m.Refresh();

        Assert.Equal(50 * 60, m.RemainingSeconds);
        Assert.Equal(50 * 60, m.PhaseTotalSeconds);
        Assert.Equal(1, m.Round);
    }

    [Fact]
    public void Progress_drains_from_one_to_zero()
    {
        var m = Machine(Settings(focus: 1));

        Assert.Equal(1.0, m.Progress);
        for (var i = 0; i < 30; i++) m.Tick();
        Assert.Equal(0.5, m.Progress, 3);
    }

    [Fact]
    public void Mid_phase_is_false_at_both_ends_and_true_between()
    {
        var m = Machine(Settings(focus: 1));

        Assert.False(m.IsMidPhase);   // untouched
        m.Tick();
        Assert.True(m.IsMidPhase);    // started
        RunOut(m);
        Assert.False(m.IsMidPhase);   // rolled into a fresh phase
    }
}
