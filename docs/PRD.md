# PRD — LLD Guide

| | |
|---|---|
| **Status** | v0.3 |
| **Owner** | Akus0ni |
| **Last updated** | 2026-10-14 |
| **Platforms** | Android · iOS · Windows · macOS |
| **App name / ID** | LLD Guide · `com.akus0ni.lldguide` |
| **License** | MIT |
| **Repo** | https://github.com/Akus0ni/LowLevelDesignGuide (public) |

### Changelog
| Version | Date | Change |
|---|---|---|
| v0.1 | 2026-10-14 | First draft: vision, MVP features F1–F7, architecture, data model |
| v0.2 | 2026-10-14 | Content bundled in app; self-hosting on owner's PC via ngrok (F8); access by request + admin console (F9); email as username; server status via signed ntfy.sh topic; development process requirements; full requirements register (§13) |
| v0.3 | 2026-10-14 | Spec 001 decisions: lockout, sign out of all devices, roles, audit log, token storage and lifetimes, password policy (R53–R60) |
| v0.4 | 2026-10-15 | Spec 002 decisions: status signing and freshness, quota stop at 95 %, DPAPI secrets + installer, daily local backup, localhost-only API (R61–R66) |

---

## 1. Problem
Low-Level Design is learned from scattered blog posts, videos and repos. None of
them combine theory, real C# code and fast revision in one place, and none tie
LLD to the code you actually write day to day. Revising before an interview
means re-reading long articles.

## 2. Vision
**A personal, cross-platform LLD handbook that is also its own case study.**
The app teaches OOP, SOLID, design patterns, UML and LLD case studies. The app
itself is designed with those same principles and built one spec at a time, so
every feature (starting with authentication) becomes a worked example drawn
from real code. It doubles as the owner's self-help guide for deepening .NET/C#.

## 3. Goals and non-goals
### Goals
1. Learn LLD end to end: concepts → patterns → diagrams → case studies.
2. Deepen .NET/C#: Aspire, ASP.NET Core, EF Core, Identity, MAUI, testing.
3. Fast revision: a **cheat sheet for every concept**, readable in under 2 minutes, for pre-interview review.
4. Personal touch: case studies can come from the owner's 6.5 years of real work.
5. One codebase that runs on Android, iOS, Windows and macOS, online and offline.
6. Learn by building: the app is built **by applying the LLD concepts it teaches**, and its own code is used as lesson material.

### Non-goals (MVP)
- Quizzes or gamification.
- In-app content editor (Phase 2).
- Social features, comments, sharing.
- Cloud hosting. The API is **self-hosted on the owner's PC** and exposed through a tunnel (F8).
- Open self-registration. Access is **by request and admin approval** (F1, F9).
- Paid services of any kind (see constraints).
- High-level / distributed system design (only as "extensions" inside case studies).

## 4. Personas
| Persona | Need |
|---|---|
| **Owner (primary learner)**: .NET engineer, 6.5 yrs | Structured LLD learning, interview revision, a place to record their own case studies, hands-on .NET practice |
| **Other learners (100–500)** | Request access, follow the same curriculum, track their own progress and notes on any device |
| **Admin (the owner)** | Approve access requests, create users, reset passwords, from a console on their PC |

