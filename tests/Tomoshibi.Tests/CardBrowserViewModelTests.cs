using System;
using System.IO;
using System.Linq;
using Tomoshibi.Models;
using Tomoshibi.Services;
using Tomoshibi.ViewModels;
using Xunit;

namespace Tomoshibi.Tests;

/// <summary>The card browser's bulk actions. Every one of them works on the
/// ticked rows, and every one of them used to be permanently enabled — so with
/// nothing ticked, suspend, unsuspend, bury and delete all did nothing at all,
/// silently. Four buttons that look broken.
///
/// <para>It holds a <c>DispatcherTimer</c> to debounce the search box, so it
/// needs the headless session to construct.</para></summary>
[Collection(HeadlessCollection.Name)]
public class CardBrowserViewModelTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "tomoshibi-tests", Guid.NewGuid().ToString("N"));

    public CardBrowserViewModelTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* temp dir */ }
    }

    private (CardBrowserViewModel Vm, AppState State, ConfirmDeleteViewModel Confirm) Vm(int cards = 3)
    {
        var state = new AppState();
        var deck = new Deck { Name = "kanji" };
        for (var i = 0; i < cards; i++)
        {
            deck.Notes.Add(new Note
            {
                Type = NoteType.Basic,
                Fields = { $"front {i}", $"back {i}" },
                Cards = { new Card { Ord = 0, State = CardState.New, Due = DateTime.Now } }
            });
        }
        state.Decks.Add(deck);

        var confirm = new ConfirmDeleteViewModel();
        var vm = new CardBrowserViewModel(state, () => { }, () => { }, new MediaStore(_dir), confirm);
        vm.Reload();
        return (vm, state, confirm);
    }

    [Fact]
    public void With_nothing_ticked_the_bulk_actions_are_off() => Headless.Run(() =>
    {
        var (vm, _, _) = Vm();

        Assert.False(vm.HasSelection);
        Assert.False(vm.SuspendSelectedCommand.CanExecute(null));
        Assert.False(vm.UnsuspendSelectedCommand.CanExecute(null));
        Assert.False(vm.BurySelectedCommand.CanExecute(null));
        Assert.False(vm.DeleteSelectedCommand.CanExecute(null));
        Assert.False(vm.MoveSelectedCommand.CanExecute(null));
        Assert.False(vm.AddTagCommand.CanExecute(null));
        Assert.False(vm.RemoveTagCommand.CanExecute(null));
    });

    [Fact]
    public void And_the_label_says_why_rather_than_leaving_it_grey() => Headless.Run(() =>
    {
        var (vm, _, _) = Vm();

        Assert.Contains("tick some cards", vm.SelectionLabel);
    });

    [Fact]
    public void Ticking_a_row_turns_them_on() => Headless.Run(() =>
    {
        var (vm, _, _) = Vm();

        vm.Rows[0].IsSelected = true;

        Assert.True(vm.HasSelection);
        Assert.Equal(1, vm.SelectedCount);
        Assert.Equal("1 selected", vm.SelectionLabel);
        Assert.True(vm.SuspendSelectedCommand.CanExecute(null));
    });

    [Fact]
    public void Select_all_and_none_keep_the_count_honest() => Headless.Run(() =>
    {
        var (vm, _, _) = Vm(cards: 3);

        vm.SelectAllCommand.Execute(null);
        Assert.Equal(3, vm.SelectedCount);
        Assert.Equal("3 selected", vm.SelectionLabel);

        vm.SelectNoneCommand.Execute(null);
        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.HasSelection);
    });

    [Fact]
    public void Suspending_a_ticked_row_actually_suspends_it() => Headless.Run(() =>
    {
        var (vm, state, _) = Vm();
        vm.Rows[0].IsSelected = true;

        vm.SuspendSelectedCommand.Execute(null);

        Assert.Contains(state.Decks[0].Notes.SelectMany(n => n.Cards), c => c.Suspended);
    });

    [Fact]
    public void Burying_sets_the_card_aside_until_tomorrow() => Headless.Run(() =>
    {
        var (vm, state, _) = Vm();
        vm.Rows[0].IsSelected = true;

        vm.BurySelectedCommand.Execute(null);

        var buried = state.Decks[0].Notes.SelectMany(n => n.Cards).First(c => c.BuriedUntil is not null);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now).AddDays(1), buried.BuriedUntil);
    });

    [Fact]
    public void Deleting_ticked_rows_asks_first_and_counts_them() => Headless.Run(() =>
    {
        var (vm, state, confirm) = Vm(cards: 3);
        vm.SelectAllCommand.Execute(null);

        vm.DeleteSelectedCommand.Execute(null);

        Assert.True(confirm.IsOpen);
        Assert.Equal("3 cards", confirm.Name);
        Assert.Equal(3, state.Decks[0].Notes.Count);   // nothing gone yet

        confirm.ConfirmCommand.Execute(null);
        Assert.Empty(state.Decks[0].Notes);
    });

    [Fact]
    public void Rebuilding_the_list_drops_the_old_selection() => Headless.Run(() =>
    {
        // The rows are new objects after a filter change, so a stale count
        // would leave the buttons enabled with nothing behind them.
        var (vm, _, _) = Vm();
        vm.SelectAllCommand.Execute(null);
        Assert.True(vm.HasSelection);

        vm.Reload();

        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.HasSelection);
    });
}
