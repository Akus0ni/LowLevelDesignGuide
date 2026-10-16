# Spec 001: Tasks

- [x] T1 Domain: Result/Error, AggregateRoot/events, EmailAddress, AccessRequest + states, RefreshToken, AuditEntry
- [x] T2 `Lld.Domain.Tests` green
- [x] T3 Application: ports, AuthOptions, use cases, event dispatch + audit handler
- [x] T4 `Lld.Application.Tests` green (fakes + FakeTimeProvider)
- [x] T5 Infrastructure: LldDbContext + configs, Identity + AppUser + roles, adapters, JWT issuer, codec, generators, DI
- [x] T6 First EF migration `InitialAuth`; migrate + seed on start-up
- [x] T7 API: JWT auth, policies, endpoints, Result→HTTP mapping, refresh transports, rate limiting
- [x] T8 `Lld.Api.Tests` auth flows green
- [x] T9 `Lld.Admin` console + tests
- [x] T10 AppHost: JWT secret parameter, Android Dev Tunnel reference
- [x] T11 Aspire integration test: full flow
- [x] T12 Web: DataSource auth methods, HttpDataSource, AuthProvider, screens, Sonner, tests
- [x] T13 MAUI: ApiClient, AuthHttpHandler, SecureTokenStore, BridgeService auth methods
- [x] T14 Playwright auth smoke test
- [x] T15 Manual run: browser, Windows, Android (screenshots); console approve flow
- [x] T16 Case study: `content/case-studies/auth/lesson.md` + `cheatsheet.md`; Mermaid validated
- [x] T17 Docs: README (signing key, admin console usage), plan §4 outcomes, progress log, PRD if needed
- [x] T18 PR with acceptance-criteria table
