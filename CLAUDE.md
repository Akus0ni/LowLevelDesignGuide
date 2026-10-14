# LLD Guide — project conventions

A cross-platform app (Android, iOS, Windows, macOS) for learning Low-Level Design,
built spec by spec. The app's own code is lesson material. See `docs/PRD.md` and
`docs/plan.md`.

## Working rules
- **Ask when in doubt. Do not assume.** Any ambiguity in requirements, naming,
  scope or design → ask the user before acting.
- **Spec first.** No feature code without an approved `specs/NNN-name/spec.md`.
  Flow: spec.md → user review → plan.md + tasks.md → implement → tests green →
  commit. Never start the next spec without the user's OK.
- **Case studies:** always use the `lld-case-study` skill, which interviews the
  user *before* designing. The user may contribute their own real-work versions.
- One logical change per commit; commit messages reference the spec (`spec-001: ...`).

## Git & PR workflow (main is protected)
- **Never commit or push to `main`.** A ruleset blocks direct pushes, force-pushes
  and deletion. Every change, including docs, goes through a PR.
- One branch + one PR per spec: `spec/NNN-short-name` (e.g. `spec/001-auth`).
  Non-spec changes use `chore/...`, `docs/...` or `fix/...`.
- Open the PR to `main` with `gh pr create`; the body links the spec, lists what
  changed and how it was verified (tests and manual checks).
- PRs are **squash-merged** (only allowed method); the branch is deleted on merge.
- **The owner reviews and merges.** Claude opens the PR and stops; it never merges.
- 1 approval is required; the admin may bypass it only inside a PR.
- From spec 000 on, CI checks are added as required status checks on `main`.

## Stack
- .NET 10 (LTS), C# 14, Aspire 13 (`src/Lld.AppHost`) for local orchestration.
- `src/Lld.App`: .NET MAUI with `HybridWebView` hosting the React build.
- `web/`: React + Vite + TypeScript. One UI; data access goes through the
  `DataSource` abstraction (`HttpDataSource` in browser, `BridgeDataSource` in MAUI).
- SQLite everywhere: EF Core on the API and on device.
- Auth: ASP.NET Core Identity (email = username) + custom JWT access / rotating refresh tokens.
  No self-registration: Request access → `Lld.Admin` console approves → temporary password → forced change.
- Hosting: owner's PC (daytime only) behind ngrok (Dev Tunnels fallback); server status via signed ntfy.sh topic.
  The app must stay usable offline and within ngrok free limits (20k requests, 1 GB / month).
- Requirements register: `docs/PRD.md` §13. Add every new requirement or decision there.

## Code style
- C#: follow the `clean-code-csharp` skill (SOLID, small classes, no magic values).
- Tests: xUnit + FluentAssertions; Aspire.Hosting.Testing for integration;
  Vitest + Playwright for `web/`.
- UI: use the emilkowalski skills in `.claude/skills/` (`pick-ui-library`,
  `emil-design-eng`, `apple-design`, `mobile-native`, `animate`, `break-ui`, ...).

## Content
- Lessons live in `content/<module>/<slug>/{lesson.md, cheatsheet.md}` with YAML front-matter.
- Every concept has a cheat sheet for quick pre-interview revision.
