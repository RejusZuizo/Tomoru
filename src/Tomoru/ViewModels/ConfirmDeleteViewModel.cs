using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Tomoru.ViewModels;

/// <summary>
/// One "are you sure" for the whole app. A page stages a deletion here instead
/// of doing it, and the shell shows a single modal over whatever page raised
/// it.
///
/// <para>Shared rather than repeated per page because the subjects page had
/// the only one, and the copy that mattered — naming what goes with the thing,
/// saying plainly that it can't be undone — is exactly what gets dropped when
/// each page rolls its own. Deleting a deck took an imported collection and
/// months of scheduling on a single click; deleting a subject, which loses
/// less, asked first.</para>
/// </summary>
public partial class ConfirmDeleteViewModel : ViewModelBase
{
    private Action? _confirmed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isStaged;

    /// <summary>Heading, in the app's paired style — "削除 · delete deck".</summary>
    [ObservableProperty] private string _title = string.Empty;

    /// <summary>What is about to go: the deck's name, the subject's name.</summary>
    [ObservableProperty] private string _name = string.Empty;

    /// <summary>What goes with it, and that it can't be undone.</summary>
    [ObservableProperty] private string _detail = string.Empty;

    public bool IsOpen => IsStaged;

    /// <summary>Stage a deletion. Nothing happens until
    /// <see cref="ConfirmCommand"/> runs, so the caller does the deleting in
    /// <paramref name="onConfirmed"/> rather than before asking.</summary>
    public void Ask(string title, string name, string detail, Action onConfirmed)
    {
        Title = title;
        Name = name;
        Detail = detail;
        _confirmed = onConfirmed;
        IsStaged = true;
    }

    private bool HasStaged => IsStaged;

    [RelayCommand(CanExecute = nameof(HasStaged))]
    private void Confirm()
    {
        var go = _confirmed;

        // Clear before running: the action can open something else, and a
        // confirmation still holding a callback would fire it twice on a
        // double click.
        _confirmed = null;
        IsStaged = false;

        go?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(HasStaged))]
    private void Cancel()
    {
        _confirmed = null;
        IsStaged = false;
    }

    /// <summary>"3 assessments go with it, and this can't be undone." — the
    /// shared phrasing, so every prompt in the app counts things the same way
    /// and ends the same way.</summary>
    public static string Detailing(int count, string singular, string plural) => count switch
    {
        0 => "this can't be undone.",
        1 => $"its 1 {singular} goes with it, and this can't be undone.",
        _ => $"its {count} {plural} go with it, and this can't be undone.",
    };
}
