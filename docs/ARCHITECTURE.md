# Architecture

A short tour of how tomoshibi is put together and why.

## The shape of it

tomoshibi is a single-project Avalonia desktop app on .NET 8, using MVVM via
CommunityToolkit.Mvvm. One codebase publishes to Windows, macOS and Linux as a
self-contained download — no runtime install required on the target machine.

The project follows a conventional layered layout:

```
src/Tomoshibi/
  Models/        plain data: AppState, DailyStats, ClassSlot, TodoItem,
                 Subject + Assessment, Deck + Flashcard, DayNote, the enums
                 (Destination, WeekDay, GradeScaleKind, …)
  Services/      side effects + pure helpers behind interfaces:
                 IStorageService (JSON on disk), ISoundService (chime),
                 INotificationService (native alerts), IMusicService,
                 PomodoroMachine (the timer's rules, no clock attached),
                 TaskTemplateParser (task code grammar), IcsImporter,
                 CsvCards + ApkgImporter (Anki text and .apkg collections),
                 Fsrs + Scheduler (spaced repetition), CardGenerator,
                 ClozeParser, OcclusionLayout, SearchQueryParser,
                 ReminderService (deadline alerts),
                 WeeklyRetrospective (the week written up),
                 Guarded + Notice + ErrorLog (failures the user can read),
                 IGlobalHotkeyService (system-wide start/pause: Win32
                 RegisterHotKey / macOS Carbon / null on Linux),
                 GradeScale, ThemeService, DailyReset (midnight
                 banking rules), StateMigrations (load-time upgrades),
                 BackupRestore (backup files read back in, migrated),
                 EmberSeal (the wallet's tamper stamp)
  ViewModels/    UI state and behaviour — the MainWindow shell plus one view
                 model per destination (Dashboard / Today / Timetable / Todo /
                 Subjects / Stats / Review / Shop / Settings), the Cmd-K
                 command palette, and a few sub-models a page delegates to
                 (GradeScaleViewModel, PomodoroViewModel, TaskTemplateViewModel)
  Views/         .axaml + thin code-behind (window-state, focus checks,
                 file pickers — things only the view layer can know)
  Styles/        Palette.axaml (tokens) + Controls.axaml (control styles)
  Assets/        app icon (png/ico/icns) + the phase chime
  App.axaml      app entry, theme + resource wiring
  ViewLocator    maps a view model to its view by naming convention

tests/Tomoshibi.Tests/     xUnit tests over the pure logic (see Testing
                           approach, below)
scripts/                   packaging scripts per platform
docs/                      roadmap, architecture, screenshots
.github/workflows/ci.yml   build + test on every push/PR, across win/mac/linux
```

## Navigation

The window is a shell: a collapsible nav rail on the left, and a main content
area driven by a `Destination` enum. `MainWindowViewModel.ActiveContent`
returns the active destination's view model, a `TransitioningContentControl`
crossfades between them, and the `ViewLocator` resolves each view model to its
view by naming convention. Adding a destination = an enum entry, a view model,
a view, and a nav button.

Two view models break the one-per-destination rule on purpose. The
**Dashboard** owns no data of its own — it takes the Today, Todo, Subjects and
Review view models in its constructor and snapshots their derived figures on
`Refresh()`, so the morning glance always agrees with the pages behind it. The
**command palette** (`Cmd/Ctrl+K`) is shell-level: the shell rebuilds its
candidate list each time it opens — pages, quick actions, every subject, and
the user's content (todo tickets, decks, journal reflections) — and each row
carries an `Action` that navigates and then reveals the target.

## Why these choices

**Avalonia, not WPF/MAUI/Electron.** One XAML codebase that runs natively on
all three desktop platforms, with a real styling system. No browser runtime,
no per-platform UI fork.

**MVVM with the community toolkit.** The `[ObservableProperty]` and
`[RelayCommand]` source generators remove the usual `INotifyPropertyChanged`
boilerplate, so view models stay readable. Views bind to view models and hold
no logic of their own. View models avoid Avalonia *UI* types (no brushes,
controls or windows) so the logic stays testable; the one deliberate exception
is `DispatcherTimer` for the second-tick and the day-watcher.

**Local-first JSON.** Everything lives in one JSON file in the OS app-data
folder. It's the simplest thing that survives restarts, it's easy to inspect,
and it keeps the app fully offline. No database, no accounts.

