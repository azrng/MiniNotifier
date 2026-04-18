## [ERR-20260418-001] parallel-shell-copy-race

**Logged**: 2026-04-18T00:00:00Z
**Priority**: medium
**Status**: pending
**Area**: infra

### Summary
并行执行目录创建与依赖该目录的复制命令时出现时序竞争，导致复制失败。

### Error
```text
Copy-Item : Could not find a part of the path 'C:\Work\github\MiniNotifier\src-tauri\icons\icon.ico'.
```

### Context
- Command/operation attempted: 使用 `multi_tool_use.parallel` 同时执行 `New-Item` 建目录和 `Copy-Item` 复制图标
- Input or parameters used: 复制目标位于刚创建的 `src-tauri/icons`
- Environment details if relevant: PowerShell, Windows

### Suggested Fix
当后一个命令依赖前一个命令产出的路径时，不要并行执行；先顺序完成目录创建，再执行复制或移动。

### Metadata
- Reproducible: yes
- Related Files: src-tauri/icons/icon.ico

---
