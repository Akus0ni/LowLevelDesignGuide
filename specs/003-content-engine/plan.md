# Spec 003: Implementation plan

Companion to [`spec.md`](./spec.md). Versions checked against NuGet and npm on 2026-10-16.

## 1. Packages
| Package | Version | Used by |
|---|---|---|
| `Markdig` | 1.4.0 | Content.Authoring (Markdown AST: front-matter block, headings, links, code fences, with line numbers) |
| `YamlDotNet` | 18.1.0 | Content.Authoring (front-matter, `module.yml`) |
| `Microsoft.Data.Sqlite` | 10.0.13 | App.Data (device database, plain ADO.NET; **no EF Core on device**, see §4 T1) |

| npm package | Version | Why |
|---|---|---|
| `react-markdown` | 10.1.0 | Markdown → React elements; no raw HTML (safe by default) |
| `remark-gfm` | 4.0.1 | Tables, strikethrough, autolinks |
| `shiki` | 4.5.0 | C# and other highlighting (`shiki/core` + JavaScript regex engine, only the languages we use, no WASM) |
| `mermaid` | 12.2.0 | Diagrams; loaded with `import()` only when a lesson has a diagram |
| `github-slugger` | 2.0.0 | Heading anchors, same algorithm as GitHub (C# port shares a test vector) |

Markdown parsing is **build-time only** (and the dev API). The device never parses YAML or Markdown structure, so
Markdig and YamlDotNet never ship inside the app.

## 2. Design

### 2.1 Projects
```
Lld.Contracts            + ContentContracts.cs (DTOs shared by API, App, web types)
Lld.Content              pack model + IContentCatalog + InMemoryContentCatalog   (BCL + Contracts only; App uses it)
Lld.Content.Authoring    NEW: read content/ → validate → pack                     (Markdig, YamlDotNet; API + CLI)
Lld.Content.Cli          NEW: `lld-content validate | pack | snapshot`            (build tool, CI, Playwright data)
Lld.App.Data             NEW: device SQLite (Microsoft.Data.Sqlite) + SqliteContentCatalog + ContentPackLoader (net10.0, testable)
tests/Lld.Content.Tests  NEW: authoring, rules, packer, both catalogs (one shared contract suite), loader
```
Dependency direction: `Contracts ← Content ← Content.Authoring ← {Api, Cli}` and `Content ← App.Data ← App`.

### 2.2 Contracts (`Lld.Contracts/ContentContracts.cs`)
```
ModuleSummary      Slug, Title, Summary, Order, LessonCount
ModuleDetail       Slug, Title, Summary, Lessons: LessonSummary[]
LessonSummary      Id, ModuleSlug, Slug, Title, Summary, Order, ReadingMinutes, Tags[]
LessonDetail       LessonSummary fields + ModuleTitle, BodyMarkdown, Outline: OutlineEntry[], CheatSheetTitle,
                   Previous?: LessonLink, Next?: LessonLink
OutlineEntry       Level (2|3), Text, Anchor
LessonLink         Id, ModuleSlug, Slug, Title
CheatSheetSummary  LessonId, ModuleSlug, ModuleTitle, LessonSlug, LessonTitle, Title
CheatSheetDetail   CheatSheetSummary fields + BodyMarkdown
```
Mirrored by hand in `web/src/data/types.ts`, as for the existing DTOs.

### 2.3 Pack model and catalog (`Lld.Content`)
- `ContentPack { Format = 1, Version, Modules: PackedModule[] }`. `PackedModule { Slug, Title, Summary, Order, Lessons }`.
  `PackedLesson { Id, Slug, Title, Summary, Order, Tags, Source?, ReadingMinutes, BodyMarkdown, Outline, CheatSheet { Title, BodyMarkdown } }`.
- `ContentPackJson`: a System.Text.Json **source-generated** context, so (de)serialization survives Android trimming.
- `IContentCatalog` (the port both sides implement, Repository + Strategy):
  `GetModulesAsync`, `GetModuleAsync(slug)`, `GetLessonAsync(id)`, `GetCheatSheetsAsync(moduleSlug?)`, `GetCheatSheetAsync(lessonId)`.
  "Not found" returns `null`; callers map it to `content.not_found`.
- `InMemoryContentCatalog(ContentPack)`: used by the dev API and by the CLI `snapshot` command. Previous / next =
  neighbours in module order (no crossing into another module).

### 2.4 Authoring pipeline (`Lld.Content.Authoring`)
Pipeline: **read → validate → pack**. Each step is a small class, so a new rule never edits the others (Open/Closed).

| Class | Responsibility |
|---|---|
| `ContentRepository` (`IContentFiles`) | Lists module folders, lesson folders and files. Physical implementation + an in-memory one for tests |
| `ContentReader` | Parses `module.yml` and each Markdown file into a `SourceDocument` (front-matter map with line numbers, Markdig AST, path). YAML syntax errors become issues, not exceptions |
| `HeadingSlugger` | Port of `github-slugger`: its removal regex is copied verbatim into `GithubSluggerPattern.cs` by `web/scripts/port-slugger.mjs` (.NET and JS both match UTF-16 code units). Shared vector `tests/fixtures/content/slugs.json`, generated from the real library by `web/scripts/generate-slug-fixture.mjs`, also run by Vitest |
| `OutlineBuilder` | h2/h3 → `OutlineEntry[]` with slugger anchors |
| `ReadingTime` | Prose words / 200 (code blocks excluded), rounded up, at least 1 minute |
| `LinkResolver` | Classifies each link: lesson / cheat sheet (with optional anchor), repo file, external, or broken |
| `LinkRewriter` | Rewrites URLs inside the Markdown text by their source spans (see below) |
| `IContentRule` × N | One class per validation rule; returns `ContentIssue { Path, Line?, Code, Message }` |
| `ContentValidator` | Runs every registered rule and collects all issues |
| `ContentPacker` | Builds the `ContentPack`, ordered by `order`; `Version` = SHA-256 of the canonical pack JSON (without the version), so the same content always gives the same version |
| `ContentPipeline` | Facade: `Build(contentRoot, repoRoot) → Result<ContentPack, ContentIssue[]>` |

**Rules (spec §3.2):** `ModuleFileRequired`, `LessonFileRequired`, `CheatSheetRequired`, `ModuleFrontMatter`,
`LessonFrontMatter`, `CheatSheetFrontMatter` (required keys, types, unknown keys rejected), `LessonIdUnique`,
`LessonIdFormat` (kebab-case), `OrderUnique` (modules, and lessons within a module), `InternalLinkResolves`
(target lesson / cheat sheet exists, and the anchor exists in it), `RepoLinkResolves` (file exists under the repo root),
`MermaidNotEmpty`.

**Link rewriting** (the app uses a hash router, so `#fragment` can't carry an in-page anchor):
| Link in Markdown | Rewritten to |
|---|---|
| `../../oop-solid/srp/lesson.md#liskov` | `/learn/oop-solid/srp?at=liskov` |
| `../srp/cheatsheet.md` | `/revise/oop-solid/srp` |
| `#some-heading` (same file) | `?at=some-heading` |
| `../../../src/Lld.Domain/Access/AccessRequest.cs` | `https://github.com/Akus0ni/LowLevelDesignGuide/blob/main/src/Lld.Domain/Access/AccessRequest.cs` |
| `https://…` | unchanged |
The GitHub base URL is an option (`ContentOptions.RepositoryFileUrl`), not a literal in the rewriter.

### 2.5 Build tool and app packaging (`Lld.Content.Cli`, `Lld.App.csproj`)
- Commands (System.CommandLine, already used by `Lld.Admin`); options `--repo <root>` (default: current folder) and
  `--format Text|MSBuild`:
  - `validate` → prints `path:line: code: message` per issue; exit code 1 when any.
  - `pack --out <dir>` → `content-pack.json` + `content-version.txt`; fails like `validate`. When the folder already
    holds the same version nothing is written, so timestamps stay put.
  - `snapshot --out <dir>` → every API response as static JSON via `InMemoryContentCatalog`: `modules.json`,
    `modules/<slug>.json`, `lessons/<id>.json`, `cheatsheets.json`, `cheatsheets-by-module/<slug>.json`,
    `cheatsheets/<lessonId>.json` (camelCase, like the API). Used by Playwright, so the e2e tests read real content
    without duplicating catalog logic in TypeScript.
- `Lld.App.csproj`:
  - Target `PackContent` (before `BeforeBuild`) builds the CLI with an `MSBuild` task call that removes the app's
    platform properties (Android RID, trimming, AOT, output folders), then runs `dotnet lld-content.dll pack
    --format MSBuild` into `obj/<config>/<tfm>/content-pack/`. (A `ProjectReference` from the Android app to an
    executable would bring self-contained / RID checks into play.)
  - It runs on **every** build: a deleted lesson changes no input timestamp, so `Inputs`/`Outputs` would miss it.
    The tool skips writing when the version is unchanged, so the asset copy stays incremental.
  - **A content error fails the app build** with clickable `file(line): error code: message` lines.
  - The two files are added as `MauiAsset` with logical names `content/...` (verified inside the APK:
    `assets/content/content-pack.json`).

### 2.6 Device database (`Lld.App.Data`)
- **Plain ADO.NET** (`Microsoft.Data.Sqlite`), no EF Core: owner decision after the T1 measurements (§4).
  The rest of the app only sees the ports (`IContentCatalog`, `ContentPackLoader`), so the choice stays local.
- `DeviceDatabase` opens `<AppDataDirectory>/lld-device.db` (WAL, foreign keys on) and hands out connections.
- `SchemaMigrator`: an ordered list of `ISchemaMigration { int Version; string Sql }` (one class per step, so a new step
  never edits an old one). It reads `PRAGMA user_version` and applies each newer step in its own transaction, then
  sets `user_version`. Normal launches cost one PRAGMA read. Step 1 = content tables; specs 005/007/010 add steps.
- Tables: `Modules` (Slug PK, Title, Summary, Order), `Lessons` (Id PK, ModuleSlug FK, Slug, Title, Summary, Order,
  TagsJson, Source, ReadingMinutes, BodyMarkdown, OutlineJson, CheatSheetTitle, CheatSheetMarkdown),
  `DeviceMeta` (Key PK, Value) holding `content.version`. Unique index (ModuleSlug, Slug). All SQL is parameterised.
- `ContentPackLoader.EnsureLoadedAsync(IContentPackSource)`:
  1. Read `content-version.txt` (a few bytes). Equal to `DeviceMeta[content.version]` → done (no pack parse).
  2. Otherwise deserialize the pack and, **in one transaction**, delete content rows, insert the new ones and set the
     version. Any failure rolls back and leaves the old content in place.
- `SqliteContentCatalog : IContentCatalog`: hand-written queries mapped by small row readers; prev / next by `Order`.
- `IContentPackSource` is the seam: MAUI implements it with `FileSystem.OpenAppPackageFileAsync`, tests with files on disk.

### 2.7 MAUI (`Lld.App`)
- DI: `DeviceDatabase`, `SchemaMigrator`, `ContentPackLoader`, `SqliteContentCatalog`, `MauiContentPackSource`.
- `ContentGate`: a `Lazy<Task>` that migrates + loads once; every content bridge method awaits it, so the first call
  waits for the load and later calls don't.
- `BridgeService` gains `GetModules`, `GetModule(slug)`, `GetLesson(id)`, `GetCheatSheets(moduleSlug?)`,
  `GetCheatSheet(lessonId)` → `BridgeResult<T>` (`content.not_found` when missing), and `OpenExternal(url)`
  (`Browser.OpenAsync(..., SystemPreferred)`: a Custom Tab on Android; only `http`/`https` allowed).
- **Spike (T1, done):** EF Core was measured on the device and replaced by plain SQLite (§4).

### 2.8 API (`Lld.Api`, `Lld.AppHost`)
- `ContentEndpoints` mapped **only when `IsDevelopment()`**:
  `GET /api/content/modules`, `/modules/{slug}`, `/lessons/{id}`, `/cheatsheets?module=`, `/cheatsheets/{lessonId}`.
  Anonymous; excluded from the tunnel budget meter (never hosted anyway).
- `DevContentCatalog`: builds the pack with `ContentPipeline` from `Content:Path` (default: the repository found above the
  API's folder). Each request fingerprints `content/` (paths, sizes, write times); when the fingerprint changed it
  rebuilds, so a saved, added or deleted file shows up on the next page load. (Changed from a `FileSystemWatcher`,
  which can miss events or fire after the request and needs debouncing; listing a few dozen files costs nothing.)
- Invalid content in dev → `500` problem details, code `content.invalid`, with the issue list (shown in the browser
  so the author sees what to fix).
- AppHost passes `Content__Path` = the repo's `content/` folder.

### 2.9 Web (`web/`)
- **Data:** `DataSource` gains the five content methods (§2.2) and `openExternal(url)`. `HttpDataSource` → `/api/content/...`
  (`window.open` for external links); `BridgeDataSource` → `InvokeDotNet`. A small `useContent(key, load)` hook caches
  results per session (content can't change while the app runs), so going back is instant.
- **Routes** (hash router): `/learn`, `/learn/:module`, `/learn/:module/:lesson?at=<anchor>`, `/revise?module=<slug>`,
  `/revise/:module/:lesson`. Learn and Revise lose their "coming in" placeholder.
- **`features/content/`:**
  | Component | Notes |
  |---|---|
  | `ModuleList`, `ModulePage` | Cards with title, summary, lesson count / reading time |
  | `LessonPage` | Header (module, title, reading time, **Cheat sheet** button, `Aa` text size), body, prev / next |
  | `MarkdownView` | react-markdown + remark-gfm; custom `h2`/`h3` (slugger ids), `a` (internal → router link, `?at=` → scroll, external → `openExternal`), `table` (own horizontal scroll), `pre`/`code` (→ `CodeBlock` or `MermaidDiagram`) |
  | `CodeBlock` | Plain text first, then Shiki output in the same box (same font and line height, so nothing jumps). Dual light / dark theme with CSS variables. Unknown language → plain |
  | `MermaidDiagram` | `import('mermaid')` on first use; `securityLevel: 'strict'`; theme follows `prefers-color-scheme` and re-renders on change; on error shows the source and "Diagram failed to render"; scrolls horizontally |
  | `Outline` | Desktop (≥ `lg`): sticky side column with the current section highlighted. Phone: "On this page" button → bottom sheet (base-ui Dialog) |
  | `CheatSheetPage` | Body + "Read the lesson" link |
  | `RevisePage` | Grouped by module, filter chips (`?module=`) |
- **Reader preferences (`readerPrefs.ts`, device only):** text size `s | m | l` (CSS variable on the reader: 15 / 17 / 19 px)
  and reading position per lesson `{ anchor, offset }` (nearest heading above the top of the screen + pixels past it,
  so it survives text size changes and late diagram rendering). Stored in `localStorage` with try/catch. If the
  WebView doesn't keep it across restarts on a platform (checked in T20), it moves behind the bridge (MAUI `Preferences`).
- Highlighter warm-up: after first paint, `requestIdleCallback` creates the Shiki highlighter, so the first lesson
  open doesn't pay for it (A10).
- Bundle: Mermaid and Shiki languages are separate lazy chunks; `chunkSizeWarningLimit` raised only for the Mermaid chunk.

### 2.10 Content
- `content/case-studies/module.yml`, `content/start/module.yml` (order 0).
- Auth lesson: drop `module:`, add `summary`. Auth cheat sheet: only `title`.
- `content/start/how-to-use/{lesson.md, cheatsheet.md}`: modules and their order, lesson ↔ cheat sheet, how to
  revise before an interview, reading offline.
- `.claude/skills/lld-case-study/SKILL.md`: front-matter template updated to the new contract.

## 3. Testing
| Area | Tests |
|---|---|
| Authoring | Reader (front-matter lines, YAML errors), slugger (shared vector), outline, reading time, link resolver + rewriter, **one fixture per rule** (`tests/fixtures/content/<rule>/`), packer determinism, and "the repo's real `content/` is valid" |
| Catalogs | One contract suite run against `InMemoryContentCatalog` and `SqliteContentCatalog` (ordering, prev / next at module edges, filters, not found) |
| Device DB | Schema migrator (fresh DB, already current, a failing step rolls back that step); loader: same version → nothing parsed or written, new version → replaced, a failure mid-load → old content intact |
| API | Endpoints in Development; 404 in Production (A11); file added → visible on the next request (A2); invalid content → `content.invalid` |
| Aspire | `GET /api/content/modules` through the AppHost returns `start` and `case-studies` |
| Vitest | Content methods on both data sources, `useContent` cache, `MarkdownView` (ids, links, tables, Mermaid fallback with a mocked module), `readerPrefs`, Revise filter, prev / next |
| Playwright | Routes served from the CLI `snapshot`: A5 (Auth lesson: outline, highlighted C#, 4 diagrams, light + dark), A6 (no sideways scroll at 320 px), A7, A8 (cheat sheet heights: ≤ 3 × 844 px, case studies ≤ 5 ×), phone + desktop |
| Manual | A1 airplane-mode fresh install (Android Release), A2 add a lesson + rebuild (Windows, Android, browser), A5 on devices, A9 restart, A10 timing; screenshots |

CI: `backend` gains `tests/Lld.Content.Tests`; `web` gains `setup-dotnet` and runs `lld-content snapshot` before Playwright;
`maui-windows-android` packs content as part of the app build (so content errors fail it too).

## 4. Outcomes
_Completed at the end of the spec: deviations, findings, verification table._

### T1 spike: EF Core on the device (2026-10-16)
Android emulator (Pixel, API 35, x86_64), Release APK, one-row table, every launch measured from app start:

| Setup | EF ready after launch | First screen | APK |
|---|---|---|---|
| EF Core, runtime model | ~5.4 s (model 2.5 s, migration check 1.1 s, first query 1.3 s) | ~2.5 s | 42 MB |
| + compiled model (`dotnet ef dbcontext optimize`) | **hangs forever** | — | 42 MB |
| + compiled model, `Microsoft.EntityFrameworkCore.Issue31751` switch, migration check skipped when current | ~3.9 s (first query 2.7 s) | ~2.7 s | 42 MB |
| + full AOT (`AndroidEnableProfiledAot=false`) | ~1.1 s | ~2.0 s | 67 MB |
| Windows Release (CoreCLR) | ~0.4 s | — | — |
| **Plain `Microsoft.Data.Sqlite`** (open + query) | **~0.04–0.1 s** | — | — |

Findings:
- EF Core itself works in the trimmed + AOT Release APK (migrations, writes, LINQ), but its first use costs seconds on Mono.
- The generated compiled model initializes on a second thread inside a static constructor and `Join`s it. **Mono
  deadlocks** on this (CoreCLR doesn't), with or without trimming or AOT. EF's `Issue31751` switch restores
  same-thread initialization.
- Android Release builds already use profiled AOT; EF isn't in the startup profile, so it runs JIT-compiled.
- ILLink trim warnings are suppressed by default for Android apps, so a clean build says nothing about trim safety;
  only running the APK does.
- The unpackaged Windows app stores app data under `%LOCALAPPDATA%\User Name\com.akus0ni.lldguide\Data` (the
  default publisher name). Harmless; noted for later.

**Decision (owner, 2026-10-16):** no EF Core on the device. The device database uses `Microsoft.Data.Sqlite` with
hand-written, parameterised SQL behind the same ports. EF Core stays on the API. Recorded as PRD R67.

### T3 notes
- `github-slugger` was added to `web/` in T3 (not T13) so the fixture and the regex port can be regenerated.
- Markdig 1.x exposes source positions as **fields** (`LinkInline.UrlSpan`, `MarkdownObject.Span`), not properties.
  A reference-style use (`[text][id]`) reports its definition's `UrlSpan`, so the rewriter only touches inline links
  without a reference plus the definitions themselves.
- Folders starting with `.` or `_` under `content/` are ignored (drafts, editor folders).

### T4 notes
- **Fixtures:** instead of one folder per rule under `tests/fixtures/content/<rule>/`, the tests use one valid in-memory
  repository (`ValidRepo`) and each test breaks exactly one thing, asserting the exact `(path, line, code)`. Each failing
  case sits next to its assertion; no near-duplicate folders.
- **Two rules beyond the plan's list:** `ImagesNotSupportedRule` (images are out of scope and would show as broken) and
  folder names in `IdentifierFormatRule` (module / lesson folders become routes, so they must be kebab-case, like ids).
- `source` accepts the values the `lld-case-study` skill already defines: `classic`, `personal`, `self-referential`.
- Line endings are normalised to LF when reading, so Windows (CRLF) and Linux (LF) checkouts give the same pack version.
- YamlDotNet throws `InvalidOperationException` (no position) for some malformed input such as an unclosed `[`; the
  parser reports it as `yaml.invalid` at the start of the front-matter instead of crashing.
- Issue codes: `src/Lld.Content.Authoring/Validation/IssueCodes.cs`. Output format: `path:line: code: message`.
- Dry run on the real `content/` before T5: 8 issues, all from the front-matter migration; every link in the Auth lesson resolves.

### T7 notes
- Schema step 1 is `V1ContentTables` (`DeviceMeta`, `Modules`, `Lessons`). The order column is `SortOrder`, because
  `Order` is an SQL keyword. Tags and outlines are JSON columns written with a source-generated context (trim-safe).
- `SchemaMigrator` refuses a database written by a newer app (downgrade) instead of guessing, and checks that steps
  are numbered 1, 2, 3… with no gaps.
- Previous / next on the device come from `LAG` / `LEAD` window functions over the module's lessons.
- `RealContentParityTests` loads the repository's real pack into SQLite and checks that every catalog query returns
  exactly what the in-memory (dev API) catalog returns.

### T8 notes
- `ContentGate` starts in `App.OnStart`, so the database is ready before React asks; a failed attempt is retried by the
  next call. `ContentService` maps a missing item to `content.not_found` and a broken database to `content.unavailable`.
- `OpenExternal` accepts `http` and `https` (the plan said `https` only; an `http` link in a lesson would otherwise be
  dead). Anything else (`javascript:`, `file:`, `intent:`) returns `link.not_allowed`. It uses
  `BrowserLaunchMode.SystemPreferred`, so Android shows a Custom Tab and Back returns to the app.
- **The bridge serialises with PascalCase** (`Ok`, `Value`, `Error`, `ModuleSlug`, …) while the HTTP API uses camelCase.
  `BridgeDataSource` (T11) converts keys once for every result, so both data sources hand React the same shape.
- Verified through the WebView's DevTools protocol, with a fresh database each time:
  | Check | Windows (Debug) | Android emulator (Debug) |
  |---|---|---|
  | Modules / module / lesson / cheat sheets / cheat sheet | ✅ | ✅ |
  | Unknown lesson → `content.not_found` | ✅ | ✅ |
  | `OpenExternal('javascript:…')` → `link.not_allowed` | ✅ | ✅ |
  | `OpenExternal('https://github.com/…')` | not run (would open a desktop browser) | ✅ Chrome on top; Back returns to the app |
  | Lesson read time after start-up | 2–11 ms | 21–76 ms (Debug, JIT) |

### T9 notes
- Endpoints are mapped only when `IsDevelopment()`; the test for A11 runs the API as `Staging` (not `Production`,
  whose settings file points at the hosted database and secrets on the owner's PC) and gets 404.
- `content.invalid` responses carry an `issues` array with the same `path:line: code: message` lines the build prints.
- Tests: 7 API tests (endpoints, 404s, add + delete a lesson while running, invalid content, not mapped outside
  Development) and 1 Aspire test (the AppHost passes `Content__Path`, real content is served).
