# Security policy

## Reporting a vulnerability

Report privately. Do not open a public issue.

- Preferred: GitHub's private vulnerability reporting, through the Security tab
  of this repository.
- Alternative: rejusz.coding@gmail.com

Include what you did, what happened, and what you expected. A proof of concept
helps — for anything involving an imported file, the file itself is the proof of
concept, so attach it.

Expect an acknowledgement within seven days and an assessment within thirty.
This is a single maintainer project, so those are honest numbers rather than
aspirational ones.

There is no bug bounty.

## Supported versions

The most recent release only. Older versions are not patched.

## What this software is

A local-first, single-user desktop study app. No accounts, no server, no sync,
no multi-user access. Everything it knows lives in one JSON file plus a media
folder under the user's own profile:

- Linux — `~/.config/Tomoru/`
- macOS — `~/Library/Application Support/Tomoru/`
- Windows — `%APPDATA%\Tomoru\`

It holds coursework: subjects, grades, deadlines, timetable, flashcards, notes
and a reflection journal. Personal, but the user's own, on their own machine.

That shape determines what is worth reporting. There is no login to bypass, no
API to abuse, and no other user's data to reach. The interesting surface is
everything the app reads from outside itself.

## The network surface, in full

One HTTPS GET at launch, to the GitHub releases API, to compare the latest
release tag against the running version. Nothing about the user rides along
beyond the request itself, there is no telemetry, and the check has an off
switch in settings. That is the whole of it — see `Services/UpdateCheck.cs`.

## In scope

The imports are the real surface, since they are the only untrusted input:

- **`.apkg` (Anki) import** — reads an attacker-supplied SQLite collection
  directly. Anything that escapes the temp database, writes outside the media
  folder, or executes as a result of parsing. Sizes are capped (250MB
  collection, 25MB per media file, 250MB of media, 50,000 notes) and counted as
  bytes arrive rather than trusted from the archive's own headers, so a
  decompression bomb stops instead of filling memory or disk — report anything
  that gets past that.
- **Media extraction** — imported filenames are reduced with
  `Path.GetFileName` and prefixed with a content hash before being written.
  Anything that defeats that and lands a file outside the media folder.
- **`.ics` import, CSV/TSV deck import, backup restore** — malformed input that
  achieves more than a rejected file: path traversal, resource exhaustion
  disproportionate to the input, or corruption of existing data.
- **Card rendering** — cloze, occlusion and embedded media come from imported
  content. Anything that turns rendering a card into code execution.
- **The update check** — anything that makes it fetch, trust, or act on
  something other than the release tag it asks for.

## Out of scope

- **The ember wallet seal.** `Services/EmberSeal.cs` says so itself: the HMAC
  key ships in a public binary, so it is a speed bump against hand-editing
  `"embers": 999999`, not cryptographic protection. Re-sealing an edited wallet
  is expected to be possible for anyone who reads the source. It is a
  single-player economy and that is the right trade.
- **Local access by someone who is already you.** The data file is deliberately
  plain JSON and hand-editable. Anyone with your user account can read and edit
  it, by design.
- **Unsigned builds.** Releases are not yet code-signed, so Gatekeeper and
  SmartScreen will warn. That is a known gap, documented in the README, and
  tracked on the roadmap — not a finding.
- Anything requiring the user to run a binary you supplied them.
