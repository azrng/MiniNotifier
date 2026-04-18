# 2026-04-18 Avalonia CI 发布链路切换

## 本次目标

- 将 GitHub Actions 的发布链路从旧 WPF 工程切换到 Avalonia `win-x64 Native AOT` profile。

## 核心改动

- 更新 `.github/workflows/build.yml`，将 `PROJECT_PATH` 切换到 `src/MiniNotifier.Avalonia/MiniNotifier.Avalonia.csproj`。
- 工作流发布阶段改为复用 `NativeAot-win-x64` profile，不再在 CI 中重复拼接发布参数。
- 移除旧的 `win-x86` 与 `framework-dependent` 发布产物，统一收敛为 `MiniNotifier-win-x64-aot.zip`。
- 同步更新设计文档与 `TASK.md`，记录 Avalonia 发布链路已开始接管 CI。

## 修改文件

- `.github/workflows/build.yml`
- `doc/design/设计文档.md`
- `TASK.md`

## 校验情况

- 已执行 `dotnet publish src/MiniNotifier.Avalonia/MiniNotifier.Avalonia.csproj /p:PublishProfile=NativeAot-win-x64 -c Release`
- 已按工作流等价步骤将 `publish/win-x64-aot` 压缩为 ZIP，本地产物校验文件为 `artifacts/MiniNotifier-win-x64-aot-ci-check.zip`
- 未执行真实 GitHub Actions 远端工作流；当前仅完成与 CI 等价的本地发布验证

## 风险或遗留项

- 远端 GitHub Actions 尚未实际运行，首次推送后仍需检查 Artifact 命名、ZIP 产物和 Release 上传结果。
- Native AOT 发布仍会出现第三方依赖 `MicroCom.Runtime` 的 `IL3053` 告警，本次切换不消除该告警。
