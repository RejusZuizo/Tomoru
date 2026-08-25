using System;
using System.IO;
using Tomoru.Services;
using Xunit;

namespace Tomoru.Tests;

/// <summary>Carrying a profile across the Tomoshibi → Tomoru rename. The folder
/// and the state file are both named after the app, so renaming moved both —
/// and without this, anyone who had used the old build would open a brand-new
/// empty install with their subjects, decks and streak still sitting on disk
/// under the old name. Nothing lost, nothing said, which is the worst version.</summary>
public class LegacyProfileTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "tomoru-tests", Guid.NewGuid().ToString("N"));

    private string Old => Path.Combine(_root, "Tomoshibi");
    private string New => Path.Combine(_root, "Tomoru");

    public LegacyProfileTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* temp dir */ }
    }

    private void WriteOld(string name, string contents)
    {
        Directory.CreateDirectory(Old);
        File.WriteAllText(Path.Combine(Old, name), contents);
    }

    [Fact]
    public void An_old_profile_comes_across_under_the_new_name()
    {
        WriteOld("tomoshibi.json", """{"embers":42}""");

        var moved = LegacyProfile.Migrate(New, Old);

        Assert.True(moved);
        Assert.Equal("""{"embers":42}""", File.ReadAllText(Path.Combine(New, "tomoru.json")));
    }

    [Fact]
    public void The_decks_and_the_review_log_come_too()
    {
        // Decks live in their own file and the review log is append-only —
        // leaving either behind loses a collection or a year of history.
        WriteOld("tomoshibi.json", "{}");
        WriteOld("decks.json", """[{"name":"kanji"}]""");
        WriteOld("reviewlog.jsonl", """{"ts":"2026-01-01"}""");

        LegacyProfile.Migrate(New, Old);

        Assert.True(File.Exists(Path.Combine(New, "decks.json")));
        Assert.True(File.Exists(Path.Combine(New, "reviewlog.jsonl")));
    }

    [Fact]
    public void The_backup_keeps_its_role()
    {
        WriteOld("tomoshibi.json", "{}");
        WriteOld("tomoshibi.json.bak", """{"embers":7}""");

        LegacyProfile.Migrate(New, Old);

        // It has to land as tomoru.json.bak, or the storage service's
        // corrupt-file fallback won't find it.
        Assert.True(File.Exists(Path.Combine(New, "tomoru.json.bak")));
    }

    [Fact]
    public void The_old_folder_is_left_where_it_is()
    {
        // Copy, not move: a botched migration stays recoverable by hand.
        WriteOld("tomoshibi.json", """{"embers":42}""");

        LegacyProfile.Migrate(New, Old);

        Assert.True(File.Exists(Path.Combine(Old, "tomoshibi.json")));
    }

    [Fact]
    public void An_install_that_already_has_state_is_never_written_over()
    {
        // The dangerous case: someone runs the new build, uses it, and then a
        // stale old profile overwrites the work they just did.
        WriteOld("tomoshibi.json", """{"embers":1}""");
        Directory.CreateDirectory(New);
        File.WriteAllText(Path.Combine(New, "tomoru.json"), """{"embers":999}""");

        var moved = LegacyProfile.Migrate(New, Old);

        Assert.False(moved);
        Assert.Equal("""{"embers":999}""", File.ReadAllText(Path.Combine(New, "tomoru.json")));
    }

    [Fact]
    public void Running_it_twice_changes_nothing_the_second_time()
    {
        WriteOld("tomoshibi.json", """{"embers":42}""");

        Assert.True(LegacyProfile.Migrate(New, Old));
        Assert.False(LegacyProfile.Migrate(New, Old));
    }

    [Fact]
    public void A_fresh_install_with_no_old_profile_is_a_no_op()
    {
        var moved = LegacyProfile.Migrate(New, Old);

        Assert.False(moved);
        Assert.False(Directory.Exists(New) && File.Exists(Path.Combine(New, "tomoru.json")));
    }

    [Fact]
    public void Crash_logs_are_left_behind()
    {
        // They describe a build that no longer exists under a name that no
        // longer exists; carrying them over would only confuse the next report.
        WriteOld("tomoshibi.json", "{}");
        WriteOld("crash-20260101-000000-001.log", "boom");

        LegacyProfile.Migrate(New, Old);

        Assert.False(File.Exists(Path.Combine(New, "crash-20260101-000000-001.log")));
    }
}
