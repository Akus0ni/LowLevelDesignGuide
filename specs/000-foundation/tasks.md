# Spec 000: Tasks

- [x] T1 Solution scaffolding: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.slnx`, class libraries with references
- [x] T2 `Lld.ServiceDefaults` + `Lld.Api` (`/health`, `/alive`, `/api/info`) + `Lld.Contracts.AppInfo`
- [x] T3 `Lld.Api.Tests` (WebApplicationFactory) green
- [x] T4 `web/`: Vite + React + TS + React Router, shell with 5 sections, bottom tabs / sidebar
- [x] T5 Styling: Tailwind v4 tokens, custom `hover` variant, safe-area utilities, base layer, cva `Button`
- [x] T6 `DataSource` + `HttpDataSource` + `BridgeDataSource` + factory + context; About panel
- [x] T7 Guardrail checker + Vitest fixtures (A9); Vitest for factory (A6)
- [x] T8 Playwright smoke test (phone + desktop widths)
- [x] T9 `Lld.App` MAUI: HybridWebView page, `BridgeService`, web build target, status bar, app name/ID
- [x] T10 `Lld.App.ServiceDefaults`
- [x] T11 `Lld.AppHost`: api + web + MAUI (Windows device, Android emulator), `Lld:ApiOnly`
- [x] T12 `Lld.IntegrationTests` (Aspire.Hosting.Testing: `/health`, `/api/info`)
- [x] T13 Manual run: `aspire run` → dashboard, browser, Windows app, Android emulator (screenshots)
- [x] T14 CI workflow, green on PR; add required checks to ruleset
- [x] T15 Docs: README "how to run", ADR 0001, ADR 0002; fill plan §4 outcomes
- [x] T16 Open PR with screenshots + "to verify on a device" list (A10)
