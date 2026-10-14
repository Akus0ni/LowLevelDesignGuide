# Spec 000: Foundation

| | |
|---|---|
| **Status** | Draft v2, awaiting owner approval |
| **Branch** | `spec/000-foundation` |
| **Covers** | R03, R04, R05, R06, R07, R14, R32, R49, R50, P5 (CI checks), P7, P8 (see PRD §13) |
| **Depends on** | PR #1 (merged) |

## 1. Goal
Create the empty but **fully wired** skeleton that every later spec builds on:
one command (`aspire run`) starts the API, the React dev server and the MAUI app,
and CI proves every platform target builds. **No product features yet**: no auth,
no lessons, no sync.

## 2. User stories
1. *As the owner,* I run one command and see the API, web UI and Windows app running and healthy in the Aspire dashboard.
2. *As the owner,* I open the app on the Android emulator and see the same React shell, loaded from the app package.
3. *As the owner,* I open the React app in a browser during development and it calls the API's info endpoint.
4. *As the owner,* every PR runs CI, and `main` only accepts PRs whose checks pass.
5. *As a learner (later),* I see an app shell with navigation to the future sections (Learn, Revise, Search, Notes, Profile), each showing a "coming soon" placeholder.

## 3. Scope

### 3.1 Solution (.NET 10)
- `LowLevelDesignGuide.slnx` with:
  - `src/Lld.AppHost`: Aspire 13 AppHost.
  - `src/Lld.ServiceDefaults`: OpenTelemetry, health checks, resilience, service discovery.
  - `src/Lld.Domain`, `src/Lld.Application`, `src/Lld.Infrastructure`, `src/Lld.Contracts`, `src/Lld.Content`: empty class libraries; the dependency rule from `plan.md` §2 is enforced by project references.
  - `src/Lld.Api`: Minimal API with `/health` and `/alive` (ServiceDefaults) and `GET /api/info` returning `{ name, version, environment }`.
  - `src/Lld.App`: .NET MAUI app (`net10.0-android`, `-ios`, `-maccatalyst`, `-windows`). Display name **LLD Guide**, app ID **`com.akus0ni.lldguide`**. One page with a full-screen `HybridWebView` loading the React build from `Resources/Raw/wwwroot`.
  - `tests/Lld.Api.Tests` (xUnit + FluentAssertions) and `tests/Lld.IntegrationTests` (Aspire.Hosting.Testing).
- `Directory.Build.props` (nullable, warnings as errors, analyzers), `Directory.Packages.props` (central package versions), `global.json` (pins the .NET 10 SDK).
- **Tooling prerequisite:** the Aspire CLI is installed as a .NET global tool (owner approved).

### 3.2 React app (`web/`)
- Vite + React + TypeScript (strict), npm, **React Router**.
- **Navigation:** bottom tab bar on phones; left sidebar on tablet/desktop (breakpoint `md`). Sections: **Learn, Revise, Search, Notes, Profile**, each a "coming soon" placeholder.
- `DataSource` interface with two implementations, chosen at startup by `createDataSource()`:
  - `HttpDataSource`: `fetch` to the API (browser dev mode).
  - `BridgeDataSource`: `window.HybridWebView.InvokeDotNet(...)` (inside MAUI).
  - One method in this spec: `getInfo()`. Covered by Vitest.
- An "About" panel (under Profile) showing `getInfo()` results, so both transports are visibly working.
- Light/dark theme following the OS (Tailwind `dark:` via `prefers-color-scheme`).
- Vitest + Testing Library, ESLint, Prettier.

### 3.3 Styling stack and guardrails
**Stack:** Tailwind CSS v4 + `clsx` + `cva` + `base-ui` (per `pick-ui-library`), reviewed against `emil-design-eng`, `mobile-native` and `apple-design`. Decision and reasoning recorded in `docs/adr/0002-styling-tailwind-base-ui.md`.

**Theme tokens (override Tailwind defaults):**
| Token / variant | Value | Source |
|---|---|---|
| `--ease-out` | `cubic-bezier(0.23, 1, 0.32, 1)` | emil-design-eng |
| `--ease-in-out` | `cubic-bezier(0.77, 0, 0.175, 1)` | emil-design-eng |
| `--ease-drawer` | `cubic-bezier(0.32, 0.72, 0, 1)` | emil-design-eng |
| `ease-in` | **removed** from the theme | emil-design-eng: never ease-in for UI |
| `hover:` variant | `@media (hover: hover) and (pointer: fine)` | mobile-native §1 |
| `pt-safe`, `pb-safe`, `pl-safe`, `pr-safe` | `env(safe-area-inset-*, 0px)` | mobile-native §7 |
| Press feedback | `active:scale-[0.97]`, ~120 ms `--ease-out`, in the cva base of every pressable | emil-design-eng, mobile-native §5 |
| Type | System font stack; tight tracking on large headings, ~0 on body; `rem` spacing | apple-design §15 |

**Base CSS layer (mobile-native baseline):** viewport `width=device-width, initial-scale=1, viewport-fit=cover, interactive-widget=resizes-content`; `-webkit-tap-highlight-color: transparent`; `-webkit-text-size-adjust: 100%`; `overscroll-behavior: none` on `html, body`; 16px inputs on `(pointer: coarse)`; `touch-action: manipulation` + `user-select: none` on controls only; `color-scheme: light dark`; zoom is **never** disabled.

