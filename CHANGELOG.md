# Changelog

Notable changes to tomoru. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
versioning follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Milestones before v2.0 are recorded in [docs/ROADMAP.md](docs/ROADMAP.md) rather
than here — the tags and the milestone numbers drifted apart between v1.2 and
v1.8, and the roadmap is the honest account of that stretch.

## [2.3.0] - 2026-08-27

### Changed

- **Renamed from Tomoshibi to Tomoru.** 灯る — for a light to come on; the verb
  to 灯火's noun, so the idea behind the name survives even though the word
  doesn't.

  The reason is prosaic: another study and Pomodoro app already goes by
  Tomoshibi. A shared name only really costs you when it's the same kind of
  thing, because then it's the same people searching — and this was.

  Done now rather than later because the whole cost of a rename is migrating
  the people already using it, and nobody was. That number only goes up, so
  this was the cheapest it would ever be.

  Your data comes with you. The folder and the state file were both named
  after the app, so both moved; on first launch the old profile is copied
  across — copied, not moved, so the original stays where it is. Nothing is
  written over an install that already has state of its own.

  The five releases before this one keep the old name. Tags can't be rewritten,
  only superseded.

- **Zen mode has clock faces.** The shop sells three beyond the plain readout:
  the time written in kanji, a ring that drains as the block does, and a candle
  that burns down and goes out during your break. Every face reads the same
  four things, so the choice changes how the timer looks and never what it
  says.

- **Avalonia 12.** The app builds and runs on 12.1.1. Building now needs the
  .NET 10 SDK — the targets stay `net8.0`; only the toolchain moves, because
  Avalonia 12's source generators need a Roslyn version older SDKs don't carry.
  `global.json` enforces it so a too-old SDK says so plainly.

### Fixed

- **The ComboBox dropdown was never themed.** Fluent sets that panel's colours
  on the element itself, which outranks a style setter, so every open dropdown
  had been painting Fluent's grey rather than the theme's surface — on every
  theme, since the styles were written. Not an Avalonia 12 regression; it looked
  right in screenshots because only the closed control was ever photographed.
- **The update check said "up to date" when it simply couldn't ask.** A DNS
  failure, a timeout and a rate limit all arrived as the same silent nothing,
  which the settings page renders as being current. Offline now says so.
- **Clashing classes hid each other on the week grid.** Two classes at the same
  hour drew in the same place, so whichever sorted later covered the other —
  no error, just a timetable missing a class. They take a lane each now.
- Overlapping-but-not-clashing classes keep their full width, and a lane frees
  up once its class has finished, so one double-booked morning no longer
  narrows the whole day.
- A flaky test that failed about one run in eight, caused by a view-model
  constructor writing app-wide grade-scale state as a side effect.
- The legacy `deadlines` key is no longer written to every save. It had been
  serialising as an empty array in every file since deadlines became tickets.

### Security

- **Every destructive delete asks first.** Ten paths — deck, note, selected
  cards, bulk tag removal, assessment (both routes), subject, ticket, class and
  deadline. Only the subjects page confirmed before; deleting a deck took the
  deck, its notes, every card and every card's scheduling history on a single
  click, with no undo. Each prompt names what goes with the thing.

## [2.2.3] - 2026-08-19

### Security

- The `.apkg` importer enforces size limits: 250MB collection, 25MB per media
  file, 250MB of media in total, 50,000 notes. Bytes are counted as they arrive
  rather than trusted from the archive's headers, so a decompression bomb — a
  few hundred KB that expands to gigabytes — stops instead of filling memory
  and disk. It's the one path that parses an attacker-supplied SQLite database,
  and it was the only importer without a cap; `.ics` and text decks already had
  one.

### Changed

- An oversized or truncated Anki import says so rather than failing obscurely
  or appearing to succeed.

## [2.2.2] - 2026-08-16

### Changed

- Flashcard decks live in their own `decks.json` instead of inside
  `tomoru.json`. The main file is rewritten in full on every debounced save,
  and an imported Anki collection is megabytes — 5MB for a 6,000-note deck,
  17MB for a large shared one — so ticking a subtask was rewriting the whole
  collection. Decks are now written only when something changes one. Existing
  files migrate on first launch with nothing to do.

### Fixed

- Backups include flashcard decks. Splitting decks out of the state file would
  otherwise have produced backups with no cards in them.

## [2.2.1] - 2026-08-16

### Fixed

