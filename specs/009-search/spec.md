# Spec 009: Search

| | |
|---|---|
| **Status** | Approved by owner (2026-10-21), all §8 recommendations taken |
| **Branch** | `spec/009-search` |
| **Covers** | R19, R117–R124 (PRD §13); PRD F6, §10 (`LessonSearch`), §11 (offline, performance) |
| **Depends on** | Spec 003 (content pack, reader, heading anchors, device loader), spec 007 (notes, owner = user or device guest, `NotesStore`), spec 005 (store pattern) |
| **Case study** | None planned. Optional later, only if the owner asks (`lld-case-study`). |

## 1. Goal
Find anything in the course in a few keystrokes: a lesson section, a cheat sheet or one of your own notes. Results
appear as you type, show the matched words highlighted in a short snippet, and open in the right place: the lesson at
the matching section with the words marked, the cheat sheet, or the note.

Search runs **on the device** against SQLite FTS5 (PRD F6), fully offline, with no server calls (ngrok budget). The
browser dev mode uses a Development-only API copy of the same search code. Target: results in **under 200 ms on
device** for the current content (55 lessons, about 700 KB of Markdown).

## 2. User stories
1. *As a learner,* I type `decor` and see Decorator before I finish the word.
2. *As a learner,* I search `inherits` and also find sections about *inheritance*; `patterns` finds *pattern*.
3. *As a learner preparing for an interview,* I search `SRP` and find *Single Responsibility*, and `DI` finds
   *Dependency Injection*, even when the text never writes the acronym.
4. *As a learner,* I open a result and land on that section of the lesson with my search words marked. I can step
   through the marks and clear them.
5. *As a learner,* I narrow results to Lessons, Cheat sheets or Notes, and to one module.
6. *As a learner,* I search my own notes, including their `#tags`, and a result opens the note in its lesson.
7. *As a learner,* with an empty search box I see my recent searches and a few suggestions, so I can repeat a search
   with one tap.
8. *As a learner on Windows or a desktop browser,* I press `Ctrl+K` (or `/`) anywhere, type, and press Enter to open
   the top result without leaving the keyboard.
9. *As a learner on a shared device,* my notes and my recent searches never appear in another user's results.
10. *As the owner,* search works in airplane mode and makes no network calls.

## 3. Scope

### 3.1 What is indexed
| Kind | Unit (one result) | Fields | Opens |
|---|---|---|---|
| Lesson | one `h2` section (plus an intro unit for text before the first `h2`) | lesson title, hook, section heading, section text | the lesson at `?at=<anchor>` with marks (§3.6) |
| Cheat sheet | one cheat sheet section (by its `h2`, or the whole sheet if it has none) | lesson title, section heading, text | the cheat sheet in Revise at that section, with marks |
| Note | one note | lesson title, section name (or "Whole lesson"), note body (tags included) | the lesson at the note's section with the notes sheet open on that note (as the Notes tab does) |

- **Text that is indexed:** headings, paragraphs, lists, tables, callouts (REMEMBER, interview Q&A, Go deeper, smells,
  Mistakes, ...), including folded content. If a match is inside a fold, the fold opens when the result is opened.
- **Text that is not indexed:** code blocks, Mermaid diagrams, front-matter, link URLs, image sources. (Code and
  diagram search is out of scope, §4.) Inline code spans are indexed as plain words.
- **Removed or renamed sections:** a note on a section that no longer exists is indexed under "Whole lesson", as the
  notes sheet shows it (spec 007 §3.2). Notes on a removed lesson are found and open in the Notes tab.
- **Deleted notes** are never indexed.

### 3.2 Matching
- **Prefix as you type:** the last word typed matches as a prefix (`decor` → Decorator, `obs` → Observer), both of
  the stemmed and of the unstemmed word, so a half-typed word (`decorat`) keeps its results. Earlier words match whole
  words, after stemming.
- **Word forms:** English stemming (FTS5 `porter` over `unicode61`, diacritics removed), so `inherits` matches
  *inheritance* and *inherit*, `patterns` matches *pattern*.
- **All words must match** (AND) within one unit. `"quoted phrase"` matches the words next to each other. Case and
  punctuation are ignored; `#weak` matches the tag `#weak` and the word *weak*.