## 5. Constraints
| # | Constraint |
|---|---|
| C1 | **Free only.** No paid hosting, database, auth or notification services. |
| C2 | Server runs on the **owner's PC**, which is on **only during the day**. Downtime is normal. |
| C3 | **No Mac for now.** Windows + Android are developed and verified first. iOS/macOS builds (CI and devices) are **Phase 2**. |
| C4 | Solo developer; public GitHub repo; `main` is protected and changes go through PRs. |
| C5 | Stack fixed: .NET 10, Aspire 13, .NET MAUI (HybridWebView), React + Vite + TypeScript, SQLite. |
| C6 | Not Cloudflare for tunnelling (owner's choice). |
| C7 | ngrok free limits: 20,000 HTTP requests and 1 GB data per month, 1 fixed dev domain. |

## 6. Scope: MVP features

### F1. Authentication and access (also Case Study #1)
- **Email is the username** everywhere (request access, login). It is mandatory and unique.
- **No self-registration.** A "Request access" page collects **email (required)**, name and an optional note.
  - If the server is offline, the request is **saved on the device** and sent automatically when the server is back online. The user sees "Request saved, will be sent when the server is online".
  - A request moves through **Pending → Approved | Rejected**. Duplicate pending requests for the same email are merged.
- **Admin approval** happens in the admin console (F9). Approving creates the user with a **temporary password**, which the console prints and the admin **shares with the user manually** (no email is sent by the system).
- **First login with a temporary password** forces a "Set new password" screen before anything else. The temporary password stops working once changed.
- Log in, log out, refresh session, change password, **sign out of all devices**.
- **Account lockout** after 5 failed logins (15 minutes). **Roles:** Admin and Learner. **Audit log** of security events.
- Password policy: at least 12 characters, no composition rules, not equal to the email.
- ASP.NET Core Identity for the user store; **custom** `TokenService` issuing JWT access tokens (**60 minutes**) and **rotating refresh tokens** (30 days) persisted as entities; reusing a revoked refresh token revokes the whole token family.
- Tokens stored in MAUI `SecureStorage` on device; in browser dev mode the access token stays in memory and the refresh token in an httpOnly cookie.
- **Acceptance:** a request made offline reaches the server once it is online; the admin approves it from the console and gets a temporary password; the user logs in with it, is forced to set a new password, and continues; access tokens expire and refresh silently; logout revokes the refresh token; a reused refresh token revokes the whole token family.
- **Learning output:** `content/case-studies/auth/{lesson.md, cheatsheet.md}`, written from this repo's real classes (request state machine, token rotation, password policy, admin console as a second client of the same application layer).

### F2. Content library
- Modules: **OOP & SOLID**, **GoF Design Patterns**, **UML**, **Case Studies**.
- Lessons authored as Markdown with YAML front-matter in `/content` (Phase 1). An in-app admin editor comes in Phase 2.
- **Content ships inside the app** (packaged as MAUI assets). On first launch, or when the bundled content version changes, the app loads it into **local SQLite**. Reading never needs the network.
- The API exposes the same content **read-only**, parsed from the same folder by a shared library (`Lld.Content`). It is used only by browser dev mode and tests; devices never depend on it.
- Reader: code highlighting (C#), Mermaid diagrams (class / sequence / state), table of contents.
- **Acceptance:** a fresh install in airplane mode shows all lessons; adding a `.md` file and rebuilding the app makes it appear.

### F3. Cheat sheets
- **Every concept/lesson has a companion `cheatsheet.md`** for quick revision before interviews.
- Dedicated "Revise" view listing all cheat sheets, filterable by module.
- **Acceptance:** each lesson links to its cheat sheet in one tap; a cheat sheet fits one phone screen with at most 2 scrolls.

### F4. Progress tracking
- Mark a lesson done; % complete per module and overall; "continue where you left off".
- **Acceptance:** progress survives app restart and syncs between devices for the same user.

### F5. Personal notes and bookmarks
- Free-text notes per lesson (Markdown); bookmark lessons; a notes/bookmarks list view.
- **Acceptance:** notes are created offline and appear on another device after sync.

### F6. Search
- Full-text search across lessons, cheat sheets and the user's own notes (SQLite FTS5).
- **Acceptance:** results return in under 200 ms on device for the MVP content set; matches are highlighted.

### F7. Offline-first sync
- Device SQLite is the **source of truth for the UI**: every read and write is local.
- Only **user data** syncs (progress, notes, bookmarks). Content is bundled, not synced.
- `SyncService` pushes local changes and pulls remote ones in **one combined call**; last-write-wins per record using `UpdatedAt`, plus a device ID.
- Sync runs on app open (if the last sync was over 15 minutes ago), on "Sync now", when the app goes to the background with pending changes, and automatically when the server comes back online (F8). There is **no timer-based polling**.
- First login needs the network; afterwards the session is kept in `SecureStorage` and the app works offline.
- **Acceptance:** airplane mode → edit notes and progress → reconnect → data is on the server and other devices.

### F8. Self-hosting and server status
- The API runs on the **owner's PC**, only while it is on. Downtime is expected and handled.
- Exposed to the internet through **ngrok** (free fixed `*.ngrok-free.dev` domain, HTTPS, no open router ports). **Microsoft Dev Tunnels** is the documented fallback (no published request cap, 5 GB/month; generated URL; deleted after 30 idle days).
- The API address is an **app setting**, so switching tunnels is a configuration change, not a code change.
- Every API request from the app sends the `ngrok-skip-browser-warning` header (avoids ngrok's browser warning page).
- **Server status signal** (replaces a webhook, because devices cannot receive inbound calls):
  - The PC publishes **signed** `online` / `offline` messages to a secret **ntfy.sh** topic on startup and graceful shutdown.
  - Open apps receive the message live; newly opened apps read the latest message. Checking status uses **no ngrok quota**.
  - The app verifies the signature with the server's public key; unsigned or invalid messages are ignored.
  - A failed sync also marks the server offline (covers crashes and power loss, where no `offline` message is sent).
- **While offline:** every app shows "Server offline", the **Sync button is disabled for everyone**, reading/notes/progress keep working locally, and access requests are queued.
- **When back online:** the Sync button is re-enabled, and pending changes and queued access requests are sent automatically.
- **Request budget:** the app must stay under ngrok's free limits (C7) at 500 users. Means: bundled content, one combined sync call, no polling, ~1-hour access tokens, API rate limiting, client retry with back-off. If a limit is hit, the app behaves as "server offline" until the monthly reset.
- **Operations:** API and ngrok run as **Windows services** (start at boot, no login needed); daily SQLite backup at 13:00 to a local folder, catching up after a missed run (the PC is off at night; decided in spec 002); health endpoint; sleep disabled while plugged in.
- **Acceptance:** stopping the API flips all open apps to "Server offline" with Sync disabled; starting it flips them back and pending changes sync without user action; a spoofed ntfy message has no effect.

### F9. Admin console (`Lld.Admin`)
- A .NET console app that runs **on the owner's PC only**. It uses the same application and database code as the API directly; **no admin endpoints are exposed** over the tunnel.
- Commands: `requests list`, `requests approve <id>` (prints a temporary password), `requests reject <id>`, `users create <email>` (prints a temporary password), `users reset-password <email>`, `users list`, `users disable <email>`.
- Temporary passwords are random, meet the password policy, are shown once, and only their hash is stored. The user must change it on first login.
- **Acceptance:** each command works against the live database while the API is running, and every action is logged.

## 7. Content requirements
- **Modules (MVP):** OOP & SOLID · GoF design patterns · UML (Mermaid) · LLD case studies.
- **Every lesson has a cheat sheet** (F3).
- **Case Study #1 is Auth**, explained using this app's real code (F1).
- **Every other case study starts by asking the owner** whether to use their own real-work version, plus scope, scale and emphasis. Enforced by the project skill `.claude/skills/lld-case-study`. Nothing is designed silently.
- Case study template: problem → requirements → entities → class diagram → sequence/state diagrams → patterns and why → SOLID mapping → C# code → trade-offs → interview follow-ups, plus a one-screen cheat sheet.
- C# samples must compile (real `src/` code or `samples/`).

## 8. Phase 2 (post-MVP)
- Admin UI to create and edit lessons in-app (Markdown stays the source of truth or gets imported; decide then).
- **Apple builds:** re-enable Apple CI (currently manual-only) and run/verify iOS + macOS on a Mac. Findings and to-dos: `docs/plan.md` §8.
- More case studies, many from the owner's own work.
- Optional: move from the PC + ngrok to cloud hosting if usage outgrows the free tunnel.

## 9. Architecture overview
- **UI:** React + Vite + TypeScript, one UI for all platforms.
- **Native shell:** .NET MAUI `HybridWebView` hosts the React build; a C# `BridgeService` is called from JS via `HybridWebView.InvokeDotNet`.
- **Device data:** EF Core + SQLite in the MAUI app, holding bundled content plus the user's progress, notes, bookmarks and queued access requests.
- **Backend:** ASP.NET Core Minimal API + EF Core + SQLite + Identity + JWT. It owns accounts and access requests and is the sync hub for user data; content is read-only, for browser dev mode.
- **Hosting:** owner's PC (Windows services) exposed through ngrok (Dev Tunnels fallback); server status published to a signed ntfy.sh topic.
- **Admin:** `Lld.Admin` console on the PC, sharing the application layer with the API.
- **Local dev:** Aspire 13 AppHost orchestrating the API, the Vite dev server and the MAUI app (Windows device; Android emulator via Dev Tunnel), with OpenTelemetry in the Aspire dashboard.
- **Data access in UI:** `DataSource` interface; `HttpDataSource` (browser) or `BridgeDataSource` (MAUI) chosen at startup (Strategy + Adapter).

See `docs/plan.md` for diagrams, solution layout and the spec roadmap.

## 10. Data model (initial, refined per spec)
| Entity | Key fields | Where |
|---|---|---|
| `User` (Identity) | Id, Email (= username), DisplayName, MustChangePassword, IsDisabled | API |
| `AccessRequest` | Id, Email, Name, Note, Status (Pending/Approved/Rejected), CreatedAt, DecidedAt | API (queued on device while offline) |
| `RefreshToken` | Id, UserId, TokenHash, FamilyId, ExpiresAt, RevokedAt, ReplacedById | API |
| `Module` | Id, Slug, Title, Order | Device (bundled); API read-only for dev |
| `Lesson` | Id, ModuleId, Slug, Title, Order, BodyMd, CheatSheetMd, Tags, ContentHash | Device (bundled); API read-only for dev |
| `LessonProgress` | UserId, LessonId, Status, CompletedAt, UpdatedAt, DeviceId | API + device |
| `Note` | Id, UserId, LessonId, BodyMd, UpdatedAt, DeletedAt, DeviceId | API + device |
| `Bookmark` | UserId, LessonId, CreatedAt, DeletedAt | API + device |
| `LessonSearch` (FTS5) | Title, Body, CheatSheet, user notes | Device (API copy for browser dev only) |

## 11. Non-functional requirements
- **Offline:** all reading, progress and notes work without network.
- **Availability:** the server is up only while the owner's PC is on. The app must be fully usable offline and recover automatically when the server returns.
- **Security:** Identity password hashing; refresh tokens and temporary passwords stored hashed; HTTPS via Aspire in dev and via ngrok when hosted; no admin endpoints exposed; API rate limiting; signed server-status messages; no secrets in the repo (user-secrets).
- **Request budget:** stay under 20,000 requests and 1 GB per month at 500 users.
- **Backups:** daily SQLite backup (13:00, catch-up after a missed run), 14 days kept.
- **Performance:** app cold start under 3 s on a mid-range Android; lesson open under 300 ms.
- **Accessibility:** WCAG 2.1 AA contrast, keyboard navigation on desktop, respects reduced motion.
- **Quality:** each spec ships with unit tests plus at least one end-to-end test through Aspire; CI builds Windows + Android (Apple builds join in Phase 2).

## 12. Development process requirements
| # | Requirement |
|---|---|
| P1 | **Ask when in doubt; never assume.** Ambiguities are resolved with the owner before acting. |
| P2 | **Plan before code.** PRD and `docs/plan.md` are approved before implementation starts. |
| P3 | **Spec-wise build.** Each feature has `specs/NNN-name/{spec.md, plan.md, tasks.md}`; the spec is reviewed by the owner before implementation. |
| P4 | **One branch + one PR per spec** (`spec/NNN-name`); nothing is pushed to `main` directly. |
| P5 | `main` protected by the "Protect main" ruleset: no direct push, force-push or deletion; PR with 1 approval (admin may bypass only inside a PR); squash merge only; linear history; branch deleted on merge; CI checks required from spec 000. |
| P6 | **The owner reviews and merges every PR.** Claude opens PRs and never merges. |
| P7 | Local end-to-end dev and testing run under **Aspire**. |
| P8 | All 15 **emilkowalski/skills** are installed project-scoped and used for UI work; the custom `lld-case-study` skill is used for every case study; `clean-code-csharp` for C#. |
| P9 | Every decision is recorded in this PRD (§13) and in `docs/plan.md` (decisions log). |

## 13. Requirements register
Every requirement and decision gathered so far, so none is skipped. "Covered by" points at the section or feature; "Spec" is where it gets built (numbering per `docs/plan.md` §3).

| ID | Requirement / decision | Covered by | Spec |
|---|---|---|---|
| R01 | A self-help guide to learn LLD, supporting .NET/C# learning | §2, §3 | all |
| R02 | Build it spec by spec | P3 | all |
| R03 | Apps for Android, iOS, Windows and macOS | §9, C3 | 000 |
| R04 | React frontend | §9 | 000 |
| R05 | .NET MAUI shell (HybridWebView hosting React) | §9 | 000 |
| R06 | SQLite database (device and server) | §9, §10 | 000+ |
| R07 | Aspire for local dev and end-to-end testing | P7 | 000 |
| R08 | Write a PRD with a small, incremental approach first | this doc | — |
| R09 | Install all 15 emilkowalski skills in this repo and use them | P8 | done |
| R10 | Initialise as a public GitHub repo | header | done |
| R11 | Ask when in doubt; do not assume | P1 | all |
| R12 | Product = learning app + spec-wise build (repo is the course) | §2 | all |
| R13 | Offline-first device SQLite plus an API with sync | F7, §9 | 010 |
| R14 | No Mac now: Windows + Android first; iOS/macOS later | C3, §8 | 000, P2 |
| R15 | Content: OOP & SOLID, GoF patterns, UML, LLD case studies | F2, §7 | 004, 006, 008, 011+ |
| R16 | Owner's own work in case studies; ask before designing each one; a spec skill for case studies | §7 | 011+ |
| R17 | Progress tracking | F4 | 005 |
| R18 | Personal notes (and bookmarks) | F5 | 007 |
| R19 | Search | F6 | 009 |
| R20 | Cheat sheet for every concept, for pre-interview revision | F3 | 003 |
| R21 | No quizzes | §3 | — |
| R22 | Multi-user with login | F1 | 001 |
| R23 | Auth is Case Study #1, explained with this app's own code | F1, §7 | 001 |
| R24 | Build the app by applying LLD concepts; use its code in lessons | §3 goal 6 | all |
| R25 | Content authored as Markdown first; admin UI in a later phase | F2, §8 | 003, P2 |
| R26 | ASP.NET Core Identity + custom JWT and refresh tokens | F1 | 001 |
| R27 | Lightweight `/specs` folder process | P3 | all |
| R28 | `plan.md` before implementing | P2 | done |
| R29 | A PR to `main` for every spec; keep `main` safe | P4, P5 | all |
| R30 | 1 approval with admin bypass; squash merge; owner merges | P5, P6 | all |
| R31 | MIT license | header | done |
| R32 | App name "LLD Guide", ID `com.akus0ni.lldguide` | header | 000 |
| R33 | Interleaved build order (features and content alternate) | plan §3 | — |
| R34 | Lessons bundled in the app; API only for accounts and user-data sync | F2, F7 | 003, 010 |
| R35 | Free hosting for 100–500 users | C1, F8 | 002 |
| R36 | Host on the owner's PC; no Cloudflare; ngrok with Dev Tunnels fallback | F8, C6 | 002 |
| R37 | Deployment built early, right after Auth | plan §3 | 002 |
| R38 | PC on only during the day; downtime accepted | C2, F8 | 002 |
| R39 | "Request access" page instead of open sign-up | F1 | 001 |
| R40 | Admin console app creates users with a temporary password | F9 | 001 |
| R41 | Temporary password must be changed after first login | F1 | 001 |
| R42 | Temporary password shared manually by the admin | F1, F9 | 001 |
| R43 | Admin console runs directly on the PC; no admin API | F9 | 001 |
| R44 | Server offline → "Server offline" shown, Sync button disabled for everyone | F8 | 002, 010 |
| R45 | Server back online → all subscribed devices are notified and sync is re-enabled (signed ntfy.sh topic instead of a webhook) | F8 | 002, 010 |
| R46 | Access requests made offline are queued on device and sent when online | F1, F8 | 002 |
| R47 | Email is mandatory and is the username for requests and login | F1 | 001 |
| R48 | Keep this PRD complete so nothing is skipped | §13, P9 | ongoing |
| R49 | Styling: Tailwind v4 + clsx/cva + base-ui, critiqued with the installed skills; guardrails enforced by theme tokens and lint | spec 000 §3.3, ADR 0002 | 000 |
| R50 | No physical Android device for now: emulator only; hardware-only checks listed as pending in each PR | C3, spec 000 A10 | 000+ |
| R51 | Apple CI disabled for now (manual-only workflow); fixing and re-enabling it is Phase 2, with all findings kept in the plan | §8, plan §8 | P2 |
| R52 | README has a "Local setup" section listing every manual dependency so any developer can build and run the app | README | 000 |
| R53 | Account lockout: 15 minutes after 5 failed logins | F1 | 001 |
| R54 | Users can sign out of all devices (revoke every session) | F1 | 001 |
| R55 | Roles Admin and Learner, carried in the access token; first admin created from the console | F1, F9 | 001 |
| R56 | Audit log of security events, readable from the admin console | F1, F9 | 001 |
| R57 | Browser dev mode: access token in memory, refresh token in an httpOnly cookie; app uses SecureStorage | F1 | 001 |
| R58 | Lifetimes: access token 60 min; refresh token 30 days, rotating, reuse revokes the family | F1 | 001 |
| R59 | Password policy: at least 12 characters, no composition rules, not equal to the email | F1 | 001 |
| R60 | Auth case study: self-referential, deep-dive lesson + interview cheat sheet, showcasing State, Strategy, Repository + Unit of Work, Result + domain events | §7 | 001 |
| R61 | Server status messages signed with ECDSA P-256; the public key is committed in the app settings, the ntfy topic is injected at build time from a git-ignored file | F8 | 002 |
| R62 | Status freshness: heartbeat every 6 h; with no news for 7 h the app shows "unknown" (treated as offline, but sign-in and access requests are still attempted) | F8 | 002 |
| R63 | At 95 % of either ngrok monthly limit the API announces "offline (quota)" until the reset; usage shown by `lld-admin status budget` | F8, C7 | 002 |
| R64 | Hosted secrets (JWT key, status private key, topic) live in DPAPI-protected, ACL-restricted files; one idempotent installer, `deploy/install.ps1` | F8 | 002 |
| R65 | Daily backup at 13:00 (catches up after a missed run) to a local folder, `D:\Applications\LldGuide\backups`, keeping 14 days | F8 | 002 |
| R66 | Hosted API listens on localhost only (ngrok is the only way in); `/health` is public without details; global per-client-IP rate limit | F8 | 002 |

## 14. Success metrics (personal)
- All 4 MVP content modules have lessons **and** cheat sheets.
- At least 3 case studies, at least 1 of them from the owner's own work.
- Owner can explain the Auth case study purely from the cheat sheet.
- App runs on Windows + Android via `aspire run`; iOS/macOS build and run in Phase 2.
- Real users can request access, be approved and sync through the self-hosted API, staying within the ngrok free limits.

## 15. Open questions
1. Which case studies come after Auth. Asked one at a time via the `lld-case-study` skill.

## 16. Glossary
| Term | Meaning |
|---|---|
| LLD | Low-Level Design: class-level design of a system (entities, relationships, patterns) |
| HybridWebView | .NET MAUI control that hosts a web app (here React) and lets JS call C# |
| Aspire | .NET tooling that runs and observes all parts of the app together in local dev |
| ngrok / Dev Tunnels | Services that give a PC a public HTTPS address without opening router ports |
| ntfy.sh | Free publish/subscribe notification service; used here to broadcast the server's online/offline status |
| Temporary password | One-time password created by the admin; the user must replace it at first login |
