## [ERR-20260428-001] powershell-set-content-encoding

**Logged**: 2026-04-28T15:32:01+08:00
**Priority**: medium
**Status**: pending
**Area**: docs

### Summary
Using PowerShell `Set-Content` on `TASK.md` changed the file encoding so `apply_patch` could not read it as UTF-8.

### Error
```text
apply_patch verification failed: Failed to read file to update C:\Work\github\MiniNotifier\TASK.md: invalid utf-8 sequence
```

### Context
- Attempted to update task state with a PowerShell text replacement.
- The repository instructions require using `apply_patch` for manual code edits.
- The file had to be converted back to UTF-8 before patching could continue.

### Suggested Fix
Use `apply_patch` for tracked text edits. If PowerShell is unavoidable for recovery, explicitly restore UTF-8 and immediately verify with `git diff --check`.

### Metadata
- Reproducible: yes
- Related Files: TASK.md

---

## [ERR-20260428-002] search-tool-fallback

**Logged**: 2026-04-28T17:28:00+08:00
**Priority**: low
**Status**: resolved
**Area**: infra

### Summary
The local shell environment did not have `rg`, so repository search had to fall back to PowerShell `Get-ChildItem` and `Select-String`.

### Error
```text
rg : The term 'rg' is not recognized as the name of a cmdlet, function, script file, or operable program.
```

### Context
- Attempted to search reminder popup references while optimizing popup performance.
- A follow-up PowerShell search also failed when a non-existent `tests` path was included.

### Suggested Fix
When `rg` is unavailable, use `Get-ChildItem` only against existing source paths, or check paths before including optional test directories.

### Metadata
- Reproducible: yes
- Related Files: src/MiniNotifier/Services/Implementations/ReminderPreviewService.cs

### Resolution
- **Resolved**: 2026-04-28T17:28:00+08:00
- **Notes**: Used PowerShell search against the existing `src/MiniNotifier` path.

---
