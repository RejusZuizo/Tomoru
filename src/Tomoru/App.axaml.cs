using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Tomoru.Services;
using Tomoru.ViewModels;
using Tomoru.Views;

namespace Tomoru;

public partial class App : Application
{
    private TrayIcon? _tray;
    private NativeMenuItem? _trayToggle;
    private IGlobalHotkeyService? _hotkey;
    private MainWindowViewModel? _vm;
    private DispatcherTimer? _instanceWatch;

    /// <summary>The single-instance lock this process holds, handed over by
    /// <see cref="Program"/> — it's taken before Avalonia starts, so that a
    /// second launch costs nothing and never flashes a window.</summary>
    public static SingleInstance? Instance { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            IStorageService storage = new JsonStorageService();
            _vm = new MainWindowViewModel(storage);

            // Apply the saved theme before the window shows so there's no flash.
            ThemeService.Apply(_vm.FollowSystemTheme
                ? ThemeService.SystemThemeId()
                : _vm.ActiveThemeId);

            // Re-dress when the desktop flips light/dark, so an app that's open
            // all evening follows the machine into the night rather than
            // staying whatever it was at launch.
            if (PlatformSettings is { } settings)
                settings.ColorValuesChanged += (_, _) =>
                {
                    if (_vm?.FollowSystemTheme == true)
                        ThemeService.Apply(ThemeService.SystemThemeId());
                };

            desktop.MainWindow = new MainWindow { DataContext = _vm };

            // Closing the window hides it; the timer keeps running and the
            // tray is the way back. Quitting happens via the tray menu (or
            // Cmd+Q, which goes through Shutdown and is allowed past the
            // hide-on-close guard in MainWindow).
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) =>
            {
                _instanceWatch?.Stop();
                _hotkey?.Dispose();
                _vm?.FlushSave();
                _vm?.Music.Shutdown();
                _vm?.ShutdownMedia();
            };

            SetupTray(desktop);
            SetupHotkey(desktop);
            WatchForSecondLaunch(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_vm is null)
            return;

        _trayToggle = new NativeMenuItem("start");
        _trayToggle.Click += (_, _) => _vm.Today.Pomodoro.ToggleRunCommand.Execute(null);

        var skip = new NativeMenuItem("skip");
        skip.Click += (_, _) => _vm.Today.Pomodoro.SkipCommand.Execute(null);

        var reset = new NativeMenuItem("reset");
        reset.Click += (_, _) => _vm.Today.Pomodoro.ResetCommand.Execute(null);

        var show = new NativeMenuItem("show tomoru");
        show.Click += (_, _) => ShowMainWindow(desktop);

        var quit = new NativeMenuItem("quit");
        quit.Click += (_, _) => desktop.Shutdown();

        var menu = new NativeMenu();
        menu.Items.Add(_trayToggle);
        menu.Items.Add(skip);
        menu.Items.Add(reset);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(show);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(quit);

        _tray = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Tomoru/Assets/tray.png"))),
            ToolTipText = "灯る · tomoru",
            Menu = menu
        };

        // Double-purpose: clicking the icon itself brings the window back.
        _tray.Clicked += (_, _) => ShowMainWindow(desktop);

        TrayIcon.SetIcons(this, new TrayIcons { _tray });

        // Keep the tooltip and the start/pause entry in step with the timer.
        _vm.Today.Pomodoro.PropertyChanged += OnPomodoroChanged;
        UpdateTray();
    }

    /// <summary>Someone launched tomoru again while it was already running —
    /// most likely from the launcher, after closing the window to the tray.
    /// That copy quit immediately and left a note; bring this window forward,
    /// which is what they were asking for.
    ///
    /// <para>A poll rather than a file watcher: it's one <c>File.Exists</c>
    /// every couple of seconds, and it behaves the same on all three platforms
    /// — which a watcher, over the various network and synced folders app-data
    /// can live on, does not.</para></summary>
    private void WatchForSecondLaunch(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (Instance is null)
            return;

        _instanceWatch = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _instanceWatch.Tick += (_, _) =>
        {
            if (Instance.ConsumeSignal())
                ShowMainWindow(desktop);
        };
        _instanceWatch.Start();
    }

    /// <summary>Pick the platform's global-hotkey flavour and hand it to the
    /// shell, which claims the chord if the setting says so. The Windows one
    /// rides the main window's handle, so this runs after the window exists.</summary>
    private void SetupHotkey(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_vm is null || desktop.MainWindow is not { } window)
            return;

        _hotkey = OperatingSystem.IsWindows() ? new WindowsHotkeyService(window)
            : OperatingSystem.IsMacOS() ? new MacHotkeyService()
            : new NullHotkeyService();

        _vm.AttachHotkey(_hotkey);
    }

    private void OnPomodoroChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PomodoroViewModel.TimeDisplay)
            or nameof(PomodoroViewModel.IsRunning))
        {
            UpdateTray();
        }
    }

    private void UpdateTray()
    {
        if (_tray is null || _trayToggle is null || _vm is null)
            return;

        var p = _vm.Today.Pomodoro;
        _tray.ToolTipText = p.IsRunning
            ? $"{p.TimeDisplay} · {p.PhaseShortLabel} — tomoru"
            : "灯る · tomoru";
        _trayToggle.Header = p.StartPauseLabel;
    }

    private static void ShowMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (desktop.MainWindow is not { } window)
            return;

        window.Show();
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
    }
}
