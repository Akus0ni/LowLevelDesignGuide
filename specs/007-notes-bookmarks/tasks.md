# Spec 007: Tasks

- [x] T1 Domain: `Note`, `Bookmark`, `BookmarkKind`, `NotesSnapshot`, `NoteRules` (`Newer`, `Merge`), `NoteLimits`, `TagParser` + shared vector `tests/fixtures/notes/tags.json`, `INotesStore`; tests
- [x] T2 Store contract suite `NotesStoreContract` + in-memory store
- [x] T3 Device: `V6Notes`, `SqliteNotesStore`, `DeviceNotes` (adopt); contract + V5 → V6 upgrade + "content reload keeps notes" tests
- [x] T4 API: rows, migration `AddNotes`, `EfNotesStore` (contract tests), use cases, contracts, `NotesWire`, endpoints; integration tests (auth, isolation, adopt, limits)
- [x] T5 App: `NotesService`, `ExportService` (share sheet), bridge methods, adoption after sign-in
- [x] T6 Web data: `DataSource` notes methods, `BridgeDataSource`, `HttpDataSource` (API / guest localStorage / adopt / batch split / download), fakes; tests
- [ ] T7 Web logic: `tags.ts` (vector), `noteRecords.ts`, `starters.ts`, `notesQuery.ts`, `exportNotes.ts`, `writeBehind.ts` (moved out of `ProgressStore`), `notesStore.ts`, `NotesProvider`, hooks; tests
- [ ] T8 Reader: `SectionAnnotations` context, `h2` with `NoteMarks` + `SectionActions`, outline marks, lesson header (Notes · n, bookmark), `?note=` deep link
- [ ] T9 Notes sheet + editor: `NotesSheet`, `NoteEditor` (autosave, Edit / Preview, starters, delete + undo), `NotePreview` ("note" mode)
- [ ] T10 Revise: cheat sheet bookmark toggle, `YourNotes`
- [ ] T11 Notes tab: Notes / Bookmarks switch, grouped lists, tag + text filters, removed lessons, empty states, export
- [ ] T12 Visual: `--tone-note` (candidates → pick), «my note» card, marks, motion, reduced motion
- [ ] T13 UI pass with the skills: screenshot critique (phone light / dark, desktop) + `break-ui` worst case; fixes
- [ ] T14 Playwright: notes flows (A1, A3, A4, A5, A8, A9, A10, A15) on phone + desktop
- [ ] T15 Manual run: Windows + Android (A1, A2, A10, A11, A14: restart, airplane mode, share sheet, guest adoption, V5 → V6 upgrade); screenshots
- [ ] T16 Docs: PRD (R18, R102–R108, §10 `Note` / `Bookmark`, version), `docs/plan.md`, README if needed, plan §5 outcomes, progress log
- [ ] T17 PR with the acceptance-criteria table
