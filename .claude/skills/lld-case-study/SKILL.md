---
name: lld-case-study
description: Designs and writes a Low-Level Design (LLD) case study for the LLD Guide app (lesson + interview cheat sheet under content/case-studies/). Use whenever a new LLD case study, machine-coding problem, or "design X" lesson is about to be planned, designed, or written — e.g. Parking Lot, Elevator, Splitwise, LRU Cache, or a case study drawn from this app's own code (like Auth). ALWAYS starts by interviewing the user before any design work.
---

# LLD Case Study

Every case study in this repo is personal: the author has 6.5 years of software
engineering experience and may want to bring a real-world version of the problem.
**Never design a case study silently.**

## Step 1 — Ask first (mandatory, before any design)

Use AskUserQuestion (or ask directly) and wait for answers. Cover:

1. **Source** — Classic textbook version, the user's own real-work version, or a
   case study taken from *this app's code* (self-referential)?
2. **Personal context** — Any system they built/maintained that maps to this
   problem? What went well, what broke, what would they redesign?
3. **Scope** — Which functional requirements are in / out? Single process or
   distributed concerns mentioned only as extensions?
4. **Scale & constraints** — Users, data volume, concurrency, latency, anything
   that changes the class design.
5. **Emphasis** — Which patterns / SOLID principles should this case study
   showcase? Interview-style (45 min) or deep-dive?

If an answer is vague, ask a follow-up. Do not fill gaps with assumptions —
list remaining unknowns and confirm them.

## Step 2 — Confirm the outline

Present a short outline (requirements + entity list + chosen patterns) and get
an explicit OK before writing code or the full lesson.

## Step 3 — Write the lesson

Output: `content/case-studies/<slug>/lesson.md` (`<slug>` is kebab-case) with front-matter:

```yaml
---
id: case-study-<slug>          # stable forever: progress and notes point at it
title: "<Title>"                # quote it if it contains a colon
order: <n>                       # position in the module, unique
summary: "<one sentence for the lesson list>"
tags: [<patterns>, <principles>]
source: classic | personal | self-referential
---
```

The module comes from the folder; there is no `module:` key. The contract is in
`specs/003-content-engine/spec.md` §3.1 and is enforced by the build.

Sections, in order:

1. **Problem statement** — 3–5 lines. If personal, the real-world context.
2. **Requirements** — Functional (numbered) and non-functional.
3. **Core entities** — table: entity · responsibility · key fields.
4. **Class diagram** — Mermaid `classDiagram`.
5. **Key flows** — Mermaid `sequenceDiagram` for 1–3 main use cases
   (add `stateDiagram-v2` when an entity has a lifecycle).
6. **Patterns used & why** — for each: pattern, where, what it buys, what the
   alternative would have cost.
7. **SOLID mapping** — one line per principle pointing at a concrete class.
8. **C# implementation** — idiomatic .NET 10 / C# 14, following the
   `clean-code-csharp` skill. For **self-referential** case studies, do not
   duplicate code: link the real files (`src/...`) and quote only short excerpts.
9. **Trade-offs & extensions** — what changes at 10× scale, concurrency,
   persistence, testing approach.
10. **Common interview follow-ups** — 5–8 questions with short answers.

## Step 4 — Write the cheat sheet

Output: `content/case-studies/<slug>/cheatsheet.md`. Its front-matter is only
`title: "Cheat sheet: <Title>"`; everything else comes from the lesson.
Case-study cheat sheets fit a phone screen with at most 4 scrolls (concept
cheat sheets: 2): requirements in 5 bullets, entity list, the class diagram,
patterns with one-line reasons, 3 gotchas, 30-second pitch.

## Step 5 — Verify

- Mermaid blocks render (validate if a Mermaid tool is available).
- C# samples compile (put runnable samples under `samples/case-studies/<slug>/`
  or point at real `src/` code with tests).
- Links to `src/` files and between lessons resolve, and the front-matter is
  valid: the content tests (`dotnet test --project tests/Lld.Content.Tests`) and
  the app build both fail with `path:line: code: message` otherwise.
