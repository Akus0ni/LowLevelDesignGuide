# Spec 008: UML content

| | |
|---|---|
| **Status** | Approved by owner (2026-10-20); open questions delegated to Claude (§8) |
| **Branch** | `spec/008-uml` |
| **Covers** | R15 (UML part), R20, R24 (PRD §13); PRD F2, F3, §7 ("C# samples must compile") |
| **Depends on** | Spec 003 (Mermaid, interactive class / sequence diagrams, Step through), spec 004 (concept card, snippets, callouts, «package» map, mnemonics), spec 006 (picker, phrase mnemonics, stamps) |
| **Case study** | None (content spec). The "In this app" diagrams draw this repo's own code. |

## 1. Goal
Ship a **UML** module that lets a .NET developer read and sketch the diagrams an LLD interview asks for, fast and
correctly: the class diagram in depth (members, the six arrows, multiplicity), sequence, state and activity diagrams,
and short lessons on object, use case and component / deployment diagrams. The module ends with **"UML in an LLD
interview"**: which diagram answers which question, and how to sketch it on a whiteboard in minutes.

Every lesson is a short visual card (spec 004 style). Diagrams that stand for code are **paired with compiled C#**
and checked against it at pack time, so the drawing and the code cannot drift apart.

The reader gains four features: **Code ↔ UML** side by side, **interactive state diagrams** (tap a state, fire
events), an **arrow legend** card, and an **activity step-through**. The spec 006 picker is reused as **"Which
diagram?"**.

## 2. User stories
1. *As a learner,* I read a C# class and its UML side by side; tapping a box or an arrow lights up the code that
   makes it, so I learn to translate both ways.
2. *As a learner,* I can tell the six relationship arrows apart at a glance and say which C# each one means; the
   arrow legend is one tap away from any class diagram.
3. *As a learner,* I tap a state to see its transitions and fire events to walk a state machine from its start.
4. *As a learner,* I step through an activity diagram node by node and choose a branch at each decision.
5. *As a learner,* I answer two or three questions and the "Which diagram?" picker tells me which diagram fits the
   interview question in front of me.
6. *As a learner,* I see this app drawn in UML (its access-request state machine, login sequence, components,
   deployment) and recognise the code behind each diagram.
7. *As a learner,* before an interview I revise each diagram from its cheat sheet in under two minutes.
8. *As the owner,* I add a "From my work" story to any lesson later without touching code.

## 3. Scope

### 3.1 The module
`content/uml/`, title "UML", `order: 4` (between Enterprise & .NET patterns (3) and Case studies (5), as spec 006
already reserved), `template: diagram`. Lesson `id` = folder slug, unique across the app (hence the `-diagram`
suffix: `state` is already a GoF lesson).

| # | Slug | Title | Kind | Mermaid | In this app (checked against `src/` while writing) |
|---|---|---|---|---|---|
| 1 | `class-basics` | Classes and members | Structure | `classDiagram` | `EmailAddress` (value object), `Result` |
| 2 | `class-relationships` | Relationships: the six arrows | Structure | `classDiagram` | `AccessRequest` + states, `AggregateRoot`, `IPasswordPolicy` |
| 3 | `multiplicity` | Multiplicity and navigability | Structure | `classDiagram` | `RefreshToken` family, `Note` per lesson |
| 4 | `object-diagram` | Object diagram | Structure | `classDiagram` (objects as `name : Type`) | one refresh-token family at a moment in time |
| 5 | `component-deployment` | Component and deployment diagrams | Structure | `flowchart` with subgraphs | solution projects and the dependency rule; owner PC + ngrok + ntfy + phone |
| 6 | `use-case-diagram` | Use case diagram | Behaviour | `flowchart` (actor + ovals in a system box) | request access, approve, sign in, change password |
| 7 | `sequence-diagram` | Sequence diagram | Behaviour | `sequenceDiagram` | login, then refresh-token rotation |
| 8 | `state-diagram` | State diagram | Behaviour | `stateDiagram-v2` | `AccessRequest` (`PendingState` → `DecidedState`) |
| 9 | `activity-diagram` | Activity diagram | Behaviour | `flowchart` (start / end dots, decisions, fork bars) | notes write-behind flush |
| 10 | `uml-in-interviews` | UML in an LLD interview | Practice | all | which diagram for which question; a 10-minute sketch order |

Mermaid has no native use case, activity, component or deployment diagram. Those lessons draw them with `flowchart`
and say so plainly; the notation section shows what the real UML symbol looks like and how we approximate it. A
"real" UML-only symbol that Mermaid cannot draw (e.g. the underline on object names) is named in the text, not faked.

