# Spec 006: Design patterns content

| | |
|---|---|
| **Status** | Approved by owner (2026-10-20) |
| **Branch** | `spec/006-gof-patterns` |
| **Covers** | R15 (patterns part), R20, R24 (PRD §13); PRD F2, F3, §7 ("C# samples must compile") |
| **Depends on** | Spec 004 (concept template, snippets, Before / After, callouts, map, mnemonics), spec 005 (progress) |
| **Case study** | None (content spec). Lessons point at this app's own code where it really uses a pattern. |

## 1. Goal
Ship two modules that make a 7+ year .NET developer fluent in design patterns for interviews:

- **GoF Design Patterns**: all 23 patterns (Creational 5, Structural 7, Behavioral 11).
- **Enterprise & .NET patterns**: Repository, Unit of Work, Specification, Null Object, DI vs Service Locator.

Each pattern is a short visual card in the spec 004 style, plus what a senior candidate is expected to add: the
**structure** as a UML class diagram, **what modern C# uses instead**, **where .NET itself uses it**, **which
patterns it is confused with**, **which patterns it travels with** and **when it hurts**. All C# is quoted from
compiled, tested samples.

The reader gains four features: a **periodic table** of patterns on the module page, a **"Which pattern?" picker**,
a **look-alike compare**, and an **Overuse** stamp.

## 2. User stories
1. *As a learner,* I see every pattern of a module at once as a periodic table, coloured by category, and the
   letter symbols and icons help me recall them.
2. *As a learner,* I describe my problem by answering 2–3 questions and the picker points me to the pattern (and
   says why).
3. *As a learner,* when two patterns look alike (Strategy vs State, Decorator vs Proxy vs Adapter) I open them side
   by side and see the one difference and the question that tells them apart.
4. *As a learner,* each lesson shows me the classic structure, a Before / After, and how I would write it in modern
   C# today, so I can say in an interview "the classic form is X, in .NET I'd use Y".
5. *As a learner,* I can name where .NET and this app use the pattern, which patterns it combines with, and the
   signs it is being overused.
6. *As a learner,* before an interview I revise each pattern from its cheat sheet in under two minutes.
7. *As the owner,* I add a "From my work" story to any lesson later without touching code.

## 3. Scope

### 3.1 The modules
Module order becomes: Start here (0), OOP & SOLID (1), **GoF Design Patterns (2)**, **Enterprise & .NET patterns
(3)**, UML (4, spec 008), Case studies (**5**, was 4). Lesson `id` = folder slug, unique across the app.

**`content/gof-patterns/`**, title "GoF Design Patterns", reading order (simple and common first, within each
category):

| # | Category | Slug | Title | Symbol | In this app (where it is real) |
|---|---|---|---|---|---|
| 1 | Creational | `singleton` | Singleton | Sg | DI singletons (`AddSingleton`) |
| 2 | Creational | `factory-method` | Factory Method | Fm | `DesignTimeDbContextFactory` |
| 3 | Creational | `abstract-factory` | Abstract Factory | Af | (none, classic example only) |
| 4 | Creational | `builder` | Builder | Bu | `MapBuilder` (content map), test builders |
| 5 | Creational | `prototype` | Prototype | Pr | records + `with` |
| 6 | Structural | `adapter` | Adapter | Ad | `HttpDataSource` / `BridgeDataSource` behind `DataSource` |
| 7 | Structural | `facade` | Facade | Fa | `SessionManager` |
| 8 | Structural | `decorator` | Decorator | De | `DelegatingHandler` chain (also Chain) |
| 9 | Structural | `proxy` | Proxy | Px | (classic + .NET examples) |
| 10 | Structural | `composite` | Composite | Co | (classic + .NET examples) |
| 11 | Structural | `bridge` | Bridge | Br | (classic + .NET examples) |
| 12 | Structural | `flyweight` | Flyweight | Fl | string interning / `ArrayPool` (.NET) |
| 13 | Behavioral | `strategy` | Strategy | St | `IPasswordPolicy`, `IContentRule`, `DataSource` |
| 14 | Behavioral | `template-method` | Template Method | Tm | (classic + .NET examples) |
| 15 | Behavioral | `observer` | Observer | Ob | `IDomainEventObserver` (audit trail) |
| 16 | Behavioral | `command` | Command | Cm | (classic + .NET examples) |
| 17 | Behavioral | `iterator` | Iterator | It | `yield return` in the content reader |
| 18 | Behavioral | `state` | State | Sa | `AccessRequest` (`PendingState`, `DecidedState`) |
| 19 | Behavioral | `chain-of-responsibility` | Chain of Responsibility | Ch | `TunnelHeadersHandler`, `ServerReachabilityHandler`, ASP.NET middleware |
| 20 | Behavioral | `mediator` | Mediator | Md | (classic + .NET examples) |
| 21 | Behavioral | `memento` | Memento | Mm | (classic example) |
| 22 | Behavioral | `visitor` | Visitor | Vi | `CSharpSymbolFinder` (Roslyn syntax walker) |
| 23 | Behavioral | `interpreter` | Interpreter | In | (classic example) |

