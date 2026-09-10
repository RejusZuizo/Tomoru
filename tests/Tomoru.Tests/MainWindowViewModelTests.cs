using System;
using System.Collections.Generic;
using System.IO;
using Tomoru.Models;
using Tomoru.Services;
using Tomoru.ViewModels;
using Xunit;

namespace Tomoru.Tests;

/// <summary>The shell: the one view model nothing covered, because building it
/// needs a dispatcher for its midnight timer and a directory for the review log
/// and media store. The headless session supplies the first and a temp folder
/// the second, so the wiring can be driven directly rather than by hand.
///
/// <para>These pin the things a constructor quietly does — the what's-new modal
/// deciding whether to appear, the version stamp being written, navigation and
/// zen mode agreeing with each other — since every one of those has to be right
/// before the window is ever shown.</para></summary>
[Collection(HeadlessCollection.Name)]
public class MainWindowViewModelTests : IDisposable
{
    /// <summary>State in memory, location on disk: the view model derives the
    /// review log and media directories from Location, so it has to be real.</summary>
    private sealed class TempStorage : IStorageService, IDisposable
    {
        private readonly string _dir =
            Path.Combine(Path.GetTempPath(), "tomoru-tests", Guid.NewGuid().ToString("N"));

        public AppState State { get; }
        public int Saves { get; private set; }

        public TempStorage(AppState? state = null)
        {
            State = state ?? new AppState();
            Directory.CreateDirectory(_dir);
        }

