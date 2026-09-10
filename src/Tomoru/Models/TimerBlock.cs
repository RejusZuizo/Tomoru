using System;

namespace Tomoru.Models;

/// <summary>
/// The pomodoro block that was on the clock when the app last wrote its state,
/// so quitting or crashing mid-focus doesn't cost you the block.
///
/// <para>The finish line is stored as an absolute time rather than a countdown:
/// a frozen "twelve minutes left" would be a lie by the time it was read back,
/// and storing the end instead means nothing has to be written while the clock
/// runs — only when the block itself changes.</para>
/// </summary>
public class TimerBlock
{
    public PomodoroPhase Phase { get; set; }

    /// <summary>Focus round within the set, 1-based.</summary>
    public int Round { get; set; } = 1;

    /// <summary>The length the phase began with, so the progress bar comes back
    /// at the same scale even if the settings changed in between.</summary>
    public int PhaseTotalSeconds { get; set; }

    /// <summary>What a completed focus block is credited for.</summary>
    public int PhaseFocusMinutes { get; set; }

    /// <summary>What was left when the timer was paused. Read only when the
    /// timer wasn't running — a running block asks <see cref="EndsAt"/>.</summary>
    public int RemainingSeconds { get; set; }

    /// <summary>When the block would have run out. Read when the timer was
    /// running, so the time that passed while the app was shut counts.</summary>
    public DateTime EndsAt { get; set; }

    /// <summary>Whether the clock was running when this was written.</summary>
    public bool WasRunning { get; set; }
}
