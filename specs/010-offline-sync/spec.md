# Spec 010: Offline sync

| | |
|---|---|
| **Status** | Draft, awaiting owner review (the §8 questions carry recommendations) |
| **Branch** | `spec/010-offline-sync` |
| **Covers** | R13, R34, R44, R45, R87 (PRD §13); PRD F7, F8 (Sync button), §11 (offline, request budget); new R125–R132 (§9) |
| **Depends on** | Spec 001 (session, refresh tokens), spec 002 (server status, `canSync`, reachability, request budget), spec 005 (progress, device id, guest adoption), spec 007 (notes, bookmarks, soft deletes), spec 009 (notes index triggers) |
| **Case study** | Yes, **after the sync works**: "Design an offline-first sync engine", drawn from this code (`lld-case-study`, owner interview first). Tasks T18–T19 |

## 1. Goal
Your progress, notes and bookmarks follow you between devices. Read on the phone on the train, open Windows at home,
and you're at the same place with the same notes. Everything keeps working offline. Changes wait on the device and
go up with the next sync. Nothing you read or wrote is lost, even when two devices changed the same thing while both
were offline.

A sync is **one HTTPS request** (`POST /api/sync`) that sends this device's changes and brings back everyone else's.
There is **no polling**: sync runs on a few clear triggers (§3.5) so the app stays within ngrok's free limits (C7).
Content is bundled and never synced. Recent searches stay on the device (R122).

## 2. User stories
1. *As a learner with two devices,* I read sections 1–3 on my phone and 4–6 on Windows, both offline. After both
   sync, all six sections are ticked on both devices.
2. *As a learner,* I finish a lesson on Windows. When I open the phone later, the lesson is Done there too, and
   Continue points at the next lesson.
3. *As a learner,* I edit the same note on two devices while offline. After both sync, the later edit is the note and
   the other text is kept as a **conflict copy** next to it, so I can merge them by hand. Nothing is lost.
4. *As a learner,* I delete a note or remove a bookmark on one device and it disappears on the others.
5. *As a learner,* I reset a module on one device. The reset wins on every device, and nothing read before the
   reset comes back.
6. *As a learner,* in Profile I see when this device last synced and how many changes are waiting, and I can tap
   **Sync now**. While the server is offline the button is disabled and says why.
7. *As a learner,* when the server comes back online my waiting changes go up without me doing anything.
8. *As a learner,* if I sign out with changes not yet synced, the app tells me and offers **Sync now** first.
   My data stays on the device for my next sign-in.
9. *As a learner on a shared device,* only my own data syncs to my account. The device guest never syncs.
10. *As the owner,* the request count stays inside the ngrok free plan (§3.7), and I can see the count with
    `lld-admin status budget` (spec 002).

## 3. Scope

### 3.1 What syncs
| Record | Identity | Rule when two devices changed it (§3.4) |
|---|---|---|
| Lesson progress | (user, lesson) | **Merge** by field: nothing read is lost; a later reset or "Mark as not done" still wins |
| Activity day | (user, day) | Union (days are only ever added, R89) |
| Note | (user, note id) | Later edit wins + **conflict copy** of the other text |
| Bookmark | (user, kind, lesson, section) | Later edit wins (a later removal beats an earlier bookmark and the other way round) |

- Deletes travel as **tombstones** (`DeletedAt` from spec 007; a reset is already a write, spec 005).
- **Not synced:** content (bundled), recent searches (R122), reader settings (text size, theme), the device guest's
  data (it has no account; it joins a user at sign-in as today and syncs from then on).
- **Browser dev mode** already reads and writes the API directly. It has no sync and no Sync button. Its writes still
  reach the app on the next sync.

### 3.2 The sync call
- One endpoint, `POST /api/sync`, authenticated. Each call **pushes** this device's pending changes and **pulls**
  everything that changed on the server since this device's **cursor**.
- The cursor is a per-user **server version number**. The server stamps every write to a user's records with the
  next number. Device clocks are never used to decide what to pull.
- **Paging:** a request stays under the existing 1 MB limit. When there is more to push or pull, the call says so
  and the device calls again at once, within the same sync run. Only a first sync with a lot of data needs more than
  one page.
- **Safe to repeat:** if the response is lost (app killed, network drops), the next sync sends the same changes
  again. The result is the same: no duplicate notes, and no false conflict copies.
- **Edits during a sync:** a record edited while its push is in flight stays pending and goes up with the next sync.
- **Records the server refuses** (a limit or validation rule) come back with a reason and do not block the rest
  (§8 Q8 for what the learner sees).

