# Roadmap

Tomoru ships a lean **v1.0** as soon as the core app is polished, then grows
through small versioned releases. Dates are targets, not promises — the point is a
sensible order of work.

## Versioning

- **v1.0 — core.** The everyday study app: Pomodoro timer, daily intention, focus
  stats, task list with course tags, settings, all saved locally. Released after a
  short polish pass and a confirmed build.
- **v1.1 — zen focus mode.** A distraction-free full-screen layout that hides
  everything but the timer.
- **v1.2 — nav sidebar + timetable.** Introduce a togglable left navigation
  sidebar, refactor the existing app into a "today" destination behind it,
  and add a "timetable" destination with a recurring class schedule and
  upcoming deadlines, plus `.ics` import.
- **v1.3 — the daily companion.** Tasks as code driving the timer, chime +
  notifications, stats history with streaks, the todo backlog, and packaging.
- **v1.4 — stats + tray.** A streak calendar over the saved history, and
  the timer in the menu bar.
- **v1.5 — subjects.** Weighted grades per subject with targets, drop
  rules, a what-if simulator, term grouping with a year-weighted degree
  projection, exam surfacing, transcript export — and a configurable scale
  (US 4.0 / UK honours / ECTS / percentage).
- **v1.6 — dashboard.** A morning landing page that gathers the glance,
  weak-spot analysis, a study-video board, a week agenda, per-course focus,
  a light theme, and a settings page that gathers everything tweakable.
- **v1.7 — embers & palette.** An embers currency earned by focusing, a theme
  shop to spend it in, a `Cmd/Ctrl+K` command palette and a launch splash.
- **v1.8 — recall & reflection.** Spaced-repetition flashcards, study goals,
  per-subject notes, an end-of-day reflection journal, exports, deadline
  reminders and a first-run onboarding.
- **v1.9 — final upgrades.** Windows toast notifications, deck
  import/export, timetable-aware focus suggestions, a weekly retrospective
  and a global hotkey — the last feature push before the public release.
- **v2.0 — the public release.** Screenshots, repo polish and the first
  published builds.
- **Later** — soundscapes.

## v1.0 — core

Foundations and the daily-use features. Mostly done.

- [x] Solution + project scaffold (Models / Services / ViewModels / Views / Styles)
- [x] Tokyo Night theme (palette, cards, buttons, mono font)
- [x] JSON storage service + app-state model
- [x] Pomodoro timer: 25/5 cycle, long break after every 4th round
- [x] Daily intention line that persists and resets each day
- [x] Focus stats (sessions + hours) that increment and reset daily
- [x] Settings for timer lengths
- [x] Task list: add / complete / delete, with course tags
- [x] Persist tasks to JSON; carry over between launches
- [x] Polish pass: bilingual empty state, accurate settings caption, midnight reset for the always-on case
- [x] Confirm a clean `dotnet restore` + `dotnet run`
- [x] Tag v1.0

## v1.1 — zen focus mode

- [x] Full-screen layout that hides everything but the timer
- [x] Phase-coloured oversized clock, round indicator, basic controls
- [x] Toggle from the header (`⛶`) and Esc to exit

## v1.2 — nav sidebar + timetable

Introduce a left-side **navigation sidebar** that routes the main content
area between destinations, and ship the first non-"today" destination — a
class-schedule timetable with deadlines.

- [x] Nav sidebar mechanic — togglable from the header (`☰`), open/closed
      state persists
- [x] Refactor the existing app behind a "今日 · today" destination
- [x] "時間割 · timetable" destination
- [x] Models + storage for `ClassSlot` and `Deadline`
- [x] Manual add / remove for both, with course-tag autocomplete pulled from
      tasks, slots and deadlines
- [x] Responsive week-grid view (7-day columns, slots placed by hour) +
      deadlines list above
- [x] `.ics` import — file picker, weekly `RRULE`s → slots, one-shots → deadlines
- [x] Edit slots and deadlines in place

## v1.3 — the daily companion

Everything that turned the timer into something that talks back, plus the
backlog. Shipped, untagged so far.

