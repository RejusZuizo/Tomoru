using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Tomoru.Models;

namespace Tomoru.Services;

/// <summary>
/// Persists <see cref="AppState"/> to a single JSON file under the OS
/// application-data folder (so it survives reinstalls and stays out of the repo).
/// </summary>
public class JsonStorageService : IStorageService
{
    private readonly string _filePath;
    private readonly string _tmpPath;
    private readonly string _bakPath;

    // Decks live in their own file. They're the only part of the state that
    // can reach megabytes — an imported Anki collection — and they change far
    // less often than the rest, so pairing them with a save triggered by
    // typing an intention meant rewriting the whole collection to record a
    // keystroke.
    private readonly string _decksPath;
    private readonly string _decksTmpPath;

    public string Location => _filePath;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Stores under the OS application-data folder — the real app path.
    ///
    /// <para>Only this constructor carries an old-name profile across: the
    /// directory-taking one below is what the tests use, and a test pointing at
    /// a throwaway folder should never reach into the real app-data.</para></summary>
    public JsonStorageService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tomoru"))
    {
        LegacyProfile.Migrate(Path.GetDirectoryName(_filePath)!);
    }

    /// <summary>Stores under an explicit directory. Lets tests point the service
    /// at a throwaway folder instead of the user's real app-data.</summary>
    public JsonStorageService(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "tomoru.json");
        _tmpPath = _filePath + ".tmp";
        _bakPath = _filePath + ".bak";
        _decksPath = Path.Combine(directory, "decks.json");
        _decksTmpPath = _decksPath + ".tmp";
    }

    public AppState Load()
    {
        // Main file first; if it's missing or corrupt (e.g. a write was cut
        // short by a crash), fall back to the backup of the last good save
        // before giving up and starting fresh.
        var state = TryLoad(_filePath) ?? TryLoad(_bakPath) ?? new AppState();
        LoadDecks(state);
        return state;
    }

    /// <summary>Read decks.json into the state. Files written before the split
    /// still carry their decks inline, so those are adopted once and marked
    /// dirty — the next save moves them across and the main file sheds them.</summary>
    private void LoadDecks(AppState state)
    {
        try
        {
            if (File.Exists(_decksPath))
            {
                var json = File.ReadAllText(_decksPath);
                state.Decks = JsonSerializer.Deserialize<List<Deck>>(json, Options) ?? new List<Deck>();
                return;
            }

            // Migration: pull decks out of the legacy inline field.
            var legacy = TryLoadLegacyDecks(_filePath) ?? TryLoadLegacyDecks(_bakPath);
            if (legacy is { Count: > 0 })
            {
                state.Decks = legacy;
                state.DecksDirty = true;
            }
        }
        catch
        {
            // A decks file that won't parse shouldn't cost you the rest of the
            // app — the review page opens empty rather than the app not opening.
        }
    }

    private static List<Deck>? TryLoadLegacyDecks(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("decks", out var decks)
                ? JsonSerializer.Deserialize<List<Deck>>(decks.GetRawText(), Options)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static AppState? TryLoad(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppState>(json, Options);
        }
        catch
        {
            return null;
        }
    }

    public void Save(AppState state)
    {
        // The wallet travels with its seal — stamp whatever this write is
        // about to persist, so a load can tell an app save from a hand edit.
        EmberSeal.Apply(state);

        var json = JsonSerializer.Serialize(state, Options);

        // Never write over the live file in place: serialise to a temp file,
        // then swap it in atomically, rotating the previous good state to
        // .bak in the same call. A crash at any point leaves either the old
        // file or the backup intact instead of a truncated half-write.
        File.WriteAllText(_tmpPath, json);

        if (File.Exists(_filePath))
            File.Replace(_tmpPath, _filePath, _bakPath);
        else
            File.Move(_tmpPath, _filePath);

        SaveDecks(state);
    }

    /// <summary>Write decks.json only when something actually changed one.
    /// Same temp-then-swap as the main file, so a crash mid-write can't leave
    /// a truncated collection.</summary>
    private void SaveDecks(AppState state)
    {
        if (!state.DecksDirty && File.Exists(_decksPath))
            return;

        File.WriteAllText(_decksTmpPath, JsonSerializer.Serialize(state.Decks, Options));

        if (File.Exists(_decksPath))
            File.Replace(_decksTmpPath, _decksPath, null);
        else
            File.Move(_decksTmpPath, _decksPath);

        state.DecksDirty = false;
    }
}
