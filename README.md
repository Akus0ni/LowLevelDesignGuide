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
- [Hosting on this PC](docs/hosting.md): ngrok, Windows services, server status, backups
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

## Accounts and the admin console
There is no open sign-up. People use **Request access** in the app; the owner approves them from the admin
console on the PC (it talks to the same SQLite file as the API; there are no admin web endpoints).

```bash
# First time: create your own admin account (prints a temporary password once)
dotnet run --project src/Lld.Admin -- users create you@example.com --role admin

dotnet run --project src/Lld.Admin -- requests list            # pending requests (--all for every status)
dotnet run --project src/Lld.Admin -- requests approve 83c8    # ID column of 'requests list' (first characters), or the email → temporary password
dotnet run --project src/Lld.Admin -- requests reject 83c8
dotnet run --project src/Lld.Admin -- users list | reset-password <email> | disable <email>
dotnet run --project src/Lld.Admin -- audit list --take 20
```

Share temporary passwords privately; the user must set a new one (at least 12 characters) at first sign-in.

| Item | Where |
|---|---|
| Database | `%LOCALAPPDATA%\LldGuide\lld.db` (override with `Database:Path`) |
| JWT signing key | Generated by Aspire on first `aspire run` and stored in the AppHost's user secrets (`jwt-signing-key`); nothing to set up |

These commands use the **development** database. The hosted API (spec 002) has its own database; manage it with the
installed console, `C:\LldGuide\admin\lld-admin.exe`, which also has `status budget`, `status publish` and
`backup run|list`. See [docs/hosting.md](docs/hosting.md).

## Hosting on this PC (ngrok)
Real devices reach the API through **ngrok** (free static domain, HTTPS, no open router ports). The API and the
ngrok agent run as Windows services. Full guide, everyday commands and troubleshooting: [docs/hosting.md](docs/hosting.md).

### 1. ngrok account, domain and authtoken (manual, once)
1. Sign up (free) at <https://dashboard.ngrok.com/signup>.
2. Dashboard → **Universal Gateway → Domains**: note your free `*.ngrok-free.dev` domain (claim one if none is
   listed). It shows **Offline** until an agent connects, which is expected. **Don't** create an endpoint, a Cloud
   Endpoint or a traffic policy in the dashboard; the agent on this PC creates the endpoint.
3. Dashboard → **Getting Started → Your Authtoken**: keep it at hand. It's a credential: never commit it.
4. Dashboard → **Billing / Usage**: note the day the monthly usage resets. If it isn't the 1st, set
   `Budget:ResetDayOfMonth` in `src/Lld.Api/appsettings.Production.json`.

### 2. Install and update the ngrok agent (manual, once)
```powershell
winget install --id Ngrok.Ngrok -e     # the ID is case-sensitive with -e
# open a NEW terminal (so ngrok is on PATH), then:
ngrok update                           # winget ships an old 3.3.x agent; the config needs 3.2x or newer
ngrok version
```
No winget? Download the Windows zip from <https://ngrok.com/download> and put `ngrok.exe` on your PATH.

### 3. Install the services (elevated PowerShell, from the repo root)
```powershell
.\deploy\install.ps1 -Domain <your-name>.ngrok-free.dev
```
- At the hidden **authtoken** prompt, paste with **right-click** or **Ctrl+Shift+V** (Ctrl+V types a control
  character there). You should see a long row of `*`. Later runs reuse the saved token.
- The script publishes the API and admin console to `C:\LldGuide`, registers the `LldGuideApi` and `ngrok`
  services, creates the secrets, keeps the PC awake on AC power, schedules the daily 13:00 backup, and ends with
  `local health: OK` and `public health: OK`. Re-run it after pulling new code; `-WhatIf` previews every step.

