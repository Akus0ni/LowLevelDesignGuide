# Spec 009: Tasks

- [x] T1 Spike: FTS5 + `porter` + JSON + external content in `e_sqlite3` (desktop: SQLite 3.53.3, all present). The Android check runs with V7 in T6; the bridge timing of a 30-query set moves to T15
- [x] T2 `Lld.Search` pure code: `QueryParser`, `SynonymExpander`, `MatchExpressionBuilder`, `SnippetMarks` + shared vector `tests/fixtures/search/queries.json`; hostile-input tests (A2, A3)
- [x] T3 `Lld.Search` SQL: `SearchSchema` (units, two FTS5 tables, triggers, notes triggers), `SqliteSearchIndex` (union query, ranking, snippets, words, counts); `ISearchIndex` contract suite in `tests/Lld.Search.Tests` (A1, A2, A7)
- [x] T4 Content: `SearchText` (per-`h2` plain text), `search.yml` reader + 4 rules with one fixture each, pack format 4 (`Search` on lessons and cheat sheets, `Vocabulary`); packer tests (A1, A4)
- [x] T5 Content: author `content/search.yml` (≈ 40 synonyms limited to covered terms, 6 suggestions); repository test runs every suggestion through the real index
- [x] T6 Device: `V7Search`, `ContentPackLoader` fills content units and re-indexes notes in its transaction; `DeviceSearch`; tests: V6 → V7 upgrade, format-4 reload, notes edit/delete/adopt show at once (A7, A12)
- [x] T7 API dev: `DevSearchIndex`, `GET /api/search` (Development only); integration tests (content hits, signed-in notes, isolation, not mapped outside Development)
- [ ] T8 App + web data: `BridgeService.Search`, contracts + `types.ts`, `DataSource.search` in bridge / HTTP / fake; tests
- [ ] T9 Web search UI: `useSearch`, `searchResults`, `searchParams`, `recentSearches`, `SearchPage`, `ResultRow`, route + section; Vitest (A5, A8)
- [ ] T10 Reader marks: `useSearchMarks`, `findRanges`, `MatchBar`, `FoldRegistry` + `hiddenUntilFound` folds, cheat sheet `?at=` + recall reveal, Android Back clears marks; lesson-open timing before / after; Vitest (A6)
- [ ] T11 Quick search dialog (`Ctrl+K` / `⌘K` / `/`, wide + fine pointer), focus return; Vitest (A9)
- [ ] T12 Visual: `--tone-match` (3 candidates → contrast pick), result rows, match bar, dialog motion + reduced motion (A13)
- [ ] T13 UI pass with the skills: screenshot critique (phone light / dark, desktop) + `break-ui` worst cases; fixes
- [ ] T14 Playwright `e2e/search.spec.ts` on phone + desktop (A5, A6, A7, A9, A13)
- [ ] T15 Manual run: Windows + Android (A10 timing log, A11 airplane mode, A12 V6 → V7 upgrade keeps progress / notes / bookmarks); screenshots
- [ ] T16 Docs: PRD §10 `LessonSearch` row + F6, `docs/plan.md` roadmap, README authoring note for `search.yml`, plan §5 outcomes, progress log
- [ ] T17 PR with the acceptance-criteria table
