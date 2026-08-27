using System.Linq;

namespace Tomoru;

/// <summary>
/// The app's version and its "what's new" notes, in one place. Bump
/// <see cref="Version"/> and refresh the lines each release; the settings page
/// reads the version, and the what's-new modal shows the notes once after an
/// update (when the running version differs from the last one launched).
/// </summary>
public static class ReleaseNotes
{
    public const string Version = "2.3.0";

    public static string VersionTag => $"v{Version}";

    /// <summary>Shown beside the version — this build was checked and signed off
    /// by the creator.</summary>
    public const string VerifiedBy = "verified by the creator";

    public const string Title = "what's new";

    private static readonly string[] Lines =
    {
        "tomoshibi is tomoru now — 灯る, for a light to come on. your subjects, decks and streak come with you",
        "zen mode has clock faces you can buy: the time in kanji, a ring, and a candle that burns down and goes out on your break",
        "every delete asks first and says what goes with it — a deck used to take your whole collection on one click",
        "clashing classes sit side by side on the week grid instead of hiding each other",
        "the update check says when it couldn't reach github, rather than calling that up to date",
    };

    /// <summary>The notes as one bulleted block for the modal.</summary>
    public static string Body => string.Join("\n", Lines.Select(l => $"›  {l}"));
}
