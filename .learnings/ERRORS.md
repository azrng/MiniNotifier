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

## [ERR-20260418-002] git-add-removed-directory-pathspec

**Logged**: 2026-04-18T00:00:00Z
**Priority**: low
**Status**: pending
**Area**: infra

### Summary
对已按文件删除记录过的目录再次执行 `git add -u <dir>`，会出现 pathspec 不匹配错误。

### Error
```text
error: pathspec 'src-tauri/gen' did not match any file(s) known to git
error: pathspec 'src-tauri/target' did not match any file(s) known to git
```

### Context
- Command/operation attempted: 清理 Tauri 生成产物后，对目录再次执行 `git add -u`
- Input or parameters used: `git add -u src-tauri/gen src-tauri/target`
- Environment details if relevant: Windows, Git

### Suggested Fix
删除目录内容后，优先用 `git add -u` 或 `git add -A` 处理整个工作区，不要再对已经不存在的目录路径重复点名。

### Metadata
- Reproducible: yes
- Related Files: .gitignore

---
