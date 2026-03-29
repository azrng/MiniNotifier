using MiniNotifier.Models;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Services.Implementations;

public sealed class ReminderMessageService : IReminderMessageService
{
    private readonly IMouseActivityService _mouseActivityService;
    private readonly object _syncRoot = new();
    private string? _lastSignature;

    private static readonly string[] PraiseFragments =
    [
        "你今天这状态很能打，再补口水会更稳。",
        "你现在这份专注感，很配一口体面的补水。",
        "像你这样认真推进事情的人，连喝水都显得很利落。",
        "你这会儿的节奏挺漂亮，补水一下会更丝滑。",
        "能把日程撑起来的人，通常也配拥有一口舒服的水。",
        "你现在这股稳定输出的劲头，补水一下会更顺。",
        "你今天已经够靠谱了，再让身体跟上节奏会更完整。",
        "这种在线状态很难得，顺手喝口水会更稳。",
        "你现在这股认真劲很加分，补点水会更显从容。",
        "说真的，你这会儿很适合来一口高级感补水。",
        "你这会儿的状态很争气，喝口水会更体面。",
        "你今天这份执行力，很值得配一口舒服的水。"
    ];

    private static readonly string[] PlayfulFragments =
    [
        "水杯已经在旁边等到快要出道了。",
        "再不喝，它都快准备开个人发布会了。",
        "这不是催你，这是水杯在礼貌刷存在感。",
        "今天的嘴唇不想参加沙漠主题活动。",
        "工位空气已经开始期待你抬手这一下。",
        "再拖一会儿，水杯都要怀疑自己失宠了。",
        "这口水现在喝，体面值会直接上涨。",
        "别让水杯一直在旁边独自营业，你也参与一下。",
        "今天这次补水，主打一个轻松完成。",
        "这一步很简单，但气质很加分。",
        "你的水杯今天戏份不多，就等你给它一个镜头。",
        "抬手喝两口这件事，和你的节奏意外地很配。"
    ];

    private static readonly string[] WorkdayFragments =
    [
        "工作日的节奏本来就紧，补水这件事更该被排进主线。",
        "工作日容易一路忙到底，这口水就是你的中途保养。",
        "今天的安排不一定轻松，但补水可以先轻松完成。",
        "工作日模式里，越忙越要给身体留一点缓冲。",
        "别让工作流跑得太满，也给自己留一点喝水的空当。",
        "认真做事很重要，顺手补水同样算专业操作。",
        "工作日里能记得喝水的人，通常状态都更稳。",
        "这会儿补一点，后面继续推进事情会舒服不少。"
    ];

    private static readonly string[] WeekendFragments =
    [
        "周末可以松弛，但补水这件事别一起放假。",
        "周末节奏更自由，刚好适合优雅喝一口。",
        "既然今天不用那么赶，这口水更值得慢慢喝。",
        "周末主打一个舒服，补水当然也得跟上。",
        "休息日的补水，应该带一点轻松感和仪式感。",
        "周末不是只有快乐，还得配一点水分支持。",
        "今天的任务可能不多，但舒服这件事要做到位。",
        "周末就该轻轻松松把水喝掉，不必等提醒太久。"
    ];

