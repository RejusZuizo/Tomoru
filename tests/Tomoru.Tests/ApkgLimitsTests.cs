using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Tomoru.Services;
using Xunit;

namespace Tomoru.Tests;

/// <summary>An .apkg is an attacker-supplied zip wrapping an attacker-supplied
/// SQLite database — the one place the app parses something hostile. These
/// cover the size limits: a small archive must not be able to expand into
/// gigabytes of memory or disk.</summary>
public class ApkgLimitsTests : IDisposable
{
    private readonly string _dir;

    public ApkgLimitsTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tomoru-apkg-limits-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private MediaStore NewMedia() => new(Path.Combine(_dir, "media"));

    /// <summary>Highly compressible filler — a zip bomb's whole trick.</summary>
    private static byte[] Zeros(long bytes) => new byte[bytes];

    [Fact]
    public void An_oversized_collection_is_refused_rather_than_extracted()
    {
        var path = Path.Combine(_dir, "bomb.apkg");

        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            // 300MB of zeros compresses to a few hundred KB. Before the cap,
            // this was written to the temp directory in full.
            var entry = zip.CreateEntry("collection.anki2", CompressionLevel.Optimal);
            using var s = entry.Open();
            for (var i = 0; i < 300; i++)
                s.Write(Zeros(1024 * 1024));
        }

        Assert.True(new FileInfo(path).Length < 5 * 1024 * 1024, "the archive itself should be small");

        var result = ApkgImporter.Import(path, NewMedia(), keepSchedule: false);

        Assert.False(result.Ok);
        Assert.Contains("too large", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_oversized_media_file_is_skipped_and_the_import_carries_on()
    {
        var path = Path.Combine(_dir, "bigmedia.apkg");

        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            using (var s = zip.CreateEntry("collection.anki2", CompressionLevel.Optimal).Open())
                s.Write(Encoding.UTF8.GetBytes("not a real database"));

            using (var s = zip.CreateEntry("media", CompressionLevel.Optimal).Open())
                s.Write(Encoding.UTF8.GetBytes("{\"0\":\"huge.mp3\"}"));

            // 40MB, over the 25MB per-file limit.
            using (var s = zip.CreateEntry("0", CompressionLevel.Optimal).Open())
                for (var i = 0; i < 40; i++)
                    s.Write(Zeros(1024 * 1024));
        }

        // The collection is junk, so the import fails on that — the point is
        // that it fails there rather than dying while inhaling the media.
        var result = ApkgImporter.Import(path, NewMedia(), keepSchedule: false);

        Assert.False(result.Ok);
        Assert.DoesNotContain("media", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_file_with_no_collection_is_rejected_cleanly()
    {
        var path = Path.Combine(_dir, "empty.apkg");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
            using (var s = zip.CreateEntry("readme.txt", CompressionLevel.Optimal).Open())
                s.Write(Encoding.UTF8.GetBytes("nothing to see"));

        var result = ApkgImporter.Import(path, NewMedia(), keepSchedule: false);

        Assert.False(result.Ok);
        Assert.Contains("No Anki collection", result.Message);
    }

    [Fact]
    public void Something_that_isnt_a_zip_at_all_is_rejected_cleanly()
    {
        var path = Path.Combine(_dir, "not.apkg");
        File.WriteAllText(path, "this is just text");

        var result = ApkgImporter.Import(path, NewMedia(), keepSchedule: false);

        Assert.False(result.Ok);
    }
}
