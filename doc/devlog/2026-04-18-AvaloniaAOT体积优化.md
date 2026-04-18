# 2026-04-18 Avalonia AOT 体积优化

## 本次目标

- 为 Avalonia 的 `Native AOT` 发布档位进一步压缩产物体积。

## 核心改动

- 在 `src/MiniNotifier.Avalonia/MiniNotifier.Avalonia.csproj` 中新增 `PublishAotCompressed` 包引用，仅在 `Release` 配置下启用。
- 在 `src/MiniNotifier.Avalonia/Properties/PublishProfiles/NativeAot-win-x64.pubxml` 中启用 `OptimizationPreference=Size`。
- 在 `doc/design/设计文档.md` 中补充 AOT 压缩策略、AOT ZIP 排除 `.pdb` 的交付约定，以及 `StackTraceSupport=false` / `PublishLzmaCompressed=true` 的使用边界说明。
- 更新 `TASK.md` 记录本次 AOT 体积优化进展。

## 修改文件

- `src/MiniNotifier.Avalonia/MiniNotifier.Avalonia.csproj`
- `src/MiniNotifier.Avalonia/Properties/PublishProfiles/NativeAot-win-x64.pubxml`
- `doc/design/设计文档.md`
- `TASK.md`

## 校验情况

- 已执行 `dotnet publish src/MiniNotifier.Avalonia/MiniNotifier.Avalonia.csproj /p:PublishProfile=NativeAot-win-x64 -c Release`
- 已记录 AOT 主程序压缩后的体积变化：`MiniNotifier.Avalonia.exe` 发布后压缩到约 `7.2 MB`
- 已验证 AOT 发布目录最终不再包含 `MiniNotifier.Avalonia.pdb`
- 已对压缩后的 AOT 产物做 5 秒短时启动验证，进程可正常拉起

## 风险或遗留项

- `PublishAotCompressed` 基于 UPX，对少数杀软环境可能带来额外误报风险。
- Windows Native AOT 的符号文件复制发生在发布流程尾部，本次通过后置清理目标移除发布目录中的 `.pdb`，同时保留 ZIP 排除规则作为额外保险。
- 本次不默认启用 `StackTraceSupport=false`，保留异常排障能力；如果后续优先级转向极限压缩，可再单独评估。