    private static readonly MessageCatalog MorningCatalog = new(
        Titles:
        [
            "晨间补水",
            "早安续航",
            "清晨续杯",
            "早起补给",
            "晨光续杯",
            "晨间提醒",
            "状态开机",
            "水杯打卡",
            "精神加成",
            "晨间小任务"
        ],
        Headlines:
        [
            "早上先让嗓子别走旱路",
            "今天第一口水值得认真对待",
            "电脑开机了，你也顺手开个补水模式",
            "别只点亮屏幕，也点亮水杯",
            "嘴巴别一早就进入节能状态",
            "新的一天先给自己一点水分支持",
            "清晨这口水，属于低成本高收益",
            "水杯已经比闹钟更有耐心了",
            "先润一下，再继续今天的主线任务",
            "晨间状态值，建议先靠喝水拉满"
        ],
        Openings:
        [
            "今天的第一段状态条，建议先靠喝水补满。",
            "早上的你很适合来一口温和启动。",
            "这会儿补水，整个人会更快进入顺手节奏。",
            "清晨补一点，往后说话和思路都会更舒服。",
            "刚开工别干跑，身体也要收到开机补丁。",
            "先喝两口，再去处理今天的安排，会更稳。",
            "补水这件事现在做，体验通常最好。",
            "不用大动作，抬手喝一口就已经很赚了。",
            "今天别让嘴唇先上班，先给它一点支持。",
            "先续一口水，今天的开局会更丝滑。"
        ],
        Middles:
        [
            "你当前的提醒节奏是每 {0} 分钟一次，这一轮先优雅完成。",
            "就把这次当成今天第一个轻量级成就。",
            "喝完再继续推进任务，效率不会掉，舒适度还会上涨。",
            "这不是催促，是给你发一张晨间补给券。",
            "动作越简单越容易坚持，拿起杯子就是胜利。",
            "今天的水杯已经进入待命状态，只差你点确认。",
            "别担心流程复杂，这一步只需要抬手、喝水、放下。",
            "早上的好状态，往往就是靠这些小动作慢慢堆起来的。",
            "你现在补这口水，后面通常会感谢现在的自己。",
            "把身体带上节奏，今天做事会更轻盈一点。"
        ],
        Closings:
        [
            "喝完这口，我们再继续发光。",
            "这一波补水完成后，今天的起手会更漂亮。",
            "现在喝，刚刚好。",
            "先润一下，今天会更顺。",
            "完成它，你的晨间体验就已经加分了。",
            "这个动作很小，但反馈往往很大。",
            "喝两口，马上继续保持在线状态。",
            "这不是打断，这是体贴的开场白。",
            "补完这口，出发。",
            "今天先从一口舒服的水开始。"
        ]);

    private static readonly MessageCatalog NoonCatalog = new(
        Titles:
        [
            "午间续杯",
            "中场补给",
            "中午续杯",
            "午休补水",
            "午间提醒",
            "午后前置",
            "水杯播报",
            "午间补丁",
            "续航插播",
            "午间续航"
        ],
        Headlines:
        [
            "中午别只想着吃，也别忘了喝",
            "午间这口水，能给下午省很多劲",
            "你的水杯正在午间排队等翻牌",
            "饭点附近最适合顺手补一口",
            "别让下午在缺水模式里开场",
            "中午补水，是给下半场做准备",
            "先润一下，下午说话和思路更顺",
            "这会儿喝口水，对后半天很友好",
            "中场休息可以短，补水别省",
            "午间续一杯，下午更稳一点"
        ],
        Openings:
        [
            "如果上午已经忙了一轮，这会儿更适合补点水。",
            "中午这口水，常常决定你下午开局够不够顺。",
            "人可以忙，水杯别失联，这会儿正适合续一下。",
            "先来一口温和补给，再决定下一步是开会还是干饭。",
            "别让身体在下午开始前就进入干巴模式。",
            "现在补一下，后面通常能少一点发干发木。",
            "这不是插播广告，这是午间续航提醒。",
            "饭前饭后都行，重点是别把补水这件事漏掉。",
            "先润一口，下午的节奏会更容易稳住。",
            "这一步很小，但对后半天的体验挺重要。"
        ],
        Middles:
        [
            "当前的提醒节奏是每 {0} 分钟一次，刚好轮到你和水杯见一面。",
            "现在喝两口，下午更不容易出现低电量表情包。",
            "把它当成一次中场维护，顺手就能完成。",
            "别等嘴巴发出警报，先主动补一点更舒服。",
            "这件事花不了多久，但往往能换来更稳定的输出。",
            "中午这会儿补水，通常属于收益很高的操作。",
            "今天的工作可能很满，至少别让水分也空仓。",
            "水杯没有催你加班，它只是想参与一下你的中场休息。",
            "先来一口，下午再继续做效率担当。",
            "补完水再战，气场和状态都会稳一点。"
        ],
        Closings:
        [
            "喝完这口，再继续安排下午。",
            "就现在，刚刚合适。",
            "给自己一点水分支持，后面会更舒服。",
            "这不是分心，这是在给状态续航。",
            "喝完再忙，体验更好。",
            "先润一下，下午更顺滑。",
            "水到位，状态通常也会跟上。",
            "把这件小事做好，下半场更稳。",
            "轻轻一口，后面更从容。",
            "完成这一口，再继续发力。"
        ]);

