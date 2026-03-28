namespace MiniNotifier.Models;

public sealed record MouseActivitySnapshot(
    int ClicksLastMinute,
    int ClicksLastFiveMinutes,
    WorkIntensityState WorkState
);
