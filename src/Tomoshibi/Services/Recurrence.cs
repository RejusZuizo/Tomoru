using System;
using System.Linq;
using Tomoshibi.Models;

namespace Tomoshibi.Services;

/// <summary>
/// Turning a finished repeating ticket into its next occurrence.
///
/// Deliberately triggered by completion rather than by the calendar: a weekly
/// problem set you finished on Tuesday should reappear for next week, but one
/// you haven't started shouldn't silently duplicate itself every seven days
/// into a backlog you can't face. One open copy at a time.
/// </summary>
public static class Recurrence
{
    /// <summary>The next due date after <paramref name="from"/>, or null for a
    /// one-off. Skips forward until it's actually in the future: finish six
    /// weeks of backlog in one sitting and you get next week, not six copies
    /// of dates that have already gone.</summary>
    public static DateOnly? NextDue(DateOnly from, RepeatRule rule, DateOnly today)
    {
        if (rule == RepeatRule.None)
            return null;

        var next = from;
        for (var guard = 0; guard < 500; guard++)
        {
            next = rule switch
            {
                RepeatRule.Daily => next.AddDays(1),
                RepeatRule.Weekly => next.AddDays(7),
                RepeatRule.Fortnightly => next.AddDays(14),
                RepeatRule.Monthly => next.AddMonths(1),
                _ => next.AddDays(1)
            };

            if (next > today)
                return next;
        }

        return next;
    }

    /// <summary>The follow-up ticket for one that's just been completed, or
    /// null if it doesn't repeat. The copy carries the plan — title, course,
    /// description, priority, estimate and the subtask list — but none of the
    /// history: no sessions spent, no ticks, and a fresh number.</summary>
    public static TodoItem? Next(TodoItem finished, int number, DateOnly today)
    {
        if (finished.Repeat == RepeatRule.None)
            return null;

        // Undated repeats still make sense ("read a paper, weekly") — count
        // from today when there's no due date to count from.
        var basis = finished.Due ?? today;

        return new TodoItem
        {
            Number = number,
            Title = finished.Title,
            Description = finished.Description,
            Course = finished.Course,
            Priority = finished.Priority,
            EstimatePomos = finished.EstimatePomos,
            Repeat = finished.Repeat,
            Due = NextDue(basis, finished.Repeat, today),
            Status = TodoStatus.Backlog,
            IsDone = false,
            SessionsSpent = 0,
            CompletedAt = null,
            Subtasks = finished.Subtasks
                .Select(s => new Subtask { Title = s.Title, IsDone = false })
                .ToList()
        };
    }

    /// <summary>The chip shown on a repeating ticket.</summary>
    public static string Label(RepeatRule rule) => rule switch
    {
        RepeatRule.Daily => "daily",
        RepeatRule.Weekly => "weekly",
        RepeatRule.Fortnightly => "fortnightly",
        RepeatRule.Monthly => "monthly",
        _ => string.Empty
    };
}
