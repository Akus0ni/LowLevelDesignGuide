# Spec 004: OOP & SOLID content

| | |
|---|---|
| **Status** | Approved by owner (2026-10-16) |
| **Branch** | `spec/004-oop-solid` |
| **Covers** | R15 (OOP & SOLID part), R20, R24 (PRD §13); PRD F2, F3, §7 ("C# samples must compile") |
| **Depends on** | Spec 003 (content engine and reader, merged in PR #5) |
| **Case study** | None (content spec). Lessons point at this app's own code where it really applies a principle. |

## 1. Goal
Ship the **OOP & SOLID** module: 15 deep lessons, each with a cheat sheet, that take a .NET developer from the
four pillars to the SOLID principles and the everyday design heuristics an interviewer expects. Every lesson teaches
with a classic before/after example and then shows where **this app's code** applies (or bends) the principle.
All C# in the lessons is **pulled from code that compiles and is tested**, so it can never drift or rot.

The reader gains three features that concept lessons need: **Before / After code compare**, **callout boxes**,
**folded interview Q&A**, plus an interactive **principle map** on the module page.

## 2. User stories
1. *As a learner,* I read the module in order, from encapsulation to DIP, and each lesson builds on the previous ones.
2. *As a learner,* I see a design that breaks a principle next to the fixed design, with the changed lines marked,
   so I can see exactly what the refactor did.
3. *As a learner,* I see the principle in real production-style code: this app's own classes, linked to GitHub.
4. *As a learner,* before an interview I answer each lesson's interview questions in my head, then reveal the model
   answer; the cheat sheet gives me the whole lesson in under two minutes.
5. *As a learner,* on the module page a map shows how the ideas relate (e.g. OCP and LSP lead to DIP); tapping a node
   shows its links and opens the lesson.
6. *As the owner (author),* I quote a type or method from `samples/` or `src/` by name. If it is renamed or deleted,
   the build tells me which lesson and line broke. Editing a sample shows up on the next page load in browser dev mode.
7. *As the owner,* I can add a "From my work" story to any lesson later without touching code.

## 3. Scope

### 3.1 The module
`content/oop-solid/`, `order: 1` (after "Start here"; the case-studies module already expects this). Lesson `id`
equals the folder slug (unique across the app today; permanent once published).

| # | Slug / id | Title | Core questions it answers |
|---|---|---|---|
| 1 | `encapsulation` | Encapsulation | Invariants, information hiding, "tell, don't ask" vs anaemic models, `private set`, `init`, guard clauses |
| 2 | `abstraction` | Abstraction | Modelling what matters, interfaces as contracts, leaky abstractions, abstraction levels in a method |
| 3 | `inheritance` | Inheritance | is-a, `virtual` / `override` / `sealed` / `new`, fragile base class, template method, when to seal by default |
| 4 | `polymorphism` | Polymorphism | Subtype, parametric (generics) and ad-hoc (overloading); replacing `switch` on type; pattern matching as the C# alternative |
| 5 | `interfaces-vs-abstract-classes` | Interfaces vs abstract classes | Capability vs identity, default interface methods, versioning, `static abstract` members |
| 6 | `composition-over-inheritance` | Composition over inheritance | has-a, delegation, decorators, avoiding class explosions, mixing behaviour at runtime |
| 7 | `value-objects-immutability` | Value objects & immutability | Primitive obsession, `record` / `readonly record struct`, equality, `with`, thread safety |
| 8 | `coupling-cohesion` | Coupling & cohesion | Kinds of coupling, connascence (light), cohesive modules, dependency direction, layers in this repo |
| 9 | `law-of-demeter` | Law of Demeter | Train wrecks, "talk to friends", when fluent APIs and DTOs are fine |
| 10 | `srp` | Single Responsibility (SRP) | "One reason to change" / one actor, splitting by change, not by size; use cases in this app |
| 11 | `ocp` | Open/Closed (OCP) | Extension points, Strategy, plug-in rules; this app's validation rules; YAGNI tension |
| 12 | `lsp` | Liskov Substitution (LSP) | Contracts (pre/post-conditions, invariants), Rectangle/Square, `NotSupportedException` smells, contract tests |
| 13 | `isp` | Interface Segregation (ISP) | Fat interfaces, role interfaces, `IReadOnlyList<T>` style, ports in this app |
| 14 | `dip` | Dependency Inversion (DIP) | High-level policy owning the abstraction, ports and adapters, DI ≠ DIP, `Microsoft.Extensions.DependencyInjection` |
| 15 | `dry-kiss-yagni` | DRY, KISS & YAGNI | Knowledge vs code duplication, the rule of three, accidental complexity; plain SQLite over EF Core on device (R67) |

`module.yml`: title "OOP & SOLID", summary one sentence, plus the principle map (§3.6).

### 3.2 Lesson template: a visual card, then "Go deeper" (amended 2026-10-16)
The owner asked for simple, memorable language with little text: the lesson is a **card** you can read in about
5–8 minutes, and the detail lives in a folded **Go deeper** section for when you want it.

**The card** (always visible):
1. **REMEMBER stamp** (`> [!REMEMBER]`): one or two short sentences, the line to repeat in an interview (§3.8).
2. **The picture**: a small everyday analogy drawn as a diagram (an ATM vault, a TV remote, a power socket).
3. **The rule**: two or three short lines in plain words.
4. **See it**: one Before / After (§3.4), with one or two lines on what changed.
5. **Smell it**: three short bullets.
6. **In this app**: one snippet and one line.
7. **Interview questions**: 5 questions, answers of about two lines, folded (§3.5).

**Go deeper** (`## Go deeper`, folded, after the interview questions): the second worked example, C# and .NET
details, trade-offs, using it in a design interview, common misconceptions and related lessons, written in short
paragraphs and lists.

Writing style everywhere: short sentences, everyday words, one idea per paragraph, a picture or code before prose.

Cheat sheet (concept, ≤ 1 screen + 2 scrolls, R72): the same REMEMBER stamp first, then `##` blocks *Smells* ·
*The move* (a few lines of code) · *In this app* · *Traps* · *Interview one-liners*.

### 3.3 Compiled C# samples and snippets
- **`samples/Lld.Samples.OopSolid/`** (class library, .NET 10, C# 14), one folder per lesson with `Before` / `After`
  namespaces. It builds with the solution, with zero warnings like the rest of the repo; "bad" designs are bad at the
  design level, not compiler-warning level.
- **`tests/Lld.Samples.Tests/`** (xUnit + FluentAssertions) shows the behaviour each design claims, including the
  failures a violation causes (e.g. a test proving `Square` breaks a `Rectangle` caller's expectation).
- **Snippet fences.** A code fence can quote C# by symbol instead of containing code:

  ````markdown
  ```csharp snippet="samples/Lld.Samples.OopSolid/Lsp/Before/Shapes.cs#Square"
  ```
  ````
  - Path is relative to the repo root and may point into `samples/` or `src/`.
  - Selector: `#Type`, `#Type.Member` (all overloads of a member), or several separated by commas. No selector = the
    whole file without `using` lines. Doc comments and attributes come along; the code is de-indented.
  - Found by C# syntax (Roslyn), not line numbers or region markers, so production code needs no markers and moving
    code inside a file does not break lessons.
  - Inlined when content is packed: the device and the dev API only ever see finished Markdown.
- **New validation rules** (each its own class + test, with file and line): snippet file missing; selector not
  found; selector ambiguous (two types with the same name); snippet fence that also has inline code.
- Browser dev mode: the content API also reloads when a `.cs` file a lesson quotes changes.

### 3.4 Before / After code compare
- **Authoring:** two adjacent code fences flagged `before` and `after` (optional `title="..."`), each inline code or a
  snippet:

  ````markdown
  ```csharp before snippet="samples/.../Srp/Before/ReportService.cs#ReportService"
  ```
  ```csharp after snippet="samples/.../Srp/After/ReportGenerator.cs#ReportGenerator,ReportExporter"
  ```
  ````
- **Changed lines** are worked out when content is packed (line diff) and marked: removed lines in *Before*, added or
  changed lines in *After*. No diffing on the device.
- **Reader:** phone: one code box with a *Before | After* segmented switch (the box keeps its height when switching,
  so the page does not jump). Wide screens: side by side when both fit, otherwise the switch. Keyboard accessible;
  the switch animates with the app's motion rules and is instant with reduced motion.
- **Validation:** `before` without a following `after` (or the reverse) fails the build.
- A lone fence can also mark lines by hand (`mark="3-5,9"`), handy for "look at this line" in normal code blocks.

### 3.5 Callouts and folded interview Q&A
- **Callouts** use GitHub's alert syntax, so lessons stay readable on GitHub:

  ```markdown
  > [!SMELL]
  > A method that needs a comment saying "and then it also..." does two jobs.
  ```
  Types: `NOTE`, `TIP`, `INTERVIEW` (interview tip), `SMELL` (code smell), `PITFALL`, `APP` (in this app),
  `WORK` (from my work). Each has its own icon, label and token colours (light and dark, WCAG AA) in the class-diagram
  identity. An unknown type fails the build.
- **"From my work"** (`WORK`) is how the owner adds real-work stories later; lessons ship without them, and
  `plan.md` lists suggested slots.
- **Interview questions:** in a lesson, the `## Interview questions` section renders each `###` question with its answer
  folded ("Show answer"); "Show all" opens them all. Fold state is not saved. The outline shows the section, not
  every question. Lessons in this module must have the section (validation rule scoped to `oop-solid`).

### 3.6 Principle map on the module page
- **Authoring:** optional `map:` in `module.yml`, a Mermaid `flowchart` whose node ids are lesson slugs, edges labelled
  with the relation (e.g. `ocp -->|"needs"| lsp`).
- **Validation:** must be a flowchart; every node id must be a lesson slug in that module; every lesson appears.
- **Reader:** shown on the module page above the lesson list, themed like the class-diagram identity. Tapping a node
  highlights it and its direct links and shows a small card (title, summary, reading time, *Open*); tap again, tap
  empty space or press Esc to clear. Nodes are reachable by keyboard. The lesson list stays the primary navigation.
  *Implementation note (2026-10-16):* a drawn Mermaid flowchart of 15 lessons is ~1480 px wide and unreadable on a
  phone, so the map is drawn by the app instead: each `subgraph` is a `«package»` box listing its lessons, and the
  links of the tapped lesson are highlighted and labelled. Authoring stays a Mermaid flowchart.
- **Data:** the pack gains the module map as groups (from `subgraph`) and labelled links (pack format 2; the device
  schema gets a step for it), `ModuleDetail.map` in the contracts and `web/src/data/types.ts`. Other modules without
  `map:` look as they do today.

### 3.8 Memory aids (amended 2026-10-16)
Every concept should stick visually.
- **REMEMBER stamp:** a `[!REMEMBER]` callout drawn as a large stamp card (display font, accent frame). Concept lessons
  and their cheat sheets must open with one (validation rule).
- **Everyday picture:** a small diagram under the stamp; authored as Mermaid, themed like every diagram.
- **Icon per concept:** lesson front-matter `icon` (one emoji) and `hook` (≤ 40 characters, e.g. "Ask, don't
  touch"); required in concept modules. Shown on the map, module lesson list, Learn page, lesson header, cheat sheet
  and Revise list.
- **Module mnemonic:** `mnemonics:` in `module.yml`, e.g. `"A-PIE: abstraction, polymorphism, inheritance,
  encapsulation"` and `"SOLID: srp, ocp, lsp, isp, dip"`. Each letter must be the first letter of that lesson's
  title (validation rule). The module page draws each word as big letter tiles (letter, icon, title, hook); a tile
  opens its lesson.

### 3.7 UI quality
New UI follows the visual identity (spec 003 §3.7) and is built and critiqued with the skills: `frontend-design`,
`emil-design-eng`, `apple-design`, `mobile-native`, `animate`, `break-ui`. Screenshots of the compare, callouts, Q&A and
map in phone light / dark and desktop are reviewed and fixed before the PR.

## 4. Out of scope
- Progress, "done" marks on the map, notes, bookmarks, search, sync (specs 005, 007, 009, 010).
- Design patterns as full lessons (spec 006): OCP and composition mention Strategy / Decorator briefly and link later.
- UML as a topic (spec 008): diagrams are used, not taught.
- Writing the owner's "From my work" stories (slots only).
- Quizzes, scores, saved answers (R21). Folded Q&A is reveal-only.
- Snippets for languages other than C#.

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | The OOP & SOLID module shows 15 lessons in order, each with a cheat sheet, offline, in the Windows and Android apps | Manual + Playwright (snapshot) |
| A2 | Every lesson follows the template (§3.2): REMEMBER stamp, picture, icon and hook, 5–8 interview questions, a "Go deeper" section; the card (without Go deeper) reads in at most 8 minutes | Content review + validation rules + repository test |
| A3 | Every C# block in the module comes from a snippet; `samples/` builds with zero warnings and `Lld.Samples.Tests` pass in CI | CI (`backend`) + a content test that the module has no inline C# fences |
| A4 | A renamed or deleted quoted symbol, an ambiguous selector, an unpaired `before` / `after`, an unknown callout type, a bad map node and a missing Q&A section each fail validation with file, line and reason | Unit tests (one fixture per rule) |
| A5 | Snippets come out de-indented, with doc comments, and identical between the dev API and the packed app | Unit tests + snapshot comparison |
| A6 | Before / After: switch on phone, side by side on desktop, changed lines marked; no layout jump on switch; keyboard works; no sideways page scroll at 320 px | Vitest + Playwright (phone + desktop) + `break-ui` |
| A7 | All 7 callout types render with icon and label, AA contrast in light and dark | Vitest + Playwright + contrast check |
| A8 | Interview answers start folded, open on tap, "Show all" opens all; the outline lists the section only | Vitest + Playwright |
| A9 | Module map: tap a node → its links highlighted + card; Esc / tap outside clears; *Open* goes to the lesson; keyboard reachable | Vitest (SVG fixture) + Playwright |
| A10 | Each cheat sheet fits one phone screen + 2 scrolls (existing A8 check from spec 003) | Playwright |
| A11 | In browser dev mode, editing a quoted `.cs` file shows the new code on reload | Integration test (API reload) |
| A12 | Older content (Start here, Auth) is unchanged; the device loads pack format 2 after an upgrade from format 1 | Unit tests (migrator, loader) + manual upgrade on Android |
| A13 | All CI checks green; zero warnings; guardrail lint clean | CI |

## 6. LLD hooks (used inside the lessons themselves)
| Principle / pattern | Where in this repo |
|---|---|
| OCP, Strategy | one class per validation rule (`IContentRule`); new rules for this spec are new classes only |
| LSP, contract tests | `IContentCatalog` contract suite (in-memory vs SQLite), `IContentFiles` (in-memory vs physical) |
| SRP | 15 single-purpose auth use cases; packer vs validator vs reader |
| DIP, ports and adapters | `Lld.Application` ports implemented by `Lld.Infrastructure`; `DataSource` in the web app |
| Value objects | `EmailAddress`, `Result` / `Error` |
| KISS / YAGNI | plain SQLite on device instead of EF Core (R67) |

## 7. Decisions (owner, 2026-10-16)
| Topic | Decision |
|---|---|
| Lesson set | Extended: 15 lessons (§3.1) |
| Examples | A classic before/after per lesson + an "In this app" section with real `src/` code |
| Compiling samples | Snippets pulled from compiled, tested code at pack time; the build fails when they break |
| Reader features | Before / After compare, callout boxes, principle map on the module page; folded interview Q&A |
| Depth | ~~Deep, about 20–30 minutes per lesson~~ → changed after batch 1: a short visual card (≤ 8 min) + folded "Go deeper" |
| Memory aids | REMEMBER stamp, everyday picture, icon + hook per concept, module mnemonics (A-PIE, SOLID) (§3.8) |
| Module map | Drawn by the app as «package» boxes (§3.6 note); kept after the batch 1 review (owner: "yes please continue") |
| Owner's stories | Optional `WORK` callout slots, filled later by the owner |
| Snippet lookup | By C# symbol with Roslyn (syntax only); no `#region` markers in production code |
| Lesson list and order | As in §3.1 |
| Callout types | The seven in §3.5 |
| Review flow | Reader features and samples first, then lessons in four batches (pillars, design basics, SOLID, heuristics); the owner reviews each batch on the branch before the next |

## 8. Open questions
None. Resolved on 2026-10-16 (see §7).
