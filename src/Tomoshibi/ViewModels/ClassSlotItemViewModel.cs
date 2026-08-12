using System;
using Tomoshibi.Models;

namespace Tomoshibi.ViewModels;

/// <summary>
/// Read-only row representation of a <see cref="ClassSlot"/>. Holds the model
/// it wraps and exposes both the human-readable labels the row template binds
/// to and the hour-row / day-column indices the week grid uses to place the
/// slot block. Slot start/end snap to the nearest hour for grid placement;
/// the real times still show inside the block.
/// </summary>
public class ClassSlotItemViewModel : ViewModelBase
{
    /// <summary>Fallback window when there's nothing scheduled to measure.</summary>
    public const int DefaultStartHour = 8;
    public const int DefaultEndHour = 22;

    /// <summary>Height of one hour row. Blocks are placed by offset from the
    /// top of the grid rather than by Grid.Row: the window can move when the
    /// timetable changes, and re-defining rows under a live panel doesn't
    /// re-apply the row assignments of the children already in it.</summary>
    public const double RowHeight = 38;

    private readonly int _gridStart;
    private readonly int _gridEnd;

    public ClassSlot Model { get; }

    public string DayLabel => Model.Day switch
    {
        WeekDay.Mon => "mon",
        WeekDay.Tue => "tue",
        WeekDay.Wed => "wed",
        WeekDay.Thu => "thu",
        WeekDay.Fri => "fri",
        WeekDay.Sat => "sat",
        WeekDay.Sun => "sun",
        _ => "?"
    };

    public string TimeLabel => $"{Model.Start:HH\\:mm}–{Model.End:HH\\:mm}";
    public string Title => Model.Title;
    public string? Course => Model.Course;
    public bool HasCourse => !string.IsNullOrWhiteSpace(Model.Course);

    /// <summary>0..6, Mon..Sun. Used as the grid column for slot placement.</summary>
    public int DayIndex => (int)Model.Day;

    /// <summary>Hours from the top of the grid to this slot's start — the real
    /// time, not rounded to the hour, so an 11:30 class draws halfway down the
    /// 11 row. The old row-based placement couldn't express that.</summary>
    private double StartOffsetHours =>
        Math.Clamp(Model.Start.ToTimeSpan().TotalHours - _gridStart,
                   0, Math.Max(0, _gridEnd - _gridStart));

    private double EndOffsetHours =>
        Math.Clamp(Model.End.ToTimeSpan().TotalHours - _gridStart,
                   StartOffsetHours, Math.Max(0, _gridEnd - _gridStart));

    /// <summary>Distance from the top of the day column to this block.</summary>
    public Avalonia.Thickness BlockMargin => new(0, StartOffsetHours * RowHeight, 0, 0);

    /// <summary>How tall the block renders — its real duration, with a floor so
    /// a fifteen-minute slot is still readable.</summary>
    public double BlockHeight =>
        Math.Max(RowHeight * 0.6, (EndOffsetHours - StartOffsetHours) * RowHeight);

    public ClassSlotItemViewModel(ClassSlot model,
                                  int gridStart = DefaultStartHour,
                                  int gridEnd = DefaultEndHour)
    {
        Model = model;
        _gridStart = gridStart;
        _gridEnd = gridEnd;
    }
}
