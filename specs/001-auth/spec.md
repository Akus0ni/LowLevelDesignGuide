# Spec 001: Auth & access (Case Study #1)

| | |
|---|---|
| **Status** | Draft, awaiting owner review |
| **Branch** | `spec/001-auth` |
| **Covers** | R22, R23, R26, R39–R43, R47, R53–R59 (PRD §13); PRD F1 + F9 |
| **Depends on** | Spec 000 (merged) |
| **Case study** | Self-referential, deep-dive lesson + interview cheat sheet (`lld-case-study` interview done 2026-10-14) |

## 1. Goal
Multi-user authentication and access control, built so the code itself is the first LLD case study:
people **request access**, the admin **approves from a console** on the PC, users sign in with a
**temporary password they must change**, and sessions use **short-lived JWTs with rotating refresh
tokens**. Every design choice is a teachable pattern (State, Strategy, Repository + Unit of Work,
Result + domain events).

## 2. User stories
1. *As a visitor,* I request access with my email (required), name and an optional note, and I'm told the admin will contact me.
2. *As the admin,* I list pending requests in the console, approve one, and get a temporary password to send to that person myself.
3. *As a new user,* I sign in with the temporary password and must set my own password before anything else.
4. *As a user,* I stay signed in for up to 30 days on a device; my session renews silently.
5. *As a user,* I can change my password, sign out, or **sign out of all devices**.
6. *As the admin,* I can create users directly (including another admin), reset a password, disable a user, and read the audit log.
7. *As the owner,* if someone steals and replays an old refresh token, every session from that login is revoked and the event is audited.

## 3. Scope

### 3.1 Domain (`Lld.Domain`)
- `EmailAddress` value object: trimmed, lower-cased, validated; the identity key for requests and users.
- `AccessRequest` aggregate with the **State pattern**: `Pending` → `Approved` | `Rejected`; invalid transitions return a failed `Result`. A new request for an email that already has a pending one **merges** into it (name/note updated).
- `RefreshToken` entity: `TokenHash`, `FamilyId`, `ExpiresAt`, `RevokedAt`, `RevokedReason`, `ReplacedByTokenId`; methods to rotate and revoke; created by a factory method.
- `AuditEntry`: append-only security event (type, user/email, timestamp, details).
- `Result` / `Result<T>` / `Error` types, and domain events (`AccessRequestSubmitted`, `AccessRequestApproved`, `AccessRequestRejected`, `UserCreated`, `PasswordChanged`, `UserLockedOut`, `RefreshTokenReuseDetected`, `SignedOutEverywhere`).

### 3.2 Application (`Lld.Application`)
- One use-case class per action: `SubmitAccessRequest`, `ApproveAccessRequest`, `RejectAccessRequest`, `CreateUser`, `ResetPassword`, `DisableUser`, `Login`, `RefreshSession`, `Logout`, `LogoutAllDevices`, `ChangePassword`, `GetCurrentUser`, `ListAccessRequests`, `ListUsers`, `ListAuditEntries`.
- Ports (interfaces): `IAccessRequestRepository`, `IRefreshTokenRepository`, `IAuditLog`, `IUnitOfWork`, `IUserAccounts` (Adapter over Identity), `ITokenIssuer`, `IPasswordPolicy`, `ITemporaryPasswordGenerator`, `IClock`, `IDomainEventDispatcher`.
- Domain events are dispatched after the unit of work commits; an audit handler writes `AuditEntry` rows (Observer).

