use chrono::{DateTime, Datelike, Local, Timelike};
use rand::seq::SliceRandom;

use crate::models::{
    HydrationSettingsDocument, MouseActivitySnapshot, ReminderPayload, WorkIntensityState,
};

const MORNING_TITLES: &[&str] = &["晨间补水", "早安续航", "清晨续杯", "晨光续杯", "状态开机"];
const NOON_TITLES: &[&str] = &["午间续杯", "中场补给", "午间续航", "午后前置", "水杯播报"];
const AFTERNOON_TITLES: &[&str] = &["下午续杯", "工位补水", "状态补丁", "三点续航", "节奏缓冲"];
const EVENING_TITLES: &[&str] = &["傍晚续航", "收尾补给", "晚间续杯", "舒服收工", "收工前续杯"];
const NIGHT_TITLES: &[&str] = &["深夜巡航", "夜间续杯", "安静补水", "低打扰续杯", "夜航补给"];

const MORNING_HEADLINES: &[&str] = &[
    "电脑开机了，你也顺手开个补水模式。",
    "今天第一口水值得认真对待。",
    "早上的状态条，先靠喝水补满。",
];
const NOON_HEADLINES: &[&str] = &[
    "中午别只想着吃，也别忘了喝。",
    "午间这口水，能给下午省很多劲。",
    "饭点附近最适合顺手补一口。",
];
const AFTERNOON_HEADLINES: &[&str] = &[
    "你不是没电，你只是该补水了。",
    "下午最值得做的轻量动作之一，就是喝水。",
    "别让自己在下午变成沙漠模式。",
];
const EVENING_HEADLINES: &[&str] = &[
    "收尾阶段也值得拥有一口舒服的水。",
    "不管还在忙还是准备撤退，先润一下。",
    "傍晚这口水，能让今天收得更从容。",
];
const NIGHT_HEADLINES: &[&str] = &[
    "深夜可以忙，但别干着忙。",
    "这是一条很安静但很认真的补水提醒。",
    "不吵你，只想提醒你别忘了水。",
];

const PRAISE_FRAGMENTS: &[&str] = &[
    "你今天这状态已经够能扛了，别连嘴巴也一起硬扛。",
    "你现在这股认真劲挺加分，补点水会更从容。",
    "今天已经够靠谱了，身体这边也别让它掉链子。",
];

const PLAYFUL_FRAGMENTS: &[&str] = &[
    "水杯已经在旁边站岗很久了，你多少理它一下。",
    "再不喝，它都快默认自己是桌面摆件了。",
    "抬手喝两口这件事，不会耽误你继续当大忙人。",
];

pub fn build_reminder_payload(
    document: &HydrationSettingsDocument,
    activity_snapshot: &MouseActivitySnapshot,
) -> ReminderPayload {
    let now = Local::now();
    let title_pool = select_title_pool(now);
    let headline_pool = select_headline_pool(now);
    let opening_pool = select_opening_pool(now);
    let mut rng = rand::thread_rng();

    let title = title_pool
        .choose(&mut rng)
        .copied()
        .unwrap_or("Drink Water")
        .to_string();
    let headline = headline_pool
        .choose(&mut rng)
        .copied()
        .unwrap_or("该喝水了。")
        .to_string();
    let opening = opening_pool
        .choose(&mut rng)
        .copied()
        .unwrap_or("先润一下，再继续把今天的安排往前推。");
    let interval_context = format!(
        "当前提醒节奏是每 {} 分钟一次。",
        document.reminder_interval_minutes
    );
    let activity_context = activity_fragment(activity_snapshot, &mut rng);
    let accent = [
        PRAISE_FRAGMENTS
            .choose(&mut rng)
            .copied()
            .unwrap_or_default(),
        PLAYFUL_FRAGMENTS
            .choose(&mut rng)
            .copied()
            .unwrap_or_default(),
        weekday_fragment(now),
    ]
    .choose(&mut rng)
    .copied()
    .unwrap_or("现在补一点刚刚好。");

    ReminderPayload {
        title,
        headline,
        message: format!(
            "{} {} {} {}",
            opening, interval_context, activity_context, accent
        )
        .trim()
        .to_string(),
        auto_close_seconds: document.auto_close_seconds,
    }
}

fn select_title_pool(now: DateTime<Local>) -> &'static [&'static str] {
    match now.hour() {
        5..=9 => MORNING_TITLES,
        10..=12 => NOON_TITLES,
        13..=17 => AFTERNOON_TITLES,
        18..=21 => EVENING_TITLES,
        _ => NIGHT_TITLES,
    }
}