### 3.3 First sync of a device
A device that has never synced (every device upgraded from spec 009, or a fresh sign-in) sends **all** of its
user's records and pulls everything the account has. The account may already hold data from browser mode or another
device. Both sides are combined with the §3.4 rules, so nothing on either side is lost. Fresh install + sign-in =
the device receives the account's full history.

### 3.4 Conflict rules
Two changes conflict when two devices change the same record without seeing each other's change first.

**Lesson progress: merge by field** (owner decision, story 1, 2, 5):
| Field | Rule |
|---|---|
| Read sections (ticks) | Union of both sides, **except** ticks from a side last changed before the latest reset of that lesson (story 5) |
| Status (+ how / when done) | The side whose **status changed** later wins. So reading a section on a stale device does not undo a Done made elsewhere, and a later "Mark as not done" or reset still wins |
| Reading position | From the side read more recently |
| Last read, last revised | The later of the two |
| Normalisation | A lesson with read sections is at least In progress. The reader's own auto-done rule (spec 005) runs as usual the next time the lesson is opened |

Two new fields make this possible: **`StatusChangedAt`** (when the status last changed) and **`ResetAt`** (when the
lesson was last reset). Existing records get them from their current data during the upgrade.
Known limit: a device that reads again after a reset elsewhere, without syncing in between, keeps its ticks from
before the reset too. This is rare and harmless: the ticks are real reading.

**Notes: later edit wins, plus a conflict copy** (owner decision, story 3):
- Each note remembers the server version it was last synced at. When a device pushes an edit based on an older
  version, and another device changed the note since, that is a conflict.
- The later edit (`UpdatedAt`) becomes the note. If the losing side has text that is not in the winner (including
  an edit that lost to a later delete), that text is saved as a **new note on the same lesson and section**, starting
  with one line: `Conflict copy · from <device> · <date time> #conflict`. The `#conflict` tag lets the learner find
  copies in the Notes tab and in Search. The learner merges by hand and deletes the copy (Undo as usual).
- `<device>` is a short label the app sends with each sync: model and platform, e.g. *Pixel 8 (Android)* or
  *Windows PC*. No personal device names.
- Conflict copies never fail the note limits (spec 007): losing text is always kept.
- Resending your own edit (§3.2 repeat) is never a conflict.

**Bookmarks:** later edit wins. **Activity days:** union.

**Clocks:** `UpdatedAt`, `StatusChangedAt` and `ResetAt` come from device clocks. The server guards against a clock
far in the future (§8 Q7). Ties are broken by device id, so every device reaches the same result.

### 3.5 When sync runs (no polling)
Only for a signed-in user, only when the server status allows it (`canSync`, spec 002), one sync at a time:
| Trigger | Condition |
|---|---|
| App opened or resumed | Last successful sync is older than the threshold (§8 Q1), or there are pending changes |
| **Sync now** | Always (button disabled while offline) |
| App goes to the background | There are pending changes, throttled (§8 Q2) |
| Server status becomes online | There are pending changes, or the threshold has passed (story 7, R45) |
| Sign-in | Always (first sync, §3.3) |

- **No timers.** An edit does not start a sync by itself. It waits for one of the triggers.
- **Failures:** a network failure marks the server unreachable (spec 002 reachability), which disables Sync until
  status says online. A 429 or 5xx backs off (30 s, then 2 min, then 10 min) before the next automatic try. "Sync now"
  is always allowed while online.
- **Session expired** (refresh token revoked, "Log out everywhere", password changed elsewhere): Profile shows
  "Sign in again to sync". Data and pending changes stay on the device.

### 3.6 What the learner sees
- **Profile → Sync** row (signed in, app only):
  - status line: *Synced 3 min ago*, *2 changes waiting*, *Syncing…*, *Server offline: changes will sync when it's back*,
    *Sign in again to sync*;
  - **Sync now** button: disabled with the reason while offline or syncing;
  - problems, if any records were refused (§8 Q8).
- Signed out: *Sign in to sync your progress and notes across devices.*
- After a sync that brought changes, open screens (Learn, module, lesson, Revise, Notes, Profile) update in place:
  ticks, status, notes, bookmarks. A note open in the notes sheet with unsaved typing is never overwritten under the
  cursor. If it changed elsewhere, the conflict rule above applies on the next sync.
- **Sign out** with pending changes: a sheet says "*n* changes on this device haven't synced yet. They'll sync the
  next time you sign in here." with **Sync now** (when online) and **Sign out anyway**. Data stays on the device
  (owner decision).