- **Acronyms and synonyms:** an authored list (§3.3) expands a query word into its alternatives, e.g. `SRP` →
  `SRP OR "single responsibility"`. Expansions are shown under the box ("Also searching: single responsibility").
- **Safety:** user input never reaches FTS5 as raw syntax. The query is parsed into words and phrases and the MATCH
  expression is built from them, so input such as `AND (`, `*`, `"` or `NEAR` can never cause an error. The query is
  at most 100 characters and searching starts at 2 characters.
- **Ranking:** BM25 with field weights (title > heading > text) and a boost when a word matches a lesson title
  (exact weights: open question Q1).

### 3.3 Search vocabulary (authored)
A new authored file `content/search.yml`, packed with the content and checked at build time like the other content
rules (one `IContentRule` class per rule, `file(line): reason`).
```yaml
synonyms:            # word → alternatives (one-way: SRP finds "single responsibility", not the reverse)
  SRP: [single responsibility]
  DI:  [dependency injection]
  LSP: [liskov substitution]
suggestions:         # shown when the box is empty
  - SOLID
  - Observer
  - sequence diagram
```
- **Rules:**
  - keys are single words;
  - no duplicate keys, ignoring case;
  - every alternative appears somewhere in the indexed content;
  - every suggestion returns at least one result.
- **First version:** about 40 entries covering SOLID letters (SRP, OCP, LSP, ISP, DIP), DI / IoC, GoF, UoW, DRY,
  KISS, YAGNI, UML, LLD and common pattern nicknames, limited to terms the content actually covers. The exact list is
  reviewed with the PR (open question Q4).

### 3.4 The Search tab
Replaces the "Coming soon" page.
- **Box** at the top, focused when the tab opens, with `type="search"`, `enterKeyHint="search"`, a clear button and
  no zoom on focus (16 px text). Results update while typing; a newer query always replaces an older one, and a late
  older response is dropped.
- **Filters** under the box: chips **All · Lessons · Cheat sheets · Notes** (one at a time) and a **module** picker
  (All modules or one). They combine with the query, are reflected in the URL (`/search?q=…&kind=…&module=…`) and
  remembered for the session, so Back from a result returns to the same list and scroll position.
- **Results** are grouped by module, with a count per group and in total ("23 results"). Each row shows:
  - the kind icon (lesson section, cheat sheet, note);
  - its reference (*Module · Lesson › Section*);
  - a snippet of about two lines with the matched words marked (open question Q3);
  - notes show the «my note» redline mark from spec 007 and their tags as chips.
  Group and row order: open question Q2.
- **Empty box:** recent searches (§3.5) and the suggestions from `search.yml`, each one tap away.
- **No results:** "No matches for *x*." plus the expansions tried, and a hint to remove a filter when one is set.
- **Keyboard:** ↑ / ↓ move through results, Enter opens, Esc clears the box; Android Back closes the keyboard first,
  then leaves the tab (existing hardware-back rules).

### 3.5 Recent searches
- A search is saved as recent when a result is opened from it (not on every keystroke). The last **8** distinct
  queries are kept, newest first, together with the kind / module filter used.
- They are kept **on the device only, per owner** (device guest or user), so another user on the same device never
  sees them. A **Clear** action empties the list. They are never sent to the server and are not synced (spec 010).

### 3.6 Marks in the lesson and cheat sheet
- Opening a result passes the query to the reader (`?at=<anchor>&q=<query>`). The reader scrolls to the section,
  opens any fold that holds a match and marks every matched word **in the whole lesson** (the words the index matched,
  stems and synonyms included, not just the typed text).
- A small **match bar** sits above the tab bar on phone and at the top of the reading column on desktop:
  *"observer" · 3 of 7 · ↑ ↓ · ✕*. ↑ / ↓ (and Enter / Shift+Enter on desktop) step through the marks, and ✕, Esc or
  Android Back clears them and removes `q` from the URL.
- Marks are not stored and do not change reading progress: a section reached through the match bar ticks by the same
  time-on-screen rule as normal reading (R101).
- Cheat sheets: the matched section is scrolled into view and marked. With recall mode on, only the matched section
  is revealed; the rest stays hidden (open question Q8).

