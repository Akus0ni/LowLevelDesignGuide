# LLD Guide

A cross-platform (Android · iOS · Windows · macOS) app for learning **Low-Level Design**
in C#/.NET, built spec by spec. The app is its own case study: each feature is designed
using the principles it teaches.

- **UI:** React + Vite + TypeScript, hosted in .NET MAUI `HybridWebView`
- **Backend:** ASP.NET Core Minimal API · EF Core · SQLite · Identity + JWT, self-hosted on the owner's PC via ngrok
- **Local dev:** Aspire 13
- **Content:** OOP & SOLID · GoF patterns · UML · LLD case studies, each with a cheat sheet

## Docs
- [Product Requirements (PRD)](docs/PRD.md)
- [Implementation plan](docs/plan.md)
- [Progress log](docs/progress.md): current status and how to resume
- Specs: [`specs/`](specs/)
- Decisions: [`docs/adr/`](docs/adr/)

## Local setup (one-time, manual)
Everything a developer must install by hand before the app builds and runs. Windows is the
primary dev OS; macOS notes are at the end.

### 1. Install the tools
| # | Dependency | Version | Install (Windows) | Needed for |
|---|---|---|---|---|
| 1 | Git | any recent | `winget install Git.Git` | cloning |
| 2 | .NET SDK | **10.0.401+** (pinned in `global.json`) | `winget install Microsoft.DotNet.SDK.10` | everything .NET |
| 3 | Node.js (+ npm) | **24** | `winget install OpenJS.NodeJS.LTS` (or nvm) | React app, MAUI web bundle |
| 4 | Java JDK | **17** (Microsoft OpenJDK) | `winget install Microsoft.OpenJDK.17` | Android builds |
| 5 | .NET MAUI workloads | match SDK | **admin** terminal: `dotnet workload install maui-windows maui-android` (or Visual Studio Installer → ".NET Multi-platform App UI development", Windows + Android ticked) | MAUI app |
| 6 | Android SDK, platform & emulator image | API 35+ | Android Studio's SDK Manager, or after step 5: `dotnet build src/Lld.App -t:InstallAndroidDependencies -f net10.0-android -p:AndroidSdkDirectory="%LOCALAPPDATA%\Android\Sdk" -p:AcceptAndroidSDKLicenses=true` | Android build + emulator |
| 7 | Android emulator (AVD) | Pixel, API 35 | Android Studio → Device Manager → Create device | running on Android |
| 8 | Aspire CLI | **13.6.1** | `dotnet tool install -g Aspire.Cli --version 13.6.1` | `aspire run` |
| 9 | Dev Tunnels CLI | latest | `winget install Microsoft.devtunnel`, then **`devtunnel user login`** (Microsoft or GitHub account) | Android/iOS reaching the PC (telemetry now, API from spec 001) |
| 10 | WebView2 Runtime | evergreen | preinstalled on Windows 11 (`winget install Microsoft.EdgeWebView2Runtime` otherwise) | Windows app |
| 11 | Playwright browser | bundled version | `cd web && npx playwright install chromium` | `npm run test:e2e` only |

Optional: Visual Studio 2026 (or VS Code + C# Dev Kit) for debugging; GitHub CLI (`winget install GitHub.cli`) for PRs.

### 2. One-time configuration
| Step | Command | Why |
|---|---|---|
| Trust the HTTPS dev certificate | `aspire certs trust` (accept the Windows prompt) | Otherwise the browser warns and the dashboard loses its live connection |
| (Optional) pick an emulator | `dotnet user-secrets set Lld:AndroidEmulatorId emulator-5554 --project src/Lld.AppHost` | Only needed with several devices running. The value is an **adb serial** (see `adb devices`), not an AVD name; empty = the running emulator |
| Environment variables | `ANDROID_HOME` = your Android SDK path (e.g. `%LOCALAPPDATA%\Android\Sdk`); `JAVA_HOME` = JDK 17 path | Android build tools find the SDK and JDK |
| Check the setup | `aspire doctor` and `dotnet workload list` | Shows anything still missing |

### 3. First build
```bash
git clone https://github.com/Akus0ni/LowLevelDesignGuide.git
cd LowLevelDesignGuide
cd web && npm ci && cd ..
dotnet build src/Lld.Api
dotnet build src/Lld.App -f net10.0-windows10.0.19041.0   # also builds the React bundle
aspire run
```

### Troubleshooting
| Symptom | Fix |
|---|---|
| `NETSDK1147: ... workloads must be installed: maui-android` | Step 5 (admin terminal), or build Windows only with `-p:SkipAndroid=true` |
| `NETSDK1047: Assets file doesn't have a target for net10.0-android/android-arm64` | Stale restore: `dotnet restore src/Lld.App --force` |
| Dev Tunnel resource `FailedToStart` ("devtunnel was not found") | Step 9, then open a **new** terminal so PATH includes it |
| `XA0010: Selected device is not running` on `app-android` | Boot the emulator before starting the resource |
| Dashboard shows "Lost connection to the AppHost" | `aspire certs trust`, or use the `http` launch profile below |
| `Testing with VSTest target is no longer supported` | Use `dotnet test --project <path>` (repo opts into Microsoft.Testing.Platform) |

### macOS (later, for iOS / Mac Catalyst)
Xcode (version required by the installed iOS workload), then `dotnet workload install maui-ios maui-maccatalyst`.
iOS and Mac Catalyst targets are only built on macOS (`src/Lld.App/Lld.App.csproj`).

## Run everything
```bash
aspire run
```
The terminal prints the **dashboard** link, e.g. `https://localhost:17230/login?t=<token>`.
Open it in a browser; the token changes every run. Without a trusted certificate, use the
HTTP profile instead: `dotnet run --project src/Lld.AppHost --launch-profile http`
(dashboard on `http://localhost:15291`, login link printed in the console).

| Resource | What it is | How to open it |
|---|---|---|
| `api` | ASP.NET Core API | `http://localhost:5281/api/info` |
| `web` | React app in the browser (talks to the API) | URL shown in the dashboard |
| `app-windows` | MAUI app on Windows (talks to C# through the bridge) | Start it from the dashboard |
| `app-android` | MAUI app on the Android emulator | **Boot the emulator first** (`emulator -avd <YourAvdName>` or Android Studio Device Manager), then start it from the dashboard. Aspire does not boot emulators |

Without the Android workload, run Windows only with `SkipAndroid=true` in the environment.

## Develop and test
```bash
# Backend
dotnet test --project tests/Lld.Api.Tests
dotnet test --project tests/Lld.IntegrationTests   # starts the AppHost (API only)

# Web (from web/)
npm run dev          # browser dev server (start the API first, or use aspire run)
npm run lint         # ESLint + design guardrails
npm test             # Vitest
npm run test:e2e     # Playwright smoke tests (phone + desktop)

# MAUI (one target framework at a time)
dotnet build src/Lld.App -f net10.0-windows10.0.19041.0
dotnet build src/Lld.App -f net10.0-android
```

## Contributing
`main` is protected. Every change goes through a branch and a PR; see `CLAUDE.md`.

## License
[MIT](LICENSE)
