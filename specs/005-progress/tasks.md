# Spec 005: Tasks

- [ ] T1 Domain: `LessonProgress`, `ProgressSnapshot`, `ProgressOwner`, `ProgressRules` (`Newer`, `Merge`), limits, `IProgressStore`; tests
- [ ] T2 Store contract suite + in-memory store
- [ ] T3 Device: `V4Progress`, `SqliteProgressStore`, `DeviceIdentity`; contract + upgrade + "content reload keeps progress" tests
- [ ] T4 API: rows, migration `AddProgress`, `EfProgressStore` (contract tests), use cases, contracts, endpoints; integration tests (auth, isolation, adopt, LWW)
- [ ] T5 Content contract: `LessonSummary.SectionCount` (device SQL, dev catalog, web type); tests
- [ ] T6 App: session remembers the user id, `ProgressService` (owner, adopt on sign-in), bridge methods
- [ ] T7 Web data: `DataSource` progress methods, `BridgeDataSource`, `HttpDataSource` (API / guest localStorage / adopt), fakes; tests
- [ ] T8 Web logic: `lessonProgress.ts`, `progressMath.ts`, `progressStore.ts` (queue + flush), `ProgressProvider`, hooks, legacy import; tests
- [ ] T9 Reader: tracker through the store (ticks, position, auto done), `DoneControl`, activity day; cheat sheet "revised"
- [ ] T10 UI: `StatusMark`, `ContinueCard`, Learn (hatching, multiplicity, member marks), module page, Revise (marks, dates, filter), Profile (`ProgressSummary`, `ActivityStrip`, `ResetSheet`)
- [ ] T11 Motion: Done check, module-complete stamp, once-only memory, reduced motion
- [ ] T12 UI pass with the skills: screenshot critique (phone light / dark, desktop) + `break-ui` worst case; fixes
- [ ] T13 Playwright: progress flows (A1, A3, A7, A9, A14) on phone + desktop
- [ ] T14 Manual run: Windows + Android (A4, A5, A11: restart, airplane mode, two accounts, guest adoption, V3 → V4 upgrade); screenshots
- [ ] T15 Docs: PRD (R85+, F4 note, §3 activity-strip exception, v0.7), `docs/plan.md`, plan §5 outcomes, progress log
- [ ] T16 PR with the acceptance-criteria table
