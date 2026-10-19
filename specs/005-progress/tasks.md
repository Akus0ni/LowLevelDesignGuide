# Spec 005: Tasks

- [x] T1 Domain: `LessonProgress`, `ProgressSnapshot`, `ProgressOwner`, `ProgressRules` (`Newer`, `Merge`), limits, `IProgressStore`; tests
- [x] T2 Store contract suite + in-memory store
- [x] T3 Device: `V4Progress`, `SqliteProgressStore`, `DeviceIdentity`; contract + upgrade + "content reload keeps progress" tests
- [x] T4 API: rows, migration `AddProgress`, `EfProgressStore` (contract tests), use cases, contracts, endpoints; integration tests (auth, isolation, adopt, LWW)
- [x] T5 Content contract: `LessonSummary.SectionCount` (device SQL, dev catalog, web type); tests
- [x] T6 App: session remembers the user id, `ProgressService` (owner, adopt on sign-in), bridge methods
- [x] T7 Web data: `DataSource` progress methods, `BridgeDataSource`, `HttpDataSource` (API / guest localStorage / adopt), fakes; tests
- [x] T8 Web logic: `lessonProgress.ts`, `progressMath.ts`, `progressStore.ts` (queue + flush), `ProgressProvider`, hooks, legacy import; tests
- [x] T9 Reader: tracker through the store (ticks, position, auto done), `DoneControl`, activity day; cheat sheet "revised"
- [x] T10 UI: `StatusMark`, `ContinueCard`, Learn (hatching, multiplicity, member marks), module page, Revise (marks, dates, filter), Profile (`ProgressSummary`, `ActivityStrip`, `ResetSheet`)
- [x] T11 Motion: Done check, module-complete stamp, once-only memory, reduced motion
- [x] T12 UI pass with the skills: screenshot critique (phone light / dark, desktop) + `break-ui` worst case; fixes
- [x] T13 Playwright: progress flows (A1, A3, A7, A9, A14) on phone + desktop
- [ ] T14 Manual run: Windows + Android (A4, A5, A11: restart, airplane mode, two accounts, guest adoption, V3 → V4 upgrade); screenshots
- [ ] T15 Docs: PRD (R85+, F4 note, §3 activity-strip exception, v0.7), `docs/plan.md`, plan §5 outcomes, progress log
- [ ] T16 PR with the acceptance-criteria table
