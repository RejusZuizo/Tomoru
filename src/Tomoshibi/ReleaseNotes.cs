using System.Linq;

namespace Tomoshibi;

/// <summary>
/// The app's version and its "what's new" notes, in one place. Bump
/// <see cref="Version"/> and refresh the lines each release; the settings page
/// reads the version, and the what's-new modal shows the notes once after an
/// update (when the running version differs from the last one launched).
/// </summary>
public static class ReleaseNotes
{
    public const string Version = "2.2.0";

    public static string VersionTag => $"v{Version}";

    /// <summary>Shown beside the version — this build was checked and signed off
    /// by the creator.</summary>
    public const string VerifiedBy = "verified by the creator";

    public const string Title = "what's new";

    private static readonly string[] Lines =
    {
        "tickets can repeat — daily, weekly, fortnightly or monthly; the next one appears when you finish this one",
        "the backlog reads on two lines now, and the buttons say what they do",
        "exams count down — \"in 26 days\" instead of a date to work out",
        "the week grid fits your actual hours, so deadlines and exams aren't buried below it",
        "a class at 11:30 sits halfway down the row instead of rounding to the hour",
        "export your focus history as a spreadsheet",
        "the app can follow your desktop's light / dark setting",
    };

    /// <summary>The notes as one bulleted block for the modal.</summary>
    public static string Body => string.Join("\n", Lines.Select(l => $"›  {l}"));
}
