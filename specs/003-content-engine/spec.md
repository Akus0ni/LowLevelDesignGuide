# Spec 003: Content engine (reader)

| | |
|---|---|
| **Status** | Draft; open questions answered (2026-10-16), waiting for owner approval |
| **Branch** | `spec/003-content-engine` |
| **Covers** | R20, R25, R34 (content part) (PRD §13); PRD F2, F3 |
| **Depends on** | Spec 000 (shell, `DataSource`), spec 002 (merged, PR #4) |
| **Case study** | None (feature spec). The content pipeline is worth an LLD note later (see §6). |

## 1. Goal
Make lessons and cheat sheets readable in the app **with no network at all**. Markdown under `content/`
is validated and packed at build time, shipped inside the app, and loaded into the device's SQLite on
first launch (or when the content changes). React renders it: headings with a table of contents, C#
highlighting and Mermaid diagrams. A "Revise" tab lists every cheat sheet for quick review before an
interview. The browser dev mode reads the same content from a read-only API.

## 2. User stories
1. *As a learner,* I install the app with no network and can browse modules, read lessons and open their cheat sheets.
2. *As a learner,* I read a lesson with highlighted C# code, rendered diagrams and an "On this page" outline, in light or dark mode.
3. *As a learner,* from any lesson I open its cheat sheet in one tap, and in "Revise" I see all cheat sheets, filtered by module.
4. *As a learner,* I move through a module with previous / next links, the app reopens a lesson where I left off, and I can pick a text size.
5. *As the owner (author),* I add a folder with `lesson.md` + `cheatsheet.md`, rebuild, and it shows up. If I make a mistake (missing cheat sheet, duplicate id, broken link), the build tells me which file and why.
6. *As the owner,* while writing in browser dev mode, a saved `.md` file shows up on the next page load without restarting anything.

## 3. Scope

### 3.1 Content format (authoring contract)
```
content/
  <module>/                  e.g. case-studies, oop-solid, start
    module.yml               title, order, summary
    <lesson-slug>/
      lesson.md              required
      cheatsheet.md          required (F3: every lesson has one)
```
- **`module.yml`:** `title`, `order`, `summary` (one sentence). The folder name is the module's slug.
- **`lesson.md` front-matter:** `id` (stable, unique, never changes once published: progress, notes and bookmarks will
  point at it), `title`, `order` (within the module), `summary` (one sentence, shown in lists), `tags`, optional `source`.
  The module comes from the folder; the current `module:` key is removed so there is one source of truth.
- **`cheatsheet.md` front-matter:** `title` only. Module, order and tags come from its lesson.
- **Derived at pack time:** reading time, the heading outline (h2/h3 with stable anchors), a content hash per lesson.
- **Links inside content:**
  - to another lesson or cheat sheet (`../../oop-solid/srp/lesson.md`, optionally `#anchor`) → in-app navigation;
  - to repo files (`../../../src/...`) → the file on GitHub (`main`), opened in the external browser;
  - absolute `https://` links → external browser.
- **Images are out of scope** for now (no lesson uses them yet).

### 3.2 Validation and packing (`Lld.Content`, shared library)
- Parses YAML front-matter and Markdown structure (headings, links, code fences). It does **not** produce HTML.
- **Validation rules**, each reported with file path (and line where it applies):
  missing `module.yml` / `lesson.md` / `cheatsheet.md`; missing or wrongly typed front-matter keys; unknown keys;
  duplicate lesson `id`; duplicate `order` within a module; broken internal links or anchors; links to repo files
  that don't exist; empty Mermaid blocks.
- **Pack:** validated content is written to one `content-pack.json` (modules, lessons, cheat sheets, outlines, a
  pack version = hash of all content). **Invalid content fails the build** of the app and the CI `backend` job.
- Mermaid syntax itself is checked at render time (§3.5), not at build time.

### 3.3 Device (`Lld.App`)
- `content-pack.json` is a MAUI asset. On launch the app compares the pack version with the one stored in the
  device SQLite (EF Core). When it differs, content tables are replaced in **one transaction**; when equal, nothing is loaded.
- This is the **first device database**. It holds only content for now; progress, notes, bookmarks and sync come in
  specs 005, 007 and 010. The content store lives in a plain .NET library so it can be unit-tested outside MAUI.
- `BridgeService` gains the content methods (§3.4); all reads are local, so reading works offline and while the server is offline.

### 3.4 Data access (`DataSource` + API)
- `DataSource` gains: `getModules()`, `getModule(slug)` (with its lesson list), `getLesson(id)` (Markdown + outline +
  prev/next), `getCheatSheets(moduleSlug?)`, `getCheatSheet(lessonId)`.
- `BridgeDataSource` → `BridgeService` → device SQLite. `HttpDataSource` → read-only API.
- **API (browser dev mode only):** `GET /api/content/modules`, `/modules/{slug}`, `/lessons/{id}`, `/cheatsheets?module=`,
  `/cheatsheets/{lessonId}`. Anonymous (open reading). Mapped **only in the Development environment**: the hosted
  service never serves content, so it costs no ngrok budget. It reads `content/` with `Lld.Content` and reloads when a
  file changes (story 6).
- Shared DTOs in `Lld.Contracts`, mirrored in `web/src/data/types.ts`.

### 3.5 Reader (`web/`)
- **Open reading:** Learn and Revise work without signing in.
- **Learn tab:** modules (title, summary, lesson count) → module page (ordered lessons with summary and reading time)
  → lesson reader at `/learn/<module>/<lesson-slug>`.
- **Rendering:** Markdown rendered in React (react-markdown + GFM tables). C# highlighting with **Shiki**, limited to
  the languages the content uses (csharp, ts/tsx, json, bash/powershell, xml, yaml, sql), with light and dark themes.
  **Mermaid** is loaded only when a lesson has a diagram, follows light/dark, and shows the source plus an error note if
  a diagram fails to render.
- **Table of contents:** h2/h3 outline. Desktop: sticky side column. Phone: an "On this page" button that opens a sheet.
- **Cheat sheet in one tap** from the lesson header; the cheat sheet links back to its lesson.
- **Previous / next** links at the end of a lesson, in module order.
- **Reading position:** reopening a lesson scrolls back to where the reader left it (saved on this device only;
  "done" / progress is spec 005).
- **Text size:** small / medium / large for reading text, saved on the device, set from the reader toolbar.
- **Revise tab:** every cheat sheet grouped by module, with module filter chips; tapping one opens it.
- **Layout rules:** code blocks, tables and diagrams scroll horizontally inside themselves, so the page never scrolls
  sideways (down to 320 px). Keyboard navigation on desktop, reduced motion respected, WCAG AA contrast in both themes.
- UI work follows the installed skills (`emil-design-eng`, `apple-design`, `mobile-native`, `break-ui`).

### 3.6 Content shipped in this spec
- **New module "Start here"** (`content/start/`, first in order) with a short **"How to use LLD Guide"** lesson and its
  cheat sheet: what the modules are, how lessons and cheat sheets work together, how to revise before an interview.
- **Auth case study** migrated to the new front-matter (drop `module:`, add `summary`; cheat sheet keeps `title` only).
- `module.yml` for `case-studies` and `start`.

## 4. Out of scope
- Progress / "mark done" / "continue where you left off" across devices (spec 005); notes and bookmarks (spec 007);
  search (spec 009); sync (spec 010).
- Images and other media in lessons; an in-app content editor (Phase 2); server-delivered content updates.
- Real lessons for OOP & SOLID, patterns and UML (specs 004, 006, 008).
- Serving content from the hosted API.

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | A fresh install of the Android Release APK, opened in **airplane mode** and never signed in, shows the modules, the Auth lesson and its cheat sheet | Manual (emulator) + screenshots |
| A2 | Adding a lesson folder and rebuilding makes it appear in the Windows and Android apps; in browser dev mode it appears on reload without a restart | Manual + integration test (API reload) |
| A3 | Each validation rule (§3.2) fails the build / CI with the file path and reason | Unit tests (one fixture per rule) |
| A4 | The device reloads content only when the pack version changes, in one transaction | Unit tests (content store) |
| A5 | The Auth lesson renders all headings, the outline, highlighted C#, all 4 Mermaid diagrams (light and dark), tables and quotes; in-app and GitHub links go to the right place | Playwright + manual (Windows, Android) |
| A6 | At 320 px no page scrolls sideways; long code, tables and diagrams scroll inside their own box | Playwright (phone viewport) + `break-ui` pass |
| A7 | Lesson → cheat sheet in one tap and back; Revise lists every cheat sheet and filters by module | Playwright |
| A8 | Each shipped cheat sheet fits one phone screen with at most 2 scrolls (height ≤ 3 × a 390 × 844 viewport at medium text size); **case-study** cheat sheets may use up to 4 scrolls (≤ 5 × viewport) | Playwright |
| A9 | Previous / next follow module order; reading position and text size survive an app restart | Vitest + manual |
| A10 | Opening a lesson takes under **300 ms** on the Android emulator (Release, after first launch, diagrams excluded) | Manual measurement |
| A11 | The hosted API (non-Development) returns 404 for `/api/content/*` | API test |
| A12 | All CI checks green; zero warnings; guardrail lint clean | CI |

## 6. LLD hooks (worth noting in later lessons)
| Pattern / principle | Where |
|---|---|
| Strategy + Adapter | `DataSource`: bridge (device SQLite) vs HTTP (dev API) |
| Pipeline / Chain of Responsibility | parse → validate (one rule class per rule) → pack |
| Composite-ish hierarchy | Module → Lesson → CheatSheet / Outline |
| Repository | device content store behind a port, testable outside MAUI |
| Idempotent load / versioning | pack version hash decides whether to reload |
| Open/Closed | new validation rule = new class, no change to the validator |

## 7. Decisions (owner, 2026-10-16)
| Topic | Decision |
|---|---|
| Sign-in for reading | Not needed: Learn and Revise are open; sign-in is only for the user's own data (later specs) |
| Rendering | C# parses, validates and packs; React renders Markdown (react-markdown + Shiki + lazy Mermaid) |
| Shipped content | Auth case study + a new "How to use LLD Guide" lesson and cheat sheet |
| Reader extras | Remember reading position, text size setting, previous / next links (no copy button on code) |
| Welcome content | New "Start here" module (`content/start/`, first) with a normal lesson + cheat sheet |
| Front-matter | Lessons drop `module:` (folder decides); cheat sheets keep only `title` |
| Cheat sheet length | Concept cheat sheets: one screen + at most 2 scrolls (F3). Case-study cheat sheets: up to 4 scrolls. PRD F3 updated to match |
| Packaging | Validate + pack at build time into `content-pack.json`; invalid content fails the build |

## 8. Open questions
None. Resolved on 2026-10-16 (see §7): "Start here" module, simpler front-matter, case-study cheat sheet exception,
packing at build time.
