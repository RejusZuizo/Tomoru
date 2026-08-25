using System;
using System.IO;
using System.Linq;

namespace Tomoru.Services;

/// <summary>
/// Writes what went wrong next to tomoru.json, where settings → open
/// folder leads. Two kinds, kept apart on purpose: "crash" is the process
/// dying, "error" is something the app caught and carried on from. Mixing
/// them would bury the one that matters in the noise of the ones that don't.
/// </summary>
public static class ErrorLog
{
    /// <summary>The process is going down.</summary>
    public static void Crash(Exception? ex) => Write("crash", ex);

    /// <summary>Handled — the app told the user and stayed up.</summary>
    public static void Handled(string action, Exception? ex) => Write("error", ex, action);

    private static void Write(string kind, Exception? ex, string? action = null)
    {
        if (ex is null)
            return;
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Tomoru");
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, $"{kind}-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log");
            var what = action is null ? string.Empty : $"while trying to {action}\n";
            File.WriteAllText(path,
                $"tomoru {ReleaseNotes.Version} · {Environment.OSVersion} · " +
                $".NET {Environment.Version}\n{DateTime.Now:O}\n{what}\n{ex}\n");

            // Keep the five newest of this kind; a loop shouldn't fill the folder.
            var old = Directory.GetFiles(dir, $"{kind}-*.log")
                .OrderByDescending(f => f, StringComparer.Ordinal)
                .Skip(5);
            foreach (var stale in old)
                File.Delete(stale);
        }
        catch
        {
            // Logging a failure must never throw on top of it.
        }
    }
}