    private static readonly MessageCatalog AfternoonCatalog = new(
        Titles:
        [
            "下午续杯",
            "工位补水",
            "三点续航",
            "下午提醒",
            "状态补丁",
            "水分回充",
            "补水插播",
            "节奏缓冲",
            "效率维护",
            "工位续杯"
        ],
        Headlines:
        [
            "别让自己在下午变成沙漠模式",
            "三点之后，水杯的发言权会明显提高",
            "你不是没电，你只是该补水了",
            "键盘很忙，但水杯也不能被冷落",
            "这会儿喝一口，下午后半程会舒服很多",
            "别等嘴巴开裂式上班，现在就补一点",
            "下午最值得做的轻量动作之一，就是喝水",
            "你的状态条已经不错，再加一口会更稳",
            "工位续航这一步，就交给手边这杯水",
            "先补水，再继续把事情稳稳往前推"
        ],
        Openings:
        [
            "你已经专注了差不多 {0} 分钟，这会儿喝水很合适。",
            "下午最容易把补水忘掉，所以这条提醒来得很及时。",
            "现在补一点，能有效降低后面干巴巴工作的概率。",
            "别让下午的自己靠意志力硬扛，水分支持也要跟上。",
            "先把身体照顾到位，做事通常会更轻松。",
            "这会儿抬手喝两口，性价比很高。",
            "不需要离开工位太久，先把这一口完成就很好。",
            "下午的好状态，很多时候就是靠这些小动作托住的。",
            "喝水不耽误事，反而经常能让后面更顺。",
            "就现在，正适合插入一个补水节点。"
        ],
        Middles:
        [
            "你可以把它理解成一次给状态回稳的小维护。",
            "这不是中断流程，这是给流程做保养。",
            "如果继续忙下去，这口水只会显得越来越值。",
            "办公室隐藏任务更新了，目标是优雅地喝两口。",
            "今天不允许嘴巴和脑袋一起发干。",
            "先续一点水，后面的专注感会更稳当。",
            "这一口下去，通常能把下午的体验往回拉一截。",
            "它看起来普通，但往往比你想象中更有用。",
            "别让自己在后半程只剩下硬撑模式。",
            "这一步不费事，但对状态很友好。"
        ],
        Closings:
        [
            "喝完这口，再继续漂亮输出。",
            "这不是分神，是在帮状态回稳。",
            "补完再战，体验更好。",
            "现在来一口，刚刚好。",
            "水杯就在手边，顺手喝两口吧。",
            "喝两口，继续稳住。",
            "这一轮补水，建议现在完成。",
            "完成它，下午会更顺滑。",
            "轻轻一口，继续在线。",
            "现在补上，后面会轻松很多。"
        ]);

