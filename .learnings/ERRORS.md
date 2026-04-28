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
