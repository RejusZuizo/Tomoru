using System.Linq;
using Tomoru.Models;
using Tomoru.Services;
using Tomoru.ViewModels;
using Xunit;

namespace Tomoru.Tests;

/// <summary>Buying and wearing a zen-mode clock face. Same shape as the theme
/// shop, and the rules that matter are the ones about money: a face you can't
/// afford stays unbought, and buying one twice must not charge twice.</summary>
public class ClockFaceShopTests
{
    private static (ShopViewModel Shop, AppState State, WalletViewModel Wallet) Shop(int embers)
    {
        var state = new AppState { Embers = embers };
        var wallet = new WalletViewModel(state, () => { });
        return (new ShopViewModel(state, () => { }, wallet), state, wallet);
    }

    private static ShopClockFaceViewModel Row(ShopViewModel shop, string id) =>
        shop.ClockFaces.First(f => f.Face.Id == id);

    [Fact]
    public void The_default_face_is_free_and_already_owned()
    {
        var (shop, _, _) = Shop(0);

        Assert.True(Row(shop, "digital").IsOwned);
    }

    [Fact]
    public void Only_the_default_is_free()
    {
        // Otherwise the shop has nothing to sell.
        var (shop, _, _) = Shop(0);

        Assert.All(shop.ClockFaces.Where(f => f.Face.Id != ClockFaces.DefaultId),
                   f => Assert.False(f.IsOwned));
    }

    [Fact]
    public void Buying_one_spends_the_embers_and_wears_it()
    {
        var (shop, state, wallet) = Shop(500);

        shop.ActivateFaceCommand.Execute(Row(shop, "kanji"));

        Assert.Equal(500 - 150, wallet.Balance);
        Assert.Contains("kanji", state.OwnedClockFaceIds);
        Assert.Equal("kanji", state.ActiveClockFaceId);
    }

    [Fact]
    public void Wearing_one_you_already_own_is_free()
    {
        // The button says "wear" once it's bought; it must not charge again.
        var (shop, state, wallet) = Shop(500);
        shop.ActivateFaceCommand.Execute(Row(shop, "kanji"));
        var afterBuying = wallet.Balance;

        shop.ActivateFaceCommand.Execute(Row(shop, "ring"));
        shop.ActivateFaceCommand.Execute(Row(shop, "kanji"));

        Assert.Equal(afterBuying - 200, wallet.Balance);   // only the ring was bought
        Assert.Equal("kanji", state.ActiveClockFaceId);
    }

    [Fact]
    public void One_you_cant_afford_stays_unbought()
    {
        var (shop, state, wallet) = Shop(50);

        shop.ActivateFaceCommand.Execute(Row(shop, "ring"));

        Assert.Equal(50, wallet.Balance);
        Assert.Empty(state.OwnedClockFaceIds);
        Assert.NotEqual("ring", state.ActiveClockFaceId);
        Assert.Contains("more", shop.Flash);
    }

    [Fact]
    public void Applying_a_face_tells_the_shell_so_zen_changes()
    {
        // Without this the purchase saves but zen keeps the old face until
        // the next launch, which reads as the buy having failed.
        var (shop, _, _) = Shop(500);
        string? told = null;
        shop.FaceApplied = id => told = id;

        shop.ActivateFaceCommand.Execute(Row(shop, "kanji"));

        Assert.Equal("kanji", told);
    }

    [Fact]
    public void Every_face_in_the_catalogue_has_somewhere_to_render()
    {
        // A face the shop sells but zen can't draw is a purchase that does
        // nothing — the shop stays trimmed to what's actually implemented.
        var known = new[] { "digital", "kanji", "ring" };

        Assert.All(ClockFaces.All, f => Assert.Contains(f.Id, known));
    }
}