    private static readonly MessageCatalog EveningCatalog = new(
        Titles:
        [
            "傍晚续航",
            "收尾补给",
            "晚间续杯",
            "傍晚提醒",
            "收工前续杯",
            "夜幕预备",
            "晚间补丁",
            "舒服收工",
            "晚间续航",
            "下班前补水"
        ],
        Headlines:
        [
            "天快黑了，状态别先黑屏",
            "收尾阶段也值得拥有一口舒服的水",
            "不管还在忙还是准备撤退，先润一下",
            "傍晚这口水，能让今天收得更从容",
            "不是困，是身体在礼貌提醒你补水",
            "先续一口，再决定继续冲还是准备收工",
            "今天后半场，也该给水杯一点存在感",
            "别把一天收尾收成干巴巴模式",
            "今晚的好状态，也从这口水开始算",
            "下班前补一口，体验通常会更完整"
        ],
        Openings:
        [
            "到了傍晚，身体对水分支持会更有感知。",
            "今天忙到现在，先给自己一点柔和续航。",
            "收尾阶段喝口水，会让整个人松快不少。",
            "这会儿补一点，晚上不容易一直觉得发干。",
            "不管你还在推进任务，还是准备收工，这口都值得。",
            "先润一润，再继续做最后一段安排。",
            "晚一点也没关系，重点是别让身体被晾着。",
            "这不是催促，是给今天一个更舒服的收尾。",
            "来一口温和补给，傍晚节奏会更顺。",
            "先照顾一下状态，再决定下一步。"
        ],
        Middles:
        [
            "当前提醒间隔是 {0} 分钟一次，这轮刚好轮到水杯露脸。",
            "这一口很小，但经常能显著改善收尾体验。",
            "事情可以慢慢收，水分先补上通常更明智。",
            "别让今天最后几小时靠硬扛完成。",
            "喝点水，讲话和思路都会更柔顺一点。",
            "这次补水，属于今天后半场的体贴操作。",
            "你已经挺努力了，给身体一点支持也很应该。",
            "这不是打断节奏，而是在帮你把节奏托住。",
            "现在喝两口，晚上的状态通常更舒服。",
            "水一到位，心情和脑袋常常都会更顺一点。"
        ],
        Closings:
        [
            "喝完这口，今天就能更体面地收尾。",
            "先润一下，再继续安排。",
            "收工前这口水，值得。",
            "轻轻一口，状态更圆满。",
            "现在补，后面更从容。",
            "今天的后半场，别让水杯缺席。",
            "完成这一步，再继续你的傍晚剧情。",
            "喝完就好，不折腾。",
            "这一口到位，晚点会更舒服。",
            "先续杯，再决定下一步。"
        ]);

    private static readonly MessageCatalog LateNightCatalog = new(
        Titles:
        [
            "深夜巡航",
            "夜猫子补给",
            "夜间续杯",
            "深夜提醒",
            "安静补水",
            "深夜续航",
            "夜间补丁",
            "低打扰续杯",
            "夜航补给",
            "安静续航"
        ],
        Headlines:
        [
            "还没休息的话，先给自己补点水",
            "深夜可以忙，但别干着忙",
            "你还在线，水杯也想申请上线",
            "这是一条很安静但很认真的补水提醒",
            "别让深夜状态只靠意志力维持",
            "先续口水，再决定今晚还冲多久",
            "夜里最适合低调完成一次补水",
            "不吵你，只想提醒你别忘了水",
            "深夜模式也别把补水功能关掉",
            "水杯虽然不说话，但它确实在等你"
        ],
        Openings:
        [
            "如果你现在还在忙，补一点水会比继续硬扛更划算。",
            "深夜时段更容易忘记补水，所以这条提醒尽量说得温柔一点。",
            "这会儿来两口，嘴巴和脑袋通常都会舒服很多。",
            "深夜不鼓励熬，但既然你还在线，至少别把自己晾干了。",
            "先照顾一下身体，再决定还要不要继续推进。",
            "夜里补水不需要隆重，轻轻一口就很好。",
            "安静地喝两口，往往就能让状态顺回来一点。",
            "这时候的补水，通常比白天更容易被身体感谢。",
            "低打扰、不煽情，只提醒你现在适合喝口水。",
            "先润一下，深夜流程会柔和不少。"
        ],
        Middles:
        [
            "当前提醒节奏是每 {0} 分钟一次，水杯准时来打卡了。",
            "深夜也要补水，别让嘴唇先进入加班模式。",
            "哪怕只喝几口，也比彻底忘掉强很多。",
            "这一步不会打断你太久，但对状态很友好。",
            "先把水分补上，再继续今晚的支线任务。",
            "深夜状态最怕干巴巴，这口水就很关键。",
            "你可以把它当成一份安静的夜间补丁。",
            "这会儿喝一点，通常能明显降低深夜发干感。",
            "继续忙也行，先让水杯别继续守空城。",
            "现在喝口水，比后面口干舌燥时再想起来舒服得多。"
        ],
        Closings:
        [
            "轻轻一口，继续保持舒服。",
            "先补水，再决定今晚的下一步。",
            "不急，喝完再继续。",
            "深夜这口，往往特别值。",
            "现在来一口，刚刚好。",
            "低调完成，高级舒服。",
            "喝两口，状态回一点。",
            "别折腾，先润一下。",
            "完成它，夜里会更顺。",
            "这一口不求热血，只求舒服。"
        ]);

