using System;
using Tomoru.Models;
using Tomoru.Services;
using Xunit;

namespace Tomoru.Tests;

/// <summary>The timer is the app's whole reason to exist, and until the rules
/// were lifted out of the view model nothing here could be driven from a test.
/// Now a whole study afternoon runs in a loop.</summary>
public class PomodoroMachineTests
{
    /// <summary>A clock the test moves by hand. Every read advances a second by
    /// default, so a tick loop reads exactly like the one-second timer the app
    /// runs — but a test can also jump it forward to stand in for a lid closed
    /// mid-block.</summary>
    private sealed class FakeClock
    {
        private DateTime _now = new(2025, 3, 1, 9, 0, 0);

        /// <summary>How much time a read lets pass — the app's timer interval.</summary>
        public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);

        /// <summary>Read the clock and let the interval pass, as the app's timer does.</summary>
        public DateTime Read()
        {
            var now = _now;
            _now += Interval;
            return now;
        }

        /// <summary>Jump forward without a tick — the machine slept, or the UI
        /// thread was busy, and nobody counted the seconds.</summary>
        public void Skip(TimeSpan gap) => _now += gap;

        public Func<DateTime> Func => Read;
    }

    private static PomodoroSettings Settings(int focus = 25, int shortBreak = 5,
                                             int longBreak = 15, int rounds = 4) => new()
    {
        FocusMinutes = focus,
        ShortBreakMinutes = shortBreak,
        LongBreakMinutes = longBreak,
        RoundsBeforeLongBreak = rounds
    };

    private static PomodoroMachine Machine(PomodoroSettings? s = null)
        => Machine(new FakeClock(), s);

    private static PomodoroMachine Machine(FakeClock clock, PomodoroSettings? s = null)
    {
        var settings = s ?? Settings();
        return new PomodoroMachine(() => settings, clock.Func);
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
        var m = Machine(new FakeClock(), settings);
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
        var m = Machine(new FakeClock(), settings);

        settings.ShortBreakMinutes = 9;
        RunOut(m);

        Assert.Equal(PomodoroPhase.ShortBreak, m.Phase);
        Assert.Equal(9 * 60, m.RemainingSeconds);
    }

    [Fact]
    public void A_completed_focus_is_credited_the_minutes_it_started_with()
    {
        var settings = Settings();
        var m = Machine(new FakeClock(), settings);

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
        var m = Machine(new FakeClock(), settings);

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

    // ---- The clock on the wall ----
    //
    // A tick used to be worth exactly one second no matter how long it had
    // really been since the last one. Close the lid ten minutes into a block
    // and the countdown picked up where it left off, so a 25-minute block ate
    // 35 minutes of the evening and the app said otherwise.

    [Fact]
    public void A_tick_charges_the_time_that_actually_passed()
    {
        var clock = new FakeClock();
        var m = Machine(clock);

        m.Tick();                        // the block is running
        clock.Skip(TimeSpan.FromMinutes(10));  // the lid closes
        m.Tick();                        // and opens again

        // Ten minutes and the two ticks either side of them.
        Assert.Equal(25 * 60 - (10 * 60 + 2), m.RemainingSeconds);
    }

    [Fact]
    public void A_gap_past_the_end_of_the_block_finishes_it()
    {
        var clock = new FakeClock();
        var m = Machine(clock);

        m.Tick();
        clock.Skip(TimeSpan.FromMinutes(40)); // slept clean through the block
        var done = m.Tick();

        Assert.NotNull(done);
        Assert.Equal(PomodoroPhase.Focus, done!.Value.Phase);
        Assert.Equal(25, done.Value.FocusMinutes);
    }

    [Fact]
    public void A_long_gap_only_ever_ends_one_block()
    {
        var clock = new FakeClock();
        var m = Machine(clock);

        m.Tick();
        clock.Skip(TimeSpan.FromHours(3)); // long enough for a whole afternoon
        m.Tick();

        // The break that follows starts whole. Sleeping through three hours
        // must not bank three focus blocks nobody sat through.
        Assert.Equal(PomodoroPhase.ShortBreak, m.Phase);
        Assert.Equal(5 * 60, m.RemainingSeconds);
        Assert.Equal(1, m.Round);
    }

    [Fact]
    public void Time_spent_paused_is_not_charged_to_the_block()
    {
        var clock = new FakeClock();
        var m = Machine(clock);

        m.Tick();
        var left = m.RemainingSeconds;

        clock.Skip(TimeSpan.FromMinutes(30)); // paused, and away from the desk
        m.Resume();
        m.Tick();

        Assert.Equal(left - 1, m.RemainingSeconds);
    }

    [Fact]
    public void Ticks_that_land_short_of_a_second_still_drain_the_block()
    {
        // A dispatcher timer is not a metronome: it can fire a shade early, and
        // charging only whole seconds would then round every gap to nothing and
        // stop the clock dead. The remainder has to carry.
        var clock = new FakeClock { Interval = TimeSpan.FromMilliseconds(400) };
        var m = Machine(clock);

        m.Resume();
        for (var i = 0; i < 10; i++)
            m.Tick();

        // Four seconds of clock passed since the reference, so four came off.
        Assert.Equal(25 * 60 - 4, m.RemainingSeconds);
    }

    // ---- coming back to a block that was already running ----
    //
    // Nothing about the running block was written down, so quitting or crashing
    // twenty minutes into a focus block lost it outright: the next launch
    // opened on a fresh 25:00 with no sign there had been anything else.

    /// <summary>A clock that only moves when the test says so.</summary>
    private static FakeClock StillClock() => new() { Interval = TimeSpan.Zero };

    /// <summary>A machine wound forward to <paramref name="elapsed"/> into its
    /// first focus block.</summary>
    private static PomodoroMachine Running(FakeClock clock, TimeSpan elapsed)
    {
        var m = Machine(clock);
        m.Resume();
        clock.Skip(elapsed);
        m.Tick();
        return m;
    }

    [Fact]
    public void A_block_left_running_comes_back_with_what_is_really_left()
    {
        var clock = StillClock();
        var saved = Running(clock, TimeSpan.FromMinutes(5)).Snapshot(running: true);

        clock.Skip(TimeSpan.FromMinutes(5)); // the app was shut for five minutes
        var m = Machine(clock);

        Assert.True(m.Restore(saved));
        Assert.Equal(15 * 60, m.RemainingSeconds);
    }

    [Fact]
    public void A_block_left_paused_comes_back_where_it_was_paused()
    {
        var clock = StillClock();
        var saved = Running(clock, TimeSpan.FromMinutes(5)).Snapshot(running: false);

        // Paused is paused: an hour away from the desk takes nothing off it.
        clock.Skip(TimeSpan.FromHours(1));
        var m = Machine(clock);

        Assert.True(m.Restore(saved));
        Assert.Equal(20 * 60, m.RemainingSeconds);
    }

    [Fact]
    public void A_block_that_ran_out_while_the_app_was_shut_is_not_resumed()
    {
        var clock = StillClock();
        var saved = Running(clock, TimeSpan.FromMinutes(5)).Snapshot(running: true);

        clock.Skip(TimeSpan.FromHours(4));
        var m = Machine(clock);

        // Nobody sat through it, so there's nothing to hand back — and nothing
        // to credit either. A fresh block is the honest answer.
        Assert.False(m.Restore(saved));
        Assert.Equal(25 * 60, m.RemainingSeconds);
    }

    [Fact]
    public void The_phase_and_the_round_come_back_too()
    {
        var clock = StillClock();
        var source = Machine(clock);
        source.Advance();          // focus 1 → short break
        source.Advance();          // short break → focus, round 2
        source.Resume();
        clock.Skip(TimeSpan.FromMinutes(3));
        source.Tick();

        var saved = source.Snapshot(running: false);
        var m = Machine(clock);

        Assert.True(m.Restore(saved));
        Assert.Equal(PomodoroPhase.Focus, m.Phase);
        Assert.Equal(2, m.Round);
        Assert.Equal(22 * 60, m.RemainingSeconds);
    }

    [Fact]
    public void A_restored_block_reads_as_paused_rather_than_untouched()
    {
        var clock = StillClock();
        var saved = Running(clock, TimeSpan.FromMinutes(5)).Snapshot(running: true);
        var m = Machine(clock);

        m.Restore(saved);

        // What the view dims on: stopped, but part-way through.
        Assert.True(m.IsMidPhase);
    }

    [Fact]
    public void An_untouched_timer_is_nothing_to_come_back_to()
    {
        var clock = StillClock();
        var saved = Machine(clock).Snapshot(running: false);

        Assert.False(Machine(clock).Restore(saved));
    }

    [Fact]
    public void Nothing_saved_means_nothing_restored()
    {
        Assert.False(Machine().Restore(null));
    }

    [Fact]
    public void A_settings_change_while_the_app_was_shut_leaves_the_block_alone()
    {
        var clock = StillClock();
        var settings = Settings();
        var source = Machine(clock, settings);
        source.Resume();
        clock.Skip(TimeSpan.FromMinutes(5));
        source.Tick();
        var saved = source.Snapshot(running: false);

        settings.FocusMinutes = 50;
        var m = Machine(clock, settings);
        m.Restore(saved);

        // The block resumes at the scale it started with, or the progress bar
        // it comes back to would be measuring a different block.
        Assert.Equal(25 * 60, m.PhaseTotalSeconds);
        Assert.Equal(20 * 60, m.RemainingSeconds);
    }

    [Fact]
    public void A_clock_that_steps_backwards_never_adds_time()
    {
        var clock = new FakeClock();
        var m = Machine(clock);

        m.Tick();
        var left = m.RemainingSeconds;
        clock.Skip(TimeSpan.FromHours(-1)); // the OS corrected the clock
        m.Tick();

        Assert.Equal(left, m.RemainingSeconds);
    }
}
