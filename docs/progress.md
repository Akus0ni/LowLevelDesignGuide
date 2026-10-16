# Progress log

The single place to see where the project stands and what comes next. Update it at the end
of every spec (or session) so the next session can pick up without re-reading history.

_Last updated: 2026-10-16 (spec 004 batch 1)_

## Where we are
| Item | Status |
|---|---|
| PRD | v0.5 (spec 003 decisions R67–R75) |
| Plan | merged (PR #1); Phase 2 Apple findings added in PR #2 (`docs/plan.md` §8) |
| `main` protection | Ruleset "Protect main": PR + 1 approval (admin bypass in PR), squash only, no direct push; **required checks: `backend`, `web`, `maui-windows-android`** |
| Spec 000 Foundation | ✅ Merged (PR #2) |
| Spec 001 Auth & access | ✅ Merged (PR #3) |
| Spec 002 Self-hosting | ✅ Merged (PR #4); owner checks A1/A2/A3/A6/A10 done |
| Spec 003 Content engine | ✅ Merged (PR #5) |
| Spec 004 OOP & SOLID | 🚧 Draft PR #6: engine, reader, memory aids done; batches 1–3 (14 lessons) as visual cards, **waiting for owner review of batch 3 (SOLID)** |
| Hosting | ✅ Installed on the owner PC: services `LldGuideApi` + `ngrok` running, `https://doodle-diocese-revenue.ngrok-free.dev/health` OK, signed status on ntfy verified |

## Spec roadmap status
| # | Spec | Status |
|---|---|---|
| 000 | Foundation | ✅ merged (PR #2) |
| 001 | Auth & access (Case Study #1) + admin console | ✅ merged (PR #3) |
| 002 | Self-hosting & server status (ngrok, ntfy) | ✅ merged (PR #4) |
| 003 | Content engine (reader) | ✅ merged (PR #5) |
| 004 | OOP & SOLID content | 🚧 draft PR #6 (batches 1–3 of 4 written) |
| 005 | Progress | planned |
| 006 | GoF patterns content | planned |
| 007 | Notes & bookmarks | planned |
| 008 | UML content | planned |
| 009 | Search | planned |
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

## How to resume (next session)
1. `git switch spec/004-oop-solid && git pull`. Read `specs/004-oop-solid/tasks.md`.
2. If the owner reviewed batch 3: apply comments, then batch 4 = DRY, KISS & YAGNI (T16, slug `dry-kiss-yagni`, order
   15): samples + tests first, then lesson (stamp, vertical picture, short card, Go deeper) + cheat sheet; add it to
   the map (a "Heuristics" package). Then T11 break-ui, T12 README authoring guide, T18 manual Windows + Android run,
   T19 docs (PRD R76+), T20 mark PR #6 ready.
3. Run the UI locally: API with `Jwt__SigningKey` and `ConnectionStrings__lld` env vars (`dotnet run --project
   src/Lld.Api --launch-profile http`), Vite with `API_HTTP=http://localhost:5281`.
4. Spec 003 follow-up still open: time first lesson open on a Release build with a real phone.
