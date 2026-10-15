# Spec 002: Tasks

- [x] T1 `Lld.ServerStatus`: message, envelope, signer, verifier, tracker; shared test vector in `tests/fixtures/server-status/`
- [x] T2 `Lld.ServerStatus.Tests` green
- [x] T3 Infrastructure: protected-secrets config source, `UsageCounter` + migration `AddUsageCounter`, `SqliteBackupService`, `NtfyClient`
- [ ] T4 API: Windows service, `appsettings.Production.json`, forwarded headers, global rate limiter, request budget middleware + flusher + OTel metric
- [ ] T5 API: `ServerStatusPublisher` (health probe, online/heartbeat/quota/shutdown)
- [ ] T6 `Lld.Api.Tests` for T4–T5 green
- [ ] T7 Admin: `hosting init`, `status keygen --force`, `status publish`, `status budget`, `backup run/list` + tests
- [ ] T8 `deploy/install.ps1`, `uninstall.ps1`, `ngrok.template.yml`; `docs/hosting.md` (ngrok sign-up, Dev Tunnels fallback, troubleshooting)
- [ ] T9 MAUI: embedded settings (+ optional local file), base-URL strategy, `TunnelHeadersHandler`, `ServerReachabilityHandler`
- [ ] T10 MAUI: `ServerStatusMonitor` (poll + stream + lifecycle), bridge push via `SendRawMessage` (verify API first), `AccessRequestOutbox`
- [ ] T11 Web: types, TS verifier + tracker, `BridgeDataSource`/`HttpDataSource` status + outbox, `ServerStatusProvider`, `useServerStatus`
- [ ] T12 Web: `ServerOfflineBanner`, request-access queued UX, outbox toast; Vitest + Playwright green
- [ ] T13 Owner setup (with guide): ngrok account + static domain, run `install.ps1`, commit the public key printed by `hosting init`
- [ ] T14 Manual run: A1 reboot, A2 phone on mobile data, A3 stop/start flip (Windows app, browser, phone), A6 kill, A8 LAN check, A10 install twice + uninstall, A11 ACL check; screenshots
- [ ] T15 Docs: README (hosting section link), PRD §13 new decisions, plan §4 outcomes, progress log
- [ ] T16 PR with acceptance-criteria table
