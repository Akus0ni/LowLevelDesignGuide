# Spec 005: Progress

| | |
|---|---|
| **Status** | Draft, open questions resolved (2026-10-19); waiting for owner review |
| **Branch** | `spec/005-progress` |
| **Covers** | R17 (PRD §13); PRD F4, §10 (`LessonProgress`), §11 (offline) |
| **Depends on** | Spec 003 (reader: section ticks, reading position), spec 004 (OOP & SOLID module), spec 001 (users) |
| **Case study** | None planned. Optional mini case study "Progress tracking" later, only if the owner asks (`lld-case-study`). |

## 1. Goal
Show learners where they are and get them back into the right lesson in one tap. A lesson moves from **Not started**
to **In progress** to **Done**, either on its own when every section has been read or when the learner marks it.
Each module and the whole curriculum show % complete. Learn opens with **Continue: <lesson>**. The Revise page
shows when each cheat sheet was last revised.

Progress belongs to a **user**. Signed-out reading is tracked as a **guest** on the device and handed to the first
user who signs in there. It is stored where the app already keeps its data: the device SQLite in the MAUI app, and
the API (per user) in browser mode. Syncing between devices stays in spec 010. This spec makes the data ready for it
(`UpdatedAt` and `DeviceId` on every record).

## 2. User stories
1. *As a learner,* when I open Learn I see "Continue: Liskov Substitution · 60% read" and one tap puts me back at the
   line I left.
2. *As a learner,* when I've read every section of a lesson it is marked **Done** for me. I can also mark it done (or
   not done) by hand.
3. *As a learner,* I see % complete for each module and overall, and which lessons are done, in progress or not
   started, on Learn, on the module page and in Profile.
4. *As a learner revising before an interview,* the Revise page tells me which lessons I've finished, when I last
   revised each cheat sheet, and lets me filter to "not revised yet".
5. *As a learner,* I see which days this week I studied, so I keep going.
6. *As a learner,* I can reset my progress for one module or for everything, after confirming.
7. *As a learner,* finishing a lesson or a whole module gets a small, quick celebration that I can skip and that
   respects reduced motion.
8. *As a learner on a shared device,* I start reading without signing in. When I sign in, what I read becomes mine.
   Another person who signs in on the same device sees only their own progress.
9. *As the owner,* reading on the device never needs the server: progress works fully offline (PRD §11).

## 3. Scope

### 3.1 What is tracked (per user, per lesson)
| Field | Meaning |
|---|---|
| `Status` | `NotStarted` (no record) → `InProgress` (lesson opened) → `Done` |
| `DoneBy` | `Auto` (all sections read) or `Manual` (learner tapped) |
| `CompletedAt` | When it became Done (cleared on unmark) |
| `ReadSections` | Anchors of the sections already read (the outline ticks, moved here from localStorage) |
| `Position` | Reading position (`anchor` + `offset`, moved here from localStorage) |
| `LastReadAt` | Last time the lesson was read; drives "Continue" |
| `LastRevisedAt` | Last time its cheat sheet was revised (§3.5) |
| `UpdatedAt`, `DeviceId` | For spec 010's last-write-wins sync |

Activity days for the activity strip (§3.6): one row per local calendar day on which the learner read a lesson or revised a cheat sheet.
Text size and recall-mode preference stay **device preferences** in localStorage (not progress).

### 3.2 Status rules
- Opening a lesson makes it **In progress** (if it was Not started).
- **Auto done:** when the last unread section is ticked, the lesson becomes Done (`DoneBy = Auto`).
- **Manual:** the Done control at the end of the lesson (§3.8). Unmarking
  returns it to In progress; the read ticks are kept.
- **Unmark wins:** after a manual unmark, auto done does not fire again until a section is newly ticked.
- A lesson removed from content later is ignored in all counts. Its record is kept, so it comes back if the lesson
  returns.

