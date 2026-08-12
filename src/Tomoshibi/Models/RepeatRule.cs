namespace Tomoshibi.Models;

/// <summary>
/// How often a ticket comes back. A weekly problem set is the single most
/// common thing on a student's list, and before this it had to be retyped
/// every week — or quietly stopped happening.
/// </summary>
public enum RepeatRule
{
    /// <summary>One-off. The default, and what every ticket was before.</summary>
    None,
    Daily,
    Weekly,
    Fortnightly,
    Monthly
}