- [x] Tasks as code: the template grammar, the editor, the simple form modal
- [x] Active task drives the pomodoro phase lengths
- [x] Chime + native notification on phase change (notification: macOS/Linux;
      Windows pending an app identity)
- [x] Auto-continue, paused dimming, round dots, live window title, Space
- [x] Daily stats history + day streak with a 14-day dot strip
- [x] "やること · todo" backlog destination with send-to-today
- [x] App icon, title, macOS .app packaging; Windows/Linux pack scripts
      (written, unverified on those OSes)

## v1.4 — stats + tray

- [x] 記録 · stats destination: streak calendar (month heat grid), best
      streak, all-time totals
- [x] Tray icon: start/pause/skip from the menu bar, live tooltip,
      close-to-tray keeps the timer running

## v1.5 — subjects

- [x] 科目 · subjects destination: assessments per subject, weighted toward a
      running grade and a GPA
- [x] Configurable grade scale — US GPA, letter bands, or custom boundaries
- [x] Targets, drop rules, a what-if simulator, term grouping and a
      year-weighted degree projection
- [x] Exam surfacing, a per-subject page with outlook + linked context, and a
      transcript export

## v1.6 — dashboard

- [x] ダッシュボード · dashboard: today's glance, week momentum, the next-7-days
      agenda, what's due, the standing and weak-spot analysis
- [x] Study-video board, per-course focus tracking, a light theme and keyboard
      navigation
- [x] A settings page gathering everything tweakable; debounced saves

## v1.7 — embers & palette

- [x] Embers currency earned by focusing, and a theme shop to spend it in
- [x] `Cmd/Ctrl+K` command palette over pages, actions, subjects — and now
      todo tickets, decks and journal reflections
- [x] Launch splash and a polished, animated nav rail

## v1.8 — recall & reflection

- [x] 復習 · review destination: spaced-repetition flashcard decks with a
      scheduling queue
- [x] Study goals, per-subject notes, and an end-of-day reflection that banks
      into a journal look-back
- [x] Deadline / exam reminders, first-run onboarding, and data exports
- [x] Hardened notification escaping and a capped `.ics` import

## v1.9 — final upgrades

The last feature push before the public release. (The milestone numbers
above stopped matching the git tags after v1.2 — the v1.3–v1.8 feature work
landed in one untagged run before tagging resumed at v1.4.0. From v1.8.0 on,
tags and milestones line up again.)

- [x] Windows toast notifications (a Windows-flavoured build + the app
      identity registration toasts require)
- [x] Flashcard deck import/export — TSV, compatible with Anki's text format
- [x] Timetable-aware focus: suggest the class happening now as the course
- [x] Weekly retrospective — an auto-written look-back over the week's
      focus, courses and journal
- [x] Global start/pause hotkey — ctrl+alt+P / ⌃⌥P behind an interface
      (Win32 RegisterHotKey + macOS Carbon; Linux ships the null service)
- [x] Backup restore — read a backup file back over the live state and
      relaunch into it (pulled forward from Later)
- [x] Bump ReleaseNotes to 1.9.0 and tag v1.9.0

## v2.0 — the public release

Dress the repo for visitors and publish the first real builds.

- [x] Source-available license (MIT stays in force for ≤ v1.9.0)
- [x] Seal the ember wallet against casual JSON edits
- [x] Screenshots in the README (docs/screenshots, taken over demo data)
- [x] First-run tour — a four-page primer behind "take the quick tour" on
      the welcome, reopenable from settings and the palette; two new
      checklist steps (tasks-as-code, the palette) — and ⌘K now works on
      mac even while typing
- [x] Palette polish — fuzzy, typo-tolerant matching (pulled forward from
      Later); frecency, so familiar picks float up; arrows always drive the
      selection; theme switching, music and mark-intention-kept join the
      actions. The build journal is retired
- [x] A short demo GIF for the README
- [x] Repo description + topics on GitHub
- [x] Launch-time update check pointing at the releases page
- [x] Download & install section in the README (Gatekeeper / SmartScreen
      notes for the unsigned builds)