### 3.7 Quick search (desktop and Windows)
- `Ctrl+K` (`⌘K` on macOS) or `/` (when focus is not in a text field) opens a **search dialog** from any screen. It
  shows the same results as the tab (no filters, top 8 per kind) and has a "See all results" link that opens the
  Search tab with the query.
- ↑ / ↓ and Enter open a result; Esc closes the dialog and returns focus to where it was.
- It is shown only with a fine pointer and a hardware keyboard (wide layouts). On phones the Search tab is the only
  entry point.

### 3.8 Storage and data access
- **Shared search code:** a new library (working name `Lld.Search`) holds the query parser, synonym expansion, the
  MATCH builder, ranking and snippet / mark extraction, behind an `ISearchIndex` port. One contract test suite runs it
  against the device index and the API dev index, like the progress and notes stores.
- **Content index:** the content packer writes the plain text of each lesson and cheat-sheet section into the pack
  (Markdown stripped, code and diagrams removed, fold content kept), together with `search.yml`. The device loads it
  into an FTS5 table in the **same transaction** as the rest of the content, only when the pack version changes
  (spec 003). Pack format **4** and device schema step **V7**.
- **Notes index:** a second FTS5 table holds the owner's notes (owner kept but not indexed as text). The device notes
  store updates it in the same transaction as each note write, delete and adoption, so it never drifts. Schema V7
  builds it from the existing notes on upgrade.
- **App:** `DataSource.search(request)` goes over the bridge to the device index. **No network.**
- **Browser dev mode:** a Development-only `GET /api/search` endpoint (like the content API) runs the same library on
  an in-memory index of the content folder and the signed-in user's notes (open question Q7 for guest notes).
- **Web:** a `useSearch` hook with a short debounce, cancellation and "latest wins"; the Search tab and the quick
  search dialog both use it.

### 3.9 Visual design
- This builds on the drafting-table identity. Results read like the **sheet index of a drawing set**: each hit has its
  reference (module · lesson › section) in the small mono label style, and the matched words are **highlighter marks**.
- **One new token, `--tone-match`,** is used only for search marks, in snippets and in the reader. Marked text stays AA
  in light and dark, and stays distinct from note redlines (`--tone-note`) and from the accent. The current mark (the
  one the match bar is on) gets a stronger outline, not only a different colour.
- Every mark and kind icon has a text equivalent for screen readers ("match", "note", "cheat sheet").
- Built and critiqued with `frontend-design`, `emil-design-eng`, `apple-design`, `mobile-native`, `animate`, `break-ui`.
  Motion: results swap without animation while typing; the quick search dialog and match bar enter quickly and
  respect reduced motion.
- `break-ui` cases:
  - a 100-character query;
  - a query that matches nothing, and one that matches nearly everything (`the`);
  - a 60-character section title;
  - a 20,000-character note;
  - FTS syntax typed as text (`AND (`, `"`, `*`);
  - 320 px wide.
- Screenshots on phone (light and dark) and on desktop are reviewed and fixed before the PR.

