# 2026-04-18 Avalonia AOT 收口

## 本次目标

- 为 Avalonia 主工程补齐更稳妥的 Native AOT 保留配置，并重新验证发布链路。

## 核心改动

- 在 `src/MiniNotifier.Avalonia/` 新增 `Roots.xml`，使用 `TrimmerRootDescriptor` 保留 Avalonia 入口、窗口、ViewModel 与 JSON 序列化上下文。
- 更新 `src/MiniNotifier.Avalonia/MiniNotifier.Avalonia.csproj`，将 `Roots.xml` 接入发布流程。
- 在 `doc/design/设计文档.md` 补充 Avalonia 发布链路的 AOT 保留策略说明。
- 更新 `TASK.md` 记录本次 `T004` 的发布链路进展。

## 修改文件

- `src/MiniNotifier.Avalonia/MiniNotifier.Avalonia.csproj`
- `src/MiniNotifier.Avalonia/Roots.xml`
- `doc/design/设计文档.md`
- `TASK.md`

## 校验情况

- 已执行 `dotnet build src/MiniNotifier.Avalonia/MiniNotifier.Avalonia.csproj -c Release`
- 已执行 `dotnet publish src/MiniNotifier.Avalonia/MiniNotifier.Avalonia.csproj -c Release -r win-x64`
- 已对发布后的 `MiniNotifier.Avalonia.exe` 做 5 秒短时启动验证，进程可正常拉起

## 风险或遗留项

- 当前发布仍会出现依赖链 `MicroCom.Runtime` 的 `IL3053` 警告，属于第三方库侧的 AOT 分析告警，暂未在本次改动中消除。
- 目前只对发布链路做收口，未新增自动化测试覆盖 AOT 运行时交互场景。