**Side effects shell out.** Sound and notifications call the OS's own tools
(afplay / osascript on macOS, paplay / notify-send on Linux) instead of
pulling in audio or notification libraries. A missing tool means silence, not
a crash. The one exception is Windows toasts: there is nothing to shell out
to, so the Windows-flavoured build (see the csproj's per-OS target framework)
uses the notifications toolkit, which also registers the app identity an
unpackaged EXE otherwise lacks.

## Data flow

```
launch ─▶ JsonStorageService.Load() ─▶ AppState
                                          │
                                          ▼
                                 MainWindowViewModel
                    (applies StateMigrations, then DailyReset, at load)
                       │        │        │        │        │
                       ▼        ▼        ▼        ▼        ▼
                    Today   Timetable  Todo   Subjects  Review  … (one per
                       │                                          destination)
                            two-way binding │ ▲
                                          ▼ │
                                       Views (.axaml)
                                          │
                       change ─▶ Save(AppState) back to disk
```

State is loaded once at startup into `AppState`. On load the shell applies
`StateMigrations` — the forward migrations (standalone deadlines → todo
tickets, legacy task list → template text, theme ids) — then `DailyReset`:
when the calendar date rolls over it banks the finished day's stats into
`History` and its intention + reflection into the `Journal`, then clears
them. Both are pure state-in/state-out services, kept out of the view models
so their rules stay unit-testable. Each destination's
view model exposes its slice as observable properties, and anything meaningful
writes the whole state back to disk through a short debounce. Persistence is
deliberately simple — serialise everything and replace the file — but never in
place: the save writes a temp file, rotates the previous good copy to `.bak`,
then swaps the temp in, so a crash mid-write leaves the old file or the backup
intact rather than a truncated half-state. The data is tiny.

The today task list is a special case: the persisted form is the *raw template
text* the user wrote, and `TaskTemplateParser` re-derives the task blocks on
every edit. Structured edits (the done checkbox, "send to today" from the
backlog) edit the text surgically rather than regenerating it, so the user's
own formatting survives.

## Theming

Colours, the monospace font and shape tokens live in `Styles/Palette.axaml` as
a merged resource dictionary. Reusable control styles (cards, buttons, inputs,
nav, headings) live in `Styles/Controls.axaml`. Views reference tokens through
`DynamicResource`, so the whole look is defined in one place. Because of that,
`ThemeService` can swap the palette at runtime: a light theme and the extra
themes bought in the shop are just alternate token sets, applied on launch
before the window shows so there's no flash.

## Testing approach

232 xUnit tests, all over pure logic: `PomodoroMachine`, the FSRS scheduler and
review log, the grade engine, `TaskTemplateParser` (parse + the done-toggle
source surgery), storage round-trip and crash recovery, the daily-reset/banking
rules, the load-time migrations, `IcsImporter`, the deck readers and `.apkg`
import, card generation, cloze and occlusion layout, the search query parser,
the palette matcher and its frecency ordering, `EmberSeal`, `WeeklyRetrospective`
and `BackupRestore`.

There's a pattern behind that list. Every one of them started life inside a view
model and was pulled out into a plain state-in/state-out type *so that* it could
be tested. The Pomodoro rules were the last and most stubborn: the phase logic
was welded to a `DispatcherTimer`, so nothing could drive it. `PomodoroMachine`
now holds the rules with time entering only through `Tick()`, and the view model
is left with labels and a timer — which is why a test can run a whole study
afternoon in a loop.

The view models were the long-standing gap, and the cost was visible: UI
changes had to be verified by screenshot, a timetable-grid change shipped a
regression that only surfaced by looking, and a row-alignment fix took three
passes because nothing could assert it.

They're covered now, at least where it matters. `TodoViewModel`,
`TimetableViewModel` and `SubjectsViewModel` hold no Avalonia types, so a test
constructs them directly — no app, no dispatcher, no window — and drives their
commands. What's asserted is the behaviour that was expensive to check by eye:
the week grid measuring itself from the timetable, a block landing at its real
time rather than rounded to the hour, a repeating ticket actually putting its
follow-up on the list, and the subject delete confirmation staging rather than
deleting.

What's still uncovered is the shell (`MainWindowViewModel`) and the review
page, both of which reach for `DispatcherTimer` and platform services. Those
need the seams pulled out first — the same treatment `PomodoroMachine` got.

## Failing in front of the user

Three small pieces handle "something went wrong", because the app has no dialogs
outside its own modals and nowhere else to put an error:

- `Guarded.RunAsync` wraps the file-picker handlers. Those are `async void` —
  unavoidably, since that's an event handler's signature — and an exception
  after the first `await` in an `async void` goes to the dispatcher and kills
  the process. Wrapping the body in a task that never faults makes awaiting it
  safe.
- `Notice` is the one-line banner it surfaces on, a single static instance
  because the app is one window and threading a path to the shell through
  fifteen file handlers isn't worth the wiring.
- `ErrorLog` writes the detail next to `tomoshibi.json`, keeping crashes
  (`crash-*.log`) and handled failures (`error-*.log`) in separate files so one
  can't bury the other.

## Known limitations

- Save-on-change rewrites the entire file. Fine at this scale; revisit only if
  the data grows a lot.
- The package versions in the csproj are pinned to a known-good set and lag the
  latest Avalonia release deliberately: Dependabot holds Avalonia at 11.x
  because 12 is a breaking major, and `Avalonia.Diagnostics` carries its own
  version property since it has no 12.x at all. The migration is planned in
  v2.2; the ignore rule comes off on that branch.
- `.ics` import reads times as wall-clock and only maps weekly recurrences;
  exotic RRULEs are counted and skipped.
- The dashboard's paired cards use a `UniformGrid` to keep their heights level;
  the agenda sits on its own full-width row because its height varies with the
  week and would otherwise unbalance whichever column held it.
