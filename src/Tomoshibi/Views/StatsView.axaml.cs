using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Tomoshibi.ViewModels;

namespace Tomoshibi.Views;

public partial class StatsView : UserControl
{
    private StatsViewModel? _vm;

    public StatsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // Two paths reach a reveal: the user is already on this page (the event
        // fires while we're subscribed) or the palette navigates here fresh
        // (the view attaches after the event, so we scroll to whatever's
        // already flagged once we're on screen).
        AttachedToVisualTree += (_, _) => ScrollToHighlighted();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            _vm.JournalRevealRequested -= ScrollJournalIntoView;

        _vm = DataContext as StatsViewModel;

        if (_vm is not null)
            _vm.JournalRevealRequested += ScrollJournalIntoView;
    }

    private void ScrollToHighlighted()
    {
        var entry = _vm?.Journal.FirstOrDefault(j => j.Highlighted);
        if (entry is not null)
            ScrollJournalIntoView(entry);
    }

    /// <summary>Bring the palette-chosen journal row into view. Posted so the
    /// list has laid out before we ask the control for the row's container.</summary>
    private void ScrollJournalIntoView(JournalEntryViewModel entry)
    {
        // Avalonia 12 dropped the GetVisualRoot extension. TopLevel.GetTopLevel
        // asks the same question — is this attached to a window yet — and is
        // what the rest of the code-behind already uses.
        if (TopLevel.GetTopLevel(this) is null)
            return;

        Dispatcher.UIThread.Post(
            () => JournalList.ContainerFromItem(entry)?.BringIntoView(),
            DispatcherPriority.Background);
    }
}
