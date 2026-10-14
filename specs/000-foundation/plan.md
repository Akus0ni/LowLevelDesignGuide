# Spec 000: Implementation plan

Companion to [`spec.md`](./spec.md). Versions verified against NuGet/npm on 2026-10-14.

## 1. Versions
| Area | Package | Version | Note |
|---|---|---|---|
| SDK | .NET SDK | 10.0.401 (`global.json`, `rollForward: latestFeature`) | |
| Aspire | `Aspire.AppHost.Sdk`, `Aspire.Hosting.JavaScript`, `Aspire.Hosting.Testing` | 13.6.1 | |
| Aspire | `Aspire.Hosting.Maui` | 13.6.1-preview.1.26506.6 | **Preview only**; no stable release yet |
| Service defaults | `Microsoft.Extensions.ServiceDiscovery`, `Microsoft.Extensions.Http.Resilience` | 10.11.0 | |
| Telemetry | `OpenTelemetry.*` | 1.19.x | |
| MAUI | `Microsoft.Maui.Controls` | 10.0.110 | |
| MAUI | `CommunityToolkit.Maui` | 15.0.1 | Status bar colour |
| Tests | `xunit.v3` 4.0.2, `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.11.0 | | |
| Tests | `FluentAssertions` | **7.2.2** | Pinned to 7.x: v8 moved to a commercial licence (free only for non-commercial use). 7.x is Apache-2.0 |
| Tests | `Microsoft.AspNetCore.Mvc.Testing` | 10.0.13 | |
| Web | React 19, Vite 8, TypeScript 5.9 | | Mirrors the Aspire 13.6.1 React starter |
| Web | `react-router` 8.4, `tailwindcss` + `@tailwindcss/vite` 4.3, `clsx` 2.1, `class-variance-authority` 0.7, `@base-ui/react` 1.9 | | |
| Web tests | `vitest` 5, `@testing-library/react` 16, `jsdom` 30, `@playwright/test` 1.64 | | |

## 2. Design

### 2.1 Projects and references
```
Lld.Domain            (no refs)
Lld.Application   →   Domain
Lld.Infrastructure→   Application
Lld.Contracts         (no refs)        AppInfo record lives here
Lld.Content           (no refs)
Lld.ServiceDefaults   (ASP.NET shared framework)
Lld.Api           →   Infrastructure, Contracts, ServiceDefaults
Lld.App.ServiceDefaults (MAUI variant, from `maui-aspire-servicedefaults` template)
Lld.App           →   Contracts, Content, App.ServiceDefaults
Lld.AppHost       →   Api (ProjectReference); MAUI app by path only (Aspire rule: no ProjectReference to MAUI)
```

### 2.2 Shared contract
`Lld.Contracts.AppInfo(string Name, string Version, string Environment)` is returned by both
`GET /api/info` (Environment = ASP.NET environment) and `BridgeService.GetInfo()` (Environment = device platform).
The web `DataSource.getInfo()` returns the same shape, so the UI doesn't care which transport answered.

### 2.3 AppHost
```csharp
var api = builder.AddProject<Projects.Lld_Api>("api").WithHttpHealthCheck("/health");