    private static readonly MessageCatalog PausedPreviewCatalog = new(
        Titles:
        [
            "预览提醒",
            "暂停模式预览",
            "小窗演示",
            "静音预览",
            "界面预演",
            "弹窗彩排",
            "演示提醒",
            "预览模式",
            "提醒试播",
            "小范围试运行"
        ],
        Headlines:
        [
            "虽然你暂停了，但水杯没记仇",
            "系统没有催你，这次只是演示一下",
            "现在是预览时间，不是强制任务",
            "这条只是出来打个招呼",
            "提醒暂停中，但界面还是想秀一下自己",
            "这是一条低压力的预览弹窗",
            "放心，这次不是正式催水",
            "只是看看效果，水杯先友好亮个相",
            "预览归预览，顺手喝一口也不亏",
            "当前属于演示场次，请轻松围观"
        ],
        Openings:
        [
            "当前提醒处于暂停状态，所以这次只是一个温柔的界面演示。",
            "你现在看到的是预览版本，不会改变正式提醒节奏。",
            "这次出现的主要目标，是帮你看看弹窗效果顺不顺眼。",
            "虽然系统没有正式催你，但水杯还是很想参与一下。",
            "就把它当成一次轻量试播，不用有任何压力。",
            "提醒暂停中，不过弹窗偶尔也想证明自己还在线。",
            "这条消息的气氛比正式提醒更轻松一点。",
            "你可以把它理解成一场不打扰工作的彩排。",
            "现在不要求立刻行动，纯看效果也完全没问题。",
            "这是一条友好的演示消息，不是硬核催办。"
        ],
        Middles:
        [
            "如果你顺手喝一口，那也算预览阶段的额外彩蛋。",
            "正式提醒仍然保持暂停，不会因为这次预览偷偷恢复。",
            "界面先出来打个卡，后续节奏还按你的设置走。",
            "看看样式、感受一下文案，就算完成任务了。",
            "弹窗的工作先做到这里，剩下的交给你心情决定。",
            "它只是想优雅出现一下，不会对你步步紧逼。",
            "演示期间一切从简，但轻松感必须在线。",
            "这次的存在感主要来自设计，不来自催促。",
            "预览结束后，系统仍旧会尊重你当前的暂停选择。",
            "安心围观即可，正式流程没有被改写。"
        ],
        Closings:
        [
            "想喝就喝，看看效果也完全可以。",
            "这波主打一个轻松出现。",
            "看完效果，我们就安静退场。",
            "这次只是彩排，不抢戏。",
            "围观完毕即可，感谢配合。",
            "如果顺手喝一口，那属于额外加分。",
            "演示到此，氛围感拉满就够了。",
            "这条消息的任务，就是轻轻出现一下。",
            "看完就好，不给你添乱。",
            "保持轻松，继续你的节奏。"
        ]);

    public ReminderMessageService(IMouseActivityService mouseActivityService)
    {
        _mouseActivityService = mouseActivityService;
    }

    public ReminderMessage Create(HydrationSettingsDto settings, DateTimeOffset now)
    {
        var catalog = settings.IsPaused ? PausedPreviewCatalog : GetCatalog(now);
        return BuildMessage(
            catalog,
            settings.IsPaused,
            settings.ReminderIntervalMinutes,
            now,
            _mouseActivityService.GetSnapshot()
        );
    }

    private ReminderMessage BuildMessage(
        MessageCatalog catalog,
        bool isPaused,
        int reminderIntervalMinutes,
        DateTimeOffset now,
        MouseActivitySnapshot activitySnapshot
    )
    {
        ReminderMessage message;
        string signature;

        lock (_syncRoot)
        {
            var attempts = 0;

            do
            {
                message = new ReminderMessage(
                    Pick(catalog.Titles),
                    Pick(catalog.Headlines),
                    BuildBody(catalog, isPaused, reminderIntervalMinutes, now, activitySnapshot)
                );

                signature = $"{message.Title}|{message.Headline}|{message.Message}";
                attempts++;
            }
            while (signature == _lastSignature && attempts < 6);

            _lastSignature = signature;
        }

        return message;
    }

