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

    public PomodoroMachine(Func<PomodoroSettings> settings)
    {
        _settings = settings;
        SetPhase(PomodoroPhase.Focus, resetRound: true);
    }

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

    /// <summary>One second of clock. Returns the block that just ended, or null
    /// if the phase is still running.</summary>
    public CompletedBlock? Tick()
    {
        if (RemainingSeconds > 0)
            RemainingSeconds--;

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
