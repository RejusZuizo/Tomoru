using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tomoshibi.Models;
using Tomoshibi.Services;

namespace Tomoshibi.ViewModels;

/// <summary>Which slice of the backlog the list shows.</summary>
public enum TodoFilter
{
    All,
    Open,
    Done
}

/// <summary>
/// The todo backlog destination — a small ticket tracker for coursework:
/// numbered items with status (backlog/doing/done), priority, descriptions,
/// due dates, effort estimates and subtask checklists, searchable and
/// filterable. Items can be sent to the today task template with one click.
/// </summary>
public partial class TodoViewModel : ViewModelBase
{
    private readonly AppState _state;
    private readonly Action _save;
    private readonly ConfirmDeleteViewModel _confirm;
    private readonly Action<TodoItem> _sendToToday;

    private TodoItem? _editing;

    public ObservableCollection<TodoItemViewModel> Items { get; } = new();

    /// <summary>Courses seen across the app, for the form's autocomplete.</summary>
    public ObservableCollection<string> KnownCourses { get; } = new();

    /// <summary>Priority options for the form's picker.</summary>
    public IReadOnlyList<TodoPriority> Priorities { get; } = Enum.GetValues<TodoPriority>();

    [ObservableProperty] private bool _hasVisibleItems;
    [ObservableProperty] private int _openCount;
    [ObservableProperty] private int _doingCount;
    [ObservableProperty] private int _doneCount;