- The existing "Server offline" banner (spec 002) is unchanged.

### 3.7 Request budget (C7: 20,000 requests and 1 GB per month)
Estimate with the §8 recommendations. One active learner on one day:
| Request | Per active day |
|---|---|
| Session restore at a cold start (spec 001, exists today) | ~1.5 |
| Sync on open (first open of the day; carries the session refresh, Q3) | ~1 |
| Background sync with pending changes (throttled, carries the refresh, Q3) | ~1.5 |
| **Total** | **~4** |

- 150 daily-active learners (30 % of 500 users) × 4 × 30 days ≈ **18,000 requests/month**: inside the limit, with
  the spec 002 quota guard (R63) as the backstop. The plan also folds the cold-start restore into the open sync
  when one is due (≈ 3 per day, ≈ 13,500/month).
- Data: a typical sync is under 5 KB each way. A first sync of a heavy account is at most a few MB. Well under
  1 GB.
- The manual run records the real count per sync (A11) and the plan (§5) records the final estimate.

### 3.8 Storage changes
- **API** (migration `AddSync`): a server `Version` on progress, activity day, note and bookmark rows; a per-user
  version counter; `StatusChangedAt` and `ResetAt` on progress; a small `SyncDevices` table (user, device id,
  label, last sync) used for conflict-copy labels.
- **Device** (schema **V8**): `StatusChangedAt` and `ResetAt` on progress; per record a **pending** flag and the
  server version last seen; a `SyncState` row per user (cursor, last sync, last error). The upgrade marks every
  user record pending, so the first sync (§3.3) sends it.
- Browser mode endpoints (`/api/progress`, `/api/notes`, `/api/bookmarks`) stamp versions too, so their writes are
  pulled by devices, and use the same merge rules.

### 3.9 Quality
- One set of conflict rules in `Lld.Domain`, used by the API and by the device. Pure functions with table tests.
- Two-device scenarios run end to end in integration tests: two simulated devices (real device SQLite stores)
  against the real API (Aspire.Hosting.Testing).
- No new UI library. The Profile row and sign-out sheet follow the existing design system. Critique with the UI
  skills: `emil-design-eng`, `mobile-native`, `break-ui` (long device labels, 999+ waiting changes, 320 px).

## 4. Out of scope
- Live updates between devices (push notifications, WebSocket). Changes arrive on the triggers in §3.5.
- Real-time co-editing or a merge UI for notes (conflict copies are merged by hand).
- Syncing content, settings or recent searches. Syncing the device guest.
- A device list in Profile ("your devices", sign out a device).
- Account deletion and data export from the server.
- Changing browser dev mode to work offline.
- iOS / macOS builds (Phase 2). The code is cross-platform. Only Windows and Android are verified.

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | Story 1 and 2: ticks from two offline devices are combined; a Done on one device is not undone by reading on a stale one; a later "Mark as not done" wins | Domain table tests + two-device integration test |
| A2 | Story 5: a reset on one device wins; ticks from before the reset don't come back | Domain table tests + integration test |
| A3 | Story 3: concurrent note edits → later edit wins + one conflict copy with the label line and `#conflict`; an edit that loses to a delete is kept as a copy; resending your own edit makes no copy | Domain tests + integration test |
| A4 | Deletes and bookmark removals sync; activity days are combined | Integration test |
| A5 | A lost response followed by a resend gives the same server state (no duplicates, no copies); an edit made during a sync stays pending | Integration test + device store tests |
| A6 | First sync of a device with existing data and an account with browser-mode data: nothing lost on either side | Integration test + manual (Android V7 → V8 upgrade) |
| A7 | Triggers fire only as §3.5 (open/resume threshold, Sync now, background throttle, status → online, sign-in); no timer-based sync; one sync at a time; back-off on 429/5xx | Unit tests with a fake clock |
| A8 | Offline: Sync now disabled with the reason; edits pile up as pending; status → online sends them with no tap | Vitest + manual (Android airplane mode, stop/start the API) |
| A9 | Sign-out with pending changes warns and offers Sync now; data stays; a second user on the device never receives or sends the first user's records; the guest never syncs | Vitest + integration test |
| A10 | Expired session shows "Sign in again to sync" and keeps data; the next sign-in syncs it | Integration test + Vitest |
| A11 | **Two real devices:** read, note, bookmark and reset on Android (airplane mode) and Windows; after sync both show the same state; requests per sync = 1 (`status budget` before/after) | Manual run log + screenshots |
| A12 | Paging: an account with 5,000 notes completes a first sync in pages under 1 MB | Integration test |
| A13 | After a pull, open screens update; pulled notes are searchable at once (spec 009 triggers) | Vitest + device test |
| A14 | All CI checks green, zero warnings, guardrail lint clean | CI |

