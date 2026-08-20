# Roadmap

Tomoshibi ships a lean **v1.0** as soon as the core app is polished, then grows
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
- [ ] A short demo GIF for the README
- [x] Repo description + topics on GitHub
- [x] Launch-time update check pointing at the releases page
- [x] Download & install section in the README (Gatekeeper / SmartScreen
      notes for the unsigned builds)
- [x] Bump ReleaseNotes to 2.0.0
- [x] Tag v2.0.0
- [ ] **Publish a GitHub Release with the platform builds** — all three now,
      since `pack-linux.sh` has been run on a real Arch box and produces a
      working tarball. Still the one thing standing between the app and its
      users: tags run through v2.1.2, but the only Release is an unpublished
      v2.0.0 draft, so `/releases/latest` 404s — which breaks both the README
      download link and the launch-time update check.

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

232 tests, all passing. The pure logic is covered: the grade engine, the
task-template parser (including the done-toggle source surgery), storage
round-trip + crash recovery, the daily-reset/banking rules, the load-time
migrations, the `.ics` importer, the ember seal, the palette matcher and its
frecency ordering — and, since v2.1, the FSRS scheduler, card generation,
cloze and occlusion layout, the search query parser, the review log and
`.apkg` import.

The long-standing gap — the Pomodoro state machine — closed in v2.2: the
phase logic moved out of `PomodoroViewModel` into a clock-free
`PomodoroMachine`, where time arrives only through `Tick()`, and 16 tests now
drive whole study afternoons in a loop.

What's still uncovered is the view models themselves, which is why a change
like the grade-scale extraction has to be verified by hand. Worth a look if
anything there starts changing often.

## Out of scope (for now)

Deliberately off the list to keep things focused: cloud sync, accounts, mobile,
multi-profile, and any always-on network features. Tomoshibi is a local-first,
single-user desktop app.

## v2.3 — Avalonia 12 (blocked on a toolchain bump)

Attempted and parked. The migration itself is smaller than expected; the
blocker is the build toolchain, not the code.

**What was already done and works** (on the `avalonia-12` branch):

- [x] `Avalonia*` bumped to 12.1.1, and the Dependabot major-version ignore
      removed
- [x] `Avalonia.Diagnostics` → `AvaloniaUI.DiagnosticsSupport` (2.2.3) — the old
      package never shipped a 12.x
- [x] The `Tmds.DBus.Protocol` pin deleted. It existed to force the patched
      0.21.3 over a vulnerable 0.20.0 (GHSA-xrw6-gwf8-vvr9); Avalonia 12 depends
      on 0.94.1, so the pin only held it back
- [x] `this.GetVisualRoot()` → `TopLevel.GetTopLevel(this)` in `StatsView` — the
      extension is gone in 12, and the replacement asks the same question

**The blocker:**

    CS9057: The analyzer assembly 'Avalonia.Generators.dll' references
    version '4.14.0.0' of Microsoft.CodeAnalysis...

Avalonia 12's source generators need **Roslyn 4.14**, which ships with the .NET
10 SDK. The library targets `net8.0` perfectly well, but an 8.0.x SDK can't run
its analyzers — so `InitializeComponent()` and every `x:Name` field silently
fail to generate, and all 14 views fail to compile. It's a warning, not an
error, which is why the symptom looks like broken code rather than a missing
tool.

**So v2.3 needs, in this order:**

- [ ] Install the .NET 10 SDK locally
- [ ] `dotnet-version` in `ci.yml` and `release.yml` → `10.0.x` (the projects can
      keep targeting `net8.0`; only the build toolchain moves)
- [ ] Update the README's "you'll need the .NET 8 SDK"
- [ ] Consider a `global.json` pinning the SDK feature band, so a machine with
      an older SDK fails loudly rather than mysteriously
- [ ] Then the actual work: ~50 `/template/` selectors in `Controls.axaml` reach
      into Fluent internals and will stop applying **silently** rather than
      failing the build. That needs a visual pass over every control