        public string Location => Path.Combine(_dir, "tomoru.json");
        public AppState Load() => State;
        public void Save(AppState state) => Saves++;

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* temp dir */ }
        }
    }

    /// <summary>Storage that won't write: a full disk, a file the sync client
    /// has open, a drive that went away mid-session. Everything else about it
    /// is real, because the shell reads Location on the way up.</summary>
    private sealed class FailingStorage : IStorageService, IDisposable
    {
        private readonly string _dir =
            Path.Combine(Path.GetTempPath(), "tomoru-tests", Guid.NewGuid().ToString("N"));

        /// <summary>How many writes fail before the disk comes back. Every one
        /// of them, unless a test says otherwise.</summary>
        public int FailFirst { get; init; } = int.MaxValue;

        public int Attempts { get; private set; }
        public int Written { get; private set; }

        public FailingStorage() => Directory.CreateDirectory(_dir);

        public string Location => Path.Combine(_dir, "tomoru.json");
        public AppState Load() => new();

        public void Save(AppState state)
        {
            Attempts++;
            if (Attempts <= FailFirst)
                throw new IOException("There is not enough space on the disk.");
            Written++;
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* temp dir */ }
        }
    }

    private readonly List<TempStorage> _made = new();
    private readonly List<FailingStorage> _failing = new();

    private MainWindowViewModel Shell(AppState? state = null)
    {
        var storage = new TempStorage(state);
        _made.Add(storage);
        return new MainWindowViewModel(storage);
    }

    private MainWindowViewModel Shell(FailingStorage storage)
    {
        _failing.Add(storage);
        return new MainWindowViewModel(storage);
    }

    public void Dispose()
    {
        foreach (var s in _made) s.Dispose();
        foreach (var s in _failing) s.Dispose();
    }

    // ---- the block that was on the clock ----
    //
    // Nothing about the running timer was written down, so quitting or crashing
    // twenty minutes into a focus block lost it: the next launch opened on a
    // fresh 25:00 with no sign there had been anything else.

    private static AppState WithBlock(int remainingSeconds, bool running, int endsInSeconds = 0) =>
        new()
        {
            Timer = new TimerBlock
            {
                Phase = PomodoroPhase.Focus,
                Round = 2,
                PhaseTotalSeconds = 25 * 60,
                PhaseFocusMinutes = 25,
                RemainingSeconds = remainingSeconds,
                EndsAt = DateTime.Now.AddSeconds(endsInSeconds),
                WasRunning = running
            }
        };

    [Fact]
    public void A_paused_block_is_waiting_where_it_was_left() => Headless.Run(() =>
    {
        var vm = Shell(WithBlock(15 * 60, running: false));

        Assert.Equal("15:00", vm.Today.Pomodoro.TimeDisplay);
        Assert.False(vm.Today.Pomodoro.IsRunning);
        Assert.True(vm.Today.Pomodoro.IsPaused);
    });

    [Fact]
    public void A_running_block_comes_back_minus_the_time_the_app_was_shut() => Headless.Run(() =>
    {
        // Written down with twenty minutes left, but its finish line is ten
        // minutes out — the app was closed for the ten in between.
        var vm = Shell(WithBlock(20 * 60, running: true, endsInSeconds: 10 * 60));

        var minutes = int.Parse(vm.Today.Pomodoro.TimeDisplay.Split(':')[0]);
        Assert.InRange(minutes, 9, 10);
    });

    [Fact]
    public void A_block_that_ran_out_while_the_app_was_shut_opens_fresh() => Headless.Run(() =>
    {
        var vm = Shell(WithBlock(20 * 60, running: true, endsInSeconds: -60));

        Assert.Equal("25:00", vm.Today.Pomodoro.TimeDisplay);
        Assert.False(vm.Today.Pomodoro.IsPaused);
    });

    [Fact]
    public void The_round_survives_the_restart_too() => Headless.Run(() =>
    {
        var vm = Shell(WithBlock(15 * 60, running: false));

        // Round 2 of the set — coming back on round 1 would put the long break
        // in the wrong place for the rest of the afternoon.
        Assert.Equal("● ● ○ ○", vm.Today.Pomodoro.RoundLabel);
    });

    [Fact]
    public void Starting_the_timer_writes_the_block_down() => Headless.Run(() =>
    {
        var state = new AppState();
        var vm = Shell(state);

        vm.Today.Pomodoro.ToggleRunCommand.Execute(null);

        Assert.NotNull(state.Timer);
        Assert.True(state.Timer!.WasRunning);

        vm.Today.Pomodoro.ToggleRunCommand.Execute(null);

        Assert.False(state.Timer!.WasRunning);
    });

    // ---- the collection is not rewritten per card ----
    //
    // Every graded card used to mark the decks dirty, so the next debounced
    // save rewrote decks.json in full — 6.4MB and ~70ms of frozen UI for a
    // 6,000-note import, between one card and the next, to record a few bytes
    // of scheduling. The write is throttled now and flushed at the edges.

    /// <summary>A deck of new cards, due now.</summary>
    private static Deck DueDeck(params string[] fronts)
    {
        var deck = new Deck { Name = "big import" };
        foreach (var front in fronts)
        {
            deck.Notes.Add(new Note
            {
                Type = NoteType.Basic,
                Fields = { front, $"answer to {front}" },
                Cards = { new Card { Ord = 0, State = CardState.New, Due = DateTime.Now.AddMinutes(-1) } }
            });
        }
        return deck;
    }

    private static void Grade(MainWindowViewModel vm, int cards)
    {
        vm.Review.ReviewAllCommand.Execute(null);
        for (var i = 0; i < cards; i++)
        {
            vm.Review.FlipCommand.Execute(null);
            vm.Review.GradeGoodCommand.Execute(null);
        }
    }

    [Fact]
    public void Grading_cards_doesnt_ask_for_the_collection_to_be_rewritten() => Headless.Run(() =>
    {
        var state = new AppState();
        state.Decks.Add(DueDeck("one", "two", "three"));

        var vm = Shell(state);
        Grade(vm, 3);

        // The debounced save that follows each card writes tomoru.json — the
        // ember, the reviewed count — and leaves the collection alone.
        Assert.False(state.DecksDirty);
    });

    [Fact]
    public void Leaving_the_review_page_writes_the_collection() => Headless.Run(() =>
    {
        var state = new AppState();
        state.Decks.Add(DueDeck("one", "two"));

        var vm = Shell(state);
        vm.ActiveDestination = Destination.Review;
        Grade(vm, 1);
        vm.ActiveDestination = Destination.Dashboard;

        Assert.True(state.DecksDirty);
    });

    [Fact]
    public void The_way_out_writes_the_collection_whatever_happened() => Headless.Run(() =>
    {
        var state = new AppState();
        state.Decks.Add(DueDeck("one"));

        var vm = Shell(state);
        Grade(vm, 1);
        vm.FlushSave();

        // The throttle is a performance measure; it must never be the reason a
        // session's scheduling didn't reach the disk.
        Assert.True(state.DecksDirty);
    });

    // ---- when the disk says no ----
    //
    // Every file picker in the app runs through Guarded; the save itself never
    // did. A throw inside the debounced write goes to the dispatcher and takes
    // the process with it — losing the session the atomic write exists to
    // protect.

    [Fact]
    public void A_save_that_cant_write_doesnt_take_the_app_down() => Headless.Run(() =>
    {
        Notice.Current.DismissCommand.Execute(null);
        var vm = Shell(new FailingStorage());

        vm.FlushSave();

        Assert.True(Notice.Current.IsVisible);
        Assert.Contains("couldn't save", Notice.Current.Text);
    });

    [Fact]
    public void A_restore_that_cant_be_written_leaves_the_live_state_alone() => Headless.Run(() =>
    {
        // The restore stops saving the old world the moment it believes the new
        // one is on disk. If that write threw, the app would be left unable to
        // save at all — every edit for the rest of the session dropped on the
        // floor — on top of not having restored anything.
        var storage = new FailingStorage { FailFirst = 1 };
        var vm = Shell(storage);

        vm.SettingsPage.RestoreHandler!(new AppState { DailyIntention = "the backup" });
        vm.FlushSave();

        Assert.Equal(1, storage.Written);
    });

    [Fact]
    public void A_write_that_fails_once_doesnt_stop_the_next_one() => Headless.Run(() =>
    {
        // The drive reappears, or the file is closed again. Nothing should have
        // latched off in the meantime.
        var storage = new FailingStorage { FailFirst = 1 };
        var vm = Shell(storage);

        vm.FlushSave();
        vm.FlushSave();

        Assert.Equal(1, storage.Written);
    });

    // ---- what the constructor decides ----

    [Fact]
    public void A_fresh_install_is_not_told_whats_new() => Headless.Run(() =>
    {
        // Empty LastSeenVersion means nobody has run this before. There's no
        // "since last time" to describe, so the modal would be showing release
        // notes for a version they've never not had.
        var vm = Shell(new AppState { LastSeenVersion = string.Empty });

        Assert.False(vm.IsWhatsNewOpen);
    });

    [Fact]
    public void An_updated_install_is() => Headless.Run(() =>
    {
        var vm = Shell(new AppState { LastSeenVersion = "0.0.1" });

        Assert.True(vm.IsWhatsNewOpen);
    });

    [Fact]
    public void Relaunching_the_same_build_says_nothing_the_second_time() => Headless.Run(() =>
    {
        var vm = Shell(new AppState { LastSeenVersion = ReleaseNotes.Version });

        Assert.False(vm.IsWhatsNewOpen);
    });

    [Fact]
    public void The_running_version_is_stamped_so_the_modal_only_lands_once() => Headless.Run(() =>
    {
        var state = new AppState { LastSeenVersion = "0.0.1" };

        var vm = Shell(state);

        Assert.True(vm.IsWhatsNewOpen);
        Assert.Equal(ReleaseNotes.Version, state.LastSeenVersion);
    });

    [Fact]
    public void The_greeting_follows_the_saved_preference() => Headless.Run(() =>
    {
        Assert.True(Shell(new AppState { ShowWelcome = true }).IsWelcomeOpen);
        Assert.False(Shell(new AppState { ShowWelcome = false }).IsWelcomeOpen);
    });

    [Fact]
    public void Every_destination_is_wired_up() => Headless.Run(() =>
    {
        // A null here is a page that opens to nothing at runtime.
        var vm = Shell();

        Assert.NotNull(vm.Dashboard);
        Assert.NotNull(vm.Today);
        Assert.NotNull(vm.Timetable);
        Assert.NotNull(vm.Todo);
        Assert.NotNull(vm.Subjects);
        Assert.NotNull(vm.Stats);
        Assert.NotNull(vm.Review);
        Assert.NotNull(vm.Shop);
        Assert.NotNull(vm.SettingsPage);
        Assert.NotNull(vm.Music);
        Assert.NotNull(vm.Wallet);
        Assert.NotNull(vm.CommandPalette);
    });

    // ---- navigation ----

    [Fact]
    public void The_saved_destination_is_where_it_opens() => Headless.Run(() =>
    {
        var vm = Shell(new AppState { ActiveDestination = Destination.Review });

        Assert.Equal(Destination.Review, vm.ActiveDestination);
        Assert.True(vm.IsReviewActive);
        Assert.False(vm.IsDashboardActive);
    });

    [Fact]
    public void Only_one_page_is_active_at_a_time() => Headless.Run(() =>
    {
        var vm = Shell();

        vm.NavigateByIndex(3);   // timetable, third in nav order

        Assert.Equal(Destination.Timetable, vm.ActiveDestination);
        Assert.True(vm.IsTimetableActive);
        Assert.False(vm.IsTodayActive);
        Assert.False(vm.IsDashboardActive);
    });

    [Fact]
    public void An_index_off_the_end_of_the_nav_is_ignored() => Headless.Run(() =>
    {
        var vm = Shell();
        var before = vm.ActiveDestination;

        vm.NavigateByIndex(0);
        vm.NavigateByIndex(99);
        vm.NavigateByIndex(-1);

        Assert.Equal(before, vm.ActiveDestination);
    });

    [Fact]
    public void Zen_mode_takes_the_navigation_away() => Headless.Run(() =>
    {
        // Zen is the whole point: nothing but the timer. A chord that moved
        // the page out from under it would be a way to get stuck.
        var vm = Shell();
        vm.ToggleZenCommand.Execute(null);
        var whereWeWere = vm.ActiveDestination;

        vm.NavigateByIndex(5);

        Assert.True(vm.IsZenMode);
        Assert.Equal(whereWeWere, vm.ActiveDestination);
    });

    [Fact]
    public void Leaving_zen_gives_it_back() => Headless.Run(() =>
    {
        var vm = Shell();
        vm.ToggleZenCommand.Execute(null);

        vm.ExitZenCommand.Execute(null);
        vm.NavigateByIndex(5);

        Assert.False(vm.IsZenMode);
        Assert.Equal(Destination.Subjects, vm.ActiveDestination);
    });

    // ---- modals ----

    [Fact]
    public void A_modal_stands_the_global_shortcuts_down() => Headless.Run(() =>
    {
        // Space toggles the timer from the Today page. While a dialog is up it
        // has to stop, or typing in the dialog drives the timer.
        var vm = Shell(new AppState { ShowWelcome = false });
        Assert.False(vm.AnyModalOpen);

        vm.OpenTourCommand.Execute(null);

        Assert.True(vm.AnyModalOpen);
    });

    [Fact]
    public void A_pending_deletion_counts_as_a_modal() => Headless.Run(() =>
    {
        // Space toggles the timer. With a delete confirmation up, that has to
        // stop — a stray space while deciding shouldn't reach past the dialog.
        var vm = Shell(new AppState { ShowWelcome = false });
        Assert.False(vm.AnyModalOpen);

        vm.ConfirmDelete.Ask("削除 · delete deck", "kanji", "this can't be undone.", () => { });

        Assert.True(vm.AnyModalOpen);

        vm.ConfirmDelete.CancelCommand.Execute(null);
        Assert.False(vm.AnyModalOpen);
    });

    [Fact]
    public void The_tour_closes_on_its_last_page_rather_than_running_off_the_end() => Headless.Run(() =>
    {
        var vm = Shell();
        vm.OpenTourCommand.Execute(null);

        for (var i = 0; i < 3; i++)
            vm.NextTourPageCommand.Execute(null);
        Assert.True(vm.IsTourLastPage);

        vm.NextTourPageCommand.Execute(null);

        Assert.False(vm.IsTourOpen);
    });

    [Fact]
    public void The_tour_takes_the_stage_from_the_greeting() => Headless.Run(() =>
    {
        var vm = Shell(new AppState { ShowWelcome = true });
        Assert.True(vm.IsWelcomeOpen);

        vm.OpenTourCommand.Execute(null);

        Assert.True(vm.IsTourOpen);
        Assert.False(vm.IsWelcomeOpen);
    });
}
