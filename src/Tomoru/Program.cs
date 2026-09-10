using System;
using Avalonia;
using Tomoru.Services;

namespace Tomoru;

internal static class Program
{
    // Avalonia configuration, don't remove; also used by the visual designer.
    [STAThread]
    public static void Main(string[] args)
    {
        // Last-ditch crash logging so an unhandled UI exception leaves a trace
        // on disk instead of just vanishing with the process.
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ErrorLog.Crash(e.ExceptionObject as Exception);

        // One copy only, decided before Avalonia starts so a second launch
        // costs nothing and never flashes a window. Two processes over one
        // state file means the last one to save quietly replaces the other's
        // work — and with close-to-tray on, launching again is easy to do
        // without realising the app is already there.
        var appData = JsonStorageService.DefaultDirectory;
        using var instance = SingleInstance.TryAcquire(appData);

        if (instance is null)
        {
            SingleInstance.SignalExisting(appData);
            return;
        }

        App.Instance = instance;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            ErrorLog.Crash(ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
