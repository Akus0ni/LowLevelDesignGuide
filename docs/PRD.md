# PRD — LLD Guide

| | |
|---|---|
| **Status** | Draft v0.1, awaiting review |
| **Owner** | Akus0ni |
| **Date** | 2026-10-14 |
| **Platforms** | Android · iOS · Windows · macOS |

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
from real code.

## 3. Goals and non-goals
### Goals
1. Learn LLD end to end: concepts → patterns → diagrams → case studies.
2. Deepen .NET/C#: Aspire, ASP.NET Core, EF Core, Identity, MAUI, testing.
3. Fast revision: a **cheat sheet for every concept**, readable in under 2 minutes.
4. Personal touch: case studies can come from the owner's 6.5 years of real work.
5. One codebase that runs on Android, iOS, Windows and macOS, online and offline.

### Non-goals (MVP)
- Quizzes or gamification.
- In-app content editor (Phase 2).
- Social features, comments, sharing.
- Cloud production hosting. MVP targets local dev via Aspire; deployment is a later decision.
- High-level / distributed system design (only as "extensions" inside case studies).

## 4. Personas
| Persona | Need |
|---|---|
| **Owner (primary)**: .NET engineer, 6.5 yrs | Structured LLD learning, interview revision, a place to record their own case studies |
| **Other learners (secondary)** | Sign up, follow the same curriculum, track their own progress and notes |

## 5. Scope: MVP features

### F1. Authentication (also Case Study #1)
- Register, log in, log out, refresh session, all multi-user.
- ASP.NET Core Identity for the user store; **custom** `TokenService` issuing
  short-lived JWT access tokens and **rotating refresh tokens** persisted as entities.
- Tokens stored in MAUI `SecureStorage` on device; in memory + httpOnly cookie for the browser dev mode (to confirm in spec 001).
- **Acceptance:** a new user can register and log in on Windows and Android; access
  tokens expire and refresh silently; logout revokes the refresh token; a reused
  refresh token revokes the token family.
- **Learning output:** `content/case-studies/auth/{lesson.md, cheatsheet.md}`,
  written from this repo's real classes.

### F2. Content library
- Modules: **OOP & SOLID**, **GoF Design Patterns**, **UML**, **Case Studies**.
- Lessons authored as Markdown with YAML front-matter in `/content`; seeded into SQLite by the API.
- Reader: code highlighting (C#), Mermaid diagrams (class / sequence / state), table of contents.
- **Acceptance:** adding a `.md` file and restarting the API makes it appear in the app.

### F3. Cheat sheets
- Every concept/lesson has a companion `cheatsheet.md`.
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
- Device keeps a local SQLite copy of content, progress and notes.
- Sync with the API when online; last-write-wins per record using `UpdatedAt`, plus a device ID.
- **Acceptance:** airplane mode → edit notes and progress → reconnect → data is on the server and other devices.

## 6. Phase 2 (post-MVP)
- Admin UI to create and edit lessons in-app (Markdown remains the source of truth or gets imported; decide then).
- iOS and macOS run/verification on real Mac hardware.
- More case studies, many from the owner's own work.
- Optional: deployment target (Azure Container Apps via Aspire, or self-host).

## 7. Architecture overview
- **UI:** React + Vite + TypeScript, one UI for all platforms.
- **Native shell:** .NET MAUI `HybridWebView` hosts the React build; a C# `BridgeService` is called from JS via `HybridWebView.InvokeDotNet`.
- **Device data:** EF Core + SQLite in the MAUI app.
- **Backend:** ASP.NET Core Minimal API + EF Core + SQLite + Identity + JWT.
- **Local dev:** Aspire 13 AppHost orchestrating the API, the Vite dev server and the MAUI app (Windows device, Android emulator via Dev Tunnel), with OpenTelemetry in the Aspire dashboard.
- **Data access in UI:** `DataSource` interface. `HttpDataSource` (browser) or `BridgeDataSource` (MAUI) is chosen at startup (Strategy + Adapter).

See `docs/plan.md` for diagrams and the solution layout.

## 8. Data model (initial, refined per spec)
| Entity | Key fields | Where |
|---|---|---|
| `User` (Identity) | Id, Email, DisplayName | API |
| `RefreshToken` | Id, UserId, TokenHash, FamilyId, ExpiresAt, RevokedAt, ReplacedById | API |
| `Module` | Id, Slug, Title, Order | API + device |
| `Lesson` | Id, ModuleId, Slug, Title, Order, BodyMd, CheatSheetMd, Tags, ContentHash | API + device |
| `LessonProgress` | UserId, LessonId, Status, CompletedAt, UpdatedAt, DeviceId | API + device |
| `Note` | Id, UserId, LessonId, BodyMd, UpdatedAt, DeletedAt, DeviceId | API + device |
| `Bookmark` | UserId, LessonId, CreatedAt, DeletedAt | API + device |
| `LessonSearch` (FTS5) | Title, Body, CheatSheet | API + device |

## 9. Non-functional requirements
- **Offline:** all reading, progress and notes work without network.
- **Security:** passwords via Identity hashing; refresh tokens stored hashed; HTTPS in dev via Aspire; no secrets in the repo (user-secrets).
- **Performance:** app cold start under 3 s on a mid-range Android; lesson open under 300 ms.
- **Accessibility:** WCAG 2.1 AA contrast, keyboard navigation on desktop, respects reduced motion.
- **Quality:** each spec ships with unit tests plus at least one end-to-end test through Aspire; CI builds all four platform targets.

## 10. Success metrics (personal)
- All 4 MVP content modules have lessons **and** cheat sheets.
- At least 3 case studies, at least 1 of them from the owner's own work.
- Owner can explain the Auth case study purely from the cheat sheet.
- App runs on Windows + Android via `aspire run`; iOS/macOS compile in CI.

## 11. Open questions
1. License for the public repo (MIT?).
2. App display name and bundle ID (proposal: "LLD Guide", `com.akus0ni.lldguide`).
3. Browser dev-mode token storage (cookie vs memory). Decided in spec 001.
4. Which case studies come after Auth. Asked one at a time via the `lld-case-study` skill.
