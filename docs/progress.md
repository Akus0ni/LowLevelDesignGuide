# Progress log

The single place to see where the project stands and what comes next. Update it at the end
of every spec (or session) so the next session can pick up without re-reading history.

_Last updated: 2026-10-14_

## Where we are
| Item | Status |
|---|---|
| PRD | v0.2 merged (PR #1) |
| Plan | merged (PR #1); Phase 2 Apple findings added in PR #2 (`docs/plan.md` §8) |
| `main` protection | Ruleset "Protect main": PR + 1 approval (admin bypass in PR), squash only, no direct push; **required checks: `backend`, `web`, `maui-windows-android`** |
| Spec 000 Foundation | ✅ Merged (PR #2) |
| Spec 001 Auth & access | 🟡 In progress on `spec/001-auth`: case-study interview done, `spec.md` drafted, awaiting owner approval |

## Spec roadmap status
| # | Spec | Status |
|---|---|---|
| 000 | Foundation | ✅ merged (PR #2) |
| 001 | Auth & access (Case Study #1) + admin console | 🟡 spec drafted |
| 002 | Self-hosting & server status (ngrok, ntfy) | planned |
| 003 | Content engine (reader) | planned |
| 004 | OOP & SOLID content | planned |
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

## How to resume (next session)
1. Confirm PR #2 is merged: `gh pr view 2 --json state`.
2. `git switch main && git pull`, then `git switch -c spec/001-auth`.
3. Spec 001 is also **Case Study #1**: run the `lld-case-study` skill interview with the owner first.
4. Draft `specs/001-auth/spec.md` from PRD F1 + F9 (request access with email as username, admin console,
   temporary password + forced change, JWT + rotating refresh tokens). Ask the open questions before coding,
   including PRD §15 Q1 (browser dev-mode token storage: memory vs httpOnly cookie).
5. Follow the per-spec workflow in `docs/plan.md` §4 (spec → owner review → plan + tasks → build → PR; owner merges).