## 6. LLD hooks (for the case study)
| Idea | Where |
|---|---|
| Outbox / pending flag | device rows marked pending, cleared only when the server acknowledged that exact edit |
| Version cursor (logical clock) | per-user server version instead of timestamps for "what changed since" |
| Strategy (conflict resolvers) | one resolver per record type: field merge, later-wins + copy, later-wins, union |
| LWW registers + grow-only set with a reset epoch | the progress merge, field by field |
| State pattern | sync status: Idle, Syncing, Offline, NeedsSignIn, BackingOff |
| Observer | server status and app lifecycle → sync triggers; sync → React stores reload |
| Specification / policy object | "should sync now?" from trigger, threshold, throttle, status, pending count |
| Idempotency | resend after a lost response gives the same state |
| Port + adapters, one contract suite (LSP) | the existing stores plus a sync ledger port on device and API |

## 7. Decisions (owner, 2026-10-21)
| Topic | Decision |
|---|---|
| Progress conflicts | Merge: ticks combined, furthest status unless a later reset or "Mark as not done" (§3.4), not plain last-write-wins |
| Note conflicts | Later edit wins and the other text is kept as a conflict copy tagged `#conflict` |
| Sign-out | Keep data on the device; warn about unsynced changes and offer Sync now |
| Case study | Yes, written after the sync works, quoting real code; owner interview first |
| Process | Spec, plan and tasks first; no code until the owner says to start |

## 8. Questions for the spec review
Each has a recommendation. Say "approved" to take all of them, or change any.

| # | Question | Recommendation | Options not taken |
|---|---|---|---|
| Q1 | Sync on open / resume: threshold since the last sync | **6 hours** (pending changes always sync). PRD F7 says 15 min, which costs several syncs a day per learner | 15 min (PRD); 1 hour |
| Q2 | Background sync throttle | With pending changes, **at most once per 30 min**. Changes left over go with the next trigger | Every time the app goes to the background |
| Q3 | Session refresh | The sync call **carries the refresh token when the access token has expired** and returns the new pair, so a sync is always one request. At a cold start with a sync due, the sync also restores the session | Separate refresh request before a sync (2 requests); longer access tokens (12 h) |
| Q4 | Where sync shows | **Profile → Sync row** only (status line, Sync now, problems). The existing offline banner stays | Also a small sync indicator in the header / sidebar |
| Q5 | Changes pulled from other devices | Screens **update quietly** | A toast "Updated from your other devices" |
| Q6 | Tombstones | **Kept forever** (tiny). A deleted note's text is cleared on the server 30 days after the delete | Purge after 90 days, with a full re-download for devices not synced in that time |
| Q7 | A device clock in the future | The server **treats any time more than 5 min ahead of its own clock as "now"** | Trust device clocks; hybrid logical clocks |
| Q8 | A record the server refuses | **Shown in the Sync row** ("1 note couldn't sync: too long. Open"), not retried until it is edited | Retried silently on every sync |
| Q9 | Conflict copy label | **First line of the copy's text**: `Conflict copy · from Pixel 8 (Android) · 21 Oct 14:02 #conflict` (searchable, tag filter works, no new field) | A separate badge field on the note |

## 9. PRD changes (added to §13 when the spec is approved)
| # | Requirement |
|---|---|
| R125 | One combined `POST /api/sync` (push + pull, per-user server version cursor, pages under 1 MB, safe to repeat); browser mode keeps its direct endpoints, which also stamp versions |
| R126 | Progress across devices merges by field: ticks combined except before the latest reset; status from the later status change; position from the later read; dates latest |
| R127 | Concurrent note edits: the later edit wins and the other text is kept as a conflict copy tagged `#conflict`; bookmarks later-wins; activity days union |
| R128 | Sync triggers: open / resume over the threshold, Sync now, background with pending changes (throttled), status → online, sign-in; no polling; back-off on 429 / 5xx |
| R129 | Profile Sync row (last sync, waiting changes, Sync now disabled with the reason while offline, problems); sign-out warns about unsynced changes and keeps data |
| R130 | Tombstones and clock guard per Q6 / Q7 |
| R131 | Request budget: one request per sync (refresh carried by the sync, Q3); estimate in §3.7 |
| R132 | Case study "Design an offline-first sync engine" from this code, after the sync works |