### 4. After installing
```powershell
C:\LldGuide\admin\lld-admin.exe users create <you@example.com> --role admin   # the hosted DB starts empty
C:\LldGuide\admin\lld-admin.exe status budget                                 # usage vs the free limits
```
`hosting init` (run by the installer) prints the **app build settings**: the public key goes in the committed
`src/Lld.App/appsettings.json`; the ntfy topic and the tunnel URL go in the git-ignored
`src/Lld.App/appsettings.local.json` (format in [docs/hosting.md](docs/hosting.md#2-install-the-services)).
Release builds of the app then use the hosted server; Debug builds keep using Aspire.

## Develop and test
```bash
# Backend
dotnet test --project tests/Lld.Domain.Tests
dotnet test --project tests/Lld.Application.Tests
dotnet test --project tests/Lld.Api.Tests
dotnet test --project tests/Lld.Admin.Tests
dotnet test --project tests/Lld.Content.Tests  # authoring rules, packer, device content store
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

## Writing content
Lessons live in `content/<module>/<lesson-slug>/` (the folder names are the slugs):

```
content/
  start/
    module.yml          # title, order, summary (one sentence)
    how-to-use/
      lesson.md         # front-matter: id, title, order, summary, tags, optional source
      cheatsheet.md     # front-matter: title only; body in ## blocks (recall mode folds them)
```

- `id` is permanent once published (progress, notes and bookmarks point at it).
- Link another lesson with a relative path (`../../oop-solid/srp/lesson.md#anchor`), repo code with a relative path
  to `src/` (opens on GitHub), anything else with an absolute `https://` link.
- Cheat sheets: one screen + at most 2 scrolls; case-study cheat sheets up to 4.
- Case studies start with the `lld-case-study` skill, which asks the owner first.

### Concept lessons: short, visual, memorable (spec 004)
A module with `template: concept` in `module.yml` (e.g. `oop-solid`) follows the visual-card template. Write short
sentences in everyday words; put a picture or code before prose.

````markdown
---
id: srp
title: Single Responsibility (SRP)   # the first letter counts in mnemonics
order: 10
summary: "One sentence for lists."
tags: [solid, srp]
icon: "🎯"                            # required: one emoji (no flags: Windows shows letters)
hook: "One class, one boss"           # required: at most 40 characters
---

# Single Responsibility (SRP)

> [!REMEMBER]
> One class, one boss.               (required, before the first ## section; cheat sheets too)

```mermaid
flowchart TD                         (the everyday picture: top to bottom, at most ~2 boxes per row)
```

## The rule · ## See it · ## Smell it · ## In this app
## Interview questions               (5–8 ### questions, each with an answer; folded in the app)
## Go deeper                         (folded; not counted in reading time; keep the card ≤ 8 min)
````

| Feature | How to write it |
|---|---|
| **Compiled C#** | ```` ```csharp snippet="samples/Lld.Samples.OopSolid/Srp/After/Invoice.cs#Invoice,InvoiceHtml" ```` with an empty body. Selectors: `Type`, `Outer.Inner`, `Type.Member` (all overloads; `Type.Type` = constructors), `Box<T>`; none = whole file. Paths may point into `samples/`, `src/` or `tests/`. Concept modules may not contain inline C#. |
| **Before / After** | A ```` ```csharp before title="…" snippet="…" ```` fence directly followed by an ```` ```csharp after … ```` fence. Changed lines are marked when content is packed. |
| **Mark lines** | ```` ```csharp mark="3-5,9" ```` on any other fence. |
| **Callouts** | `> [!TYPE]` on the first line of a blockquote: `REMEMBER`, `NOTE`, `TIP`, `INTERVIEW`, `SMELL`, `PITFALL`, `APP` (in this app), `WORK` (from my work). |
| **Module map** | `map:` in `module.yml`: a Mermaid flowchart whose node ids are lesson slugs; `subgraph id["Title"]` = a package; `a -->|"label"| b` = a link. Spaces around arrows; every lesson must appear. |
| **Mnemonics** | `mnemonics: ["SOLID: srp, ocp, lsp, isp, dip"]`: one lesson per letter, each letter = the first letter of that lesson's title. |

Samples live in `samples/Lld.Samples.OopSolid/<Lesson>/{Before,After,Language}` with tests in `tests/Lld.Samples.Tests`
proving what each design claims. In browser dev mode, editing a quoted `.cs` file shows up on reload too.

```bash
dotnet run --project src/Lld.Content.Cli -- validate                       # file:line: reason, exit 1 on any issue
dotnet run --project src/Lld.Content.Cli -- pack --out <file>              # what the app build runs
dotnet run --project src/Lld.Content.Cli -- snapshot --out web/e2e/.content  # what Playwright reads
```

Building `Lld.App` validates and packs automatically (an invalid lesson fails the build). In browser dev mode the API
serves `content/` directly and picks up edits on reload.

## Contributing
`main` is protected. Every change goes through a branch and a PR; see `CLAUDE.md`.

## License
[MIT](LICENSE)
