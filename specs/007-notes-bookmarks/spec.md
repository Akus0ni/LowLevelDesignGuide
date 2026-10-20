# Spec 007: Notes & bookmarks

| | |
|---|---|
| **Status** | Approved by owner (2026-10-20) |
| **Branch** | `spec/007-notes-bookmarks` |
| **Covers** | R18 (PRD §13); PRD F5, §10 (`Note`, `Bookmark`), §11 (offline) |
| **Depends on** | Spec 003 (reader, outline, heading anchors), spec 005 (owner = user or device guest, adoption, store pattern), spec 001 (users) |
| **Case study** | None planned. Optional later, only if the owner asks (`lld-case-study`). |

## 1. Goal
Let learners keep their own words next to the material. They can write Markdown notes on a whole lesson or on one
section of it, bookmark lessons, sections and cheat sheets, and find everything again in the **Notes** tab. Before an
interview, a lesson's notes show up under its cheat sheet in Revise, and all notes can be exported as one Markdown file.

Notes and bookmarks are stored like progress (spec 005): per **user**, with a device **guest** adopted at first sign-in,
in the device SQLite in the app and on the API in browser mode. Everything works offline. Syncing between devices
stays in spec 010; this spec makes the records ready for it (`UpdatedAt`, `DeviceId`, soft deletes).

## 2. User stories
1. *As a learner reading a lesson,* I add a note on the section I'm reading, without losing my place. Later I see that the
   section has a note, both beside its heading and in the outline.
2. *As a learner,* I write notes in Markdown (lists, code), switch to a preview that renders like a lesson, and never
   press Save: the note saves as I type.
3. *As a learner,* I start a note from a starter such as "My example from work", "Interview answer (STAR)" or "Gotcha I
   hit", so my notes are the kind that help in an interview.
4. *As a learner,* I tag notes by writing `#weak` or `#ask-in-interview` in the text, and filter the Notes tab by tag.
5. *As a learner,* I bookmark a lesson, a section or a cheat sheet, and one tap from the Notes tab takes me back there.
6. *As a learner revising,* I see my notes for a lesson under its cheat sheet.
7. *As a learner,* I delete a note and can undo it for a few seconds.
8. *As a learner,* I export all my notes as one Markdown file, grouped by module and lesson.
9. *As a learner on a shared device,* notes I write signed out become mine when I sign in; another user never sees them.
10. *As the owner,* notes and bookmarks work fully offline (PRD §11) and the app makes no new server calls (ngrok budget).

## 3. Scope

### 3.1 What is stored (per owner)
- **Note:** `Id` (GUID, made by the client), `LessonId`, `Anchor` (a section's heading id, or empty for the whole
  lesson), `Body` (Markdown, at most 20,000 characters), `CreatedAt`, `UpdatedAt`, `DeletedAt` (soft delete), `DeviceId`.
  A lesson can have several notes, on the whole lesson and on any of its sections.
- **Bookmark:** `Kind` (`lesson` | `section` | `cheatSheet`), `LessonId`, `Anchor` (sections only), `CreatedAt`,
  `UpdatedAt`, `DeletedAt`, `DeviceId`. There is at most one bookmark per (owner, kind, lesson, anchor). Removing it sets
  `DeletedAt`, and bookmarking again clears it.
- **Tags** are not stored. They are read from the note text whenever needed (§3.5), so the text is the only source.
- Limits (checked by every store, like `ProgressLimits`): body ≤ 20,000 characters, ≤ 100 notes per lesson, ≤ 5,000
  notes and ≤ 2,000 bookmarks per owner, anchor ≤ 200 characters, ≤ 500 records per write.
- An empty note is never saved. If a note is emptied and closed, it is deleted, with no undo needed.

### 3.2 Notes in a lesson
- **Lesson header:** a **Notes** button with a count ("Notes · 3") and a **bookmark** toggle for the lesson.
- **Section actions:** each `h2` gets a small quiet button at the end of the heading (≥ 44 px target, shown on phone too)
  that opens a popover with **Add note here** and **Bookmark section** / **Remove bookmark**.
