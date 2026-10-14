# Spec 001: Implementation plan

Companion to [`spec.md`](./spec.md). Versions verified against NuGet/npm on 2026-10-14.

## 1. Packages
| Package | Version | Used by |
|---|---|---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.13 | Infrastructure |
| `Microsoft.EntityFrameworkCore.Sqlite` / `.Design` | 10.0.13 | Infrastructure / migrations |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.13 | Api |
| `System.CommandLine` | 2.0.13 | Admin |
| `Microsoft.Extensions.Hosting` | 10.0.13 | Admin (DI + config) |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.11.0 | tests (`FakeTimeProvider`) |
| `sonner` | 2.0.8 | web toasts |

## 2. Design

### 2.1 Domain (`Lld.Domain`)
```
Common/   Error, Result, Result<T>, IDomainEvent, AggregateRoot (collects events)
Identity/ EmailAddress (Create → Result<EmailAddress>)
Access/   AccessRequest, AccessRequestStatus, IAccessRequestState
          PendingState | ApprovedState | RejectedState (singletons), events
Sessions/ RefreshToken (Issue, Rotate, Revoke, IsActive), RevocationReason, events
Audit/    AuditEntry, AuditEventType
```
- **State pattern:** `AccessRequest` delegates `Approve`, `Reject` and `Merge` to its current state object
  (`Status` is persisted; the state object is derived from it). Only `PendingState` allows transitions.
- **Result pattern:** domain and application methods return `Result` / `Result<T>`; errors carry a stable `Code`
  (e.g. `access_request.not_pending`) that the API maps to HTTP status codes in one place.
- **Factory:** `RefreshToken.Issue(...)` creates a token in a new or existing family; `Rotate(...)` revokes the current
  token (reason `Rotated`, `ReplacedByTokenId`) and returns its successor in the same family.

### 2.2 Application (`Lld.Application`)
- One class per use case with `ExecuteAsync(command, ct) → Result<T>` (see spec §3.2).
- Ports: `IAccessRequestRepository`, `IRefreshTokenRepository`, `IAuditLog`, `IUnitOfWork`, `IUserAccounts`,
  `ITokenIssuer`, `IRefreshTokenCodec` (new raw token + SHA-256 hash), `IPasswordPolicy`,
  `ITemporaryPasswordGenerator`, `IDomainEvents` (collector for events not owned by an aggregate, e.g. `LoginFailed`).
- **Time:** `TimeProvider` (built into .NET) instead of a custom `IClock`; tests use `FakeTimeProvider`.
- **Events → audit (Observer):** `IUnitOfWork.CommitAsync` drains events from tracked aggregates and the collector,
  dispatches them to `IDomainEventHandler<T>` implementations, then saves once, so audit rows commit in the **same
  transaction** as the change (refines spec §3.2 "after commit": no audit rows can be lost).
- `AuthOptions`: access-token lifetime (60 min), refresh-token lifetime (30 days), lockout (5 / 15 min).

### 2.3 Infrastructure (`Lld.Infrastructure`)
- `LldDbContext : IdentityDbContext<AppUser>` + `AccessRequests`, `RefreshTokens`, `AuditEntries`; implements `IUnitOfWork`.
- `EmailAddress` stored via a value converter; unique index on `AccessRequests(Email)` filtered to `Status = Pending`.
- `IdentityUserAccounts` (**Adapter** over `UserManager<AppUser>`); roles **Admin** and **Learner** seeded at start-up.
- Identity options: `RequiredLength = 12`, composition rules off, `RequireUniqueEmail`, lockout 5 / 15 min;
  `PolicyPasswordValidator` bridges `IPasswordPolicy` (**Strategy**) into Identity.
- `JwtTokenIssuer` (HS256, claims `sub`, `email`, `name`, `role`, `must_change_password`).
- `CryptoTemporaryPasswordGenerator`: 16 characters from an unambiguous alphabet (no 0/O/1/l).
- Database: `Database:Path` (default `%LOCALAPPDATA%/LldGuide/lld.db`), shared by API and admin console; migrations
  applied at start-up by both. First migration: `InitialAuth`.
- `services.AddLldApplication()` / `services.AddLldInfrastructure(configuration)` extension methods.

### 2.4 API (`Lld.Api`)
- Endpoints per spec §3.4 in `Endpoints/AuthEndpoints.cs` and `Endpoints/AccessRequestEndpoints.cs`.
- `ResultHttpMapping`: `Error.Code` prefix → status (`validation` 400, `auth.invalid_credentials` 401,
  `auth.locked_out` 423, `*.not_found` 404, `*.not_pending` 409).
- Authorization: the default policy requires an authenticated user **without** `must_change_password=true`;
  policy `PasswordChangePending` (authenticated only) on `change-password` and `me`.
- **Refresh-token transport (Strategy):** `IRefreshTokenTransport` with `BodyRefreshTokenTransport` (app) and
  `CookieRefreshTokenTransport` (browser: `X-Lld-Client: web`, cookie `lld_rt`, httpOnly, Secure, SameSite=Strict,
  Path=`/api/auth`), picked per request by `RefreshTokenTransportSelector`.
- Rate limiting (fixed window per client IP) on `login`, `refresh`, `access-requests`.
- JWT signing key: config `Jwt:SigningKey` (≥ 32 bytes); fail fast if missing. Supplied by an Aspire **secret parameter**.

### 2.5 Admin (`Lld.Admin`, new)
- `Host.CreateApplicationBuilder` + `AddLldApplication/Infrastructure`; System.CommandLine root with
  `requests`, `users`, `audit` sub-commands calling the same use cases. Plain-text table output.