    // ---- Filtering ----
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAllFilter))]
    [NotifyPropertyChangedFor(nameof(IsOpenFilter))]
    [NotifyPropertyChangedFor(nameof(IsDoneFilter))]
    private TodoFilter _filter = TodoFilter.Open;

    public bool IsAllFilter => Filter == TodoFilter.All;
    public bool IsOpenFilter => Filter == TodoFilter.Open;
    public bool IsDoneFilter => Filter == TodoFilter.Done;

    // ---- Add/edit modal ----
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelModalCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmModalCommand))]
    private bool _isModalOpen;

    [ObservableProperty] private string _modalTitle = "新しいやること · new todo";
    [ObservableProperty] private string _modalAction = "add";

    [ObservableProperty] private string _formTitle = string.Empty;

    [ObservableProperty] private bool _isFormTitleInvalid;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFormError))]
    private string _formError = string.Empty;

    public bool HasFormError => FormError.Length > 0;

    partial void OnFormTitleChanged(string value)
    {
        if (IsFormTitleInvalid && !string.IsNullOrWhiteSpace(value))
            ClearFormError();
    }

    private void ClearFormError()
    {
        IsFormTitleInvalid = false;
        FormError = string.Empty;
    }
    [ObservableProperty] private string _formDescription = string.Empty;
    [ObservableProperty] private string _formCourse = string.Empty;
    [ObservableProperty] private DateTime? _formDue;
    [ObservableProperty] private TodoPriority _formPriority = TodoPriority.Normal;
    [ObservableProperty] private decimal? _formEstimate;
    [ObservableProperty] private RepeatRule _formRepeat = RepeatRule.None;

    /// <summary>Repeat options for the form's picker.</summary>
    public IReadOnlyList<RepeatRule> RepeatOptions { get; } = Enum.GetValues<RepeatRule>();

    public TodoViewModel(AppState state, Action save, Action<TodoItem> sendToToday,
                         ConfirmDeleteViewModel confirm)
    {
        _confirm = confirm;
        _state = state;
        _save = save;
        _sendToToday = sendToToday;

        MigrateLegacyItems();
        Rebuild();
        RebuildKnownCourses();
    }

    /// <summary>Re-read everything from state — called when the user
    /// navigates to this page, so session counts credited from the timer
    /// and courses added elsewhere show without a restart.</summary>
    public void Refresh()
    {
        Rebuild();
        RebuildKnownCourses();
    }

    /// <summary>State files from before the ticket upgrade: map the old
    /// IsDone flag onto Status and hand out numbers in creation order.</summary>
    private void MigrateLegacyItems()
    {
        var dirty = false;

        foreach (var todo in _state.Todos.OrderBy(t => t.CreatedAt))
        {
            if (todo.IsDone && todo.Status == TodoStatus.Backlog)
            {
                todo.Status = TodoStatus.Done;
                dirty = true;
            }

            if (todo.Number == 0)
            {
                todo.Number = _state.NextTodoNumber++;
                dirty = true;
            }
        }

        if (dirty)
            _save();
    }

    [RelayCommand]
    private void OpenAdd()
    {
        _editing = null;
        FormTitle = string.Empty;
        FormDescription = string.Empty;
        FormCourse = string.Empty;
        FormDue = null;
        FormPriority = TodoPriority.Normal;
        FormRepeat = RepeatRule.None;
        FormEstimate = null;

        ModalTitle = "新しいやること · new todo";
        ModalAction = "add";
        IsModalOpen = true;
    }

    /// <summary>Surface a single ticket — used when the command palette jumps
    /// to it. Drop any status filter, search the list down to this title, and
    /// expand the matching row so its detail is open when the page appears.</summary>
    public void Reveal(TodoItem todo)
    {
        Filter = TodoFilter.All;
        SearchText = todo.Title;

        // Filter/SearchText changes rebuild the list synchronously; expand the
        // row in place. (If neither value actually changed, the row is already
        // there to expand.)
        var row = Items.FirstOrDefault(r => r.Model.Id == todo.Id);
        if (row is not null)
            row.IsExpanded = true;
    }

    public void BeginEdit(TodoItemViewModel row)
    {
        _editing = row.Model;
        FormTitle = row.Model.Title;
        FormDescription = row.Model.Description;
        FormCourse = row.Model.Course ?? string.Empty;
        FormDue = row.Model.Due is { } d ? d.ToDateTime(TimeOnly.MinValue) : null;
        FormPriority = row.Model.Priority;
        FormRepeat = row.Model.Repeat;
        FormEstimate = row.Model.EstimatePomos > 0 ? row.Model.EstimatePomos : null;

        ModalTitle = $"編集 · edit {row.NumberLabel}";
        ModalAction = "save";
        IsModalOpen = true;
    }

    [RelayCommand(CanExecute = nameof(CanUseModal))]
    private void CancelModal() => IsModalOpen = false;

    [RelayCommand(CanExecute = nameof(CanUseModal))]
    private void ConfirmModal()
    {
        var title = FormTitle?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            IsFormTitleInvalid = true;
            FormError = "a ticket needs a title.";
            return;
        }

        ClearFormError();

        var target = _editing ?? new TodoItem { Number = _state.NextTodoNumber++ };

        target.Title = title;
        target.Description = FormDescription?.Trim() ?? string.Empty;
        target.Course = string.IsNullOrWhiteSpace(FormCourse) ? null : FormCourse.Trim();
        target.Due = FormDue is { } d ? DateOnly.FromDateTime(d) : null;
        target.Priority = FormPriority;
        target.Repeat = FormRepeat;
        target.EstimatePomos = FormEstimate is { } e ? (int)e : 0;

        if (_editing is null)
            _state.Todos.Add(target);

        _editing = null;
        IsModalOpen = false;

        Rebuild();
        RebuildKnownCourses();
        _save();
    }

    private bool CanUseModal() => IsModalOpen;

    [RelayCommand]
    private void Remove(TodoItemViewModel? item)
    {
        if (item is null)
            return;

        // A ticket carries its subtask checklist, and nothing here is undoable.
        _confirm.Ask("削除 · delete ticket", item.Model.Title,
                     ConfirmDeleteViewModel.Detailing(item.Model.Subtasks.Count, "subtask", "subtasks"),
                     () => Delete(item));
    }

    private void Delete(TodoItemViewModel item)
    {

        if (item.Model == _editing)
        {
            _editing = null;
            IsModalOpen = false;
        }

        _state.Todos.Remove(item.Model);
        Rebuild();
        RebuildKnownCourses();
        _save();
    }

    /// <summary>Copy the todo into today's task template. The backlog entry
    /// stays put — planning a thing isn't the same as finishing it.</summary>
    [RelayCommand]
    private void SendToToday(TodoItemViewModel? item)
    {
        if (item is null)
            return;

        _sendToToday(item.Model);
    }

    [RelayCommand] private void ShowAll() => Filter = TodoFilter.All;
    [RelayCommand] private void ShowOpen() => Filter = TodoFilter.Open;
    [RelayCommand] private void ShowDone() => Filter = TodoFilter.Done;

    partial void OnFilterChanged(TodoFilter value) => Rebuild();
    partial void OnSearchTextChanged(string value) => Rebuild();

    /// <summary>A repeating ticket has just been finished — put its next
    /// occurrence on the backlog. The page owns the list and the numbering, so
    /// the row hands the finished ticket back rather than doing this itself.</summary>
    private void SpawnRepeat(TodoItem finished)
    {
        var next = Recurrence.Next(finished, _state.NextTodoNumber, DateOnly.FromDateTime(DateTime.Now));
        if (next is null)
            return;

        _state.NextTodoNumber++;
        _state.Todos.Add(next);
        Rebuild();
        _save();
    }

    /// <summary>Re-filter, re-sort and re-wrap. Doing first, then backlog,
    /// then done; high priority first within a status, then due date (none
    /// last), then ticket number. Expansion survives rebuilds by id.</summary>
    private void Rebuild()
    {
        var expanded = Items.Where(r => r.IsExpanded).Select(r => r.Model.Id).ToHashSet();
        Items.Clear();

        var query = _state.Todos.AsEnumerable();

        query = Filter switch
        {
            TodoFilter.Open => query.Where(t => t.Status != TodoStatus.Done),
            TodoFilter.Done => query.Where(t => t.Status == TodoStatus.Done),
            _ => query
        };

        var search = SearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t =>
                t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (t.Course?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var sorted = query
            .OrderBy(t => t.Status switch
            {
                TodoStatus.Doing => 0,
                TodoStatus.Backlog => 1,
                _ => 2
            })
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.Due ?? DateOnly.MaxValue)
            .ThenBy(t => t.Number);

        foreach (var model in sorted)
        {
            var row = new TodoItemViewModel(model, _save, OnRowNeedsResort, SpawnRepeat)
            {
                IsExpanded = expanded.Contains(model.Id)
            };
            Items.Add(row);
        }

        HasVisibleItems = Items.Count > 0;
        OpenCount = _state.Todos.Count(t => t.Status == TodoStatus.Backlog);
        DoingCount = _state.Todos.Count(t => t.Status == TodoStatus.Doing);
        DoneCount = _state.Todos.Count(t => t.Status == TodoStatus.Done);
    }

    private void OnRowNeedsResort()
    {
        _save();
        Rebuild();
    }

    private void RebuildKnownCourses()
    {
        var courses = _state.Todos.Select(t => t.Course)
            .Concat(_state.ClassSlots.Select(s => s.Course))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        KnownCourses.Clear();
        foreach (var c in courses)
            KnownCourses.Add(c);
    }
}
