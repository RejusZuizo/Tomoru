using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tomoshibi.Models;
using Tomoshibi.Services;

namespace Tomoshibi.ViewModels;

/// <summary>
/// The card browser: a searchable, flat list of every card across every deck,
/// with checkbox multi-select and bulk actions (suspend, bury, delete, move
/// deck, tag). Search uses <see cref="SearchQueryParser"/>; filtering is
/// debounced so typing over a big collection stays smooth. Editing a row opens
/// the shared note editor.
/// </summary>
public partial class CardBrowserViewModel : ViewModelBase
{
    private const int MaxRows = 1000;

    private readonly AppState _state;
    private readonly ConfirmDeleteViewModel _confirm;
    private readonly Action _save;
    private readonly Action _refreshDecks;
    private readonly MediaStore _media;
    private readonly DispatcherTimer _filterTimer;

    public ObservableCollection<BrowserRowViewModel> Rows { get; } = new();

    /// <summary>How many rows are ticked. Every bulk action works on the
    /// selection, so this drives whether they can run at all — they used to be
    /// permanently enabled and silently do nothing when nothing was ticked,
    /// which reads as four broken buttons rather than a missing step.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(SelectionLabel))]
    [NotifyCanExecuteChangedFor(nameof(SuspendSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnsuspendSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(BurySelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveTagCommand))]
    private int _selectedCount;

    public bool HasSelection => SelectedCount > 0;

    /// <summary>Sits next to the bulk buttons so the disabled state has a
    /// reason attached rather than just being grey.</summary>
    public string SelectionLabel => SelectedCount == 0
        ? "tick some cards to act on them"
        : SelectedCount == 1 ? "1 selected" : $"{SelectedCount} selected";

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultLabel = string.Empty;
    [ObservableProperty] private string _bulkTagText = string.Empty;
    [ObservableProperty] private Deck? _moveTargetDeck;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    private NoteEditorViewModel? _editor;

    private BrowserRowViewModel? _editingRow;
    public bool IsEditing => Editor is not null;

    public IReadOnlyList<Deck> Decks => _state.Decks;

    public CardBrowserViewModel(AppState state, Action save, Action refreshDecks, MediaStore media,
                                ConfirmDeleteViewModel confirm)
    {
        _confirm = confirm;
        _state = state;
        _save = save;
        _refreshDecks = refreshDecks;
        _media = media;

        _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _filterTimer.Tick += (_, _) => { _filterTimer.Stop(); ApplyFilter(); };
    }

    /// <summary>Rebuild the list from scratch — called when the browser opens.</summary>
    public void Reload()
    {
        CloseEditor();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        _filterTimer.Stop();
        _filterTimer.Start();
    }

    private void ApplyFilter()
    {
        var predicate = SearchQueryParser.Parse(SearchText, DateTime.Now);

        foreach (var old in Rows)
            old.PropertyChanged -= OnRowChanged;
        Rows.Clear();

        var shown = 0;
        var total = 0;
        foreach (var deck in _state.Decks)
        foreach (var note in deck.Notes)
        foreach (var card in note.Cards)
        {
            if (!predicate(new CardMatch(deck, note, card))) continue;
            total++;
            if (shown < MaxRows)
            {
                var row = new BrowserRowViewModel(deck, note, card);
                row.PropertyChanged += OnRowChanged;
                Rows.Add(row);
                shown++;
            }
        }

        RecountSelection();

        ResultLabel = total > MaxRows
            ? $"showing {MaxRows} of {total} cards"
            : total == 1 ? "1 card" : $"{total} cards";
    }

    private IEnumerable<BrowserRowViewModel> Selected => Rows.Where(r => r.IsSelected);

    private void OnRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BrowserRowViewModel.IsSelected))
            RecountSelection();
    }

    private void RecountSelection() => SelectedCount = Rows.Count(r => r.IsSelected);

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var r in Rows) r.IsSelected = true;
        RecountSelection();
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var r in Rows) r.IsSelected = false;
        RecountSelection();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void SuspendSelected() => Bulk(r => r.Card.Suspended = true);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void UnsuspendSelected() => Bulk(r => r.Card.Suspended = false);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void BurySelected()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
        Bulk(r => r.Card.BuriedUntil = tomorrow);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        var victims = Selected.ToList();
        if (victims.Count == 0) return;

        // The one that can take the most in a single click: a select-all in
        // the browser is however many cards the filter matched.
        _confirm.Ask("削除 · delete cards",
                     victims.Count == 1 ? "1 card" : $"{victims.Count} cards",
                     "their review history goes with them, and this can't be undone.",
                     () => Delete(victims));
    }

    private void Delete(List<BrowserRowViewModel> victims)
    {
        foreach (var r in victims)
        {
            r.Note.Cards.Remove(r.Card);
            // A note with no cards left is empty — drop it too.
            if (r.Note.Cards.Count == 0)
                r.Deck.Notes.Remove(r.Note);
            Rows.Remove(r);
        }

        AfterBulk();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveSelected()
    {
        if (MoveTargetDeck is null) return;

        foreach (var r in Selected.ToList())
        {
            if (r.Deck == MoveTargetDeck) continue;
            r.Deck.Notes.Remove(r.Note);
            if (!MoveTargetDeck.Notes.Contains(r.Note))
                MoveTargetDeck.Notes.Add(r.Note);
        }

        AfterBulk();
        ApplyFilter(); // deck column changed for moved rows
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void AddTag()
    {
        var tag = BulkTagText.Trim();
        if (tag.Length == 0) return;
        Bulk(r => { if (!r.Note.Tags.Contains(tag)) r.Note.Tags.Add(tag); });
        BulkTagText = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveTag()
    {
        var tag = BulkTagText.Trim();
        if (tag.Length == 0) return;

        // Bulk, and silent about its reach: this strips the tag from every
        // selected note at once, however many the filter matched.
        var count = Selected.Count();
        if (count == 0) return;

        _confirm.Ask("削除 · remove tag", tag,
                     count == 1
                         ? "it comes off 1 note. this can't be undone."
                         : $"it comes off {count} notes. this can't be undone.",
                     () =>
                     {
                         Bulk(r => r.Note.Tags.RemoveAll(
                             t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)));
                         BulkTagText = string.Empty;
                     });
    }

    private void Bulk(Action<BrowserRowViewModel> action)
    {
        var any = false;
        foreach (var r in Selected.ToList())
        {
            action(r);
            r.Refresh();
            any = true;
        }
        if (any) AfterBulk();
    }

    private void AfterBulk()
    {
        _save();
        _refreshDecks();
    }

    // ---- edit a note ----

    [RelayCommand]
    private void EditRow(BrowserRowViewModel? row)
    {
        if (row is null) return;
        _editingRow = row;
        Editor = new NoteEditorViewModel(row.Note, OnNoteEdited, _media);
    }

    [RelayCommand]
    private void CloseEditor()
    {
        _editingRow?.Refresh();
        _editingRow = null;
        Editor = null;
    }

    private void OnNoteEdited()
    {
        _editingRow?.Refresh();
        _save();
        _refreshDecks();
    }
}