### 2.6 AppHost
- `jwt-signing-key` secret parameter (generated + persisted to user secrets) → `Jwt__SigningKey` on the API.
- Android: public Dev Tunnel for the API, `app-android.WithReference(api, tunnel)`; Windows `WithReference(api)`.
- Exact Aspire APIs (`AddParameter` defaults, `AddDevTunnel`) verified during implementation; deviations recorded in §4.

### 2.7 MAUI (`Lld.App`)
- `ApiClient` (typed `HttpClient`, base address `https+http://api`), `AuthHttpHandler` (bearer + single-flight refresh
  on 401), `SecureTokenStore` (`SecureStorage`).
- `BridgeService` auth methods return a `BridgeResult<T>` (`ok`, `value`, `error { code, message }`) so React gets
  typed errors instead of exceptions.

### 2.8 Web (`web/`)
- `DataSource` adds `requestAccess`, `login`, `logout`, `logoutAll`, `changePassword`, `getSession`.
- `HttpDataSource`: access token in memory, header `X-Lld-Client: web`, one automatic refresh on 401;
  `getSession()` restores the session via the refresh cookie on load.
- `AuthProvider` (`loading | signedOut | signedIn | mustChangePassword`); when `mustChangePassword` the shell shows only
  the Set-new-password screen.
- Screens under `src/features/auth/`: `SignInForm`, `RequestAccessForm`, `ChangePasswordForm`, `AccountPanel`;
  shared `TextField`; Sonner `<Toaster />` in the shell.

### 2.9 Tests
| Project | Focus |
|---|---|
| `Lld.Domain.Tests` (new) | `EmailAddress`, `AccessRequest` states, `RefreshToken` rotation/revocation |
| `Lld.Application.Tests` (new) | every use case with in-memory fakes + `FakeTimeProvider` |
| `Lld.Api.Tests` | HTTP flows on a temp SQLite file: lockout, reuse detection, must-change policy, cookie flags, logout-all, audit |
| `Lld.IntegrationTests` | Aspire end-to-end: request → approve (via application layer on the same DB) → login → change password |
| `web` (Vitest + Playwright) | `HttpDataSource` refresh, `AuthProvider`, forms; Playwright sign-in → forced change → profile with mocked API |

## 3. Risks
| Risk | Mitigation |
|---|---|
| Identity's `UserManager` saves on its own, so a use case touching users isn't one atomic transaction | Order operations so the user change happens last; document the trade-off in the lesson |
| Concurrent refreshes from the same device trip reuse detection | App: single-flight refresh in `AuthHttpHandler`; web: single in-flight refresh promise |
| SQLite file locked by API + console at the same time | WAL mode; short transactions |
| Dev Tunnel for API needs anonymous access | Use the tunnel only for the Android resource; document in README |

## 4. Outcomes (2026-10-15)

### Verification
| Check | Result |
|---|---|
| Tests | 23 domain, 27 application, 12 API, 4 admin console, 3 Aspire integration, 31 Vitest, 3 Playwright (× phone/desktop) |
| Browser (real API via Vite proxy) | Request access → `lld-admin requests approve` → temporary-password sign-in → forced change → signed in; session survives reload |
| Browser cookie | `lld_rt`: HttpOnly, Secure, SameSite=Strict, Path=/api/auth; not visible in `document.cookie` |
| Windows app | Sign-in through the bridge (React → `SessionService` → API); session restored from SecureStorage after an app restart |
| Android emulator | Sign-in through the Dev Tunnel (https); bridge reports `Android` |
| Audit log | `lld-admin audit list` shows AccessRequested → AccessApproved → UserCreated → LoginSucceeded → PasswordChanged → LoginSucceeded |
| Case study | `content/case-studies/auth/{lesson,cheatsheet}.md`; 5 Mermaid diagrams validated; 17 code links resolve |

### Deviations from the spec / plan
| Planned | Actual | Why |
|---|---|---|
| `IClock` port | .NET `TimeProvider` | Built into .NET; `FakeTimeProvider` for tests |
| Events dispatched after commit | Dispatched inside `EfUnitOfWork`, before the single `SaveChanges` | Audit rows commit in the same transaction; none can be lost |
| `IDomainEventHandler` | `IDomainEventObserver` | Analyzer CA1711 forbids the `EventHandler` suffix; the name also matches the Observer pattern |
| MAUI `AuthHttpHandler` (DelegatingHandler) | `SessionService` with `WithAccessTokenAsync` (single-flight refresh, one retry) | Simpler than a handler that must call the refresh endpoint through its own pipeline |
| API base address `https+http://api` | `http+https://api` | Windows app would reject the untrusted dev certificate on https; Android falls back to the https tunnel |
| Dev Tunnel for all API endpoints | Only the API's **https** endpoint is tunnelled | The tunnel's http port answered 502 |

### Findings worth remembering
- **Retries vs. rotation:** the standard resilience handler retries POSTs; a retried refresh presents an already-rotated
  token and trips reuse detection. MAUI service defaults now use `Retry.DisableForUnsafeHttpMethods()`.
- **Identity saves on its own:** `UserManager` commits outside the unit of work, so `ApproveAccessRequest` creates the account last.
- **JWT key:** an Aspire secret parameter (`jwt-signing-key`, generated once, persisted to the AppHost's user secrets).
  The issuer loads it lazily, so the admin console never needs it.
- **SQLite + DateTimeOffset:** stored via `DateTimeOffsetToBinaryConverter` so comparisons and ordering translate to SQL.
- **EF migrations** are marked `generated_code` in `.editorconfig` (analyzers otherwise fail the build on them).

