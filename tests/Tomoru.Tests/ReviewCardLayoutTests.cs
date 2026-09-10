using System;
using System.IO;
using Avalonia.Controls;
using Tomoru.Models;
using Tomoru.Services;
using Tomoru.ViewModels;
using Tomoru.Views;
using Xunit;

namespace Tomoru.Tests;

/// <summary>The review card, measured rather than looked at.
///
/// <para>It used to hold a fixed 380–620 with the prompt pushed to the top edge
/// and the answer to the bottom, which framed a long card well and left the
/// short ones — most of a real collection — as two lines with a void between.
/// That is exactly the kind of thing screenshots hide, since a screenshot is
/// always of the card someone chose to photograph.</para></summary>
[Collection(HeadlessCollection.Name)]
public class ReviewCardLayoutTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "tomoru-tests", Guid.NewGuid().ToString("N"));

    public ReviewCardLayoutTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* temp dir */ }
    }

    /// <summary>The height the card used to be pinned to whatever was on it.</summary>
    private const double OldFixedFloor = 380;

    private ReviewViewModel Reviewing()
    {
        var state = new AppState();
        state.Decks.Add(new Deck
        {
            Name = "kanji",
            Notes =
            {
                new Note
                {
                    Type = NoteType.Basic,
                    Fields = { "火", "fire" },
                    Cards = { new Card { Ord = 0, State = CardState.New, Due = DateTime.Now.AddMinutes(-1) } }
                }
            }
        });

        var vm = new ReviewViewModel(state, () => { }, new WalletViewModel(state, () => { }),
                                     new NullReviewLog(), new MediaStore(_dir),
                                     new ConfirmDeleteViewModel());
        vm.ReviewAllCommand.Execute(null);
        return vm;
    }

    /// <summary>Lay the page out at a realistic window size and hand back the
    /// card's measured height.</summary>
    private static double CardHeight(ReviewViewModel vm)
    {
        var view = new ReviewView { DataContext = vm };
        var window = new Window { Content = view, Width = 1280, Height = 900 };
        window.Show();
        window.UpdateLayout();

        var card = view.FindControl<Border>("CardFrame");
        Assert.NotNull(card);
        return card!.Bounds.Height;
    }

    [Fact]
    public void A_short_card_no_longer_holds_open_a_tall_frame() => Headless.Run(() =>
    {
        var height = CardHeight(Reviewing());

        Assert.True(height < OldFixedFloor,
            $"a one-line prompt drew a {height:0} tall card");
    });

    [Fact]
    public void Revealing_the_answer_grows_the_card_rather_than_filling_a_gap() => Headless.Run(() =>
    {
        var vm = Reviewing();
        var asked = CardHeight(vm);

        vm.FlipCommand.Execute(null);
        var answered = CardHeight(vm);

        Assert.True(answered > asked,
            $"the card was {asked:0} before the answer and {answered:0} after");
    });
}

/// <summary>A review log that keeps entries in memory — these tests are about
/// layout and have no use for the disk.</summary>
internal sealed class NullReviewLog : IReviewLogService
{
    private readonly System.Collections.Generic.List<ReviewLogEntry> _entries = new();

    public void Append(ReviewLogEntry entry) => _entries.Add(entry);
    public System.Collections.Generic.IReadOnlyList<ReviewLogEntry> All() => _entries;
    public int CountToday(Guid deckId, CardState stateBefore, DateOnly today) => 0;
}
