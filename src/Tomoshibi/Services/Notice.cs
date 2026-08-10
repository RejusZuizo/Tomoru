using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Tomoshibi.Services;

/// <summary>
/// The app's one-line "that didn't work" banner. Tomoshibi has no dialogs
/// beyond its modals and nowhere to put an error, so failures used to have
/// only two options: vanish silently, or take the process down with them.
/// This is the third one.
///
/// A single static instance rather than something threaded through every
/// view: the app is one window with one user, the views own their own page
/// view models, and giving fifteen file handlers a path to the shell just to
/// say "the disk is full" isn't worth the wiring.
/// </summary>
public partial class Notice : ObservableObject
{
    public static Notice Current { get; } = new();

    private readonly DispatcherTimer _hide;

    private Notice()
    {
        // Long enough to read a sentence, short enough not to sit there. The
        // ✕ dismisses it early; a second notice restarts the clock.
        _hide = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7) };
        _hide.Tick += (_, _) => Dismiss();
    }

    [ObservableProperty] private string _text = string.Empty;

    [ObservableProperty] private bool _isVisible;

    /// <summary>Show a message. Safe to call from any thread — file work runs
    /// off the UI thread and its failures arrive from there.</summary>
    public void Show(string text)
    {
        if (Dispatcher.UIThread.CheckAccess())
            Post(text);
        else
            Dispatcher.UIThread.Post(() => Post(text));
    }

    private void Post(string text)
    {
        Text = text;
        IsVisible = true;
        _hide.Stop();
        _hide.Start();
    }

    [RelayCommand]
    private void Dismiss()
    {
        _hide.Stop();
        IsVisible = false;
    }
}