- **Markers:** a section with notes shows a note chip after its heading ("✎ 2"); tapping it opens the notes sheet at that
  section. A bookmarked section shows a ribbon mark. The outline (side rail and "On this page" sheet) shows the same two
  small marks next to the section name.
- **Notes sheet:** a bottom drawer on phone, a side panel on desktop. The lesson underneath keeps its scroll position. It
  lists the lesson's notes grouped as *Whole lesson*, then each section in reading order. **New note** asks where:
  *Whole lesson* or the current section (preselected).
- **Editor:** a plain text area that saves automatically (800 ms after typing stops, and when the editor closes or the
  page is hidden). It has an **Edit / Preview** switch, where Preview uses the lesson Markdown renderer (GFM, code
  highlighting, no raw HTML, links open as in lessons). A quiet "Saved" status is shown. There is no toolbar.
- **Starters** appear while a note is empty. Picking one fills in its outline and its tag, and the cursor goes to the
  first blank:
  | Starter | Inserts |
  |---|---|
  | My example from work | `#work` · **Where:** · **What it solved:** · **What I'd change:** |
  | Interview answer (STAR) | `#interview` · **Situation:** · **Task:** · **Action:** · **Result:** |
  | Gotcha I hit | `#gotcha` · **Symptom:** · **Cause:** · **Fix:** |
- **Anchors that move:** if a section is renamed or removed by a content update, its notes and bookmarks show under
  *Whole lesson* with a small "section no longer exists" label. Nothing is lost. A lesson removed from the content keeps
  its notes in the Notes tab under **Removed lessons**, and they are still exported.

### 3.3 Bookmarks
- **Lesson:** the toggle in the lesson header.
- **Section:** from the section actions popover.
- **Cheat sheet:** a toggle in the cheat sheet header (Revise).
- Every toggle has a short toast ("Bookmarked" / "Bookmark removed", with **Undo**). The toggle's own state change is
  instant and is the main feedback.

### 3.4 The Notes tab
Replaces the "Coming soon" page. It has a **Notes / Bookmarks** switch at the top.
- **Notes:** grouped by module (course order), then by lesson, with the lesson that has the latest edit first. Within a
  lesson, notes follow reading order. Each row shows the section name (or "Whole lesson"), the first two lines as plain
  text, its tags as chips and "edited 2 days ago". Tapping a row opens the lesson at that section with the notes sheet open on
  that note.
- **Filters:** tag chips (every tag in use, with counts) and a text filter (plain "contains", case-insensitive; full
  search is spec 009). Tags and text combine (AND). The filters are remembered for the session.
- **Bookmarks:** grouped by module, then by lesson. Each row has a kind icon (lesson, section, cheat sheet) and opens the
  lesson, the lesson at the section (`?at=`) or the cheat sheet. **Remove** (with Undo) is on each row.
- **Export notes** button: see §3.7.
- **Empty states** that direct the learner: "No notes yet. Open a lesson and use **Add note here** on any section."
  "Nothing bookmarked yet."

### 3.5 Tags
- A tag is `#` followed by a letter and then up to 29 letters, digits or `-`, e.g. `#weak`, `#ask-in-interview`. Tags
  are matched without regard to case and shown in lower case.
- These are **not** tags: text inside code spans or code blocks, Markdown headings (`# Title`, which have a space after
  the `#`), and URL fragments.
- One pure function parses tags. Both C# (export, later search) and the web use it, checked against a shared test vector
  (like the mnemonics vector in spec 006).

### 3.6 Notes in Revise
Under each cheat sheet, a folded **Your notes (n)** block lists that lesson's notes in reading order, rendered as in
Preview. It appears only when there are notes. It is not part of recall mode: **Reveal all** doesn't open it, and it
doesn't count towards "revised" (spec 005 §3.5).

### 3.7 Export
- **Export notes** creates one Markdown file, `lld-notes-YYYY-MM-DD.md`:
  `# My LLD notes` → `## <module>` → `### <lesson>` → `#### <section | Whole lesson>` → note bodies as written, oldest
  first, separated by rules. Removed lessons come last. Deleted notes are left out. Bookmarks are not exported.