- [x] Bump ReleaseNotes to 2.0.0
- [x] Tag v2.0.0
- [x] **Publish a GitHub Release with the platform builds.** Done, and it was
      the thing that had stood between the app and its users the longest: for
      several versions the tags ran ahead while the only Release was an
      unpublished v2.0.0 draft, so `/releases/latest` 404'd and took the README
      download link and the launch-time update check down with it. The release
      workflow builds all three platforms now — including Linux, since
      `pack-linux.sh` has been run on a real Arch box — and v2.3.0 is published
      with its assets attached.

## v2.1 — recall, rebuilt

Shipped after the 2.0 release, in three quick versions. Feature work on the
flashcards, then two passes of visual polish across the app.

- [x] **v2.1.0 — flashcards as a real SRS.** The review destination rebuilt
      around an FSRS scheduler with its own review log and stats: card
      generation from note types, cloze parsing, image occlusion, a search
      query parser over the collection, media storage, and `.apkg` import
      that reads Anki's SQLite collection directly (the TSV path from v1.9
      stays)
- [x] **v2.1.1 — subjects, folded around the term.** Subject cards redesigned
      into calm two-line rows, the page reorganised around what matters this
      term, and one shared 960px content column adopted across every page
- [x] **v2.1.2 — the wide screen.** Dashboard reflows into two columns and
      the timetable grid stretches to match; pages ease in and out instead of
      snapping; the review card frames prompt and answer; tag chips follow the
      theme (no more dark chips in the light palette); pomodoro header icons
      aligned

## v2.2 — foundations

The version that pays down what shipping fast left behind, then moves the
framework forward. Ordered deliberately: **publish the release first**, so
users get a stable build before anything churns underneath them.

### Robustness — done, ahead of the rest

- [x] No import or export can take the app down. Every file-picker handler
      was an `async void` doing file I/O with no `try`/`catch`, so a full
      disk or a malformed `.apkg` ended the session; they now run through a
      guard that reports the failure in a notice and keeps the app up
- [x] Forms say why they rejected you instead of doing nothing — eleven
      commands returned silently, the assessment modal worst of all (two
      independent reasons, no hint which)
- [x] Deleting a subject asks first, and names the assessments going with it
- [x] 44 icon buttons carry an accessibility name; there were none anywhere
- [x] The Pomodoro rules extracted into a clock-free `PomodoroMachine` and
      covered by 16 tests — the app's central feature, previously the one
      piece with no coverage
- [x] Grade-scale editing moved out of the 881-line `SubjectsViewModel`
- [x] `pack-linux.sh` proven on a real Arch box; Linux is a download, not a
      build-from-source footnote
- [x] libvlc handed back at shutdown — `VlcMediaService` was disposable and
      never disposed

### Avalonia — moved to v2.3

Kept out of this release deliberately: it's a breaking major across a heavily
custom theme, and a regression from it shouldn't be confused with a bug in the
features above.

This is the plan as it stood then, kept for the reasoning. What actually
happened — including the two things this list didn't predict — is in the v2.3
section below.



- [ ] **11.2.1 → 11.3.20 first.** Nineteen patch releases, no API change.
      Dependabot offers it now the NuGet job runs again. Land it, confirm
      nothing moved, and migrate from a known-good baseline
- [ ] **Then Avalonia 12.** The API surface is a non-event: the codebase uses
      none of the documented breaking changes — no `SystemDecorations`, no
      `GotFocus`/`LostFocus` handlers, no data annotations, no direct
      SkiaSharp, no TreeDataGrid — and compiled bindings, on by default in
      12, are already switched on here. `net8.0` stays supported
- [ ] The real work is the theme: `Controls.axaml` has ~50 `/template/`
      selectors, a dozen reaching into `PART_BorderElement`. Those are Fluent
      internals and won't fail at build — they'll silently stop applying, so
      this needs a visual pass over every control, not an API rewrite
- [ ] `Avalonia.Diagnostics` → `AvaloniaUI.DiagnosticsSupport` +
      `AttachDeveloperTools()`. Check what the free package still covers; the
      standalone Developer Tools app is a paid Accelerate product
