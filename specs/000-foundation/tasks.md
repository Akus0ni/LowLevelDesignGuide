# Spec 000: Tasks

- [ ] T1 Solution scaffolding: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.slnx`, class libraries with references
- [ ] T2 `Lld.ServiceDefaults` + `Lld.Api` (`/health`, `/alive`, `/api/info`) + `Lld.Contracts.AppInfo`
- [ ] T3 `Lld.Api.Tests` (WebApplicationFactory) green
- [ ] T4 `web/`: Vite + React + TS + React Router, shell with 5 sections, bottom tabs / sidebar
- [ ] T5 Styling: Tailwind v4 tokens, custom `hover` variant, safe-area utilities, base layer, cva `Button`
- [ ] T6 `DataSource` + `HttpDataSource` + `BridgeDataSource` + factory + context; About panel
- [ ] T7 Guardrail checker + Vitest fixtures (A9); Vitest for factory (A6)
- [ ] T8 Playwright smoke test (phone + desktop widths)
- [ ] T9 `Lld.App` MAUI: HybridWebView page, `BridgeService`, web build target, status bar, app name/ID
- [ ] T10 `Lld.App.ServiceDefaults`
- [ ] T11 `Lld.AppHost`: api + web + MAUI (Windows device, Android emulator), `Lld:ApiOnly`
- [ ] T12 `Lld.IntegrationTests` (Aspire.Hosting.Testing: `/health`, `/api/info`)
- [ ] T13 Manual run: `aspire run` → dashboard, browser, Windows app, Android emulator (screenshots)
- [ ] T14 CI workflow, green on PR; add required checks to ruleset
- [ ] T15 Docs: README "how to run", ADR 0001, ADR 0002; fill plan §4 outcomes
- [ ] T16 Open PR with screenshots + "to verify on a device" list (A10)