    private static string BuildBody(
        MessageCatalog catalog,
        bool isPaused,
        int reminderIntervalMinutes,
        DateTimeOffset now,
        MouseActivitySnapshot activitySnapshot
    )
    {
        return isPaused
            ? BuildPausedBody()
            : BuildCompactBody(catalog, reminderIntervalMinutes, now, activitySnapshot);
    }

    private static string BuildCompactBody(
        MessageCatalog catalog,
        int reminderIntervalMinutes,
        DateTimeOffset now,
        MouseActivitySnapshot activitySnapshot
    )
    {
        var usePraiseAccent = Random.Shared.NextDouble() < 0.78;
        var usePlayfulAccent = Random.Shared.NextDouble() < 0.52;

        var firstSentence = Pick(
        [
            PickShortSentence(catalog.Openings, 22),
            GetIntervalContext(reminderIntervalMinutes),
            GetDayContext(now),
            GetActivityContext(activitySnapshot)
        ]);

        var secondSentence = PickDifferentSentence(
            [
                PickShortSentence(catalog.Middles, 20),
                PickShortSentence(catalog.Closings, 16),
                usePraiseAccent ? PickShortSentence(PraiseFragments, 18) : string.Empty
            ],
            firstSentence
        );

        var body = $"{firstSentence}{secondSentence}";
        var thirdSentence = PickDifferentSentence(
            [
                PickShortSentence(catalog.Closings, 14),
                usePlayfulAccent ? PickShortSentence(PlayfulFragments, 14) : string.Empty,
                usePraiseAccent ? PickShortSentence(PraiseFragments, 15) : string.Empty
            ],
            firstSentence,
            secondSentence
        );

        return body.Length + thirdSentence.Length <= 40 && Random.Shared.NextDouble() < 0.3
            ? $"{body}{thirdSentence}"
            : body;
    }

    private static string BuildPausedBody()
    {
        var firstSentence = Pick(
        [
            "这次只是预览一下提醒样式。",
            "正式提醒还按你的设置走。",
            "现在只是轻量演示，不会打乱节奏。",
            "看看效果就好，没有催办压力。",
            "这条消息只是出来轻轻露个脸。"
        ]);

        var secondSentence = Pick(
        [
            "想喝就喝，顺手看看效果也很好。",
            "围观完毕后，它会安静退场。",
            "顺手喝一口，也算额外加分。",
            "这波主打一个轻松出现。",
            "看完效果，继续你的节奏就好。"
        ]);

        return firstSentence == secondSentence ? $"{firstSentence}轻松看看就好。" : $"{firstSentence}{secondSentence}";
    }

    private static string GetDayContext(DateTimeOffset now)
    {
        var weekendOrWorkday = IsWeekend(now) ? Pick(WeekendFragments) : Pick(WorkdayFragments);
        var weekdayName = GetWeekdayNameFragment(now.DayOfWeek);

        return Pick(
        [
            NormalizeSentence(weekendOrWorkday),
            NormalizeSentence(weekdayName)
        ]);
    }

    private static string GetActivityContext(MouseActivitySnapshot snapshot)
    {
        return NormalizeSentence(snapshot.WorkState switch
        {
            WorkIntensityState.DeepFocus => Pick(
            [
                "看起来你刚刚点得不多，更像是在沉浸推进事情，专注感很在线。",
                "最近这段时间点击很少，像是在认真进入深度专注区，这状态挺漂亮。",
                "你的鼠标最近很安静，这通常意味着脑袋正在稳定输出，挺厉害的。",
                "刚才的点击节奏偏少，更像是专注读写、思考或整理内容，节奏很稳。",
                "你这会儿像在深度模式里，补水一下刚好能把这份状态托住。"
            ]),
            WorkIntensityState.SteadyFlow => Pick(
            [
                "最近的点击节奏挺稳，像是在有条不紊地推进工作，很像你的风格。",
                "你这会儿的操作频率很均匀，属于稳稳往前推的状态，挺可靠。",
                "刚才这段时间像是在正常流速处理事情，不急不躁，很有分寸。",
                "你的鼠标节奏挺顺，像是在稳定推进当前任务，这股稳定劲很加分。",
                "这类平稳工作状态最适合顺手补一口，不会打乱节奏。"
            ]),
            WorkIntensityState.ActiveHandling => Pick(
            [
                "最近这段时间点击挺密，像是在来回处理不少事项，反应很快。",
                "看起来你刚刚切换和操作都不少，明显在高频推进任务，执行力挺在线。",
                "你这会儿的点击活跃度挺在线，像是在密集处理事情，手感不错。",
                "刚才的鼠标节奏偏快，像是在多任务之间灵活穿梭，挺利落的。",
                "最近操作频率不低，这口水很适合给节奏降一点燥。"
            ]),
            _ => Pick(
            [
                "最近这阵点击非常密集，像是在高强度连轴转，火力挺足。",
                "你这会儿操作很密，像是在集中处理一串事情，节奏压得很稳。",
                "刚刚的点击密度很高，像是在处理一串连发任务，动作很利落。",
                "最近鼠标几乎没闲着，这通常意味着你正在快速切换和处理，状态很顶。",
                "你刚才节奏挺快，这口水刚好帮你缓一下。"
            ])
        });
    }