- [ ] Drop the Avalonia major-version ignore from `dependabot.yml` on the
      migration branch

### Features

- [x] **Recurring tickets** — daily / weekly / fortnightly / monthly. The next
      occurrence is created when the current one is finished, not by the
      calendar, so an untouched repeat never multiplies
- [x] **Exam countdowns** — "in 26 days" rather than a date to do arithmetic on
- [x] **Focus history export** — sixty days of sessions and minutes had no way
      out but the JSON
- [x] **Follow the desktop's light/dark setting**, live rather than at launch
- [x] **The week grid fits its hours** — a fixed 08–22 buried the deadlines and
      exams below six rows of empty evening. Blocks are placed by real time
      now, so an 11:30 class draws halfway down the row
- [x] **The backlog reads properly** — two-line rows, labelled form fields,
      and buttons that say "edit" rather than ✎
- [ ] **Palette content beyond titles** — search descriptions and course codes
      too, so "MATH201" finds every row that touches the course. (The fuzzy,
      typo-tolerant matching itself shipped in v2.0.)
- [ ] **Group-project awareness** — an optional owner on todo subtasks, so a
      shared project's split shows in the backlog without any sync or accounts

### Ambient soundscapes — dropped

- [ ] ~~Rain / café / waves / night.~~ **Cut from v2.2.** The blocker was never
      the code — LibVLC has been able to play them since v2.1 — it's that no
      licensed loops of usable quality turned up, and shipping something you
      can't legally redistribute in a public release isn't a trade worth
      making. The local-folder music player covers the same need with audio
      the user already owns. Revisit only if assets appear.

## Later

- **Bundle a coding font** — pixel-identical look across OSes.
- **Code signing** — signed/notarized builds, so SmartScreen and Gatekeeper
  trust the download without a click-through.

## Testing

472 tests, all passing. The pure logic is covered: the grade engine, the
task-template parser (including the done-toggle source surgery), storage
round-trip + crash recovery, the daily-reset/banking rules, the load-time
migrations, the `.ics` importer, the ember seal, the palette matcher and its
frecency ordering — and, since v2.1, the FSRS scheduler, card generation,
cloze and occlusion layout, the search query parser, the review log and
`.apkg` import.

The long-standing gap — the Pomodoro state machine — closed in v2.2: the
phase logic moved out of `PomodoroViewModel` into a clock-free
`PomodoroMachine`, where time arrives only through `Tick()`, and 16 tests now
drive whole study afternoons in a loop. v2.4 made that clock injectable, which
is what let a test close the lid on a block and open it again.

The view models are covered now too, including the two that were the awkward
ones. `MainWindowViewModel` and the review page both hold a `DispatcherTimer`,
so the plan was to pull those seams out first — but a headless Avalonia session
has a real dispatcher, so both construct exactly as they do in the app and no
refactor was needed.

That headless session also bought a second kind of test the project didn't
have. `Controls.axaml` reaches into Fluent's own control templates through
about fifty `/template/` selectors, and a selector that stops matching is
neither an error nor a warning — the build stays green and the control quietly
renders in Fluent's default blue. `ThemeTemplateTests` builds real templates
and asserts the setters landed. It found a live bug on its first run: the
ComboBox dropdown had been painting Fluent's grey rather than the theme's
surface, on every theme, since the styles were written.

## Out of scope (for now)

Deliberately off the list to keep things focused: cloud sync, accounts, mobile,
multi-profile, and any always-on network features. Tomoru is a local-first,
single-user desktop app.

## v2.3 — Avalonia 12, the rename, and zen clock faces (shipped)

Shipped. The app builds warning-free on Avalonia 12.1.1, 436 tests pass across
win/mac/linux, and flashcard video was confirmed working by hand — the one
thing no headless test could reach, and the last item gating the tag.

**Done:**

- [x] `Avalonia*` bumped to 12.1.1, and the Dependabot major-version ignore
      removed
- [x] `Avalonia.Diagnostics` → `AvaloniaUI.DiagnosticsSupport` (2.2.3) — the old
      package never shipped a 12.x