### 3.3 Where progress shows
| Place | What |
|---|---|
| **Learn** (top) | **Continue** card, the first thing on the page: a small class box with the amber "you are here" pin, the lesson's icon, title, module and "3 of 5 sections read". The whole card is the tap target. It names the most recently read In-progress lesson; if none, the next Not-started one in curriculum order ("Start: …"); hidden when everything is Done. One mono line inside it gives the overall count ("23 of 61 lessons done"). No separate stats row. |
| **Learn** (diagram) | Module boxes are "pencilled" until done and "inked" as lessons finish (§3.11). The count sits in the box corner in JetBrains Mono, like a UML multiplicity (`7/15`). Lesson members carry the shared status mark. |
| **Module page** | The same box treatment and `n/m done`. Each lesson row shows the status mark; In progress also shows "3 of 5 sections". |
| **Lesson page** | Done control at the end of the lesson (§3.8); outline ticks as today, now per user. |
| **Profile** | Progress summary: overall count, one row per module with its count, the activity strip (§3.6), **Reset progress** (§3.7). |
| **Revise** | Status mark per cheat sheet, "Revised 3 days ago" / "Not revised yet", and an **All / Not revised** segmented control. |

% complete = Done lessons / lessons in the module (or in all modules). It is shown as counts (`7/15`), not percents,
so it never gets mixed up with "sections read". "Start here" and case studies count like any other module.

### 3.4 "Continue where you left off"
Tapping Continue opens the lesson at its saved position (existing spec 003 restore logic, now reading from the
progress store). Opening a lesson from anywhere else behaves as today.

### 3.5 Revised
A cheat sheet counts as **revised** when the learner opens it and either reaches its end or reveals every block in
recall mode. Only the latest date is stored. No counts and no scheduling (spaced repetition is out of scope).

### 3.6 Activity strip (instead of a streak)
A quiet **activity strip** in Profile, not a 🔥 counter on Learn. Seven small mono
boxes `M T W T F S S` for this week, filled for each day with reading or revising, and "4 days this week". It never
resets to 0 or punishes a missed day. Nothing on Learn, no reminders, no notifications.

### 3.7 Reset progress
Profile → Reset progress → choose a module or "Everything" → a confirm sheet (bottom sheet on phones; Android Back
closes it) that names what will be cleared. The button names the scope ("Reset OOP & SOLID"); the toast then says
"OOP & SOLID progress reset". This is the only confirmation in the feature, because it is the only destructive action.
It clears
statuses, ticks, positions and revised dates for those lessons. Activity days are kept.
Each reset record keeps `UpdatedAt`/`DeviceId`, so spec 010 can sync it (a reset is a write, not a silent delete).

### 3.8 Done control and celebration
- **Control:** at the end of the lesson, just above Next up. Not done: a blueprint-blue button **Mark as done**.
  Done: an inked **✓ Done** state with a quiet **Mark as not done** link beside it. No confirmation: the toggle is the undo.
  44 px minimum target, press feedback on `:active` (`scale(0.97)`, 120 ms), no text selection on long-press.
- **Feedback sits where the cause is:** the reader is at the end of the lesson both when they tap the button and when
  auto done fires (the last section ends there), so the control itself shows the change. The check draws in
  (`clip-path` reveal, 240 ms, strong ease-out) and the button turns into the Done state. **No toast for a lesson**,
  because it would repeat what is already on screen.
- **Module complete** (rare, so this is where delight goes): the next time the module box is shown on the module page,
  its frame inks from pencil to ink and a `«complete»` stamp, styled like the REMEMBER stamp, presses in once
  (≤ 600 ms, the same budget as the Learn intro). A toast "OOP & SOLID complete" appears only when it happens away
  from that page.
- Each moment plays **once** per lesson or module (stored), so unmarking and marking again does not replay it.
- Learn and module boxes don't animate their counts on every visit (they're opened many times a day). Counts render
  in place.
- Reduced motion: no drawing or pressing, only a short opacity fade. No sound, no confetti.

### 3.9 Guest and users
- Signed out → progress is stored under a **guest** owner on this device.
- First sign-in of a user on this device → guest progress is **adopted** by that user and merged with what the user
  already has on this device: for each lesson the furthest status wins (Done > In progress), ticks are combined, and the
  newest position and dates are kept. The guest is then empty.
- Signing out keeps that user's progress on the device (hidden). A later guest starts empty. Signing back in shows it again.
- Browser mode: signed out = guest in localStorage. Signed in = the API. Adoption sends the guest data to the API once.

### 3.10 Storage and data access
- **Device (MAUI):** new tables in the device SQLite (`lesson_progress`, `activity_day`) through a new schema step
  (V4), plain `Microsoft.Data.Sqlite` (R67). Content reloads must never touch these tables. Reads and writes go through
  the bridge; no network.
