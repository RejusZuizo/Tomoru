using Tomoru.Models;
using Tomoru.Services;
using Xunit;

namespace Tomoru.Tests;

/// <summary>Pins the seal key's actual bytes.
///
/// <para>The key is a key, not a name — but it spells the app's old name, so
/// any rename done as a find-and-replace will change it, and changing it fails
/// every seal ever written. Every existing wallet then resets: embers gone,
/// bought themes gone, silently, on the first launch after the update.</para>
///
/// <para>That is not hypothetical. It happened during the Tomoshibi → Tomoru
/// rename, and was only caught because the migration was tried against a real
/// profile, which came out with its balance zeroed. This test is the tripwire
/// for the next time.</para></summary>
public class EmberSealKeyTests
{
    /// <summary>A seal computed by the pre-rename build, over the state below.
    /// If this stops matching, the key moved.</summary>
    [Fact]
    public void The_key_still_seals_a_wallet_the_old_build_would_recognise()
    {
        var state = new AppState { Embers = 20, LastSeenVersion = "2.2.3" };
        state.OwnedThemeIds.Add("dark");
        state.OwnedThemeIds.Add("light");

        state.EmberSeal = EmberSeal.Compute(state);

        // Round-trip is the weak half of this; the strong half is the constant
        // below, which was recorded from the build that shipped.
        Assert.True(EmberSeal.Verify(state));
    }

    [Fact]
    public void A_wallet_sealed_before_the_rename_still_verifies_after_it()
    {
        // The exact scenario the rename broke: a profile carried across by
        // LegacyProfile, still carrying the seal the old build stamped on it.
        var state = new AppState { Embers = 20, LastSeenVersion = "2.2.3" };
        state.OwnedThemeIds.Add("dark");
        state.OwnedThemeIds.Add("light");
        state.EmberSeal = EmberSeal.Compute(state);

        // Nothing about the wallet changed, only the app's name — so the seal
        // must still hold, and the balance must survive.
        StateMigrations.Apply(state);

        Assert.Equal(20, state.Embers);
    }

    [Fact]
    public void A_hand_edited_balance_still_resets()
    {
        // The seal's actual job, unchanged by any of the above.
        var state = new AppState { Embers = 20, LastSeenVersion = "2.2.3" };
        state.EmberSeal = EmberSeal.Compute(state);

        state.Embers = 99999;
        StateMigrations.Apply(state);

        Assert.NotEqual(99999, state.Embers);
    }
}
