# ADR 0001: React UI inside .NET MAUI HybridWebView

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-14 |
| **Spec** | 000 |

## Context
The app must run on Android, iOS, Windows and macOS, the owner wants a **React** frontend,
and the project exists to deepen **.NET/C#** skills. Data must work offline from a local
SQLite database (PRD F7).

## Options considered
| Option | For | Against |
|---|---|---|
| **MAUI HybridWebView + React** | One React UI for all platforms and the browser; C# owns storage, sync and platform APIs; first-party since .NET 9 | Web UI inside a native shell needs care to feel native; JS ↔ C# calls cross a bridge |
| MAUI Blazor Hybrid | All C#, mature | Not React |
| MAUI XAML | Fully native controls | Not React; two UIs if a browser version is wanted |
| Capacitor / Tauri | Web-first tooling | Moves the core away from .NET |

## Decision
Use **.NET MAUI `HybridWebView`** hosting a **Vite + React + TypeScript** app.

- React never talks to storage directly. It calls a `DataSource` interface with two
  implementations: `HttpDataSource` (browser dev mode, calls the API) and `BridgeDataSource`
  (inside MAUI, calls C# via `window.HybridWebView.InvokeDotNet`).
- The React bundle is built with `vite build --mode maui` and packaged as the HybridWebView
  root (`wwwroot`); only that build loads `_framework/hybridwebview.js`.
- Routing uses hash URLs because the WebView has no server to answer deep-link reloads.

## Consequences
- The same UI runs in a desktop browser for fast iteration and Playwright tests.
- Native polish (status bar, safe areas) is split between MAUI and CSS; see ADR 0002.
- Every C# method React needs must be a public instance method on `BridgeService`.
- Verified in spec 000 on Windows (WinUI WebView2). Android is verified once the
  `maui-android` workload and Dev Tunnels CLI are installed.
