using System;
using Avalonia;
using Tomoshibi.Services;

namespace Tomoshibi;

internal static class Program
{
    // Avalonia configuration, don't remove; also used by the visual designer.
    [STAThread]
    public static void Main(string[] args)
    {
        // Last-ditch crash logging so an unhandled UI exception leaves a trace
        // on disk instead of just vanishing with the process.
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ErrorLog.Crash(e.ExceptionObject as Exception);

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
