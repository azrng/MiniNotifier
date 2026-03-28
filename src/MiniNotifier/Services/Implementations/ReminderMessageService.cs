using MiniNotifier.Models;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Services.Implementations;

public sealed class ReminderMessageService : IReminderMessageService
{
    private static readonly ReminderMessage[] MorningMessages =
    [
        new("晨间补水", "早上先润一下嗓子", "今天的第一份状态值，从一口水开始补满。"),
        new("起床续航", "别让嘴唇先进入省电模式", "先喝两口，再去继续推进今天的任务。"),
        new("清醒模式", "大脑已开机，水杯也该上线", "晨间补水能让整个人更快进入顺手节奏。"),
        new("晨光补给", "你的水杯在等一个帅气动作", "抬手、喝水、继续发光，流程就这么简单。"),
        new("轻量唤醒", "别只叫醒电脑，也叫醒身体", "距离上次补水已经有一会儿了，来一口刚刚好。"),
        new("开机别干跑", "CPU 热了，水分也得跟上", "先补点水，今天的工作体验会更丝滑。"),
        new("晨间小任务", "请领取今日第一口水", "这个任务没有难度，完成后舒适度直接加分。"),
        new("精神加成", "喝口水，顺手把状态条往上拉", "你已经连续专注了一阵子，补水很值。")
    ];

    private static readonly ReminderMessage[] NoonMessages =
    [
        new("午间续杯", "别让自己被干饭和开会夹击", "先喝口水，下午的续航会更稳一点。"),
        new("午后前置准备", "吃饭前后都别忘了补水", "今天不是在赶进度，就是在去赶进度的路上，先润一下。"),
        new("中场补给", "给身体来个温和补蓝", "这口水喝下去，下午不容易变成低电量模式。"),
        new("午休友好提醒", "你的水杯正在申请被翻牌", "动作不用大，抬手喝两口就行。"),
        new("中午也要稳", "人可以忙，水杯不能失联", "补一点水，下午说话和思路都会更舒服。"),
        new("补水插播", "这不是广告，这是续航提示", "今天的节奏不慢，更要给自己一点水分支持。"),
        new("状态补丁", "请为午间版本安装补水更新", "安装时间很短，体验提升很实在。"),
        new("喝一口再战", "先别急着下一项", "让嘴巴和脑袋都润一润，效率反而更高。")
    ];

    private static readonly ReminderMessage[] AfternoonMessages =
    [
        new("下午回蓝", "别让自己在三点后变成沙漠模式", "你已经专注了 {0} 分钟，喝两口水，把状态拉回来。"),
        new("工位补水", "键盘很忙，你也别忘了水杯", "现在补一下，能有效避免下午后半程发干发木。"),
        new("嘴唇保卫战", "今天不允许嘴巴开裂式上班", "来点水分，继续保持丝滑输出。"),
        new("反摸鱼提示", "喝水不算摸鱼，这是高质量维护", "花几秒补水，远比后面状态掉线划算。"),
        new("下午稳住", "你不是没电，只是该补水了", "水一到位，脑袋和心情通常都会更顺。"),
        new("轻松一口", "先喝水，再继续当效率选手", "这一口很短，但对下午的体验很友好。"),
        new("工作流优化", "建议插入一个补水节点", "这个节点几乎零成本，却很能提升舒适度。"),
        new("办公室隐藏任务", "请在同事发现前优雅地喝口水", "完成后你的状态条会偷偷上涨。")
    ];

    private static readonly ReminderMessage[] EveningMessages =
    [
        new("傍晚续航", "天快黑了，状态别先黑屏", "来一口水，给今天的后半场加点柔和续航。"),
        new("收尾补给", "事情可以慢慢收，水得先补上", "喝两口再继续，收尾也能更从容。"),
        new("下班前别干巴", "无论还在忙还是准备撤退", "补一下水，让今天以更舒服的状态结束。"),
        new("傍晚小回血", "不是困，是身体在提醒你补水", "这一口下去，人通常会顺一点。"),
        new("夜幕预备", "晚上也值得拥有好状态", "先补水，再决定是继续冲还是准备休息。"),
        new("晚间柔和提醒", "这条提醒不吵，只想请你喝口水", "你已经努力很久了，补水这件事别落下。"),
        new("续一杯状态", "水杯此刻很需要一点存在感", "给自己一点轻松动作，顺手补水就好。"),
        new("舒服收工", "喝口水，别让身体陪你硬抗到最后", "轻轻一口，今天的节奏会更完整。")
    ];

    private static readonly ReminderMessage[] LateNightMessages =
    [
        new("深夜巡航", "还没休息的话，先补点水", "熬夜不是鼓励，只是提醒你别把自己晾干了。"),
        new("夜猫子补给", "代码和文档都能再等等，水先到位", "嘴巴干的时候，思路通常也没那么丝滑。"),
        new("安静提醒", "这是一条低打扰补水消息", "轻轻喝两口，继续做事也会更舒服。"),
        new("深夜别硬撑", "今晚可以忙，但别干着忙", "哪怕只喝几口，也比完全忘掉强。"),
        new("夜间续命", "传奇操作之前，先做基础补水", "英雄也得补水，何况还在熬的你。"),
        new("补水不打烊", "杯子可能不说话，但它一直在等你", "深夜状态更容易发干，来一口刚刚好。"),
        new("安静回蓝", "现在最适合低调喝水", "不折腾、不煽情，补点水继续保持舒服。"),
        new("还在线的话", "请给身体发一份补水补丁", "这个补丁安装很快，副作用只有更顺。")
    ];

    private static readonly ReminderMessage[] PausedPreviewMessages =
    [
        new("预览提醒", "虽然你暂停了，但水杯没记仇", "这次只是预览弹窗，真正提醒仍然处于暂停状态。"),
        new("暂停模式预览", "系统没有催你，只是演示一下", "放心看效果就行，不过顺手喝一口也不亏。"),
        new("小窗演示", "现在是预览时间，不是强制任务", "提醒暂停中，这条消息只是出来打个招呼。"),
        new("静音预览", "它只是轻轻出现一下", "虽然当前暂停提醒，但补水这件事仍值得被温柔记起。"),
        new("界面预演", "先看效果，想喝也可以顺手喝", "暂停不代表不补水，只是不主动打扰你。")
    ];

    public ReminderMessage Create(HydrationSettingsDto settings, DateTimeOffset now)
    {
        if (settings.IsPaused)
        {
            return Pick(PausedPreviewMessages);
        }

        var baseMessage = Pick(GetPool(now));
        return baseMessage with
        {
            Message = string.Format(baseMessage.Message, settings.ReminderIntervalMinutes)
        };
    }

    private static ReminderMessage[] GetPool(DateTimeOffset now)
    {
        var hour = now.Hour;

        return hour switch
        {
            >= 5 and < 10 => MorningMessages,
            >= 10 and < 13 => NoonMessages,
            >= 13 and < 18 => AfternoonMessages,
            >= 18 and < 22 => EveningMessages,
            _ => LateNightMessages
        };
    }

    private static ReminderMessage Pick(IReadOnlyList<ReminderMessage> pool)
    {
        return pool[Random.Shared.Next(pool.Count)];
    }
}
