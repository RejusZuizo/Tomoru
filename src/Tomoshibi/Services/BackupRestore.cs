using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Tomoshibi.Models;

namespace Tomoshibi.Services;

/// <summary>
/// Reads a backup file back into a usable <see cref="AppState"/> — the other
/// half of the settings-page backup button. The JSON shape matches what the
/// storage service and the backup export both write (camelCase); anything
/// that doesn't parse comes back null rather than half-restored. Migrations
/// run on the way in, so a backup taken by an older build lands in today's
/// shape, exactly as if it had been loaded from disk.
/// </summary>
public static class BackupRestore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>A backup carries everything, decks included. They're excluded
    /// from the live state file (they live in decks.json), so they have to be
    /// re-attached here or a backup would restore an empty collection.</summary>
    public static string Build(AppState state)
    {
        var json = JsonSerializer.Serialize(state, Options);

        using var doc = JsonDocument.Parse(json);
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
                prop.WriteTo(writer);

            writer.WritePropertyName("decks");
            JsonSerializer.Serialize(writer, state.Decks, Options);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    public static AppState? Parse(string json)
    {
        AppState? state;
        try
        {
            state = JsonSerializer.Deserialize<AppState>(json, Options);

            // Decks ride along under "decks" — the same shape the pre-split
            // state files used, so old backups restore through this path too.
            if (state is not null)
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("decks", out var decks))
                {
                    state.Decks = JsonSerializer.Deserialize<List<Deck>>(decks.GetRawText(), Options)
                                  ?? new List<Deck>();
                    state.DecksDirty = true;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        if (state is null)
            return null;

        StateMigrations.Apply(state);
        return state;
    }
}