- **Browser:** a download.
- **App:** a new bridge method hands the text to the native side, which writes a temporary file and opens the system
  share sheet (MAUI `Share`, on Android, Windows, iOS and macOS). No network.

### 3.8 Guest and users
The same rules as spec 005 §3.9:
- Signed out, notes and bookmarks belong to the device guest.
- At first sign-in, the user adopts them. Notes merge by `Id`, and the later `UpdatedAt` wins. Bookmarks merge by
  (kind, lesson, anchor), and the later `UpdatedAt` wins, so a removal can win too.
- Signing out hides the user's data. Another user never sees it.
- Browser mode: the guest lives in localStorage; signed in, the data is on the API, and adoption sends the guest data once.

### 3.9 Storage and data access
- **Domain:** `Lld.Domain/Notes`: `Note`, `Bookmark`, `NoteLimits`, `NoteRules` (later edit wins, merge), `TagParser`,
  and `INotesStore`, which follows the `IProgressStore` pattern, with an owner and one contract test suite for the
  in-memory, device and EF Core stores.
- **Device (MAUI):** new tables `Notes` and `Bookmarks` through schema step **V6**, using plain `Microsoft.Data.Sqlite`
  (R67). A content reload never touches them. Reads and writes go over the bridge.
- **API:** `Note` and `Bookmark` entities, an EF Core migration `AddNotes`, and endpoints for the signed-in user only:
  `GET /api/notes` (notes and bookmarks), `PUT /api/notes` (a batch upsert, including soft deletes), `PUT /api/bookmarks`,
  and `POST /api/notes/adopt` (the guest's notes and bookmarks). Browser mode uses them now, and the sync in spec 010
  uses them later. **The app doesn't call them in this spec.**
- **Web:** `DataSource` gains `getNotes()`, `saveNotes(notes)`, `saveBookmarks(bookmarks)` and
  `exportNotes(fileName, markdown)`. A `NotesStore` (the same shape as `ProgressStore`: one in-memory copy, subscribers
  and a write-behind queue) keeps the lesson page, the outline, Revise and the Notes tab in step without refetching.
- **Soft deletes** stay as tombstones; nothing is purged in this spec. Spec 010 decides when tombstones go.

### 3.10 Visual design
- This builds on the drafting-table identity. Content callouts are already UML notes, so the learner's own notes must
  look different from the author's: they are **redlines**, the markups a reviewer adds to a finished drawing.
- **Note tone:** one new token, `--tone-note`, kept for the learner's own marks: note chips, the «my note» stereotype on
  note cards and the outline mark. It is AA on `--surface` in light and dark mode, and is not used for anything else.
- **Bookmarks** use a ribbon glyph in ink. Colour is never the only signal: every mark has a text label for screen readers.
- This is the spec's signature: notes read as *your* pencil on *their* drawing. Everything else (sheet, lists, filters)
  stays quiet and uses existing parts (Base UI Drawer and Popover, sonner toasts).
- **Phone:** a thumb-sized **New note**. The editor sits above the keyboard (visual viewport), and the sheet uses
  `overscroll-behavior: contain`. Toasts sit above the tab bar.

### 3.11 UI quality
- It is built and critiqued with `frontend-design`, `emil-design-eng`, `apple-design`, `mobile-native`, `animate` and
  `break-ui`.
- `break-ui` cases:
  - 0, 1 and 100 notes on a lesson;
  - a 20,000-character note;
  - a 40-character tag;
  - notes on a removed section and on a removed lesson;
  - 320 px wide.
- Screenshots on phone (light and dark) and on desktop are reviewed and fixed before the PR.

## 4. Out of scope
- Syncing between devices, the `/sync` call, conflicts across devices and purging tombstones (spec 010).
- Searching notes (spec 009 indexes them).
- Highlights on selected text, a rich-text toolbar, images or attachments in notes.
- Sharing notes with other users, public notes, comments.
- Exporting bookmarks; import.
- Reminders and spaced repetition.

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | Notes can be created, edited, previewed and deleted on a whole lesson and on a section. Autosave keeps the text after an app restart. An emptied note is removed | Vitest + Playwright + manual (Windows, Android) |
| A2 | Everything in A1 and A4 works in airplane mode on Android and offline on Windows, with no network calls | Manual + device data tests |
| A3 | Section notes and bookmarks show beside the heading and in the outline. Tapping a note chip opens the sheet at that section, and the lesson keeps its scroll position | Vitest + Playwright |
| A4 | Lessons, sections and cheat sheets can be bookmarked and un-bookmarked, with undo; Notes → Bookmarks opens each in the right place | Vitest + Playwright |
| A5 | The Notes tab groups and orders as in §3.4. Tag and text filters combine. The empty states show | Vitest + Playwright |
| A6 | Tags parse per §3.5, with C# and TS agreeing on one shared test vector | Unit tests (.NET + Vitest) |
| A7 | Starters insert their outline and tag and put the cursor on the first blank | Vitest |
| A8 | Delete and bookmark removal can be undone within the toast time. The record is a soft delete | Vitest + store contract tests |
| A9 | Revise shows "Your notes (n)" under the right cheat sheets, folded; recall mode and "revised" are unchanged | Vitest + Playwright |
| A10 | The export file matches §3.7 (order, headings, removed lessons, no deleted notes): a download in the browser, the share sheet in the app | Unit tests (exporter) + Playwright (download) + manual (share sheet) |
| A11 | Guest notes and bookmarks are adopted at first sign-in by the merge rules in §3.8; two users on one device see only their own | Unit tests (merge) + integration test (adopt) + manual |
| A12 | The API endpoints require sign-in, touch only the caller's rows and enforce the limits | Integration tests (Aspire) |
| A13 | Notes on a renamed or removed section, or a removed lesson, are kept and shown as in §3.2 | Vitest + store tests |
| A14 | Schema V6 upgrades a V5 database; a content reload keeps all notes and bookmarks; progress is untouched | Unit tests (migrator, loader) + manual upgrade on Android |
| A15 | Note marks are AA in light and dark and have screen-reader labels; no sideways scroll at 320 px | Playwright + contrast check |
| A16 | All CI checks are green, with zero warnings and the guardrail lint clean | CI |

## 6. LLD hooks (worth noting in later lessons)
| Idea | Where |
|---|---|
| Repository + one contract suite (LSP) | `INotesStore`: in-memory, SQLite (device), EF Core (API) |
| Command (+ undo) | Delete and un-bookmark as commands with an inverse for the Undo toast |
| Observer | `NotesStore` subscribers (lesson, outline, Revise, Notes tab) |
| Specification | Notes-tab filters (tag AND text) as small composable predicates |
| Derived data, single source of truth | Tags parsed from the text, never stored |
| Soft delete / tombstones | `DeletedAt`, so spec 010 sync can carry deletes |
| Strategy | `DataSource.exportNotes`: download vs native share sheet |

## 7. Decisions (owner, 2026-10-20)
| Topic | Decision |
|---|---|
| Note anchor | Whole lesson or a section (several notes per lesson) |
| Bookmarks | Lessons, sections and cheat sheets |
| Editor | Markdown text box with autosave and Edit / Preview; no toolbar |
| Extras | Notes in Revise, export as Markdown, note starters, tags |
| Storage | Same as progress: device SQLite (V6) + API tables and endpoints, guest adopted at sign-in; no app API calls until spec 010 |
| Notes tab | Two lists, Notes / Bookmarks, grouped by module and lesson, with tag and text filters |
| Delete | Immediate, with an Undo toast; soft delete |
| Tags | Free `#tags` written in the text |

## 8. Open questions
None. Everything above follows the owner's answers. The visual signature (redlines, §3.10) and the section actions
button (§3.2) are proposals for the owner to review with this spec.

## 9. PRD changes when this spec is approved
- R18 points to this spec.
- New rows R102–R108:
  - notes per lesson / section;
  - bookmarks (three kinds);
  - Notes tab;
  - tags;
  - starters;
  - notes in Revise;
  - export.
- §10 `Note` / `Bookmark` gain `Anchor`, `Kind`, `CreatedAt`, `UpdatedAt` and `DeviceId`.
