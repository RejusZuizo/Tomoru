# tomoshibi 灯火

[![ci](https://github.com/socom1/Tomoshibi/actions/workflows/ci.yml/badge.svg)](https://github.com/socom1/Tomoshibi/actions/workflows/ci.yml)

**A calm, late-night study companion for university students.**

Most study tools make you choose: a timer here, flashcards there, a spreadsheet
for grades, a calendar for deadlines. Tomoshibi is the one window where those
stop being separate. A Pomodoro timer and a daily intention sit at the centre;
around them are your class timetable, a coursework backlog, spaced-repetition
flashcards, weighted grades and a focus journal — gathered on a morning
dashboard, reachable from a `Cmd/Ctrl+K` palette, and wired together so a
finished focus block lands against the right course without you filing it.

No account. No sync. No telemetry. Everything is one JSON file on your own
machine, and it works with the wifi off.

> *tomoshibi* (灯火) — a small light or lamp; the bit of light you keep on while
> you work into the night.

![a run through tomoshibi — grades, a subject breakdown, the focus streak and a spaced-repetition review session](docs/screenshots/demo.gif)

## Download & install

Grab the build for your OS from the
[releases page](https://github.com/socom1/Tomoshibi/releases/latest), unzip,
and run. The builds are self-contained — no .NET install needed.

The builds aren't code-signed (yet), so on first launch your OS will be
suspicious on your behalf:

- **macOS** — Gatekeeper says the app "cannot be opened because it is from an
  unidentified developer". Right-click the app → **Open** → **Open** once;
  after that it launches normally.
- **Windows** — SmartScreen shows "Windows protected your PC". Click
  **More info** → **Run anyway** once.
- **Linux** — no warning to click through. Extract the tarball anywhere and
  run `Tomoshibi`, or drop `tomoshibi.desktop` into
  `~/.local/share/applications` (fix its `Exec`/`Icon` paths to wherever you
  extracted) to get it in your app menu.

The app tells you in settings when a newer release is out (you can turn that
check off).

**On Linux, install `libvlc` for audio.** The build carries everything else,
but the media service loads the system libvlc — without it the app runs fine
and simply stays quiet: flashcard audio and video, and the music player, do
nothing. On Arch that's `sudo pacman -S vlc`; on Debian/Ubuntu,
`sudo apt install libvlc-dev vlc`.

## Features

The **dashboard** is the morning landing page — today's intention and focus so
far, the week's momentum, the next seven days' agenda, what's due, where the
grades stand and which subjects need work. `Cmd/Ctrl+K` opens a **command
palette** that jumps to any page, runs a quick action, or searches straight to
a subject, todo ticket, deck or past reflection.

At the centre is the **Pomodoro timer**: focus and break cycles with a longer
break every Nth round, a soft chime and a native notification when the phase
turns, auto-continue, and a global hotkey (`ctrl+alt+P` / `⌃⌥P`) that starts or
pauses from any app. **Zen mode** strips it back to the clock, your intention
and the controls. Today's plan is **written as code** — a tiny grammar of
`// title`, `study: 25`, `course: MATH101`, `done` — edited as a simple list, a
form, or raw source; click a task and it drives the timer. One line sets the
day's **intention** and another reflects on how it went, and both bank into the
**journal** at the midnight rollover.

Around that sits the coursework. The **timetable** holds your week's classes on
a grid (or a list) alongside dated deadlines, with `.ics` import for university
exports; while a class is on, the timer offers it as a one-click focus so the
session lands on the right course. The **todo backlog** keeps longer-horizon
work as numbered tickets — statuses, priorities, due dates, effort estimates,
subtask checklists — any of which goes to today's plan with a click. **Grades**
are per-subject assessments weighted against a scale you choose (US GPA, letter
bands, or boundaries you set yourself), with an overall goal and what each
remaining piece needs to reach it. **Deadline reminders** arrive as desktop
notifications once as a date approaches and again on the day.

**Flashcards** are scheduled by FSRS, the same modern algorithm Anki uses. If
you've used Anki the loop will feel familiar: again / hard / good / easy, each
button showing the interval it buys you. There are cloze deletions, image
occlusion, note types that generate several cards from one, and a searchable
card browser. Your existing collection comes across directly — `.apkg` files
are read as-is, and Anki-compatible text imports and exports work both ways.
Tomoshibi isn't affiliated with Anki; it reads its files because that's where
everyone's decks already are.

**Focus stats** put the history in view: a month calendar tinted by focus,
current and best streak, a 14-day sparkline, focus-by-course, an auto-written
weekly retrospective and the journal look-back. Focus also earns **embers**,
spent in a small shop on extra colour themes, and a music player will loop a
local folder while you work. All of it is **local-first** — one JSON file on
your computer, written atomically with a `.bak` fallback, backed up to a file
of your choosing and restored straight back. No account, no telemetry; the one
optional network touch is a launch-time update check you can switch off.

What's planned next is in [docs/ROADMAP.md](docs/ROADMAP.md); what's already
changed is in [CHANGELOG.md](CHANGELOG.md).

## Screenshots

| | |
|---|---|
| ![the dashboard — today's intention, focus so far, what's due and where the grades stand](docs/screenshots/dashboard.png) | ![the weekly class schedule on the timetable grid, with dated deadlines beneath](docs/screenshots/timetable.png) |
| ![subjects ranked by standing, each against its target grade](docs/screenshots/subjects.png) | ![one subject in full — weighted assessments, what's graded, and the outlook](docs/screenshots/subject.png) |
| ![focus stats — the streak calendar, all-time totals and an auto-written weekly retrospective](docs/screenshots/stats.png) | ![the todo backlog as numbered tickets with statuses, due dates and subtasks](docs/screenshots/todo.png) |
| ![spaced-repetition decks with new and due counts](docs/screenshots/review.png) | ![a card mid-review, with the interval each answer buys](docs/screenshots/review-session.png) |

## Tech

Avalonia 12 + .NET 8, MVVM with CommunityToolkit.Mvvm. One codebase, published
self-contained for Windows, macOS and Linux. The rules that matter — the
Pomodoro state machine, the FSRS scheduler, the grade engine, the importers —
are pure types with no UI attached, which is why most of the 374 tests can
drive them without a window. The project layout and the reasoning behind it are
in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md); how to report a vulnerability
is in [SECURITY.md](SECURITY.md).

Building needs the .NET 10 SDK even though the app targets .NET 8 — see
[Building from source](#building-from-source).

## Building from source

You'll need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
Tomoshibi still *targets* .NET 8 — the newer SDK is only the build toolchain,
because Avalonia 12's source generators need a Roslyn version that older SDKs
don't carry. `global.json` enforces this, so a too-old SDK says so plainly
instead of failing later with a few hundred missing-symbol errors.

```bash
git clone https://github.com/socom1/Tomoshibi.git tomoshibi
cd tomoshibi
dotnet restore
dotnet run --project src/Tomoshibi
```

Run the tests with:

```bash
dotnet test
```

## Building a release

Use the packaging scripts — they handle the per-platform wrapping:

```bash
# macOS: (tested)
./scripts/pack-mac.sh

# Linux: tar.gz with a .desktop launcher (tested on Arch)
./scripts/pack-linux.sh

# Windows: zip of a self-contained folder (tested)
pwsh scripts/pack-win.ps1
```

> Note: avoid `-p:PublishSingleFile=true` on macOS — SkiaSharp's native
> library doesn't survive single-file extraction there, which is why
> `pack-mac.sh` ships the publish folder inside the .app instead.

## Where your data lives

A single `tomoshibi.json` file in your OS application-data folder:

- Windows — `%APPDATA%\Tomoshibi\`
- macOS — `~/Library/Application Support/Tomoshibi/`
- Linux — `~/.config/Tomoshibi/`

Delete it to reset the app to a clean state. When something goes wrong a log
lands in the same folder — `crash-*.log` if the app went down, `error-*.log` if
it caught the problem and carried on. Settings → open folder takes you there;
attaching the newest one to an
[issue](https://github.com/socom1/Tomoshibi/issues) makes the bug far easier to
catch. Found a security problem instead? [SECURITY.md](SECURITY.md) has the
private reporting route.

## License

Source-available — you're welcome to download, build and run tomoshibi
for yourself, but not to redistribute copies or reuse the code elsewhere.
See [LICENSE](LICENSE). Releases up to v1.9.0 remain MIT.