fn select_headline_pool(now: DateTime<Local>) -> &'static [&'static str] {
    match now.hour() {
        5..=9 => MORNING_HEADLINES,
        10..=12 => NOON_HEADLINES,
        13..=17 => AFTERNOON_HEADLINES,
        18..=21 => EVENING_HEADLINES,
        _ => NIGHT_HEADLINES,
    }
}

fn select_opening_pool(now: DateTime<Local>) -> &'static [&'static str] {
    match now.hour() {
        5..=9 => &[
            "今天的第一段状态条，建议先靠喝水补满。",
            "早上的你很适合来一口温和启动。",
            "先喝两口，再去处理今天的安排，会更稳。",
        ],
        10..=12 => &[
            "中午这口水，常常决定你下午开局够不够顺。",
            "先来一口温和补给，再决定下一步是开会还是干饭。",
            "饭前饭后都行，重点是别把补水这件事漏掉。",
        ],
        13..=17 => &[
            "下午最容易把补水忘掉，所以这条提醒来得很及时。",
            "这会儿抬手喝两口，性价比很高。",
            "喝水不耽误事，反而经常能让后面更顺。",
        ],
        18..=21 => &[
            "到了傍晚，身体对水分支持会更有感知。",
            "今天忙到现在，先给自己一点柔和续航。",
            "先润一润，再继续做最后一段安排。",
        ],
        _ => &[
            "如果你现在还在忙，补一点水会比继续硬扛更划算。",
            "夜里补水不需要隆重，轻轻一口就很好。",
            "低打扰、不煽情，只提醒你现在适合喝口水。",
        ],
    }
}

fn activity_fragment(
    snapshot: &MouseActivitySnapshot,
    rng: &mut rand::rngs::ThreadRng,
) -> &'static str {
    match snapshot.work_state {
        WorkIntensityState::Unavailable => "当前没有拿到鼠标活跃度信号，这轮先按温和提醒处理。",
        WorkIntensityState::DeepFocus => *[
            "看起来你刚刚点得不多，更像是在沉浸推进事情。",
            "你的鼠标最近很安静，这通常意味着你又进了专注区。",
            "这会儿像在深度模式里，补水一下刚好能把状态托住。",
        ]
        .choose(rng)
        .unwrap_or(&"你现在像在专注推进事情。"),
        WorkIntensityState::SteadyFlow => *[
            "最近的点击节奏挺稳，像是在有条不紊地推进工作。",
            "你这会儿的操作频率很均匀，属于稳稳往前推的状态。",
            "这类平稳工作状态最适合顺手补一口。",
        ]
        .choose(rng)
        .unwrap_or(&"你现在处在平稳推进的节奏里。"),
        WorkIntensityState::ActiveHandling => *[
            "最近这段时间点击挺密，像是在来回处理不少事项。",
            "你这会儿的点击活跃度挺在线，像是在密集处理事情。",
            "最近操作频率不低，这口水很适合给节奏降一点燥。",
        ]
        .choose(rng)
        .unwrap_or(&"你现在像在高频处理事务。"),
        WorkIntensityState::RapidFire => *[
            "最近这阵点击非常密集，像是在高强度连轴转。",
            "你这会儿操作很密，像是在集中处理一串事情。",
            "刚刚的点击密度很高，这时候更需要顺手润一下。",
        ]
        .choose(rng)
        .unwrap_or(&"你刚才节奏挺快，这口水刚好帮你缓一下。"),
    }
}

fn weekday_fragment(now: DateTime<Local>) -> &'static str {
    if matches!(now.weekday(), chrono::Weekday::Sat | chrono::Weekday::Sun) {
        return "周末可以松弛，但补水这件事别一起放假。";
    }

    match now.weekday() {
        chrono::Weekday::Mon => "周一先别把自己忙干了，补一口会更好开局。",
        chrono::Weekday::Tue => "周二适合继续稳扎稳打，也适合顺手喝水。",
        chrono::Weekday::Wed => "周三这种中场位置，补水尤其显得聪明。",
        chrono::Weekday::Thu => "周四往往容易闷头赶进度，更要记得润一下。",
        chrono::Weekday::Fri => "周五的快乐在路上，水也别落下。",
        _ => "今天这口水值得及时完成。",
    }
}