if (!builder.Configuration.GetValue<bool>("Lld:ApiOnly"))
{
    builder.AddViteApp("web", "../../web").WithReference(api).WaitFor(api);

    var app = builder.AddMauiProject("app", "../Lld.App/Lld.App.csproj");
    app.AddWindowsDevice().WithReference(api);
    app.AddAndroidEmulator("app-android", builder.Configuration["Lld:AndroidEmulatorId"])
       .WithOtlpDevTunnel();
}
```
- `Lld:ApiOnly=true` lets the Aspire integration test (and Linux CI) start only the API, without npm or a MAUI device.
- `Lld:AndroidEmulatorId` is optional (an adb serial such as `emulator-5554`); see §4.
- Android → API reference through a Dev Tunnel is deferred to spec 001 (the app doesn't call the API in this spec); OTLP goes through `WithOtlpDevTunnel()`.

### 2.4 Web
- Structure: `src/app` (router, layout), `src/features/<section>` (placeholder pages), `src/data` (`DataSource`, implementations, factory), `src/ui` (cva components), `src/styles` (tokens, base layer).
- `createDataSource(win = window)` returns `BridgeDataSource` when `win.HybridWebView` exists, else `HttpDataSource`. Injected through a React context so tests can swap it.
- Vite dev server proxies `/api` to the API (env var injected by Aspire), so no CORS is needed.
- **Guardrails:** `scripts/check-guardrails.mjs` scans `src/**` and `index.html` for banned tokens (`transition-all`, `h-screen`, `min-h-screen`, `ease-in` not followed by `-out`, `user-scalable=no`, `maximum-scale`). Runs inside `npm run lint`. A Vitest test feeds it fixtures to prove each rule fires (A9).
- **Playwright smoke test:** runs against `vite preview` with `/api/info` mocked by route interception; it checks the shell, the tab bar at phone width and the sidebar at desktop width. The real API path is covered by the Aspire integration test. (This narrows spec A2's "Playwright" part to the UI; the real-API part stays manual.)

### 2.5 MAUI
- Created from `dotnet new maui`, then trimmed: no AppShell, one `MainPage` with a full-bleed `HybridWebView` (`SafeAreaEdges="None"`), `BridgeService` as the invoke target, `AddHybridWebViewDeveloperTools()` in Debug.
- `Lld.App.csproj` target `BuildWebApp` runs before `Build` unless `SkipWebBuild=true`: `npm ci` (only if `node_modules` is missing) → `npm run build` → copy `web/dist/**` into `Resources/Raw/wwwroot`.
- `index.html` loads `_framework/hybridwebview.js` only when running inside the app (the file doesn't exist in the browser): the Vite build injects `<script src="_framework/hybridwebview.js">`, and in the browser the request simply 404s and is ignored. **Alternative if the 404 is noisy:** a small runtime loader that adds the script only when `window.chrome?.webview` / Android / iOS bridges exist. Decided during implementation.
- Status bar: `CommunityToolkit.Maui` `StatusBarBehavior` with `AppThemeBinding` colours matching the header token.

### 2.6 CI
`.github/workflows/ci.yml`, triggered on PRs and pushes to `main`; four jobs as in spec §3.6. MAUI jobs run `dotnet workload restore`. The `web` job installs Playwright Chromium.

## 3. Risks
| Risk | Mitigation |
|---|---|
| `Aspire.Hosting.Maui` is preview | Pin the version; record API differences here |
| `macos-latest` Xcode doesn't match the iOS workload | Pin `macos-26` runner / select an Xcode version in the job |
| Android WebView doesn't report `env(safe-area-inset-*)` | Fallback: `SafeAreaEdges="Container"` on Android; record the outcome |
| Aspire integration test needs a dev certificate on Linux CI | Use the API's `http` endpoint in tests |

## 4. Outcomes (2026-10-14)
| Topic | Outcome |
|---|---|
| Windows (WinUI WebView2) | Shell renders; bridge verified over CDP: About shows `Native bridge · LLD Guide · 0.1.0.1 · WinUI` |
| Browser via Vite proxy | Real API data on phone + desktop layouts |
| Android (emulator, API 35, WebView Chrome 124) | Runs under `aspire run`; bridge verified over the WebView DevTools socket: About shows `Native bridge · LLD Guide · 0.1.0 · Android`; status bar matches `bg`; tab navigation works with real taps |
| Safe areas | Windows: not applicable. **Android WebView reports `env(safe-area-inset-*)` as 0**, so content sat under the status bar and the gesture bar overlapped the tabs. Fallback applied: `SafeAreaEdges.All` on Android (code-behind); iOS/Windows stay full-bleed. Re-check iOS on a Mac (Phase 2) |
| Emulator selection | `Lld:AndroidEmulatorId` is an **adb serial** (`emulator-5554`), not an AVD name (an AVD name fails with `XA0010`). Empty = the running emulator. Aspire does not boot emulators; start one first |
| MAUI device resources | Not auto-started; start `app-windows` / `app-android` from the dashboard or with `aspire resource <name> start`. The Windows launcher shows *Finished* once the app window is open |
| Dev certificate | Untrusted cert broke HTTPS health checks and the Vite proxy. Health check now uses the `http` endpoint; the Vite proxy prefers `API_HTTP`. `aspire certs trust` recommended for the dashboard |
| Web bundle in MAUI | Included directly from `web/dist-app` as `wwwroot/**` MAUI assets inside a target (no copy step); evaluation-time globs miss build outputs |
| Bridge script | Injected only by `vite build --mode maui` |
| Test runner | .NET 10 requires opting in to Microsoft.Testing.Platform (`global.json` → `test.runner`) |
| Apple targets | Only on macOS hosts (owner decision); `-p:SkipAndroid=true` for Windows-only builds |
| Driving the app in tests | Launch with `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9333` and attach Playwright with `connectOverCDP` (useful for later app-level e2e) |
