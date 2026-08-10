# Changelog

Notable changes to tomoshibi. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
versioning follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Milestones before v2.0 are recorded in [docs/ROADMAP.md](docs/ROADMAP.md) rather
than here — the tags and the milestone numbers drifted apart between v1.2 and
v1.8, and the roadmap is the honest account of that stretch.

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
