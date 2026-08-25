using System;
using Tomoru.Models;
using Tomoru.ViewModels;
using Xunit;

namespace Tomoru.Tests;

/// <summary>The one "are you sure" every destructive button now goes through.
/// The rules that matter are that nothing happens until it's confirmed, that
/// cancelling really does drop the pending action, and that confirming twice
/// can't run it twice.</summary>
public class ConfirmDeleteTests
{
    [Fact]
    public void Asking_stages_the_action_without_running_it()
    {
        var ran = false;
        var confirm = new ConfirmDeleteViewModel();

        confirm.Ask("削除 · delete deck", "kanji", "2 cards go with it.", () => ran = true);

        Assert.True(confirm.IsOpen);
        Assert.Equal("kanji", confirm.Name);
        Assert.False(ran);
    }

    [Fact]
    public void Confirming_runs_it_once_and_closes()
    {
        var runs = 0;
        var confirm = new ConfirmDeleteViewModel();
        confirm.Ask("t", "n", "d", () => runs++);

        confirm.ConfirmCommand.Execute(null);

        Assert.Equal(1, runs);
        Assert.False(confirm.IsOpen);
    }

    [Fact]
    public void A_second_confirm_cant_run_it_again()
    {
        // The modal closes on the first click, but a double click still lands
        // twice — deleting the row after it, if the callback survived.
        var runs = 0;
        var confirm = new ConfirmDeleteViewModel();
        confirm.Ask("t", "n", "d", () => runs++);

        confirm.ConfirmCommand.Execute(null);
        confirm.ConfirmCommand.Execute(null);

        Assert.Equal(1, runs);
    }

    [Fact]
    public void Cancelling_drops_it()
    {
        var ran = false;
        var confirm = new ConfirmDeleteViewModel();
        confirm.Ask("t", "n", "d", () => ran = true);

        confirm.CancelCommand.Execute(null);

        Assert.False(confirm.IsOpen);
        Assert.False(ran);

        // And it stays dropped, even if something reaches for confirm after.
        confirm.ConfirmCommand.Execute(null);
        Assert.False(ran);
    }

    [Fact]
    public void Confirming_nothing_does_nothing()
    {
        var confirm = new ConfirmDeleteViewModel();

        confirm.ConfirmCommand.Execute(null);

        Assert.False(confirm.IsOpen);
    }

    [Fact]
    public void A_second_ask_replaces_the_first()
    {
        // Staging a new deletion while one is up must not leave the old
        // callback armed behind it.
        var first = 0;
        var second = 0;
        var confirm = new ConfirmDeleteViewModel();

        confirm.Ask("t", "first", "d", () => first++);
        confirm.Ask("t", "second", "d", () => second++);
        confirm.ConfirmCommand.Execute(null);

        Assert.Equal(0, first);
        Assert.Equal(1, second);
        Assert.Equal("second", confirm.Name);
    }

    [Theory]
    [InlineData(0, "this can't be undone.")]
    [InlineData(1, "its 1 card goes with it, and this can't be undone.")]
    [InlineData(4, "its 4 cards go with it, and this can't be undone.")]
    public void The_wording_counts_and_agrees(int count, string expected)
    {
        Assert.Equal(expected, ConfirmDeleteViewModel.Detailing(count, "card", "cards"));
    }
}

/// <summary>The delete paths that had no confirmation at all before.</summary>
public class DeleteConfirmationCoverageTests
{
    private static (TimetableViewModel Vm, AppState State, ConfirmDeleteViewModel Confirm) Timetable()
    {
        var state = new AppState();
        state.ClassSlots.Add(new ClassSlot
        {
            Day = WeekDay.Mon,
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(10, 0),
            Title = "Algorithms"
        });
        state.Todos.Add(new TodoItem
        {
            Number = 1,
            Title = "Essay due",
            Due = DateOnly.FromDateTime(DateTime.Now)
        });
        var confirm = new ConfirmDeleteViewModel();
        return (new TimetableViewModel(state, () => { }, confirm), state, confirm);
    }

    [Fact]
    public void Removing_a_class_asks_first()
    {
        var (vm, state, confirm) = Timetable();

        vm.RemoveSlotCommand.Execute(vm.Slots[0]);

        Assert.True(confirm.IsOpen);
        Assert.Equal("Algorithms", confirm.Name);
        Assert.Single(state.ClassSlots);

        confirm.ConfirmCommand.Execute(null);
        Assert.Empty(state.ClassSlots);
    }

    [Fact]
    public void Removing_a_deadline_warns_that_it_leaves_the_backlog_too()
    {
        // The deadlines card is a window onto the todo backlog, so this isn't
        // "hide it from this list" — the ticket goes.
        var (vm, state, confirm) = Timetable();

        vm.RemoveDeadlineCommand.Execute(vm.Deadlines[0]);

        Assert.True(confirm.IsOpen);
        Assert.Contains("backlog", confirm.Detail);
        Assert.Single(state.Todos);

        confirm.ConfirmCommand.Execute(null);
        Assert.Empty(state.Todos);
    }

    [Fact]
    public void Deleting_a_ticket_asks_and_counts_its_subtasks()
    {
        var state = new AppState { NextTodoNumber = 5 };
        var ticket = new TodoItem { Number = 1, Title = "Past paper" };
        ticket.Subtasks.Add(new Subtask { Title = "read it" });
        ticket.Subtasks.Add(new Subtask { Title = "answer it" });
        state.Todos.Add(ticket);

        var confirm = new ConfirmDeleteViewModel();
        var vm = new TodoViewModel(state, () => { }, _ => { }, confirm);

        vm.RemoveCommand.Execute(vm.Items[0]);

        Assert.True(confirm.IsOpen);
        Assert.Equal("Past paper", confirm.Name);
        Assert.Contains("2 subtasks", confirm.Detail);
        Assert.Single(state.Todos);

        confirm.ConfirmCommand.Execute(null);
        Assert.Empty(state.Todos);
    }

    [Fact]
    public void Backing_out_of_a_ticket_keeps_it()
    {
        var state = new AppState();
        state.Todos.Add(new TodoItem { Number = 1, Title = "Past paper" });
        var confirm = new ConfirmDeleteViewModel();
        var vm = new TodoViewModel(state, () => { }, _ => { }, confirm);

        vm.RemoveCommand.Execute(vm.Items[0]);
        confirm.CancelCommand.Execute(null);

        Assert.Single(state.Todos);
        Assert.Single(vm.Items);
    }
}
