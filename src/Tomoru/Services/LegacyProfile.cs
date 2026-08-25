using System;
using System.IO;

namespace Tomoru.Services;

/// <summary>
/// Carries a profile across the Tomoshibi → Tomoru rename.
///
/// <para>The app's data lives in a folder named after the app, in a file named
/// after the app. Renaming moved both, which on its own would leave anyone who
/// had used the old build looking at a brand-new empty install with their
/// subjects, decks and streak still on disk under the old name — the worst
/// kind of data loss, because nothing is actually lost and nothing says so.</para>
///
/// <para>Copies rather than moves: the old folder is small, and leaving it
/// where it is means a botched migration is recoverable by hand. Runs once —
/// the new state file existing is the signal that it already has.</para>
/// </summary>
public static class LegacyProfile
{
    private const string OldFolder = "Tomoshibi";
    private const string OldState = "tomoshibi.json";
    private const string NewState = "tomoru.json";

    /// <summary>Files worth carrying over, by their name in the old folder.
    /// Crash logs are deliberately left behind — they describe a build that no
    /// longer exists.</summary>
    private static readonly string[] Carried =
    {
        OldState, OldState + ".bak", "decks.json", "reviewlog.jsonl"
    };

    /// <summary>Copy an old-name profile into <paramref name="newDir"/> if
    /// there's one to copy and nothing there yet. Returns whether it did.</summary>
    public static bool Migrate(string newDir, string? oldDir = null)
    {
        oldDir ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            OldFolder);

        try
        {
            // Anything already here means this install has its own state —
            // never write over it.
            if (File.Exists(Path.Combine(newDir, NewState)))
                return false;

            if (!File.Exists(Path.Combine(oldDir, OldState)))
                return false;

            Directory.CreateDirectory(newDir);

            var copied = false;
            foreach (var name in Carried)
            {
                var from = Path.Combine(oldDir, name);
                if (!File.Exists(from))
                    continue;

                // The state file and its backup take the new name; the rest
                // were never named after the app.
                var to = Path.Combine(newDir, name.Replace(OldState, NewState, StringComparison.Ordinal));
                File.Copy(from, to, overwrite: false);
                copied = true;
            }

            return copied;
        }
        catch (Exception e) when (e is IOException
                                       or UnauthorizedAccessException
                                       or NotSupportedException)
        {
            // A profile that can't be carried over is a fresh start, not a
            // crash on launch. The old folder is untouched either way.
            return false;
        }
    }
}
