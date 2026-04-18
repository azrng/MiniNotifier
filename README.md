# MiniNotifier

MiniNotifier 是一个 Windows 托盘喝水提醒工具仓库，当前处于 **WPF 向 Tauri 渐进迁移** 阶段。

## 当前状态

- `src-tauri/` + `src-ui/`：当前迁移主线，已经补齐托盘、提醒弹窗、后台调度、开机自启动、配置持久化、提醒文案和鼠标活跃度判断，具备 `release-ready` 条件。
- `src/MiniNotifier/`：旧 WPF 实现，当前继续保留，作为并行迁移阶段的回滚窗口。
- 默认结论：**优先继续推进 Tauri 版本交付，不在仓库内自动删除或停用 WPF。**

## 目录说明

- `src-ui/`：React + TypeScript 设置页与提醒窗口前端
- `src-tauri/`：Tauri 2 + Rust 桌面壳、命令、服务与系统能力
- `src/MiniNotifier/`：旧 WPF 实现
- `doc/design/设计文档.md`：唯一设计文档
- `TASK.md`：当前活动任务面板
- `doc/devlog/`：开发记录

## 本地开发

### Tauri 主线

建议先切到 Node `24.15.0`：

```powershell
nvm use 24.15.0
npm ci
```

启动前端开发：

```powershell
npm run dev
```

启动 Tauri 开发模式：

```powershell
npm run tauri:dev
```

执行前端构建：

```powershell
npm run build
```

执行 Rust 校验：

```powershell
cargo test --manifest-path src-tauri/Cargo.toml
cargo check --manifest-path src-tauri/Cargo.toml
```

生成 Tauri 安装包：

```powershell
npx tauri build
```

### WPF 回滚链路

如需验证旧版本实现：

```powershell
dotnet build MiniNotifier.slnx
```

## CI / 发布产物

当前 `.github/workflows/build.yml` 使用固定发布目标开关控制产物类型：

- 在 `.github/workflows/build.yml` 顶部修改 `PUBLISH_TARGET`
  - `wpf`：发布 WPF ZIP 包
    - `MiniNotifier-win-x64.zip`
    - `MiniNotifier-win-x86.zip`
    - `MiniNotifier-framework-dependent.zip`
  - `tauri`：发布 Tauri 安装包
    - `src-tauri/target/release/bundle/msi/*.msi`
    - `src-tauri/target/release/bundle/nsis/*.exe`
- 当前默认值为 `tauri`，后续可按需要手动切换。

## 迁移约定

- 保留旧 WPF 功能可用性优先，不因技术偏好强制删除旧实现。
- 对外发布前，仍需单独决定是否切换默认入口，以及是否停止旧 WPF 版本交付。
- 任务状态、阶段门控和交付规范以 `AGENTS.md`、`TASK.md` 和 `doc/design/设计文档.md` 为准。