### 3.3 Infrastructure (`Lld.Infrastructure`)
- EF Core + SQLite: `LldDbContext` (Identity tables + access requests, refresh tokens, audit entries), first migration, implements `IUnitOfWork`.
- ASP.NET Core Identity: `AppUser` (email = username, `DisplayName`, `MustChangePassword`, `IsDisabled`), roles **Admin** and **Learner**, lockout **5 failures → 15 minutes**.
- `MinLengthPasswordPolicy` (**≥ 12 characters, no composition rules, not equal to the email**), also plugged into Identity's password validation.
- `JwtTokenIssuer`: HS256 access token, **60 minutes**, claims: user id, email, roles, `must_change_password`. Signing key from configuration (never committed).
- Refresh tokens: 256-bit random, stored as SHA-256 hash, **30 days**, rotated on every use; presenting a revoked token revokes its whole family.
- Database file location from configuration, shared by the API and the admin console (default under the user's local app-data folder).

### 3.4 API (`Lld.Api`)
| Endpoint | Auth | Purpose |
|---|---|---|
| `POST /api/access-requests` | none | Submit (or merge) a request; `202` with the same message whether new or merged |
| `POST /api/auth/login` | none | Email + password → access token (+ refresh token) and user summary; `401` generic failure; `423` when locked out |
| `POST /api/auth/refresh` | refresh token | Rotate; reuse of a revoked token → `401` + family revoked |
| `POST /api/auth/logout` | refresh token | Revoke the current family |
| `POST /api/auth/logout-all` | access token | Revoke every family of the user |
| `POST /api/auth/change-password` | access token | Current + new password; clears `must_change_password`; revokes the user's other sessions |
| `GET /api/auth/me` | access token | Email, display name, roles, `mustChangePassword` |
- While `must_change_password` is set, an authorization policy allows only `change-password`, `me` and `logout`.
- **Refresh-token transport is a Strategy:** the app sends/receives it in the JSON body; the browser (`X-Lld-Client: web`) gets an **httpOnly, Secure, SameSite=Strict** cookie scoped to `/api/auth`.
- Rate limiting on `login`, `refresh` and `access-requests`.

### 3.5 Admin console (`Lld.Admin`, new project)
- .NET console app run on the PC (`dotnet run --project src/Lld.Admin -- <command>`); uses `Lld.Application` + `Lld.Infrastructure` directly; **no admin HTTP endpoints**.
- Commands: `requests list`, `requests approve <id>`, `requests reject <id>`, `users create <email> [--name] [--role admin|learner]`, `users reset-password <email>`, `users disable <email>`, `users list`, `audit list [--take N]`.
- Approve / create / reset print the temporary password **once**. The first admin is created with `users create <owner-email> --role admin`.

### 3.6 MAUI app (`Lld.App`)
- `BridgeService` gains auth methods (`RequestAccess`, `Login`, `Logout`, `LogoutAll`, `ChangePassword`, `GetSession`), calling the API through a typed `HttpClient` (service discovery name `api`).
- Tokens in `SecureStorage` (`ITokenStore`); a `DelegatingHandler` attaches the access token and refreshes it once on `401`.
- AppHost: the API is referenced by the Android emulator through a **public Dev Tunnel**; Windows uses localhost.

### 3.7 React (`web/`)
- Screens: **Sign in**, **Request access**, **Set new password** (forced, shown before anything else), **Profile** when signed in (email, role, change password, sign out, sign out of all devices).
- `DataSource` gains the same auth operations; `HttpDataSource` keeps the access token **in memory** and relies on the refresh cookie; `BridgeDataSource` delegates to C#.
- An `AuthProvider` holds session state; signed-out users can browse the placeholder sections, and Profile shows Sign in / Request access.
- Form UX per the installed skills: 16px inputs, `type="email"`, `autocomplete` hints, `enterkeyhint`, toasts via Sonner (`ask-sonner`), worst-case data checked with `break-ui`.

### 3.8 Case study content
- `content/case-studies/auth/lesson.md` and `cheatsheet.md`, written from the real code (sections per the `lld-case-study` skill), framed as "from consuming auth to building it".
- Rendering these in the app comes with the reader in spec 003; in this spec they are Markdown files in the repo.

## 4. Out of scope
- Offline queue for access requests and the "Server offline" UI (spec 002).
- Self-service "forgot password" and any email sending (the admin shares temporary passwords manually).
- External identity providers, 2FA, and an admin UI (console only).
- Hosting / ngrok (spec 002).

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | Request → admin approve (console) → temporary-password login → forced password change → normal session | Aspire integration test + manual run (browser, Windows, Android) |
| A2 | A duplicate pending request for the same email merges instead of creating a second one | Domain + API tests |
| A3 | Invalid state transitions (approve a rejected request, etc.) fail with a `Result` error, not an exception | Domain unit tests |
| A4 | Refresh rotates the token; replaying an old token revokes the whole family and writes an audit entry | Unit + integration tests |
| A5 | 5 wrong passwords lock the account for 15 minutes (`423`) | Integration test |
| A6 | Sign out of all devices revokes every session; changing the password revokes other sessions | Integration test |
| A7 | With `must_change_password`, every endpoint except change-password / me / logout returns `403` | Integration test |
| A8 | Passwords shorter than 12 characters or equal to the email are rejected | Unit tests |
| A9 | Browser mode keeps the refresh token only in an httpOnly cookie (never readable by JS); the app keeps tokens in SecureStorage | Integration test (cookie flags) + manual check |
| A10 | Admin console commands work against the live database while the API runs; the temporary password is printed once and only its hash is stored | Console tests + manual run |
| A11 | Audit log records login, failed login, lockout, approve/reject, password change, sign-out(s), token reuse | Integration test + `audit list` |
| A12 | Android emulator signs in through the Dev Tunnel | Manual run + screenshot |
| A13 | Lesson + cheat sheet exist, Mermaid renders, code links resolve | Review + Mermaid validation |
| A14 | All CI checks green; zero warnings; guardrail lint clean | CI |

## 6. LLD hooks (what the lesson teaches)
| Pattern / principle | Where |
|---|---|
| State | `AccessRequest` (`PendingState`, `ApprovedState`, `RejectedState`) |
| Strategy | `IPasswordPolicy`, `ITemporaryPasswordGenerator`, `ITokenIssuer`, refresh-token transport (body vs cookie) |
| Repository + Unit of Work | repositories over `LldDbContext`; one commit per use case |
| Result pattern | every use case returns `Result<T>`; HTTP mapping in one place |
| Domain events + Observer | audit handler subscribes to security events |
| Adapter | `IUserAccounts` over ASP.NET Core Identity `UserManager` |
| Factory | `RefreshToken.Issue(...)` |
| SRP / DIP | one class per use case; Application depends only on ports |

## 7. Decisions (owner, 2026-10-14)
| Topic | Decision |
|---|---|
| Extras in scope | Account lockout, sign out of all devices, roles (Admin/Learner), audit log |
| Patterns to showcase | State, Strategy, Repository + UoW, Result + domain events |
| Lesson depth | Deep-dive lesson + interview cheat sheet |
| Browser token storage | Access token in memory + refresh token in httpOnly cookie |
| Lifetimes | Access 60 min; refresh 30 days, rotating, reuse revokes the family; lockout 5 tries / 15 min |
| Password policy | ≥ 12 characters, no composition rules, not equal to the email |
| Offline request queue | Stays in spec 002 |

## 8. Open questions
None. Approve this spec to proceed to `plan.md` + `tasks.md`.