- **API:** `LessonProgress` + `ActivityDay` entities (EF Core), endpoints for the signed-in user only:
  `GET /api/progress`, `PUT /api/progress/lessons/{lessonId}`, `POST /api/progress/activity`,
  `POST /api/progress/adopt`, `DELETE /api/progress?module=` (reset). Used by browser mode now and by the spec 010 sync later.
  The app does **not** call them in this spec (no request-budget impact).
- **Web:** `DataSource` gains progress methods. A `ProgressStore` in React keeps one in-memory copy, so Learn, Module,
  Revise and Profile update together without refetching. Writes from scrolling are debounced (as today).
- **Migration:** existing localStorage ticks and positions (spec 003) are imported once into the guest progress on first
  run, then removed.

### 3.11 Visual design: "pencil, then ink"
Draws on the drafting-table identity (spec 003 §3.7). An engineering drawing starts in pencil and is inked once it's
settled, and progress is shown the same way. **This is the signature of the spec; everything else stays quiet.**
- **One status mark, used everywhere** (Learn, module page, Revise, Profile), so the same thing always looks the same:
  - Not started: lesson title in muted "pencil" text, hairline mark `○`.
  - In progress: blueprint-blue half mark `◐`. The *Continue* lesson alone also gets the amber "you are here" pin
    (amber stays reserved for that).
  - Done: full ink text and an inked `✓`.
  Marks are JetBrains Mono glyphs with an accessible label ("Done", "In progress", "Not started"); colour is never the
  only signal.
- **Module boxes:** hairline frame while unfinished; the top «module» compartment fills with fine blueprint hatching in
  proportion to lessons done; at 100% the frame becomes full ink weight. Dependency arrows stay dashed. Unfinished
  boxes use a lighter solid rule, not a dashed one, so they never look like arrows.
- **Copy:** user words, not internals: "Done", "Mark as done", "Mark as not done", "Continue", "Start", "Not revised
  yet", "Reset progress". "Pencil" and "ink" are only the visual idea; they never appear in the UI.
- **Phone:** Continue card and Done control are full-width, thumb-sized targets; toasts sit above the tab bar and
  clear of safe areas; the reset sheet uses `overscroll-behavior: contain`.

### 3.12 UI quality
Built and critiqued with `frontend-design`, `emil-design-eng`, `apple-design`, `mobile-native`, `animate` and
`break-ui` (0 / 1 / all done, 100 lessons, long titles, every module complete, a new module added after 100%).
Screenshots in phone light / dark and on desktop are reviewed and fixed before the PR. The spec-stage critique is in §9.

## 4. Out of scope
- Syncing progress between devices, the combined `/sync` call, conflict handling across devices (spec 010).
- Notes and bookmarks (spec 007); search (spec 009).
- Quizzes, scores, spaced-repetition scheduling, reminders and push notifications.
- Leaderboards or any view of other users' progress; admin progress reports.
- Time-spent tracking.

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | Opening a lesson marks it In progress; reading every section marks it Done (Auto); Mark / unmark works and unmark is not undone by auto | Vitest + Playwright |
| A2 | Module and overall % are correct for 0, some and all lessons done, and ignore lessons no longer in the content | Unit tests (calculator, .NET + TS where duplicated) |
| A3 | Continue opens the right lesson at the saved position; it shows the next Not-started lesson when nothing is in progress and hides when all are done | Vitest + Playwright |
| A4 | Progress survives an app restart on Windows and Android, and works in airplane mode | Manual (T-run) + device data tests |
| A5 | Two users on one device see only their own progress; guest progress is adopted on first sign-in with the merge rules in §3.9 | Unit tests (merge) + integration test (API adopt) + manual |
| A6 | API progress endpoints require auth and only touch the caller's rows | Integration tests (Aspire) |
| A7 | Revise shows done ticks, "revised" dates and the "Not revised yet" filter; revised is recorded per §3.5 | Vitest + Playwright |
| A8 | The activity strip marks the right local days for this week, across midnight and a week change, and never shows a "streak lost" state | Unit tests (fake clock) |
| A9 | Reset (module / all) clears only what it says, after confirmation | Vitest + integration test |
| A10 | The Done check (≤ 240 ms) and the module-complete moment (≤ 600 ms) each play once; reduced motion gives a fade only; Learn counts never animate on a normal visit | Vitest + manual |
| A14 | The status mark looks and reads the same on Learn, module page, Revise and Profile; it has a text label for screen readers; AA contrast in light and dark | Vitest + Playwright + contrast check |
| A11 | A content pack reload (new pack version) keeps all progress; schema V4 upgrades a V3 database | Unit tests (migrator, loader) + manual upgrade on Android |
| A12 | Old localStorage ticks and positions are imported once | Vitest |
| A13 | All CI checks green; zero warnings; guardrail lint clean | CI |