- [x] The `Tmds.DBus.Protocol` pin deleted. It existed to force the patched
      0.21.3 over a vulnerable 0.20.0 (GHSA-xrw6-gwf8-vvr9); Avalonia 12 depends
      on 0.94.1, so the pin only held it back
- [x] `this.GetVisualRoot()` → `TopLevel.GetTopLevel(this)` in `StatsView` — the
      extension is gone in 12, and the replacement asks the same question

- [x] The .NET 10 SDK, which is the whole of what "blocked on a toolchain bump"
      meant. Avalonia 12's generators need Roslyn 4.14 and an 8.0.x SDK can't
      run them; because CS9057 is a *warning*, the generator quietly didn't run
      and every `InitializeComponent()` and `x:Name` field went missing. It read
      as broken code and was a missing tool. `global.json` now pins the feature
      band so the next machine gets a one-line "install the 10.0.100 SDK"
      instead of a few hundred missing-symbol errors.
- [x] `dotnet-version` in `ci.yml` and `release.yml` → **both** `8.0.x` and
      `10.0.x`. Only the toolchain moves; the targets stay `net8.0`, and the 8.0
      runtime still has to be present for the tests to execute against.
- [x] README now says .NET 10, and says why the target is still 8.
- [x] `Material.Icons.Avalonia` 2.1.10 → 3.0.2. **This is the one that bites.**
      2.x is built against Avalonia 11, 3.x against 12, and the mismatch does
      not fail the build — the package ships compiled XAML, so it surfaces at
      runtime as `MissingMethodException: TemplateBinding.ProvideValue()` the
      first time a template is built, which is on navigation, not on startup.
      A smoke test that only launches the app would not have caught it.
- [x] `Watermark` → `PlaceholderText` across 8 views (49 attributes). Renamed in
      12; the old name still compiles, as a warning.

- [x] `ThemeTemplateTests` — headless render tests, which is what this migration
      actually needed and the project didn't have. They build real control
      templates with the real style stack and assert the `/template/` setters
      landed, so a selector that stops matching fails a test instead of quietly
      rendering Fluent blue. Deliberately **not** using
      `Avalonia.Headless.XUnit`: it moved to xunit v3, which collides with the
      xunit 2.x the rest of the suite uses.

      Worth knowing these were checked against a known-bad tree before being
      trusted: with Material.Icons pinned back to 2.1.10 the icon test
      reproduces the exact `MissingMethodException`, and the rest still pass —
      which is also the proof that the theme assertions and the package-
      mismatch check are catching two genuinely different things.

**Before this ships:**

- [x] Flashcard video checked by hand and working, which closes the one risk a
      test couldn't. `LibVLCSharp.Avalonia` still has **no Avalonia 12 release** — 3.9.2 asks for
      Avalonia 11.0.4, and even 3.10.1 asks for 11.3.13. Narrower than it
      sounds, though: music goes through `LibVLCSharp.Shared`, which doesn't
      reference Avalonia at all and so isn't exposed to any of this. The only
      Avalonia-facing piece is `VideoView` — flashcard video — and it carries no
      compiled XAML, so it can't fail the way Material.Icons did. A test now
      covers that its type still loads. What a headless test *can't* cover is
      the native surface actually embedding, so play a card with video in it
      before tagging.
- [x] The dead `/template/` setters, which turned up something bigger. The
      `NumericUpDown` `Padding`/`Foreground` pair was the tidying it looked
      like — Fluent sets both via `TemplateBinding` on the element, which
      outranks a style setter, and the values arrived from the outer control
      anyway. The ComboBox's `PopupBorder` block was not: same mechanism, but
      there the intent went unmet, so **every open dropdown in the app had been
      painting Fluent's grey instead of the theme's surface, on every theme,
      since the styles were written**. Not an Avalonia 12 regression — 11.3.20's
      template is identical. It looked right in screenshots because only the
      closed control was ever photographed. Fixed by redefining the keys the
      template looks up, from `ThemeService`'s map, so it re-themes with
      everything else.

**Also landed alongside it** (each its own PR, all on main):