### 3.2 Lesson template `diagram`
`module.yml` gets `template: diagram` (a third value next to `concept` and `pattern`). Same writing style as spec 004
(short sentences, everyday words, picture or code before prose). Front-matter as concept lessons (`icon`, `hook`
required).

**The card** (always visible, reads in ≤ 8 minutes):
1. **REMEMBER stamp**: one or two lines to say in an interview.
2. **The picture**: an everyday analogy as a small Mermaid diagram (a family tree for inheritance, a train timetable
   for a sequence, a traffic light for state).
3. **`## The idea`**: what the diagram shows in two or three lines, then *Draw it when* (2–3 short bullets).
4. **`## Notation`**: the symbols, drawn (one small diagram) with one line per symbol. Class lessons link to the
   arrow legend (§3.5).
5. **`## Read it`**: one real diagram, read piece by piece (from this app where real, §3.1).
6. **`## Code ↔ UML`**: one paired block (§3.4) where the diagram stands for code. Lessons whose diagram is not code
   (use case, deployment) use a short *From requirements* pair instead: a requirement list and the diagram it gives.
7. **`## In an interview`**: when to draw it, how much detail, how long to spend.
8. **Mistakes stamp** (§3.7): 2–3 common mistakes.
9. **`## Interview questions`**: 5 questions, ~2-line answers, folded (spec 004 §3.5).

**`## Go deeper`** (folded): the parts of the notation interviews rarely need (stereotypes, packages, interaction
fragments `alt` / `loop` / `opt`, composite and history states, swimlanes), UML 2 names, how tools draw it, related
lessons, and a `[!WORK]` slot.

