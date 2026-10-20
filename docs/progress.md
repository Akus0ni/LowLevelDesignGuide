# Progress log

The single place to see where the project stands and what comes next. Update it at the end
of every spec (or session) so the next session can pick up without re-reading history.

_Last updated: 2026-10-21 (spec 008 merged; spec 009 spec drafted, awaiting owner review)_

## Where we are
| Item | Status |
|---|---|
| PRD | v0.10 (spec 008 decisions R109–R116) |
| Plan | merged (PR #1); Phase 2 Apple findings added in PR #2 (`docs/plan.md` §8) |
| `main` protection | Ruleset "Protect main": PR + 1 approval (admin bypass in PR), squash only, no direct push; **required checks: `backend`, `web`, `maui-windows-android`** |
| Spec 000 Foundation | ✅ Merged (PR #2) |
| Spec 001 Auth & access | ✅ Merged (PR #3) |
| Spec 002 Self-hosting | ✅ Merged (PR #4); owner checks A1/A2/A3/A6/A10 done |
| Spec 003 Content engine | ✅ Merged (PR #5) |
| Spec 004 OOP & SOLID | ✅ Merged (PR #6) |
| Spec 005 Progress | ✅ Merged (PR #7) |
| Spec 006 Design patterns | ✅ Merged (PR #8); Android upgrade check done after merge (docs PR) |
| Spec 007 Notes & bookmarks | ✅ Merged (PR #12) |
| Spec 008 UML | ✅ Merged (PR #13) |
| Spec 009 Search | 📝 `specs/009-search/spec.md` drafted, awaiting owner review (open questions Q1-Q10) |
| Decisions by Claude | `docs/claude-decisions.md`: CD-001..CD-008, ⏳ awaiting owner review |
| Hosting | ✅ Installed on the owner PC: services `LldGuideApi` + `ngrok` running, `https://doodle-diocese-revenue.ngrok-free.dev/health` OK, signed status on ntfy verified |

## Spec roadmap status
| # | Spec | Status |
|---|---|---|
| 000 | Foundation | ✅ merged (PR #2) |
| 001 | Auth & access (Case Study #1) + admin console | ✅ merged (PR #3) |
| 002 | Self-hosting & server status (ngrok, ntfy) | ✅ merged (PR #4) |
| 003 | Content engine (reader) | ✅ merged (PR #5) |
| 004 | OOP & SOLID content | ✅ merged (PR #6) |
| 005 | Progress | ✅ merged (PR #7) |
| 006 | GoF + Enterprise patterns content | ✅ merged (PR #8) |
| 007 | Notes & bookmarks | ✅ merged (PR #12) |
| 008 | UML content | ✅ merged (PR #13) |
| 009 | Search | 📝 spec in review |
| 010 | Offline sync | planned |
| 011+ | Case studies (ask owner first each time) | planned |
| P2 | Admin UI, **Apple builds** (`docs/plan.md` §8), optional cloud | deferred |

## Spec 000: what was delivered
- .NET 10 solution with layered libraries, `Lld.Api` (`/health`, `/alive`, `/api/info`), Aspire 13 AppHost, service defaults.
- React shell (`web/`): bottom tabs on phones / sidebar on desktop, `DataSource` strategy (HTTP vs native bridge), Tailwind v4 design guardrails enforced by lint.
- MAUI app (`Lld.App`): full-bleed HybridWebView, `BridgeService`, status bar colour, web bundle built and packaged automatically.
- Verified: browser (real API), Windows app (bridge → `WinUI`), Android emulator (bridge → `Android`). Screenshots in `specs/000-foundation/screenshots/`.
- Tests: 2 API unit, 2 Aspire integration, 18 Vitest, 2 Playwright. CI: `backend`, `web`, `maui-windows-android` green; Apple CI deferred to Phase 2 (manual-only `ci-apple.yml`).
- Docs: README "Local setup" (every manual dependency), ADR 0001 (HybridWebView + React), ADR 0002 (styling guardrails), spec 000 `plan.md` §4 outcomes.

## Key learnings (details in `specs/000-foundation/plan.md` §4)
- Android WebView reports `env(safe-area-inset-*)` as 0 → native `SafeAreaEdges.All` on Android.
- `Lld:AndroidEmulatorId` is an adb serial (empty = running emulator); Aspire does not boot emulators.
- Untrusted dev cert → health check and Vite proxy use the API's `http` endpoint; `aspire certs trust` for the dashboard.
- Never use `$(TargetFrameworks.TrimStart(...))` in `Lld.App.csproj`: it breaks Android RID restore.
- .NET 10 `dotnet test` needs Microsoft.Testing.Platform (`global.json`); use `dotnet test --project <path>`.
- FluentAssertions pinned to 7.x (v8 is commercially licensed).
- Apple: .NET for iOS 27 needs Xcode 27 (not on GitHub runners yet) → Phase 2.
- Driving the app in tests: Windows via `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=…` + Playwright `connectOverCDP`; Android via `adb forward` to `webview_devtools_remote_<pid>` + raw CDP.

## Owner machine setup (done on 2026-10-14)
.NET SDK 10.0.402, workloads `android` + `maui-android` + `maui-windows`, Aspire CLI 13.6.1, Node 24, JDK 17,
Android SDK + AVD `PaisaPlan_Pixel_API35`, Dev Tunnels CLI (logged in), Playwright Chromium.
Pending: `aspire certs trust` (optional, for the dashboard over HTTPS).

## Spec 001: what was delivered
- Domain: `EmailAddress`, `AccessRequest` (State pattern), `RefreshToken` (rotation + families), `AuditEntry`, `Result`.
- Application: 15 single-purpose use cases behind ports; `SessionManager`; audit trail as an event observer.
- Infrastructure: EF Core + SQLite + ASP.NET Identity (lockout 5/15 min, 12-char policy), JWT issuer, hashed refresh tokens.
- API: request access, login, refresh, logout, logout-all, change-password, me; forced-change policy; cookie vs body token transport.
- `lld-admin` console: requests, users, audit (no admin web endpoints).
- Web + MAUI: sign in, request access, forced password change, account panel; SecureStorage on device.
- Case study #1: `content/case-studies/auth/lesson.md` + `cheatsheet.md`.
- Details, deviations and findings: `specs/001-auth/plan.md` §4.

## Spec 002: what was delivered
- `Lld.ServerStatus`: signed status messages (ECDSA P-256), verifier, anti-replay tracker; shared test vector used by .NET and Vitest.
- API: Windows service on `localhost:5280`, DPAPI secrets, forwarded headers, global per-IP rate limit, tunnel request
  budget (flushed to SQLite), signed online / heartbeat / quota / shutdown announcements to ntfy.sh.
- `lld-admin`: `hosting init`, `status keygen|publish|budget`, `backup run|list`.
- `deploy/install.ps1` / `uninstall.ps1`; guide in `docs/hosting.md`.
- MAUI: hosted vs Aspire target, ngrok header, reachability handler, ntfy status monitor, access-request outbox, events to React.
- Web: status provider + `useServerStatus().canSync`, "Server offline" banner, offline request-access queue.
- Details, deviations and findings: `specs/002-self-hosting/plan.md` §4.

## Owner machine: hosting (done on 2026-10-15)
ngrok agent 3.39.11 (winget `Ngrok.Ngrok` + `ngrok update`), domain `doodle-diocese-revenue.ngrok-free.dev`,
`install.ps1` run: services `LldGuideApi` + `ngrok`, data in `C:\ProgramData\LldGuide`, backups in
`D:\Applications\LldGuide\backups`. Local app settings (topic + base URL) in the git-ignored
`src/Lld.App/appsettings.local.json`. The hosted database is separate from the dev database: create the admin
account with `C:\LldGuide\admin\lld-admin.exe users create <email> --role admin`.

## Spec 003: what was delivered
- Authoring contract (`module.yml`, lesson / cheat sheet front-matter) and `lld-content validate | pack | snapshot`;
  every rule is its own class with a test; invalid content fails the app build and CI with `file(line): reason`.
- Pack built into the app; device loads it into SQLite with plain `Microsoft.Data.Sqlite` (no EF Core, PRD R67),
  only when the pack version changes, in one transaction; schema steps via `PRAGMA user_version`.
- Content API (Development only) for browser dev mode, reloads on edit; the bridge serves the same contracts on device.
- Reader: Learn as a class diagram, module pages, lesson reader (Shiki, themed Mermaid with expand viewer, outline
  with current section and read ticks, text size, saved position, Next up), Revise + cheat sheets with recall mode,
  interactive class / sequence diagrams, code callouts; Android Back closes open sheets / viewer / popovers first, then walks the history.
- Content: "Start here" module with "How to use LLD Guide"; Auth case study adapted to the contract.
- Details, measurements and findings: `specs/003-content-engine/plan.md` §4.

## Spec 004: where it stands (2026-10-16)
- Spec approved; plan + tasks in `specs/004-oop-solid/`. Owner choices: 15 lessons, deep, classic + "in this app"
  examples, snippets by C# symbol (Roslyn), Before / After + callouts + folded interview Q&A + module map,
  optional "From my work" (`[!WORK]`) slots, review in 4 batches.
- Done: snippet engine + rules + pack format 2 (module map as groups/links, device step 3), reader UI (compare,
  UML-note callouts, Q&A, «package» map), samples project + tests in CI, batch 1 (encapsulation, abstraction,
  inheritance, polymorphism), e2e `web/e2e/concept.spec.ts`.
- Also on this branch: CI fix for `main` (MessagePack pin, GHSA-qhrr-8q5h-9q3h) and a CRLF-safe test fixture.
- After batch 1 the owner asked for **simple, visual, little text**: lessons are now a short card (REMEMBER stamp,
  everyday picture, rule, one Before / After, smells, app example, 5 short interview Qs) + a folded "Go deeper";
  icon + hook per lesson; module mnemonics drawn as acrostics (A-PIE now, SOLID in batch 3). Spec §3.2 / §3.8.
- Batch 2 (design basics) done in that style. Draft PR #6 open so CI runs on each push.

## Spec 004: what was delivered
- OOP & SOLID module: 15 lessons + cheat sheets as visual cards (REMEMBER stamp, everyday picture, short card, folded
  interview answers, folded "Go deeper"), icons + hooks, A-PIE and SOLID acrostics, a «package» map of how lessons relate.
- Every C# block quoted from compiled code by symbol (`samples/Lld.Samples.OopSolid` + 63 tests, or real `src/`);
  Before / After marks computed at pack time; 11 new validation rules; pack format 2, device schema step 3.
- Reader: Before / After compare, UML-note callouts, interview Q&A, Go deeper fold, module map, mnemonics, icons.
- CI fix for `main` (MessagePack pin). Details, deviations and findings: `specs/004-oop-solid/plan.md` §4.

## Spec 005: what was delivered
- Lesson status (Not started / In progress / Done; auto when every section is read, or by hand), per user with a device
  guest adopted at sign-in; device SQLite (schema step 4) + API `/api/progress` (migration `AddProgress`).
- `Lld.Domain/Progress` (LWW + merge rules, limits, store port), `Lld.Wire` (DTO mapping), one store contract suite
  (`tests/Lld.Progress.Tests`) for in-memory, device and EF Core.
- Web: progress store with write-behind queue, reader integration, Done control, Continue card, "pencil, then ink"
  module boxes, status marks, Revise "revised" + filter, Profile summary + activity strip + reset sheet, celebrations.
- Manual run on Windows + Android (upgrade V3 → V4, restart, offline). Details: `specs/005-progress/plan.md` §5.
- Open question for the owner: fling-to-end marks a lesson done (spec 003 tick rule). Keep?

## Spec 006: what was delivered
- Two modules: "GoF Design Patterns" (23 lessons in Creational / Structural / Behavioral) and "Enterprise & .NET
  patterns" (Repository, Unit of Work, Specification, Null Object, DI vs Service Locator); case studies moved to order 5.
- Template `pattern` (fixed sections, class diagram, Before / After, Modern C#, Seen in .NET, Travels with, Overuse
  stamp, symbol) with 11 new validation rules; nested `module.yml` values (`picker:`, `lookalikes:`), phrase mnemonics.
- Pack format 3, device schema step 5 (symbol, picker, look-alikes); contracts and web types.
- Reader: periodic table (table first on pattern modules), "Which pattern?" sheet (Esc / Android Back step back),
  look-alike compare route + lesson strip + module list, Overuse stamp, phrase mnemonics.
- Compiled samples `samples/Lld.Samples.Patterns` (Before / After / Modern for all 28) with 85 new tests.
- Docs: PRD v0.8 (R93-R100), `docs/plan.md` roadmap, README authoring guide for pattern lessons, plan section 4.
- Manual run: Windows upgrade 3 to 5 and Android upgrade 4 to 5 (SRP Done kept, picker Android Back) verified.
  Owner review done.
- Owner decisions after review: fix dark-mode white text on the light accent (app-wide, `fix/` PR); a section needs time
  on screen before it ticks, so a fling to the end no longer marks a lesson done (`fix/` PR).

## Spec 007: what was delivered
- Notes on a whole lesson or a section (marks beside headings and in the outline, a sheet with autosave, Edit /
  Preview, starters, Undo inside the sheet), bookmarks on lessons / sections / cheat sheets, the Notes tab (tag + text
  filters, export), "Your notes" under cheat sheets in Revise; `#tags` from the text with a shared C# / TS vector.
- Storage like progress: `Lld.Domain/Notes` (rules, limits, `INotesStore` + one contract suite), device schema step 6,
  API `Note` / `Bookmark` tables (`AddNotes`) and `/api/notes`, `/api/bookmarks`, `/api/notes/adopt` (browser only).
- App: `NotesService`, `NotesExport` (share sheet), guest notes adopted after sign-in; Android keyboard now resizes the
  page (`WindowSoftInputModeAdjust.Resize`).
- Shared `WriteBehind` (flush timing) now used by both the progress and notes stores; `DeviceSql` helpers on the device.
- Manual run: Windows (owner DB, schema 5 to 6, restart, export file) and Android (airplane mode, restart, keyboard,
  share sheet). Details and findings: `specs/007-notes-bookmarks/plan.md` section 5.

## Spec 008: what was delivered
- "UML" module (order 4): 10 lessons + cheat sheets (class ×3, object, component / deployment, use case, sequence,
  state, activity, UML in an LLD interview), two phrase mnemonics, a «package» map and a "Which diagram?" picker.
- Template `diagram` (4 rules), callout `MISTAKES`, fence keys `code=` / `steps` / `uml-arrows`.
- Code ↔ UML: `Uml/` Mermaid readers + Roslyn `SnippetCodeModel` + `DiagramCodeLinks`; the build fails when a diagram
  and its code disagree; the packer writes `mermaid pair links=…` + `csharp paired` (pack format still 3, CD-006).
- Reader: Code ↔ UML pair (tap a box or arrow ↔ its lines), arrow legend (every class diagram, popover / sheet),
  state Run on every state diagram, activity step-through with UML notation, picker on any module with a title.
- `samples/Lld.Samples.Uml` + 24 tests; `web/scripts/check-mermaid.mjs` renders every content diagram.
- Open questions delegated to Claude: CD-001..CD-008 in `docs/claude-decisions.md` (owner reviews later).
- Manual run: Windows (owner DB; test rows removed afterwards) and Android (airplane mode, upgrade keeps progress,
  notes, bookmarks). Details: `specs/008-uml/plan.md` section 5.

## How to resume (next session)
1. Spec 009 Search: the spec is on `spec/009-search` and awaits the owner's review (open questions Q1-Q10 in §8).
   After approval: record the answers in §7, add R117+ to the PRD, then write `plan.md` + `tasks.md`.
   Still pending: the owner's review of `docs/claude-decisions.md` (CD-001..CD-008).
2. Windows app runs: `-t:Run` only launches the existing build. Build first (`dotnet build src/Lld.App -f
   net10.0-windows10.0.19041.0`), then start `bin/.../Lld.App.exe` with `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS`.
3. Device database checks: pull `files/lld-device.db` **and** its `-wal` / `-shm` files
   (`adb exec-out run-as com.akus0ni.lldguide cat files/<name>`); without the WAL the old schema shows.
4. Shell notes: write files that contain backslash escapes with the Write / Edit tools, not Python heredocs. Screenshot
   scripts need `MSYS_NO_PATHCONV=1` and hash routes (`/#/learn/...`).
5. Run the UI locally: API with `Jwt__SigningKey` and `ConnectionStrings__lld` env vars (`dotnet run --project
   src/Lld.Api --launch-profile http`), Vite with `API_HTTP=http://localhost:5281`. Run one heavy process at a time.
6. Spec 003 follow-up still open: time first lesson open on a Release build with a real phone.