- [x] The update check tells "offline" from "up to date" — it used to catch
      everything into one null, and an empty settings page reads as current.
- [x] `GradeScale`'s bands get a single owner, removing the constructor side
      effect behind a flake that hit about one run in eight.
- [x] The legacy `deadlines` key stops being written to every save.
- [x] Clashing classes take a lane each on the week grid instead of drawing on
      top of one another.
- [x] The shell and the review page are tested — no seam extraction needed
      after all, because the headless session has a real dispatcher.
- [x] Every destructive delete asks first. Ten paths; only the subjects page
      had a confirmation before, and deleting a deck — an imported collection
      and months of scheduling — did not.

## v2.4 — the clock on the wall

Five things the app had got away with because nothing had gone wrong yet, and
one that had been in plain sight the whole time. All found by reading the code
rather than by using it, which is the point: none of them announce themselves.

- [x] **The pomodoro counts seconds, not ticks.** It drove the countdown by
      decrementing once per `DispatcherTimer` tick, and a dispatcher timer
      doesn't replay the ticks it missed — so a lid closed ten minutes into a
      block picked the countdown up exactly where it left off, and a 25-minute
      block quietly ate 35 minutes of the evening. Time enters the machine
      through an injectable clock now. A gap only ever ends **one** block:
      sleeping through an afternoon must not bank three focus sessions nobody
      sat through. The remainder carries between ticks, because a timer firing
      a shade early every time would otherwise round every gap down to nothing
      and stop the clock dead — which is how the first version of the fix hung
      the test suite.
- [x] **The block on the clock survives a restart.** Saved with an absolute
      finish line rather than a countdown, so a running block costs no writes
      at all while it runs and still comes back charged the time the app spent
      shut. A block whose finish line passed while the app was closed is
      neither resumed nor credited — nobody sat through it. It always comes
      back stopped.
- [x] **A failed save no longer takes the app down.** Every file picker ran
      through `Guarded`; the save itself never did. A full disk, a file the
      sync client had open for a moment, app-data on a drive that went away —
      any of them left a timer tick, went to the dispatcher and ended the
      process, losing the session that the temp-then-swap write exists to
      protect. The restore path was the same hole with a worse ending: it
      stopped saving the old world *before* writing the new one, so a failed
      restore left the app unable to save anything at all for the rest of the
      session.
- [x] **The collection isn't rewritten per card.** Every graded card marked the
      decks dirty, so the next save rewrote `decks.json` in full — 6.4MB and
      ~70ms of frozen UI for a 6,000-note import, between one card and the
      next, to record a few bytes of scheduling. Throttled now, and flushed on
      leaving the page and on the way out. A throttle rather than a debounce: a
      debounce restarted by every card would never come due while someone was
      actually reviewing, which is exactly when there's something to lose.
- [x] **One copy of the app.** Two processes over one state file isn't a race
      that produces a muddle, it's the second one's save replacing the first
      one's day — and with close-to-tray on, launching again is easy to do
      without realising the app is already there. Decided before Avalonia
      starts, so a second launch costs nothing and never flashes a window; it
      asks the first to show itself rather than dying quietly. Only a lock
      another copy is holding may stop a launch — a folder that can't hold one
      opens anyway, since this is decided before there's a window to explain
      itself in.
- [x] **The review card is sized to what's on it.** It held a fixed 380–620
      with the prompt pushed to the top edge and the answer to the bottom,
      which frames a long card and leaves a short one — most of them — as two
      lines with a void between. It read as deliberate in a screenshot because
      a screenshot is always of the card someone chose to photograph. Two
      headless tests measure the real card now, and both were checked against
      the old markup before being trusted.

**Left out on purpose.** A global `Dispatcher.UnhandledException` backstop.
The actual hole was the unguarded save, and that's fixed and tested; a
catch-all that swallows every dispatcher exception masks real bugs and leaves
the app running in states nobody designed for. `AppDomain.UnhandledException`
already logs the crash.

**Still open from the 2.x docs pass:** the nine README screenshots and the
demo GIF all predate the rename and still show 灯火 · tomoshibi in the nav rail
while the README calls the app tomoru.