**Chrome:** tab bar and sidebar are translucent layers (`backdrop-blur` + semi-transparent background) with content scrolling underneath; a soft scroll-edge fade instead of a hard `border` divider; no stacked translucent surfaces (apple-design §12).

**Enforced by lint (build fails):** `transition-all`, `h-screen` / `min-h-screen` (use `h-dvh` / `min-h-dvh`), `ease-in`, `user-scalable=no` / `maximum-scale`.

### 3.4 MAUI ↔ React bridge and native shell
- An MSBuild target builds `web/` (`npm ci && npm run build`) and copies `web/dist` into `src/Lld.App/Resources/Raw/wwwroot` before the MAUI build. Skippable with `-p:SkipWebBuild=true`.
- `BridgeService` (C#) is the HybridWebView invoke target, exposing `GetInfo()` (app version + platform). It does not call the API yet.
- **Status bar** colour and style are set natively to match the app header in light and dark mode (the web `theme-color` meta has no effect inside the WebView).
- **Safe areas:** the WebView is full-bleed and CSS pads with `env(safe-area-inset-*)`. If the platform WebView does not report insets, MAUI applies the safe-area padding natively instead. The outcome per platform is recorded in this spec's `plan.md`.

### 3.5 Aspire AppHost
- Resources: `api` (project), `web` (Vite app, referencing `api`), `app` (MAUI project) with a **Windows device** and an **Android emulator** (Dev Tunnel for API/OTLP reach).
- Android emulator: existing AVD **`PaisaPlan_Pixel_API35`**, configured by name in AppHost settings so it can be changed.
- Exact Aspire 13 APIs are verified against the installed packages during implementation; deviations are recorded in this spec's `plan.md`.

### 3.6 CI (GitHub Actions, `.github/workflows/ci.yml`)
| Job | Runner | Steps |
|---|---|---|
| `backend` | `ubuntu-latest` | restore, build, `dotnet test` (unit + Aspire integration) |
| `web` | `ubuntu-latest` | `npm ci`, lint (incl. guardrails), typecheck, Vitest, build |
| `maui-windows-android` | `windows-latest` | build `Lld.App` for `net10.0-windows` and `net10.0-android` |
| `maui-apple` | `macos-latest` | build `Lld.App` for `net10.0-ios` (simulator) and `net10.0-maccatalyst`; compile only, no signing |
- After the first green run, all four jobs become **required status checks** in the "Protect main" ruleset.

### 3.7 Docs
- `README.md`: prerequisites and "how to run" (`aspire run`, browser URL, Android emulator).
- `docs/adr/0001-maui-hybridwebview-react.md`: why HybridWebView + React.
- `docs/adr/0002-styling-tailwind-base-ui.md`: styling stack, skill critique, guardrails.

## 4. Out of scope
Auth, content, database schema, sync, hosting/ngrok, real visual design beyond the shell and tokens, iOS/macOS running on a device, physical Android device testing (no device available; see A10).

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | `aspire run` starts `api`, `web` and `app` (Windows); all show healthy in the dashboard | Manual + screenshot in PR |
| A2 | Browser shows the shell; About shows API info via `HttpDataSource` | Manual + Playwright smoke test |
| A3 | Windows app shows the same shell; About shows bridge info via `BridgeDataSource` | Manual + screenshot |
| A4 | Android emulator runs the app: shell, bottom tabs, status bar colour, no content under system bars | Manual + screenshot |
| A5 | `dotnet test` passes, including an Aspire integration test calling `/health` and `/api/info` | CI `backend` |
| A6 | Vitest covers `createDataSource()` choosing the right implementation | CI `web` |
| A7 | All 4 CI jobs pass on the PR and are then required on `main` | GitHub checks + ruleset |
| A8 | Zero build warnings (warnings as errors) | CI |
| A9 | Guardrail lint fails on `transition-all`, `h-screen`, `ease-in`, zoom-disabling viewport (proved by a test fixture) | CI `web` |
| A10 | Hardware-only checks (tap feel, sticky hover, keyboard, real safe areas) are listed in the PR as **"to verify on a device"** | PR description |

## 6. LLD hooks (lesson material later)
- `DataSource` + `createDataSource()`: **Strategy**, **Adapter** and a simple **Factory**.
- Project dependency rule: **Dependency Inversion** at the architecture level.
- ServiceDefaults extension methods: cross-cutting concerns kept out of business code.
- cva button variants: **Open/Closed** (add a variant without editing call sites).

## 7. Decisions (resolved 2026-10-14)
| Question | Decision |
|---|---|
| Aspire tooling | Install the Aspire CLI as a .NET global tool |
| Styling | Tailwind v4 + clsx/cva + base-ui, critiqued with emil-design-eng, mobile-native and apple-design; guardrails enforced by tokens **and** lint |
| Routing | React Router |
| Navigation | Bottom tabs on phones, sidebar on tablet/desktop |
| Android | Emulator only (`PaisaPlan_Pixel_API35`); hardware checks listed as pending |

## 8. Open questions
None. Approve this spec to proceed to `plan.md` + `tasks.md`.