## 6. LLD hooks (worth noting in later lessons)
| Idea | Where |
|---|---|
| State pattern / state machine | `LessonStatus` transitions (Not started → In progress → Done, unmark) |
| Repository + ports | `IProgressStore` with SQLite (device), EF Core (API) and in-memory implementations, one contract test suite (LSP) |
| Strategy | `DataSource` progress methods: HTTP vs bridge |
| Observer | `ProgressStore` subscribers (Learn, Module, Revise, Profile) |
| Pure functions / value objects | Progress calculator, activity-strip calculator, merge rule (easy to test) |

## 7. Decisions (owner, 2026-10-19)
| Topic | Decision |
|---|---|
| Done rule | Auto (all sections read) + manual mark / unmark |
| Signed out | Guest progress on the device, adopted by the first user who signs in; per-user on shared devices |
| Placement | Learn (Continue + %), module page, Profile, Revise |
| Storage | Device SQLite + API `LessonProgress` table and per-user endpoints; sync in spec 010 |
| Revise | Track "last revised" per cheat sheet; filter "not revised yet" |
| Statuses | Not started / In progress / Done |
| Ticks and position | Move from localStorage into the per-user progress store |
| Extras | Lean scope, plus an activity strip (instead of a streak), reset progress and a done celebration |
| Activity | A quiet 7-day strip in Profile that never resets; recorded in the PRD as the one allowed exception to "no gamification" (§3.6) |
| Unmark | Auto done stays off after a manual unmark until a section is newly ticked |
| Overall count | All modules count, including "Start here" and case studies |
| Revised | End of the cheat sheet reached, or every block revealed in recall mode |
| Visual design | "Pencil, then ink" (§3.11) after the spec-stage critique (§9) |

## 8. Open questions
None. Resolved on 2026-10-19 (see §7).

## 9. Design critique (spec stage, 2026-10-19)
Skills applied: `frontend-design`, `emil-design-eng`, `apple-design`, `animate`, `mobile-native`. Changes already
made above:

| Before | After | Why |
|---|---|---|
| Thin % bars under module titles | "Pencil, then ink": hatching fills the «module» compartment, frame inks at 100%, `7/15` as a multiplicity | A generic progress bar ignores the drafting-table identity; this way progress reads as part of the drawing (frontend-design: ground it in the subject) |
| Continue card + overall % + streak in a row on Learn | Continue card only; overall count as one mono line inside it | One clear job per screen; the "big number + stats" row is a template default (apple-design: simplicity, hierarchy; Chanel rule) |
| "60% read" on Continue next to module % | "3 of 5 sections read"; module progress as counts | Two different percentages side by side confuse; specific beats clever |
| 🔥 streak counter on Learn and Profile | Quiet 7-day activity strip in Profile only | Conflicts with the no-gamification rule; a loss-based counter adds pressure, not learning |
| Toast for every lesson done | Feedback on the Done control itself; toast only for module complete away from its page | Causality and utility: feedback where the cause is, no duplicate signals (apple-design §13) |
| Celebration "under 400 ms" for all | Check 240 ms; module-complete ≤ 600 ms, plays once; counts never animate on normal visits | Frequency gate: lesson done is occasional, module done is rare; Learn is opened tens of times a day (animate §1) |
| "Done ✓ (undo)" label | "Mark as done" ⇄ "✓ Done" + "Mark as not done" | An action keeps the same name across the flow; no jargon (frontend-design: writing) |
| Generic confirm dialog for reset | Bottom sheet on phones, scope in the button ("Reset OOP & SOLID"), Back closes it | Only destructive action gets a confirmation; native sheet behaviour (apple-design agency, mobile-native) |
| Unfinished boxes drawn dashed | Lighter solid rule | Dashed lines already mean "dependency" on Learn |
| Status only by colour | Glyph `○ ◐ ✓` + label + colour | Accessibility; familiarity: same mark everywhere |