## 4. Out of scope
- Searching code blocks and diagrams (C# names, Mermaid labels).
- Typo tolerance and "did you mean".
- Find in the current page (browser-style `Ctrl+F` inside one lesson).
- Server-side search for the app, search analytics, syncing recent searches.
- Searching bookmarks (bookmarks point at lessons and sections, which are already indexed).
- Changing the Notes tab text filter from spec 007 (open question Q9).

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | Lesson sections, cheat sheet sections and notes are found per §3.1; code blocks, diagrams and deleted notes are not | Contract tests (`ISearchIndex`) + packer unit tests |
| A2 | Prefix, stemming, AND, phrases and synonym expansion behave as in §3.2 on a fixed query vector | Unit tests (query parser, MATCH builder) + contract tests |
| A3 | Any typed input (FTS operators, quotes, `*`, 100 chars, emoji) returns results or "no matches", never an error | Unit tests (property-style input set) |
| A4 | `search.yml` rules fail with file, line and reason (duplicate key, alternative not in content, suggestion with no result) | Unit tests (one fixture per rule) |
| A5 | The Search tab shows groups, counts, snippets with marks, filters, URL state, empty state with recent searches and suggestions, and the no-results state | Vitest + Playwright |
| A6 | Opening a result lands on the right section (fold opened), marks the matched words, and the match bar steps through and clears them (✕, Esc, Android Back) | Vitest + Playwright + manual (Android) |
| A7 | A note result opens the notes sheet on that note; notes of another owner never appear; edits and deletes show in results at once | Contract tests + Vitest + Playwright |
| A8 | Recent searches: saved on open, 8 kept, per owner, Clear works, never sent to the server | Vitest |
| A9 | Quick search opens with `Ctrl+K` / `/`, is keyboard-only usable, returns focus on Esc, and is not shown on phones | Vitest + Playwright (desktop + phone) |
| A10 | **Query → results under 200 ms on device**: p95 of a fixed 30-query set over the bridge on the Android emulator (and the owner's phone if available), plus a .NET timing test over the real pack | Manual timing log + test |
| A11 | Works in airplane mode on Android and offline on Windows, with no network calls | Manual |
| A12 | Pack format 4 and schema V7 upgrade a V6 device; progress, notes and bookmarks are kept; the notes index matches the notes after the upgrade | Unit tests (migrator, loader) + manual upgrade on Android |
| A13 | Marks are AA in light and dark, distinct from note redlines, labelled for screen readers; no sideways scroll at 320 px | Playwright + contrast check |
| A14 | All CI checks green, zero warnings, guardrail lint clean | CI |

## 6. LLD hooks (worth noting in later lessons)
| Idea | Where |
|---|---|
| Interpreter / Builder | query text → parsed words and phrases → a safe FTS5 MATCH expression |
| Chain of Responsibility / Decorator | normalise → expand synonyms → mark the last word as prefix, each a small rewriter |
| Port + adapters, one contract suite (LSP) | `ISearchIndex`: device SQLite, API dev in-memory |
| Strategy | `DataSource.search`: bridge vs HTTP |
| Specification | kind and module filters as composable predicates |
| Consistency by transaction | the notes index changes in the same transaction as the note |
| Latest wins / cancellation | `useSearch` drops stale responses |

## 7. Decisions (owner, 2026-10-21)
| Topic | Decision |
|---|---|
| Indexed | Lessons by section, cheat sheets, the user's notes. Not code or diagrams |
| Matching | Prefix as you type, word forms (stemming), acronyms and synonyms. No typo tolerance |
| UI | Highlight in the lesson, filters, recent searches and suggestions, quick search shortcut |
| Open details | Asked in the spec review (§8), not delegated |
| Q1–Q10 | All recommendations in §8 accepted ("Approved", 2026-10-21) |

## 8. Questions from the spec review (answered: every recommendation taken)

| # | Question | Decision (the recommendation) | Options not taken |
|---|---|---|---|
| Q1 | Ranking weights | Title ×10, section heading ×5, text ×1; a lesson-title match always sorts first | Plain BM25, no weights |
| Q2 | Order of groups and rows | A **Best match** row on top, then module groups ordered by their best hit, rows by rank; notes mixed into their lesson's module | Modules in course order; or a separate **Your notes** group first |
| Q3 | Snippet length | About 2 lines (≈ 160 characters) around the first match, up to 3 marks | 1 line; or 3 lines |
| Q4 | Synonym list | About 40 entries in `content/search.yml` as §3.3, reviewed with the PR | You write the list; or a minimal 10 (SOLID letters + DI / IoC / GoF / UML / LLD / UoW) |
| Q5 | Suggestions | 6 authored suggestions in `search.yml` (e.g. SOLID, Observer, sequence diagram, Repository, composition, LLD interview) | Computed (most-linked lessons); or none, only recent searches |
| Q6 | Recent searches | 8, saved when a result is opened, per owner, device only | Saved on Enter too; another count |
| Q7 | Guest notes in browser dev mode (they live in localStorage, not on the API) | Not searched in browser dev mode (dev-only path; the app searches guest notes normally) | Send them with the request; or match them in TypeScript |
| Q8 | Cheat sheet result with recall mode on | Recall mode stays as it is; only the matched section is revealed and marked | Recall mode untouched (matched answer stays hidden); or recall mode off when opened from search |
| Q9 | Notes tab text filter (spec 007, plain "contains") | Keep it: it filters a list you're already looking at; Search covers word forms and synonyms | Switch it to the search engine |
| Q10 | Quick search keys | `Ctrl+K` / `⌘K` and `/` | `Ctrl+K` only |