- The pomodoro appeared to start on its own. Space toggled the timer from any
  page, and space is also how you scroll — so reading the dashboard or the
  backlog and tapping space silently began a focus block. It now toggles only
  on the pomodoro page and in zen mode, where the timer is what you're looking
  at; the other single-key timer controls were already scoped that way.
- Releases carry an Intel Mac build. The macOS runner is Apple Silicon, so the
  release only ever contained an arm64 build and an Intel Mac got a download
  that wouldn't run.

### Changed

- Settings opens on a "general" section — the app-wide options (theme,
  greeting, update check, tray, hotkey) were filed under "startup", and are now
  grouped by what they affect.

## [2.2.0] - 2026-08-12

### Added

- Tickets repeat — daily, weekly, fortnightly or monthly. The next occurrence
  is created when the current one is finished rather than by the calendar, so
  an untouched repeat never multiplies.
- Exams read as countdowns ("in 26 days") rather than as dates.
- Focus history exports as a CSV: every day's sessions, minutes, cards
  reviewed, and the split by course.
- The app can follow the desktop's light/dark setting, switching while it's
  open rather than only at launch. Off by default.

### Changed

- The week grid fits the hours you actually have instead of a fixed 08:00–22:00,
  so the deadlines and upcoming exams are no longer buried below rows of empty
  evening. Classes are placed by real time, so an 11:30 class draws halfway
  down the row rather than rounding to the hour.
- Backlog tickets are two lines: title and actions on the first, everything
  describing the ticket on a quieter second. The edit and send-to-today buttons
  say "edit" and "→ today" rather than ✎ and →.
- The ticket form is wider and every field is labelled; "est. sessions" is now
  "how many focus blocks?" with a note that one block is a 25-minute pomodoro.
- The timetable export is called "calendar" — it has always carried exams and
  due-dated tickets as well as classes, but nothing said so.

### Fixed

- Clearing an hour or minute box in the class editor threw an
  InvalidCastException at the user.

## [Unreleased]

### Added

- `SECURITY.md`, this changelog, and issue templates.
- A failure notice: when something the app does with a file goes wrong, it says
  so in the window instead of vanishing or dying.
- Form validation feedback across the app — a rejected form names what is
  missing and outlines the field, rather than the button appearing to do
  nothing.
- Deleting a subject now asks first, and says how many assessments go with it.
- Accessibility names on 44 icon-only buttons; there were none anywhere before,
  so a screen reader announced the delete button as "✕".
- `PomodoroMachine`, the timer rules with no clock attached, covered by 16
  tests. The app's central feature previously had none.

### Fixed

- An import or export that hit an I/O error took the whole app down with it.
  Every file-picker handler was an `async void` doing file work with no
  `try`/`catch`, so a full disk or a malformed `.apkg` ended the session.
- Dependabot's NuGet updater had been failing since 8 August, so no dependency
  updates — security ones included — were being proposed.
- `VlcMediaService` implements `IDisposable` and was never disposed; libvlc kept
  the audio device open past shutdown.
- Deleting a subject left its course code in the autocomplete suggestions.

### Changed

- Grade-scale editing moved out of `SubjectsViewModel` into its own view model.
- Handled errors are logged separately from crashes, so one does not bury the
  other.
- `pack-linux.sh` has now been run on a real Linux machine and produces a
  working build; Linux is a download rather than a build-from-source footnote.
  Documented the `libvlc` dependency that flashcard audio needs there.

## [2.1.2] - 2026-08-08

### Changed

- The dashboard fills a wide screen in two columns; the timetable grid stretches
  to match.
- Opening a deck, a card or a subject eases in and out instead of snapping.
- The review card frames its prompt at the top and answer at the bottom.

### Fixed

- Tag chips follow the theme — no more dark chips in the light palette.
- Pomodoro header icons line up.

## [2.1.1] - 2026-08-01

### Changed

- Subject cards redesigned into two-line rows, and the subjects page
  reorganised around the current term.
- One shared 960px content column across every page.

## [2.1.0] - 2026-07-25

### Added

- Flashcards rebuilt as a full spaced-repetition system: an FSRS scheduler with
  its own review log and stats, card generation from note types, cloze parsing,
  image occlusion, a search query parser over the collection, media storage, and
  `.apkg` import that reads Anki's SQLite collection directly.

## [2.0.0] - 2026-07-04

The public release.

### Added

- A first-run tour, screenshots in the README, and a launch-time update check
  pointing at the releases page.
- A tamper seal over the ember wallet.
- Fuzzy, typo-tolerant command palette matching, with frecency ordering.
- Crash logs written where a user can find them.

### Changed

- Moved to a source-available licence. Releases up to v1.9.0 remain MIT.
