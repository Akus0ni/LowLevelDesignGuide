# Spec 001: Tasks

- [ ] T1 Domain: Result/Error, AggregateRoot/events, EmailAddress, AccessRequest + states, RefreshToken, AuditEntry
- [ ] T2 `Lld.Domain.Tests` green
- [ ] T3 Application: ports, AuthOptions, use cases, event dispatch + audit handler
- [ ] T4 `Lld.Application.Tests` green (fakes + FakeTimeProvider)
- [ ] T5 Infrastructure: LldDbContext + configs, Identity + AppUser + roles, adapters, JWT issuer, codec, generators, DI
- [ ] T6 First EF migration `InitialAuth`; migrate + seed on start-up
- [ ] T7 API: JWT auth, policies, endpoints, Result→HTTP mapping, refresh transports, rate limiting
- [ ] T8 `Lld.Api.Tests` auth flows green
- [ ] T9 `Lld.Admin` console + tests
- [ ] T10 AppHost: JWT secret parameter, Android Dev Tunnel reference
- [ ] T11 Aspire integration test: full flow
- [ ] T12 Web: DataSource auth methods, HttpDataSource, AuthProvider, screens, Sonner, tests
- [ ] T13 MAUI: ApiClient, AuthHttpHandler, SecureTokenStore, BridgeService auth methods
- [ ] T14 Playwright auth smoke test
- [ ] T15 Manual run: browser, Windows, Android (screenshots); console approve flow
- [ ] T16 Case study: `content/case-studies/auth/lesson.md` + `cheatsheet.md`; Mermaid validated
- [ ] T17 Docs: README (signing key, admin console usage), plan §4 outcomes, progress log, PRD if needed
- [ ] T18 PR with acceptance-criteria table