**`content/enterprise-patterns/`**, title "Enterprise & .NET patterns":

| # | Slug | Title | Symbol | In this app |
|---|---|---|---|---|
| 1 | `repository` | Repository | Re | `EfAccessRequestRepository`, `EfRefreshTokenRepository` |
| 2 | `unit-of-work` | Unit of Work | Uw | `IUnitOfWork` over the EF Core `DbContext` |
| 3 | `specification` | Specification | Sp | (classic example) |
| 4 | `null-object` | Null Object | No | (classic example) |
| 5 | `di-vs-service-locator` | DI vs Service Locator | Di | composition roots in `Lld.Api` / `Lld.App` |

The "In this app" column is checked against the code while writing each lesson; a pattern without a real use says
so and uses .NET examples instead (no invented app examples).

### 3.2 Lesson template `pattern`
`module.yml` gets `template: pattern`. Same writing style as spec 004 (short sentences, everyday words, picture or
code before prose). Front-matter as concept lessons (`icon`, `hook` required) plus `symbol` (§3.5).

**The card** (always visible, reads in ≤ 8 minutes):
1. **REMEMBER stamp**: one or two lines to say in an interview.
2. **The picture**: an everyday analogy as a small Mermaid diagram (a travel plug adapter, a TV remote, a coffee
   order with add-ons).
3. **`## The idea`**: the intent in two or three lines, then *Use it when* (2–3 short bullets: the smells that call
   for it).
4. **`## Structure`**: the classic UML class diagram (Mermaid `classDiagram`, already interactive: tap a participant
   to see its links), with one line per participant.
5. **`## See it`**: one Before / After from the samples: the problem code, then the pattern.
6. **`## Modern C#`**: what a .NET developer writes today (e.g. Strategy → `Func<T>` or DI keyed services; Iterator →
   `yield return`; Visitor → pattern matching; Singleton → DI lifetime; Prototype → `record` + `with`; Observer →
   `event` / `IObservable<T>` / `Channel<T>`; Command → delegates), as one snippet and one line on the trade-off. If
   the classic form is still the right one, the section says so.
7. **`## Seen in .NET`**: 2–3 bullets naming real BCL / ASP.NET Core types (`Stream` wrappers, `IEnumerable<T>`,
   middleware, `HttpClient` handlers, `IOptions<T>`...).
8. **Look-alikes** (not authored in the lesson): the reader adds a small strip linking to each compare this pattern
   is part of (§3.7).
9. **`## Travels with`**: 2–3 patterns it combines with and why, in one line each (Composite + Visitor + Iterator;
   Command + Memento for undo), then an **Overuse stamp** (§3.8).
10. **`## In this app`** (optional): one snippet and one line, only where real (§3.1).
11. **`## Interview questions`**: 5 questions, ~2-line answers, folded (spec 004 §3.5).