    private static MessageCatalog GetCatalog(DateTimeOffset now)
    {
        var hour = now.Hour;

        return hour switch
        {
            >= 5 and < 10 => MorningCatalog,
            >= 10 and < 13 => NoonCatalog,
            >= 13 and < 18 => AfternoonCatalog,
            >= 18 and < 22 => EveningCatalog,
            _ => LateNightCatalog
        };
    }

    private static string Pick(IReadOnlyList<string> pool)
    {
        return pool[Random.Shared.Next(pool.Count)];
    }

    private static string GetIntervalContext(int reminderIntervalMinutes)
    {
        return $"到了每 {reminderIntervalMinutes} 分钟的补水节点。";
    }

    private static string PickDifferentSentence(
        IReadOnlyList<string> pool,
        params string[] excludedSentences
    )
    {
        var candidates = pool
            .Where(sentence =>
                !string.IsNullOrWhiteSpace(sentence) &&
                excludedSentences.All(excluded => !string.Equals(sentence, excluded, StringComparison.Ordinal))
            )
            .ToList();

        if (candidates.Count > 0)
        {
            return Pick(candidates);
        }

        return pool.FirstOrDefault(sentence => !string.IsNullOrWhiteSpace(sentence)) ?? string.Empty;
    }

    private static string PickShortSentence(IReadOnlyList<string> pool, int preferredMaxLength)
    {
        var candidates = new List<string>();
        string? shortest = null;

        foreach (var item in pool)
        {
            var sentence = NormalizeSentence(item);

            if (string.IsNullOrWhiteSpace(sentence))
            {
                continue;
            }

            if (shortest is null || sentence.Length < shortest.Length)
            {
                shortest = sentence;
            }

            if (sentence.Length <= preferredMaxLength)
            {
                candidates.Add(sentence);
            }
        }

        if (candidates.Count > 0)
        {
            return Pick(candidates);
        }

        return shortest ?? string.Empty;
    }

    private static string NormalizeSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();

        if (trimmed.EndsWith('.'))
        {
            trimmed = $"{trimmed[..^1]}。";
        }

        return trimmed.EndsWith('。') || trimmed.EndsWith('！') || trimmed.EndsWith('？')
            ? trimmed
            : $"{trimmed}。";
    }

    private static bool IsWeekend(DateTimeOffset now)
    {
        return now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    private static string GetWeekdayNameFragment(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "周一先别把自己忙干了，补一口会更好开局。",
            DayOfWeek.Tuesday => "周二适合继续稳扎稳打，也适合顺手喝水。",
            DayOfWeek.Wednesday => "周三这种中场位置，补水尤其显得聪明。",
            DayOfWeek.Thursday => "周四往往容易闷头赶进度，更要记得润一下。",
            DayOfWeek.Friday => "周五的快乐在路上，水也别落下。",
            DayOfWeek.Saturday => "周六如果你在放松，这口水会让舒服感更完整。",
            _ => "周日更适合把状态调柔和一点，补水很加分。"
        };
    }

    private sealed record MessageCatalog(
        IReadOnlyList<string> Titles,
        IReadOnlyList<string> Headlines,
        IReadOnlyList<string> Openings,
        IReadOnlyList<string> Middles,
        IReadOnlyList<string> Closings
    );
}
