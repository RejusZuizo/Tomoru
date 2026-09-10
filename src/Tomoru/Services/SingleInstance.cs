using System;
using System.IO;

namespace Tomoru.Services;

/// <summary>
/// Keeps the app to one copy of itself.
///
/// <para>Tomoru holds the whole state in memory and writes it back in full, so
/// two processes over one <c>tomoru.json</c> is not a race that produces a
/// muddle — it's the second one's save flatly replacing the first one's day.
/// And it's easy to arrive at: closing the window leaves the app running in
/// the tray by default, so clicking the launcher again is the obvious thing to
/// do and nothing on screen says it's already open.</para>
///
/// <para>The lock is a file held open for the life of the process. The OS
/// releases it however the app ends, including a crash, so a stale lock file
/// left behind never blocks the next launch — only a live handle does.</para>
///
/// <para>A launch that finds the app already running leaves a note asking it
/// to come to the front, and quits. Doing nothing at all would be its own
/// small mystery: the user clicked the icon and no window appeared.</para>
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string LockName = "tomoru.lock";
    private const string SignalName = "show.signal";

    private readonly FileStream? _handle;
    private readonly string _signalPath;

    private SingleInstance(FileStream? handle, string signalPath)
    {
        _handle = handle;
        _signalPath = signalPath;
    }

    /// <summary>Claim the single-instance lock. Null — and only null — means
    /// another copy is already running and this one should stand down.
    ///
    /// <para>A folder that can't hold the lock at all (read-only, or somewhere
    /// the path doesn't resolve) hands back an unlocked claim instead. This is
    /// decided before Avalonia starts, so a failure here has no window to
    /// explain itself in: an app that silently refuses to open would be a far
    /// worse outcome than the one the lock guards against.</para></summary>
    public static SingleInstance? TryAcquire(string directory)
    {
        var signalPath = Path.Combine(directory, SignalName);

        try
        {
            Directory.CreateDirectory(directory);
        }
        catch
        {
            return new SingleInstance(handle: null, signalPath);
        }

        try
        {
            // FileShare.None is the whole mechanism: the second opener is
            // refused. On Unix this rides flock, which the kernel drops when
            // the process ends however it ends — so a lock file left behind by
            // a crash never blocks the next launch, only a live handle does.
            var handle = new FileStream(Path.Combine(directory, LockName),
                                        FileMode.OpenOrCreate, FileAccess.ReadWrite,
                                        FileShare.None);

            return new SingleInstance(handle, signalPath);
        }
        catch (IOException)
        {
            // Held by the copy that's already running.
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            // The folder won't take the lock file. Open anyway, unlocked.
            return new SingleInstance(handle: null, signalPath);
        }
    }

    /// <summary>Ask the copy that's already running to show its window. Called
    /// by the launch that was refused, just before it quits.</summary>
    public static void SignalExisting(string directory)
    {
        try
        {
            File.WriteAllText(Path.Combine(directory, SignalName), string.Empty);
        }
        catch
        {
            // The other copy stays where it is. Nothing better to do from a
            // process that is about to end anyway.
        }
    }

    /// <summary>True once per ask: another launch wants this window fronted.
    /// Polled by the running app.</summary>
    public bool ConsumeSignal()
    {
        try
        {
            if (!File.Exists(_signalPath))
                return false;

            File.Delete(_signalPath);
            return true;
        }
        catch
        {
            // Mid-write, or gone between the two calls. The next poll settles it.
            return false;
        }
    }

    public void Dispose() => _handle?.Dispose();
}
