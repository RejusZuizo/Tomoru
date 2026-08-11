using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tomoshibi.Services;

/// <summary>
/// Wraps the file-picker handlers. Those are <c>async void</c> — the one place
/// the pattern is unavoidable, since that's the signature an event handler
/// has — and an exception thrown after the first await in an <c>async void</c>
/// has nowhere to return to: it goes to the dispatcher and takes the process
/// with it. Import a malformed deck, export to a full disk, and the app is
/// gone mid-session.
///
/// So every handler body goes in here instead. <see cref="RunAsync"/> returns
/// a task that never faults, which makes awaiting it from an <c>async void</c>
/// safe: the user gets a sentence explaining what failed, the details land in
/// a log, and the app stays up.
/// </summary>
public static class Guarded
{
    /// <param name="action">Phrased to follow "couldn't" — e.g. "export the
    /// deck", so a failure reads "couldn't export the deck — ...".</param>
    public static async Task RunAsync(string action, Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            ErrorLog.Handled(action, ex);
            Notice.Current.Show($"couldn't {action} — {Explain(ex)}");
        }
    }

    /// <summary>Turn an exception into something a student can act on. The
    /// stack trace is already in the log; this is the part they read.</summary>
    private static string Explain(Exception ex) => ex switch
    {
        UnauthorizedAccessException =>
            "no permission to write there. try somewhere like your documents folder.",
        FileNotFoundException or DirectoryNotFoundException =>
            "that file isn't there any more.",
        JsonException =>
            "that file isn't valid tomoshibi data.",
        // Disk full, file open in another program, a disconnected drive — the
        // OS message is genuinely the useful part here.
        IOException io => io.Message.TrimEnd('.') + ".",
        _ => "something went wrong. the details are in settings → open folder.",
    };
}
