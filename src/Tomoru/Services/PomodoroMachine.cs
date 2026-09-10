using System;
using Tomoru.Models;

namespace Tomoru.Services;

/// <summary>A block that just ended, and what it was worth.</summary>
public readonly record struct CompletedBlock(PomodoroPhase Phase, int FocusMinutes);

/// <summary>
/// The Pomodoro rules, with no clock attached: focus → short break, a long
/// break after every Nth focus round, then a fresh set.
///
/// Time enters through <see cref="Tick"/> alone, so a test can run a whole
/// afternoon of study in a loop. Everything the view model used to keep — the
/// phase, the round, the seconds left, the length the phase started with — is
/// here; the view model is left holding labels and a DispatcherTimer.
///
/// Settings arrive through a callback, read fresh at each phase start, so a
/// mid-block settings change (or a task with its own lengths) never warps the
/// block already running.
/// </summary>
public sealed class PomodoroMachine
{
    private readonly Func<PomodoroSettings> _settings;
    private readonly Func<DateTime> _clock;

    public PomodoroMachine(Func<PomodoroSettings> settings, Func<DateTime>? clock = null)
    {
        _settings = settings;
        _clock = clock ?? (() => DateTime.Now);
        SetPhase(PomodoroPhase.Focus, resetRound: true);
    }

    /// <summary>When the last tick was seen. Null before the first one, which
    /// is then worth the nominal second rather than the age of the process.</summary>
    private DateTime? _lastTick;

    /// <summary>Take the clock reference now — called when the timer starts or
    /// resumes, so the stretch it spent paused isn't charged to the block.</summary>
    public void Resume() => _lastTick = _clock();

    public PomodoroPhase Phase { get; private set; }

    /// <summary>Focus round within the current set, 1-based.</summary>
    public int Round { get; private set; } = 1;

    public int RemainingSeconds { get; private set; }

    /// <summary>The length this phase began with — held separately so changing
    /// the settings mid-block doesn't rescale the progress bar under it.</summary>
    public int PhaseTotalSeconds { get; private set; }

    /// <summary>Focus minutes as they stood when this phase started; what a
    /// completed focus block is credited for.</summary>
    public int PhaseFocusMinutes { get; private set; }

    /// <summary>Fraction of the phase still to run, 1 → 0.</summary>
    public double Progress => PhaseTotalSeconds > 0
        ? (double)RemainingSeconds / PhaseTotalSeconds
        : 0.0;

    /// <summary>Started but not finished — the difference between "paused" and
    /// "not started yet".</summary>
    public bool IsMidPhase => RemainingSeconds > 0 && RemainingSeconds < PhaseTotalSeconds;

    /// <summary>A turn of the clock. Charges the block the time that has really
    /// passed since the last tick, not a flat second: a tick is only ever a
    /// request to catch up, and the gap can be much larger than the interval —
    /// a suspended machine, or a UI thread that was busy elsewhere. Returns the
    /// block that just ended, or null if the phase is still running.</summary>
    public CompletedBlock? Tick()
    {
        var now = _clock();

        // No reference yet — ticked without a Resume, so the tick is worth the
        // nominal second rather than the age of the machine.
        var last = _lastTick ?? now.AddSeconds(-1);

        // The clock stepped backwards (an NTP correction, or the hour going
        // back). Take the new reading and charge nothing: holding the old
        // reference would freeze the block until real time caught up again.
        if (now < last)
        {
            _lastTick = now;
            return null;
        }

        // Whole seconds only, with the remainder carried into the next tick —
        // a timer that fires a shade early every time would otherwise round
        // every gap down to nothing and stop the clock dead.
        var elapsed = (int)Math.Floor((now - last).TotalSeconds);
        _lastTick = last.AddSeconds(elapsed);

        if (RemainingSeconds > 0)
            RemainingSeconds = Math.Max(0, RemainingSeconds - elapsed);

        // Only ever one block per tick. A lid closed over a whole afternoon
        // means one focus block ran out while nobody was there — not three of
        // them banked as time the user sat through.
        return RemainingSeconds <= 0 ? Advance() : null;
    }

