using System;
using System.Linq;
using Tomoshibi.Models;
using Tomoshibi.ViewModels;
using Xunit;

namespace Tomoshibi.Tests;

/// <summary>The backlog page end to end. Recurrence has its own tests as pure
/// logic; these check the wiring around it — that finishing a repeating ticket
/// actually puts the next one on the list, with a number, in the right
/// order.</summary>
public class TodoViewModelTests
{
    private static (TodoViewModel Vm, AppState State, Func<int> Saves) Vm(params TodoItem[] todos)
    {
        var state = new AppState { NextTodoNumber = 100 };
        state.Todos.AddRange(todos);
        var saves = 0;
        var vm = new TodoViewModel(state, () => saves++, _ => { });
        return (vm, state, () => saves);
    }

    private static TodoItem Ticket(string title, RepeatRule repeat = RepeatRule.None,
                                   DateOnly? due = null, TodoStatus status = TodoStatus.Backlog)
        => new()
        {
            Number = 1,
            Title = title,
            Repeat = repeat,
            Due = due,
            Status = status
        };

    // ---- the ticket form ----

    [Fact]
    public void A_ticket_with_no_title_is_refused_and_says_why()
    {
        var (vm, state, _) = Vm();
        vm.OpenAddCommand.Execute(null);
        vm.FormTitle = "  ";

        vm.ConfirmModalCommand.Execute(null);

        Assert.Empty(state.Todos);
        Assert.True(vm.IsFormTitleInvalid);
        Assert.Contains("title", vm.FormError);
        Assert.True(vm.IsModalOpen, "the form stays open so it can be fixed");
    }

    [Fact]
    public void The_complaint_clears_as_soon_as_theyre_fixing_it()
    {
        var (vm, _, _) = Vm();
        vm.OpenAddCommand.Execute(null);
        vm.ConfirmModalCommand.Execute(null);
        Assert.True(vm.IsFormTitleInvalid);

        vm.FormTitle = "Read chapter 4";

        Assert.False(vm.IsFormTitleInvalid);
        Assert.False(vm.HasFormError);
    }

    [Fact]
    public void A_completed_form_creates_the_ticket()
    {
        var (vm, state, _) = Vm();
        vm.OpenAddCommand.Execute(null);
        vm.FormTitle = "Past paper";
        vm.FormCourse = "CS201";
        vm.FormRepeat = RepeatRule.Weekly;

        vm.ConfirmModalCommand.Execute(null);

        var made = Assert.Single(state.Todos);
        Assert.Equal("Past paper", made.Title);
        Assert.Equal("CS201", made.Course);
        Assert.Equal(RepeatRule.Weekly, made.Repeat);
        Assert.False(vm.IsModalOpen);
    }

    // ---- recurrence, wired up ----

    private static TodoItemViewModel Row(TodoViewModel vm, string title) =>
        vm.Items.First(i => i.Model.Title == title);

    /// <summary>Backlog → Doing → Done.</summary>
    private static void Finish(TodoItemViewModel row)
    {
        while (row.Model.Status != TodoStatus.Done)
            row.CycleStatusCommand.Execute(null);
    }

    [Fact]
    public void Finishing_a_weekly_ticket_puts_the_next_one_on_the_backlog()
    {
        var due = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
        var (vm, state, _) = Vm(Ticket("Problem set", RepeatRule.Weekly, due));

        Finish(Row(vm, "Problem set"));

        Assert.Equal(2, state.Todos.Count);
        var next = state.Todos.Single(t => t.Status == TodoStatus.Backlog);
        Assert.Equal("Problem set", next.Title);
        Assert.Equal(due.AddDays(7), next.Due);
    }

    [Fact]
    public void The_follow_up_gets_its_own_ticket_number()
    {
        var (vm, state, _) = Vm(Ticket("Problem set", RepeatRule.Weekly,
                                        DateOnly.FromDateTime(DateTime.Now)));

        Finish(Row(vm, "Problem set"));

        var next = state.Todos.Single(t => t.Status == TodoStatus.Backlog);
        Assert.Equal(100, next.Number);
        Assert.Equal(101, state.NextTodoNumber);
    }

    [Fact]
    public void Finishing_a_one_off_leaves_the_backlog_alone()
    {
        var (vm, state, _) = Vm(Ticket("Book a study room"));

        Finish(Row(vm, "Book a study room"));

        Assert.Single(state.Todos);
    }

    [Fact]
    public void A_repeat_that_isnt_finished_doesnt_multiply()
    {
        var (vm, state, _) = Vm(Ticket("Problem set", RepeatRule.Weekly,
                                        DateOnly.FromDateTime(DateTime.Now)));

        // Moved to doing, not done — the whole point of spawning on completion.
        Row(vm, "Problem set").CycleStatusCommand.Execute(null);

        Assert.Single(state.Todos);
    }

    [Fact]
    public void The_new_ticket_shows_up_on_the_page_not_just_in_state()
    {
        var (vm, _, _) = Vm(Ticket("Problem set", RepeatRule.Weekly,
                                    DateOnly.FromDateTime(DateTime.Now)));

        Finish(Row(vm, "Problem set"));

        // The page defaults to "open", so the finished one leaves the list as
        // its replacement joins — what's left is the new, unstarted copy.
        var shown = Assert.Single(vm.Items.Where(i => i.Model.Title == "Problem set"));
        Assert.False(shown.IsDone);
        Assert.True(shown.Repeats);
        Assert.Equal("weekly", shown.RepeatLabel);
    }

    [Fact]
    public void Spawning_the_follow_up_persists()
    {
        var (vm, _, saves) = Vm(Ticket("Problem set", RepeatRule.Weekly,
                                        DateOnly.FromDateTime(DateTime.Now)));
        var before = saves();

        Finish(Row(vm, "Problem set"));

        Assert.True(saves() > before, "a new ticket that isn't saved is a lost ticket");
    }
}
