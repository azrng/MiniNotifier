# 2026-04-18 Avalonia CI 产物校验补齐

## 本次目标

- 为 Avalonia GitHub Actions 发布链路补齐基础产物校验，尽早发现路径或打包配置错误。

## 核心改动

- 在 `.github/workflows/build.yml` 中新增发布产物校验步骤，要求 `MiniNotifier.Avalonia.exe` 必须出现在 `win-x64-aot` 发布目录。
- 在 ZIP 打包后新增压缩包存在性校验，要求 `MiniNotifier-win-x64-aot.zip` 成功生成。
- 同步更新 `TASK.md` 记录本次 CI 校验补齐进展。

## 修改文件

- `.github/workflows/build.yml`
- `TASK.md`

## 校验情况

- 已执行 `dotnet publish src/MiniNotifier.Avalonia/MiniNotifier.Avalonia.csproj /p:PublishProfile=NativeAot-win-x64 -c Release`
- 已在本地验证 `src/MiniNotifier.Avalonia/bin/Release/net10.0-windows10.0.19041.0/publish/win-x64-aot/MiniNotifier.Avalonia.exe` 存在
- 已在本地验证 `artifacts/MiniNotifier-win-x64-aot-ci-check.zip` 可成功生成
- 未执行真实 GitHub Actions 远端工作流；当前仅完成与新增校验步骤等价的本地验证

## 风险或遗留项

- 远端 GitHub Actions 仍未实际运行，新增校验步骤是否完全符合 GitHub Runner 环境仍需首次推送后确认。
- 当前仅校验产物存在性，尚未在 CI 中做真实启动 smoke test。
