# Spec 010: Tasks

No code until the owner approves the spec and says to start (2026-10-21).

- [ ] T1 Domain: `LessonProgress` + `StatusChangedAt` / `ResetAt`, `ProgressLimits` checks; `Lld.Domain/Sync`: `ProgressSync.Merge`, `NoteSync.Resolve`, `ClockGuard`, `ConflictCopyText` + shared vector `tests/fixtures/sync/conflict-copy.json`; table tests (A1, A2, A3)
- [ ] T2 Contracts + wire: `SyncContracts` (request, response, acks, rejected, session), `SyncWire`, `LessonProgressDto` fields, `types.ts`; mapping tests
- [ ] T3 API storage: migration `AddSync` (versions, counters, `SyncDevices`, backfill); `EfSyncLedger` (stamp, cursor pull, paging, echo suppression, conflict copies); existing EF stores stamp versions and use `ProgressSync.Merge`; contract suites green (A4, A12)
- [ ] T4 API endpoint: `SyncUserData` use case (per-record rejection, clock clamp, label upsert), `POST /api/sync` with bearer or refresh token in the body (Q3), rate limits; tombstone body clean-up in the nightly job (Q6); API + application tests
- [ ] T5 Device storage: schema `V8Sync` (columns, `SyncState`, backfill, all pending), stores set `Pending = 1` on every write; `SqliteSyncLedger` (read pending pages, apply acks only if unchanged, merge pending rows on pull, save cursor); device tests incl. V7 → V8 upgrade and notes index after pull (A5, A6, A13)
- [ ] T6 App engine: `SyncPolicy` (Q1, Q2), `SyncEngine` (single-flight, pages, back-off, state machine, session hand-over), `SyncStatus`; unit tests with a fake clock and fake server (A7)
- [ ] T7 App wiring: triggers in `App` (start, resume, sleep + 1.5 s), status → online, sign-in / restore (cold-start fold, Q3), device label; sign-out pending check
- [ ] T8 Integration: two simulated devices against the real API (Aspire): stories 1–5, resend after a lost response, first sync with browser data, second user, expired session, 5,000-note paging (A1–A6, A9, A10, A12)
- [ ] T9 Bridge + data: `GetSyncStatus`, `SyncNow`, `GetPendingCount`, events `syncStatus` / `dataChanged`; `DataSource` in bridge / HTTP / fake; tests
- [ ] T10 Web stores: reader sets `statusChangedAt` / `resetAt`; `ProgressStore` / `NotesStore` reload on `dataChanged` (flush first, keep text being typed); Vitest (A13)
- [ ] T11 Web UI: Profile Sync row (status line, Sync now, rejected list), sign-out sheet; wording via `ux-copy`; Vitest (A8, A9, A10)
- [ ] T12 UI pass with the skills: screenshot critique (phone light / dark, desktop) + `break-ui` worst cases (long device label, 999+ waiting, 320 px); fixes
- [ ] T13 Playwright `e2e/sync.spec.ts` (phone + desktop, fake data source)
- [ ] T14 Manual run: Android (airplane mode, V7 → V8 upgrade keeps data, offline edits then online auto-sync) + Windows; two-device scenario; `status budget` before / after (A8, A11); screenshots
- [ ] T15 Budget: record real requests per sync and per day of use; update spec §3.7 estimate in plan §5
- [ ] T16 Docs: PRD F7 / R125–R132 / §10 data model, `docs/plan.md` roadmap + budget row, `docs/hosting.md` (sync in `status budget`), plan §5 outcomes, progress log
- [ ] T17 Claude decisions log for anything delegated during the build (`docs/claude-decisions.md`)
- [ ] T18 Case study interview with the owner (`lld-case-study`): scope, depth, own real-work sync stories
- [ ] T19 Case study "Design an offline-first sync engine" (`content/case-studies/offline-sync/`), quoting the real code; validated by `lld-content`; added to the PR
- [ ] T20 PR with the acceptance-criteria table (code, docs and case study)
