using System;
using System.Linq;
using Tomoshibi.Models;
using Tomoshibi.Services;
using Xunit;

namespace Tomoshibi.Tests;

/// <summary>A repeating ticket that comes back on the wrong date, or comes back
/// twice, is worse than no repeat at all — you stop trusting the backlog.</summary>
public class RecurrenceTests
{
    private static readonly DateOnly Today = new(2026, 8, 12);

    private static TodoItem Ticket(RepeatRule rule, DateOnly? due, params (string, bool)[] subs) => new()
    {
        Number = 7,
        Title = "Problem set",
        Description = "the weekly one",
        Course = "MATH201",
        Priority = TodoPriority.High,
        EstimatePomos = 3,
        SessionsSpent = 5,
        Repeat = rule,
        Due = due,
        Status = TodoStatus.Done,
        IsDone = true,
        CompletedAt = DateTimeOffset.Now,
        Subtasks = subs.Select(x => new Subtask { Title = x.Item1, IsDone = x.Item2 }).ToList()
    };

    [Fact]
    public void A_one_off_does_not_come_back()
    {
        Assert.Null(Recurrence.Next(Ticket(RepeatRule.None, Today), 8, Today));
        Assert.Null(Recurrence.NextDue(Today, RepeatRule.None, Today));
    }

    [Theory]
    [InlineData(RepeatRule.Daily, 1)]
    [InlineData(RepeatRule.Weekly, 7)]
    [InlineData(RepeatRule.Fortnightly, 14)]
    public void Each_rule_steps_by_its_own_period(RepeatRule rule, int days)
    {
        var due = Today.AddDays(1);          // still ahead, so one step is enough
        Assert.Equal(due.AddDays(days), Recurrence.NextDue(due, rule, Today));
    }

    [Fact]
    public void Monthly_steps_a_calendar_month()
    {
        Assert.Equal(new DateOnly(2026, 9, 30),
                     Recurrence.NextDue(new DateOnly(2026, 8, 30), RepeatRule.Monthly, Today));
    }

    [Fact]
    public void A_late_ticket_skips_forward_rather_than_landing_in_the_past()
    {
        // Six weeks overdue, finished today: the next one is next week, not six
        // copies of dates that have already gone.
        var stale = Today.AddDays(-42);
        var next = Recurrence.NextDue(stale, RepeatRule.Weekly, Today);

        Assert.NotNull(next);
        Assert.True(next > Today);
        Assert.True(next <= Today.AddDays(7));
    }

    [Fact]
    public void The_copy_carries_the_plan()
    {
        var next = Recurrence.Next(Ticket(RepeatRule.Weekly, Today), 8, Today);

        Assert.NotNull(next);
        Assert.Equal("Problem set", next!.Title);
        Assert.Equal("the weekly one", next.Description);
        Assert.Equal("MATH201", next.Course);
        Assert.Equal(TodoPriority.High, next.Priority);
        Assert.Equal(3, next.EstimatePomos);
        Assert.Equal(RepeatRule.Weekly, next.Repeat);
        Assert.Equal(8, next.Number);
    }

    [Fact]
    public void But_none_of_the_history()
    {
        var next = Recurrence.Next(Ticket(RepeatRule.Weekly, Today), 8, Today);

        Assert.Equal(TodoStatus.Backlog, next!.Status);
        Assert.False(next.IsDone);
        Assert.Null(next.CompletedAt);
        Assert.Equal(0, next.SessionsSpent);
    }

    [Fact]
    public void Subtasks_come_back_unticked()
    {
        var next = Recurrence.Next(
            Ticket(RepeatRule.Weekly, Today, ("read the chapter", true), ("attempt q4", true)),
            8, Today);

        Assert.Equal(2, next!.Subtasks.Count);
        Assert.All(next.Subtasks, s => Assert.False(s.IsDone));
        Assert.Equal("read the chapter", next.Subtasks[0].Title);
    }

    [Fact]
    public void An_undated_repeat_counts_from_today()
    {
        var next = Recurrence.Next(Ticket(RepeatRule.Weekly, due: null), 8, Today);

        Assert.Equal(Today.AddDays(7), next!.Due);
    }

    [Fact]
    public void The_copy_is_a_separate_ticket()
    {
        var finished = Ticket(RepeatRule.Weekly, Today, ("step", true));
        var next = Recurrence.Next(finished, 8, Today);

        Assert.NotEqual(finished.Id, next!.Id);
        // Editing the copy's checklist must not reach back into the finished one.
        next.Subtasks[0].Title = "changed";
        Assert.Equal("step", finished.Subtasks[0].Title);
    }
}
