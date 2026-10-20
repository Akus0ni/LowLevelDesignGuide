# Decisions made by Claude

The owner delegated these choices to Claude and will review them later. Each entry says what was decided, why,
and what the alternative was, so it can be reversed cheaply. When the owner reviews one, set **Review** to
✅ kept or 🔁 changed (and link the PR that changes it).

| ID | Date | Spec | Question | Decision | Why | Alternative not taken | Review |
|---|---|---|---|---|---|---|---|
| CD-001 | 2026-10-20 | 008 | Lesson order in the UML module | Structure first (Classes and members → Component and deployment), then behaviour (Use case → Activity), interview lesson last, as spec 008 §3.1 | The class diagram is the one interviews use most and its notation is reused by the object, component and state lessons; learning order ≠ drawing order. The interview lesson teaches the drawing order (use cases → classes → sequence → state) explicitly | Use case first, matching the interview drawing order | ⏳ pending |
| CD-002 | 2026-10-20 | 008 | UML mnemonics | Keep "**C**lever **R**obots **M**ake **O**bjects **C**arefully" (structure) and "**U**sers **A**lways **S**end **S**ignals" (behaviour) | Short, concrete pictures; each letter is the first letter of a lesson title, so the existing mnemonic rule checks them | Single acronyms, or one long phrase for all nine diagram lessons | ⏳ pending |
| CD-003 | 2026-10-20 | 008 | Picker reachability for non-pattern modules | "Every lesson is reachable" stays required for `pattern` modules only; other modules need every `pick` to be a valid lesson of the module, and reachability is not checked | A "which diagram?" question has no honest answer pointing at *Multiplicity* or the interview lesson; forcing them in would make the picker worse. The pattern picker keeps its completeness guarantee | Keep the rule everywhere and let class answers pick two lessons | ⏳ pending |
| CD-004 | 2026-10-20 | 008 | Where the state-diagram Run control appears | On every `stateDiagram-v2` in the app (GoF State lesson, Auth case study, UML) | Class and sequence diagrams are already interactive app-wide; one rule for all diagrams of a kind is simpler to learn and to test | UML module only | ⏳ pending |
