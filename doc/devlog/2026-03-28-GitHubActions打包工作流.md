# 2026-03-28 GitHub Actions 打包工作流

## 本次目标
- 参考 `DbTools` 仓库中的 `build.yml`，为 MiniNotifier 接入可直接打包发布的 GitHub Actions 工作流

## 核心改动
- 新增 `.github/workflows/build.yml`
- 保留 `push / pull_request / workflow_dispatch` 触发方式
- 接入 Release 构建、`win-x64` 与 `win-x86` 双架构发布、ZIP 打包、Artifact 上传与主分支自动 Release
- 发布参数与当前项目保持一致，使用单文件、自包含、原生库内嵌、压缩和关闭裁剪

## 修改文件
- `.github/workflows/build.yml`
- `TASK.md`

## 校验情况
- 执行 `dotnet publish` 验证 `win-x64` 打包成功，输出单文件 `MiniNotifier.exe`
- 执行 `dotnet publish` 验证 `win-x86` 打包成功，输出单文件 `MiniNotifier.exe`

## 风险或遗留项
- 当前已完成本地发布命令验证，但未在 GitHub 远端实际跑 Actions；首次推送后仍建议检查一次 Actions 日志与 Release 产物
