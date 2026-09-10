using System;
using System.IO;
using Tomoru.Services;
using Xunit;

namespace Tomoru.Tests;

/// <summary>Two copies of tomoru running at once is a data-loss bug, not an
/// inconvenience: each holds the whole state in memory and writes it back
/// wholesale, so whichever saves last silently eats the other's day.
///
/// <para>It's an easy thing to do by accident, too — closing the window leaves
/// the app in the tray by default, so clicking the launcher again is the
/// natural move and there's nothing on screen to say it's already there.</para></summary>
public class SingleInstanceTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "tomoru-tests", Guid.NewGuid().ToString("N"));

    public SingleInstanceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* temp dir */ }
    }

    [Fact]
    public void The_first_launch_takes_the_lock()
    {
        using var first = SingleInstance.TryAcquire(_dir);

        Assert.NotNull(first);
    }

    [Fact]
    public void A_second_launch_is_refused_while_the_first_holds_it()
    {
        using var first = SingleInstance.TryAcquire(_dir);

        using var second = SingleInstance.TryAcquire(_dir);

        Assert.Null(second);
    }

    [Fact]
    public void The_lock_comes_back_when_the_first_one_quits()
    {
        var first = SingleInstance.TryAcquire(_dir);
        first!.Dispose();

        using var second = SingleInstance.TryAcquire(_dir);

        Assert.NotNull(second);
    }

    [Fact]
    public void A_launch_that_finds_the_app_running_asks_it_to_show_itself()
    {
        using var running = SingleInstance.TryAcquire(_dir);

        SingleInstance.SignalExisting(_dir);

        Assert.True(running!.ConsumeSignal());
    }

    [Fact]
    public void A_quiet_app_is_never_asked_to_show_itself()
    {
        using var running = SingleInstance.TryAcquire(_dir);

        Assert.False(running!.ConsumeSignal());
    }

    [Fact]
    public void The_ask_is_answered_once_and_not_again()
    {
        using var running = SingleInstance.TryAcquire(_dir);
        SingleInstance.SignalExisting(_dir);

        running!.ConsumeSignal();

        // Otherwise the window would fight for the foreground every poll.
        Assert.False(running.ConsumeSignal());
    }

    [Fact]
    public void A_directory_that_isnt_there_yet_is_made()
    {
        var fresh = Path.Combine(_dir, "not-yet");

        using var first = SingleInstance.TryAcquire(fresh);

        Assert.NotNull(first);
    }

    [Fact]
    public void A_folder_that_cant_hold_a_lock_still_lets_the_app_start()
    {
        // Refusing to open at all because the lock couldn't be taken would be a
        // far worse failure than the one it guards against — and silent, since
        // this is decided before there's a window to say anything in. Only a
        // lock another copy is holding may stop a launch.
        var blocked = Path.Combine(_dir, "a-file");
        File.WriteAllText(blocked, "not a directory");

        using var instance = SingleInstance.TryAcquire(Path.Combine(blocked, "tomoru"));

        Assert.NotNull(instance);
    }
}
