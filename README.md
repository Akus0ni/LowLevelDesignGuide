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
- Specs: [`specs/`](specs/)
- Decisions: [`docs/adr/`](docs/adr/)

## Prerequisites
| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.401+ | pinned in `global.json` |
| .NET workloads | `maui-windows`, `maui-android` | `dotnet workload install maui-android` (admin terminal) |
| Aspire CLI | 13.6.1 | `dotnet tool install -g Aspire.Cli --version 13.6.1` |
| Node.js | 24 | npm comes with it |
| Android emulator | any AVD | name set in `src/Lld.AppHost/appsettings.json` (`Lld:AndroidEmulatorId`) |
| Dev Tunnels CLI | latest | Android/iOS only: `winget install Microsoft.devtunnel`, then `devtunnel user login` |

Recommended once per machine: `aspire certs trust` (trusts the HTTPS dev certificate, so the
browser shows no warning and the dashboard keeps its live connection).

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
| `app-android` | MAUI app on the Android emulator | Start it from the dashboard (needs the Android workload + Dev Tunnels) |

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