**`## Go deeper`** (folded): a sequence diagram of one call (behavioral and structural patterns, with the existing
Step through control), variations and implementation details (e.g. thread-safe Singleton, pull vs push Observer),
the classic GoF consequences in plain words, using it in a design interview, misconceptions, related lessons, and a
`[!WORK]` slot.

**Cheat sheet** (≤ 1 screen + 2 scrolls, R72): REMEMBER stamp, then `##` blocks *Use it when* · *The shape* (a few
lines of code) · *Modern C#* · *Don't confuse with* · *Interview one-liners*.

**Validation (template `pattern`)**, each rule its own class with a test: the concept rules still apply (REMEMBER
first, icon, hook, interview questions, no inline C#); plus required sections *The idea*, *Structure* (with a
`classDiagram`), *See it* (a Before / After pair), *Modern C#*, *Seen in .NET*, *Travels with*; exactly one Overuse
stamp; `symbol` present and valid.

### 3.3 Compiled samples
- **`samples/Lld.Samples.Patterns/`** (class library, .NET 10, C# 14), one folder per pattern grouped by category
  (`Creational/Builder/`, `Enterprise/Repository/`), with `Before`, `After` and `Modern` namespaces. Zero warnings.
- **`tests/Lld.Samples.Tests/`** gains tests showing the behaviour each design claims (same output before and after;
  the pattern's extension point really works, e.g. a new strategy added without editing the context).
- Enterprise samples use an in-memory store, so the samples library takes no EF Core dependency; the EF Core version
  is quoted from `src/Lld.Infrastructure` in *In this app*.
- Snippets, Before / After marks and dev-mode reload work exactly as in spec 004.

### 3.4 Module mnemonics (proposed, owner to approve)
Mnemonics may now be a **phrase**: when the part before `:` has spaces, each word's first letter is one tile (the
current single-word form like `SOLID` keeps working). The module page draws the phrase as letter tiles, the first
letter of each word big, the rest of the word small under it.

| Category | Phrase | Patterns, in letter order |
|---|---|---|
| Creational | **S**mart **B**uilders **A**lways **F**ollow **P**lans | Singleton, Builder, Abstract Factory, Factory Method, Prototype |
| Structural | **A**rchitects **B**uild **C**ities **D**esigned **F**or **F**uture **P**eople | Adapter, Bridge, Composite, Decorator, Facade, Flyweight, Proxy |
| Behavioral | **C**ats **C**an **I**gnore **I**nstructions; **M**ice **M**ostly **O**bey **S**trict **S**chedules, **T**hen **V**anish | Chain of Responsibility, Command, Interpreter, Iterator, Mediator, Memento, Observer, State, Strategy, Template Method, Visitor |
| Enterprise | **D**on't **R**un **U**ntested **S**QL **N**ightly | DI vs Service Locator, Repository, Unit of Work, Specification, Null Object |

Validation as today: each tile's letter is the first letter of its lesson's title.

### 3.5 Periodic table on the module page
- **Authoring:** lesson front-matter `symbol`: two letters, first upper case, second lower case, unique in the module.
  Categories come from the module `map:` subgraphs (as spec 004); map edges are "travels with" links, labelled.
- **Reader:** modules with `template: pattern` show a **periodic table** instead of the «package» map: one coloured
  block per category (colour tokens per category, light and dark, AA), each tile shows the symbol large, the icon,
  the title and the reading order number; Done tiles get the spec 005 ink mark. Tapping a tile highlights the tiles it
  travels with and its look-alikes, and shows a small card (hook, summary, reading time, *Open*); Esc / tap outside
  clears. Keyboard: arrow keys move between tiles. Phone: tiles wrap in a grid that fits 320 px with no sideways
  scroll. The lesson list stays below as the primary navigation.

### 3.6 "Which pattern?" picker
- **Authoring:** optional `picker:` in `module.yml`, a small decision tree. Each node is a question with 2–5 answers;
  each answer leads to another question or to 1–2 lessons with a one-line *why*.
  ```yaml
  picker:
    start: what
    questions:
      what:
        ask: "What is your problem about?"
        answers:
          - { label: "Creating objects", next: create }
          - { label: "Fitting objects together", next: structure }
          - { label: "How objects behave and talk", next: behave }
      swap:
        ask: "Who decides when the behaviour changes?"
        answers:
          - { label: "The caller picks it", pick: [strategy], why: "Swap the algorithm from outside." }
          - { label: "The object, as its state changes", pick: [state], why: "The object changes its own class-like behaviour." }
  ```
- **Validation:** `start` exists; every `next` exists; no cycles; at most 4 questions deep; every `pick` is a lesson of
  the module; **every lesson of the module is reachable** (so the picker stays complete as lessons are added).
- **Reader:** a "Which pattern?" button on the module page opens a sheet (bottom sheet on phone, dialog on desktop):
  one question at a time as large answer buttons, a Back button and the path so far ("Behaviour → Who decides?");
  the result shows the picked pattern tile(s), the *why* and *Open*. Android Back steps back through the questions
  first. Nothing is saved. GoF gets a picker; Enterprise (5 lessons) does not need one.

### 3.7 Look-alike compare
- **Authoring:** optional `lookalikes:` in `module.yml`:
  ```yaml
  lookalikes:
    - patterns: [strategy, state]
      difference: "Strategy is chosen by the caller; State switches itself as the object's state changes."
      ask: "Who swaps the behaviour: the client or the object itself?"
  ```
  Proposed GoF set: Strategy · State; Strategy · Template Method; Strategy · Command; Decorator · Proxy · Adapter;
  Factory Method · Abstract Factory · Builder; Facade · Mediator; Observer · Mediator; Composite · Decorator;
  Bridge · Adapter; Chain of Responsibility · Decorator; Command · Memento. Enterprise: Repository · Unit of Work;
  DI vs Service Locator is itself a compare lesson.
- **Validation:** 2–3 lessons of the module, no duplicates, `difference` and `ask` required (≤ 160 characters each).
- **Reader:** a compare view (opened from the lesson's look-alike strip, the periodic-table card, or a "Look-alikes"
  list on the module page). It shows, per pattern: tile (symbol, icon), hook, *The idea* summary and its *Structure*
  diagram; above them, the **one difference** and the **ask yourself** question as a stamp. Wide screens: columns side
  by side. Phone: a segmented switch between the patterns (same component and motion as Before / After, no layout
  jump), with the difference stamp fixed on top.
- **Data:** the pack gains `symbol` per lesson, the picker and the look-alikes per module (pack format 3, device schema
  step 5, contracts and `web/src/data/types.ts`). The *Structure* diagram is located by its section, not copied.

### 3.8 Overuse stamp
A new callout type `[!OVERUSE]` (R79 list), drawn as a stamp like REMEMBER but in the warning colour, with the label
"When it hurts": 2–3 signs the pattern is doing harm (e.g. Singleton: hidden dependencies, tests that share state).
Required once in every `pattern` lesson; allowed in any lesson.

### 3.9 UI quality
New UI (periodic table, picker, compare, Overuse stamp, phrase mnemonics) follows the visual identity and is built and
critiqued with `frontend-design`, `emil-design-eng`, `apple-design`, `mobile-native`, `animate`, `break-ui`.
Screenshots in phone light / dark and desktop are reviewed and fixed before the PR.

## 4. Out of scope
- Patterns outside the 28 lessons (CQRS, Event Sourcing, Saga, Circuit Breaker...): later, if asked.
- UML as a topic (spec 008): class and sequence diagrams are used, not taught.
- Quizzes, scores, saved picker answers (R21). The picker and compare are reveal-only.
- A cross-module picker (each module has its own).
- Writing the owner's "From my work" stories (slots only).
- The spec 005 open question (fling-to-end marks a lesson done): owner decides later.

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | GoF shows 23 lessons and Enterprise 5, in the order of §3.1, each with a cheat sheet, offline, in the Windows and Android apps; Case studies moves to order 5 | Manual + Playwright |
| A2 | Every lesson follows the `pattern` template (§3.2); the card reads in ≤ 8 minutes | Validation rules + content review + repository test |
| A3 | Every C# block comes from a snippet; `Lld.Samples.Patterns` builds with zero warnings and its tests pass in CI | CI (`backend`) |
| A4 | A missing pattern section, a missing `classDiagram`, a bad / duplicate `symbol`, a missing or second Overuse stamp, a picker with a dangling `next`, a cycle or an unreachable lesson, and a bad look-alike entry each fail validation with file, line and reason | Unit tests (one fixture per rule) |
| A5 | Periodic table: categories coloured, tap a tile → links + look-alikes highlighted + card; Esc / outside clears; arrow keys move; no sideways scroll at 320 px | Vitest + Playwright (phone + desktop) + `break-ui` |
| A6 | Picker: one question at a time, Back and Android Back step back, the result opens the lesson; every GoF lesson is reachable | Vitest + Playwright + validation |
| A7 | Compare: side by side on desktop, switch on phone without layout jump, difference + ask stamp visible, reachable from lesson, tile card and module list | Vitest + Playwright |
| A8 | Phrase mnemonics draw one tile per word and open the lesson; `SOLID` / `A-PIE` unchanged | Vitest + Playwright |
| A9 | Overuse stamp renders in light and dark with AA contrast | Vitest + Playwright + contrast check |
| A10 | Each cheat sheet fits one phone screen + 2 scrolls | Playwright (existing A8 check) |
| A11 | Older modules unchanged; the device loads pack format 3 after an upgrade from format 2; progress survives the upgrade | Unit tests (migrator, loader) + manual upgrade on Android |
| A12 | All CI checks green; zero warnings; guardrail lint clean | CI |

## 6. LLD hooks (used inside the lessons themselves)
| Pattern | Where in this repo |
|---|---|
| Strategy | `IPasswordPolicy`, `IContentRule`, the web `DataSource` |
| State | `AccessRequest` with `PendingState` / `DecidedState` |
| Observer | `IDomainEventObserver` (the audit trail) |
| Chain of Responsibility / Decorator | `HttpClient` `DelegatingHandler`s (`TunnelHeadersHandler`, `ServerReachabilityHandler`), ASP.NET middleware |
| Adapter | `HttpDataSource` / `BridgeDataSource` |
| Visitor | `CSharpSymbolFinder` over Roslyn syntax |
| Repository, Unit of Work | `Ef*Repository`, `IUnitOfWork` |
| Builder, Factory Method | `MapBuilder`, `DesignTimeDbContextFactory` |
| Pattern template rules, picker and look-alike rules | new `IContentRule` classes (Strategy + OCP, again) |

## 7. Decisions (owner, 2026-10-20)
| Topic | Decision |
|---|---|
| Pattern set | All 23 GoF + 5 modern extras |
| Extras home | Own module "Enterprise & .NET patterns", after GoF |
| Lesson shape | Spec 004 card + pattern parts: structure diagram, Modern C#, Seen in .NET, look-alikes, travels with, overuse |
| Reader features | Periodic table, "Which pattern?" picker, look-alike compare, Overuse stamp |
| Mnemonics | Yes, one per category; Claude proposes (§3.4), owner approves |
| Review flow | Reader features + samples first; then batches by category: Creational (5), Structural (7), Behavioral A (6: Strategy → State), Behavioral B (5: Chain → Interpreter), Enterprise (5). Owner reviews each batch on the branch |
| Spec 005 fling rule | Decide later |

## 8. Open questions (for this review)
1. **Slugs and titles:** module folders `gof-patterns` and `enterprise-patterns`, lesson slugs as in §3.1 (permanent
   once published). OK?
2. **Reading order** in §3.1 (Singleton first, Interpreter last). OK, or the classic GoF book order?
3. **Mnemonics** in §3.4 and **symbols** in §3.1: keep, or change any?
4. **Look-alike set** in §3.7: add or drop pairs?
