using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Tomoru.Models;
using Tomoru.Services;
using Xunit;

namespace Tomoru.Tests;

/// <summary>Decks moved out of tomoru.json into their own file, so the
/// frequent saves stop rewriting an imported Anki collection. Getting this
/// wrong loses somebody's decks, so: the migration, the round-trip, and the
/// rule about when decks.json is written.</summary>
public class DeckStorageTests : IDisposable
{
    private readonly string _dir;

    public DeckStorageTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tomoru-decks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    private string StatePath => Path.Combine(_dir, "tomoru.json");
    private string DecksPath => Path.Combine(_dir, "decks.json");

    private static Deck Deck(string name) => new()
    {
        Name = name,
        Notes = { new Note { Fields = { "front", "back" } } }
    };

    [Fact]
    public void Decks_round_trip_through_their_own_file()
    {
        var storage = new JsonStorageService(_dir);
        var state = new AppState { DailyIntention = "focus" };
        state.Decks.Add(Deck("Kanji"));
        state.DecksDirty = true;

        storage.Save(state);

        Assert.True(File.Exists(DecksPath));
        var loaded = new JsonStorageService(_dir).Load();
        Assert.Single(loaded.Decks);
        Assert.Equal("Kanji", loaded.Decks[0].Name);
        Assert.Equal("focus", loaded.DailyIntention);
    }

    [Fact]
    public void The_main_file_no_longer_carries_them()
    {
        var storage = new JsonStorageService(_dir);
        var state = new AppState();
        state.Decks.Add(Deck("Kanji"));
        state.DecksDirty = true;
        storage.Save(state);

        // This is the whole point: a 17MB collection must not be rewritten
        // every time an intention is typed.
        using var doc = JsonDocument.Parse(File.ReadAllText(StatePath));
        Assert.False(doc.RootElement.TryGetProperty("decks", out _));
    }

    [Fact]
    public void A_file_from_before_the_split_keeps_its_decks()
    {
        // A legacy state file: decks inline, no decks.json beside it.
        var legacy = new Dictionary<string, object>
        {
            ["dailyIntention"] = "carried over",
            ["decks"] = new[] { new { name = "Old deck", notes = Array.Empty<object>() } }
        };
        File.WriteAllText(StatePath, JsonSerializer.Serialize(legacy,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var state = new JsonStorageService(_dir).Load();

        Assert.Single(state.Decks);
        Assert.Equal("Old deck", state.Decks[0].Name);
        Assert.Equal("carried over", state.DailyIntention);
        // Marked dirty so the next save writes them across.
        Assert.True(state.DecksDirty);
    }

    [Fact]
    public void The_migration_writes_them_across_on_the_next_save()
    {
        var legacy = new Dictionary<string, object>
        {
            ["decks"] = new[] { new { name = "Old deck", notes = Array.Empty<object>() } }
        };
        File.WriteAllText(StatePath, JsonSerializer.Serialize(legacy,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var storage = new JsonStorageService(_dir);
        var state = storage.Load();
        storage.Save(state);

        Assert.True(File.Exists(DecksPath));
        Assert.Single(new JsonStorageService(_dir).Load().Decks);
    }

    [Fact]
    public void A_save_that_didnt_touch_decks_leaves_the_file_alone()
    {
        var storage = new JsonStorageService(_dir);
        var state = new AppState();
        state.Decks.Add(Deck("Kanji"));
        state.DecksDirty = true;
        storage.Save(state);

        var written = File.GetLastWriteTimeUtc(DecksPath);

        // The save that follows is a normal one — an intention, a subtask tick.
        state.DailyIntention = "something else";
        Assert.False(state.DecksDirty);
        storage.Save(state);

        Assert.Equal(written, File.GetLastWriteTimeUtc(DecksPath));
    }

    [Fact]
    public void Saving_clears_the_dirty_flag()
    {
        var storage = new JsonStorageService(_dir);
        var state = new AppState { DecksDirty = true };

        storage.Save(state);

        Assert.False(state.DecksDirty);
    }

    [Fact]
    public void A_corrupt_decks_file_costs_the_decks_and_nothing_else()
    {
        File.WriteAllText(StatePath, "{\"dailyIntention\":\"still here\"}");
        File.WriteAllText(DecksPath, "{ this is not json");

        var state = new JsonStorageService(_dir).Load();

        Assert.Empty(state.Decks);
        Assert.Equal("still here", state.DailyIntention);
    }
}
