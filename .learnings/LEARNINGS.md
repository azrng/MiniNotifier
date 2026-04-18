## [LRN-20260418-001] correction

**Logged**: 2026-04-18T00:00:00Z
**Priority**: high
**Status**: pending
**Area**: infra

### Summary
在现代前端/Tauri 依赖场景下，应先用 `nvm` 切到受支持的 Node 版本，再安装依赖和执行构建。

### Details
本次迁移任务中直接在 `Node 14.21.3` 环境执行了 `npm install`。虽然最终前端构建通过，但安装过程已经出现多个 engine warning，说明环境基线偏低。对于 Vite、Tauri 2 CLI、React Hook Form 等现代工具链，更稳妥的流程应该是先检查 Node 版本，若低于项目依赖支持范围，则先执行 `nvm use <受支持版本>`，必要时重装依赖。

### Suggested Action
后续在本仓库执行 Node 相关安装或构建前，先检查当前 `node -v`，若仍是旧版本则切到 Node 20 LTS，再清理并重装依赖。

### Metadata
- Source: user_feedback
- Related Files: package.json
- Tags: node, nvm, tauri, vite

---