    /// <summary>Move to the next phase. Returns the block left behind — the
    /// caller decides whether it counts, since a skip shouldn't be credited.</summary>
    public CompletedBlock Advance()
    {
        var finished = new CompletedBlock(Phase, PhaseFocusMinutes);

        switch (Phase)
        {
            case PomodoroPhase.Focus:
                // The long break is due once this round completes the set.
                var longDue = Round >= _settings().RoundsBeforeLongBreak;
                SetPhase(longDue ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak,
                         resetRound: false);
                break;

            case PomodoroPhase.ShortBreak:
                Round++;
                SetPhase(PomodoroPhase.Focus, resetRound: false);
                break;

            case PomodoroPhase.LongBreak:
                SetPhase(PomodoroPhase.Focus, resetRound: true);
                break;
        }

        return finished;
    }

    /// <summary>Write the block down so a later launch can pick it up.
    /// <paramref name="running"/> is the caller's, because whether the clock is
    /// ticking belongs to the timer, not to the rules.</summary>
    public TimerBlock Snapshot(bool running) => new()
    {
        Phase = Phase,
        Round = Round,
        PhaseTotalSeconds = PhaseTotalSeconds,
        PhaseFocusMinutes = PhaseFocusMinutes,
        RemainingSeconds = RemainingSeconds,
        EndsAt = _clock().AddSeconds(RemainingSeconds),
        WasRunning = running
    };

    /// <summary>Take up a block written down earlier. True if there was
    /// something worth resuming; false leaves the machine as it was — a fresh
    /// phase — and the caller can forget the block.
    ///
    /// <para>Always comes back stopped. Resuming a countdown on launch, before
    /// the user has so much as looked at the window, would be the app deciding
    /// they were back at work.</para></summary>
    public bool Restore(TimerBlock? block)
    {
        if (block is null || block.PhaseTotalSeconds <= 0)
            return false;

        // A block that was running kept its finish line, so the time the app
        // spent shut counts against it; a paused one kept its countdown.
        var remaining = block.WasRunning
            ? (int)Math.Floor((block.EndsAt - _clock()).TotalSeconds)
            : block.RemainingSeconds;

        // Ran out while nobody was there, or was never started. Neither is
        // something to hand back — and the first must not be credited, since
        // nobody sat through it.
        if (remaining <= 0 || remaining >= block.PhaseTotalSeconds)
            return false;

        Phase = block.Phase;
        Round = Math.Max(1, block.Round);
        PhaseTotalSeconds = block.PhaseTotalSeconds;
        PhaseFocusMinutes = block.PhaseFocusMinutes;
        RemainingSeconds = remaining;
        _lastTick = null;

        return true;
    }

    /// <summary>Put the current phase back to full, keeping the round.</summary>
    public void Reset() => RemainingSeconds = PhaseTotalSeconds;

    /// <summary>Re-read the settings for the phase already showing. Only safe
    /// while idle — mid-block it would move the finish line.</summary>
    public void Refresh() => SetPhase(Phase, resetRound: false);

    private void SetPhase(PomodoroPhase phase, bool resetRound)
    {
        if (resetRound)
            Round = 1;

        var s = _settings();

        Phase = phase;
        PhaseTotalSeconds = phase switch
        {
            PomodoroPhase.Focus => s.FocusMinutes * 60,
            PomodoroPhase.ShortBreak => s.ShortBreakMinutes * 60,
            PomodoroPhase.LongBreak => s.LongBreakMinutes * 60,
            _ => s.FocusMinutes * 60
        };
        PhaseFocusMinutes = s.FocusMinutes;
        RemainingSeconds = PhaseTotalSeconds;
    }
}