**The interview lesson** (`uml-in-interviews`) uses the same template, with *Notation* replaced by a one-screen
**"Which diagram for which question"** table, and *Read it* by a worked example: one LLD prompt (e.g. "design a
library") sketched in order: use cases → classes → one sequence → one state machine.

**Cheat sheet** (≤ 1 screen + 2 scrolls, R72): REMEMBER stamp, then `##` blocks *Draw it when* · *The symbols* (a
tiny diagram) · *From C#* (a few lines of code and the UML they give) · *Mistakes* · *Interview one-liners*.

**Validation (template `diagram`)**, each rule its own class with a test: the concept rules still apply (REMEMBER
first, icon, hook, interview questions, no inline C#); plus required sections *The idea*, *Notation* (with a Mermaid
diagram; the interview lesson: its table), *Read it* (with a Mermaid diagram), *Code ↔ UML* (with a paired block or
a *From requirements* pair), *In an interview*; exactly one Mistakes stamp.

### 3.3 Compiled samples
- **`samples/Lld.Samples.Uml/`** (class library, .NET 10, C# 14), one folder per lesson (`ClassBasics/`,
  `Relationships/`, `Multiplicity/`, `Sequence/`, `State/`, `Activity/`). Zero warnings.
- **`tests/Lld.Samples.Tests/`** gains tests showing the behaviour each diagram claims: composition really owns its
  parts (removing the whole removes them), aggregation does not, the state machine sample allows exactly the
  transitions drawn and rejects the rest, the activity sample takes the branch the diagram shows.
- Where a pair draws this app, the snippet quotes `src/` (as spec 004 / 006 "In this app" do).

### 3.4 Code ↔ UML pairs (authoring, check, reader)
- **Authoring:** a Mermaid fence with a `code=` attribute names the snippet it stands for:
  ````md
  ```mermaid code="samples/Lld.Samples.Uml/Relationships/Order.cs#Order"
  classDiagram
    Order *-- "1..*" OrderLine
    Order --> Customer
  ```
  ````
  The snippet path and symbol syntax is the existing `snippet=` syntax (Roslyn, spec 004). The reader shows the
  snippet's code next to the diagram; the author writes no C# by hand.
- **Link map (pack time):** for every diagram name, the line range of the matching type or enum member in the snippet;
  for every class-diagram arrow, the lines that create it: the base list for inheritance / realization, a field or
  property of that type (or a collection of it) for association / aggregation / composition, a parameter, local or
  `new` for dependency. Written into the packed fence like the Before / After marks (`FenceEdits`), so no new tables.
- **Match rule (`diagram` and every other template):** in a paired block, every class, participant or state named in
  the diagram exists as a type or enum member in the snippet; every inheritance / realization arrow matches the base
  list; every association-family arrow has a member of that type; a dependency arrow has at least one use. A
  mismatch fails the build with `file(line): reason` ("`Order --> Customer`: Order has no member of type Customer").
- **Reader:** wide screens show code and diagram side by side; phones show a segmented switch *Diagram · Code*
  (the Before / After component and motion, no layout jump). Tapping a box or an arrow highlights its code lines and
  scrolls them into view; tapping a code line that a link covers highlights its box or arrow. Esc / tap outside
  clears. Without links (an older pack, an unknown diagram) the pair stays a plain diagram plus code.

### 3.5 Arrow legend card
A reader component (not authored): the six arrows, each drawn by Mermaid from the same source as lesson diagrams
(so they look identical), with its name, a one-line meaning and the C# that makes it:

| Arrow | Name | C# |
|---|---|---|
| `A --> B` | Association | `class A { B b; }` |
| `A o-- B` | Aggregation | `class A { List<B> items; }` (B lives on its own) |
| `A *-- B` | Composition | `class A { List<B> parts = new(); }` (B dies with A) |
| `A <\|-- B` | Inheritance | `class B : A` |
| `A <\|.. B` | Realization | `class B : IA` |
| `A ..> B` | Dependency | `void Do(B b)` / `new B()` |

Opens from an **Arrows** button on every class diagram in the app (all modules, including the expanded viewer) as a
popover on desktop and a bottom sheet on phone; Esc / Android Back closes it. The texts live in one data file.
The `class-relationships` lesson and its cheat sheet show it inline.

### 3.6 Interactive state diagrams and activity step-through
- **State diagrams (all `stateDiagram-v2` in the app,** including the Auth case study):
  tapping a state highlights its incoming and outgoing transitions and dims the rest (like class diagrams). A
  **Run** control starts at the initial state, shows one button per outgoing event label ("approve", "reject"),
  moves the current-state mark with a short animation, and offers *Reset*; final states end the run. Built on the
  rendered SVG plus the Mermaid source, tested against real Mermaid output (`__fixtures__`, as spec 003 §3.8).
- **Activity step-through:** a `flowchart` whose fence has the `steps` attribute (```` ```mermaid steps ````) gets a
  Step through control (same as sequence diagrams): *Next* moves to the following node; at a decision, one button per
  labelled branch; a fork highlights all its branches together; *Back* and *Reset*. Plain flowcharts (pictures, maps)
  stay static.
- Keyboard: Tab / Enter reach states and controls; reduced motion turns the move into a plain switch.

### 3.7 Mistakes stamp
A new callout type `[!MISTAKES]` (R79 list), drawn as a stamp like Overuse (warning colour) with the label "Common
mistakes": 2–3 short items (e.g. "an arrow pointing the wrong way", "composition where the part outlives the whole").
Required once in every `diagram` lesson; allowed in any lesson.

### 3.8 "Which diagram?" picker and mnemonics
- **Picker:** the UML `module.yml` gets a `picker:` (spec 006 §3.6 syntax and sheet), e.g. *"What does the
  interviewer want to see?"* → *What the system does for whom* (use case) · *The parts and how they connect* (class /
  object / component) · *What happens over time* → *Messages between objects* (sequence) · *One object's life*
  (state) · *A workflow with branches* (activity). The picker currently shows only on pattern modules; it moves so it
  shows on any module that has one. Reachability is required for `pattern` modules only (CD-003).
- **Mnemonics** (phrase form, spec 006 §3.4), two phrases, one per kind:

  | Kind | Phrase | Lessons, in letter order |
  |---|---|---|
  | Structure | **C**lever **R**obots **M**ake **O**bjects **C**arefully | Classes and members, Relationships, Multiplicity, Object diagram, Component and deployment |
  | Behaviour | **U**sers **A**lways **S**end **S**ignals | Use case, Activity, Sequence, State |

  The interview lesson is in neither. The module page keeps the «package» map (spec 004) with two groups,
  *Structure* and *Behaviour*, plus *Practice*.

### 3.9 Data
- `template: diagram` joins the template list; the packed fences carry the `code=` link map and the `steps` flag.
- If the reader needs a field the pack does not have yet, the plan bumps the pack format (to 4) and the device
  schema step; otherwise the pack format stays 3. The plan decides; older modules are unchanged either way.

### 3.10 UI quality
New UI (Code ↔ UML, arrow legend, state Run, activity Step through, Mistakes stamp) follows the visual identity and
is built and critiqued with `frontend-design`, `emil-design-eng`, `apple-design`, `mobile-native`, `animate`,
`break-ui`. Screenshots in phone light / dark and desktop are reviewed and fixed before the PR.

## 4. Out of scope
- UML diagrams not listed (timing, communication, interaction overview, profile, composite structure, package as a
  lesson; packages are mentioned in Go deeper).
- Drawing or editing diagrams in the app (no editor, no "draw it yourself" canvas), quizzes and scores (R21).
- Generating diagrams from code automatically (the link map checks hand-written diagrams; it does not draw them).
- A non-Mermaid renderer (PlantUML etc.).
- Writing the owner's "From my work" stories (slots only).

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | UML shows 10 lessons in the order of §3.1, each with a cheat sheet, offline, in the Windows and Android apps, at module order 4 | Manual + Playwright |
| A2 | Every lesson follows the `diagram` template (§3.2); the card reads in ≤ 8 minutes | Validation rules + content review + repository test |
| A3 | Every C# block comes from a snippet; `Lld.Samples.Uml` builds with zero warnings and its tests pass in CI | CI (`backend`) |
| A4 | A missing `diagram` section, a missing / second Mistakes stamp, a `code=` pair naming a type not in the snippet, an inheritance arrow not in the base list, and an association with no member of that type each fail validation with file, line and reason | Unit tests (one fixture per rule) |
| A5 | Code ↔ UML: side by side on desktop, switch on phone without layout jump; tap box / arrow ↔ code lines both ways; Esc clears | Vitest + Playwright (phone + desktop) |
| A6 | Arrow legend opens from any class diagram (lesson and expanded viewer), closes with Esc / Android Back, renders in light and dark | Vitest + Playwright |
| A7 | State diagrams: tap a state highlights its transitions; Run fires events from the start state to a final state; Reset; works on the Auth case study diagram too | Vitest (real Mermaid fixtures) + Playwright |
| A8 | Activity step-through: Next, branch buttons at decisions, fork highlights branches, Back, Reset; plain flowcharts unchanged | Vitest + Playwright |
| A9 | "Which diagram?" picker shows on the UML module page and opens the picked lesson; the GoF picker is unchanged | Vitest + Playwright |
| A10 | Mistakes stamp renders in light and dark with AA contrast; phrase mnemonics draw and open their lessons | Vitest + Playwright + contrast check |
| A11 | Each cheat sheet fits one phone screen + 2 scrolls | Playwright (existing check) |
| A12 | Older modules unchanged; progress and notes survive the upgrade on Android (and the pack-format upgrade if §3.9 bumps it) | Unit tests + manual upgrade on Android |
| A13 | All CI checks green; zero warnings; guardrail lint clean | CI |

## 6. LLD hooks (used inside the lessons themselves)
| Diagram | Where in this repo |
|---|---|
| Class, relationships | `AccessRequest` + `PendingState` / `DecidedState`, `AggregateRoot`, `IPasswordPolicy`, `EmailAddress` |
| Multiplicity, object | `RefreshToken` families (one family, many rotated tokens) |
| Component, deployment | the solution's project graph and dependency rule (`docs/plan.md` §2); owner PC + Windows service + ngrok + ntfy |
| Use case | access requests: learner, admin, system |
| Sequence | login and refresh rotation through `SessionManager` |
| State | `AccessRequest` |
| Activity | notes / progress `WriteBehind` flush |
| `diagram` rules, the match rule | new `IContentRule` classes (Strategy + OCP, again) |

## 7. Decisions (owner, 2026-10-20)
| Topic | Decision |
|---|---|
| Diagram set | Core 4 (class, sequence, state, activity) + interview extras (use case, object, component / deployment) |
| Class diagram depth | Three lessons: classes and members; the six arrows; multiplicity and navigability |
| Reader features | Code ↔ UML side by side, interactive state diagrams, arrow legend card, activity step-through |
| Code check | Compiled samples + a match rule between diagram and snippet |
| Lesson shape | New `diagram` template (Notation, Read it, Code ↔ UML, In an interview, Mistakes stamp) |
| Picker | Final lesson "UML in an LLD interview" + "Which diagram?" picker |
| App examples | Draw this app, checked against `src/` |
| Review flow | Everything in one go: one review on the PR |
| Open questions | Delegated to Claude; logged in `docs/claude-decisions.md` for later review (§8) |

## 8. Decisions made by Claude (delegated by the owner, 2026-10-20; review later in `docs/claude-decisions.md`)
| ID | Question | Decision |
|---|---|---|
| CD-001 | Slugs, titles, order | As §3.1: structure first, behaviour next, interview last; the interview lesson teaches the drawing order |
| CD-002 | Mnemonics | Keep "Clever Robots Make Objects Carefully" and "Users Always Send Signals" (§3.8) |
| CD-003 | Picker reachability | "Every lesson reachable" applies to `pattern` modules only; other modules need valid picks only |
| CD-004 | State Run scope | Every `stateDiagram-v2` in the app (Auth case study, UML; the GoF State lesson draws a class diagram, not a state diagram) |
