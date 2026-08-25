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
    public const string Version = "2.2.3";

    public static string VersionTag => $"v{Version}";

    /// <summary>Shown beside the version — this build was checked and signed off
    /// by the creator.</summary>
    public const string VerifiedBy = "verified by the creator";

    public const string Title = "what's new";

    private static readonly string[] Lines =
    {
        "anki imports are size-limited now, so a malformed or hostile deck can't run the app out of memory or disk",
        "an oversized deck says so instead of failing strangely",
    };

    /// <summary>The notes as one bulleted block for the modal.</summary>
    public static string Body => string.Join("\n", Lines.Select(l => $"›  {l}"));
}
