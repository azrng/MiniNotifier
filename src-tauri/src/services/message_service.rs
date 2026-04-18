use chrono::{DateTime, Datelike, Local, Timelike};
use rand::{rngs::ThreadRng, seq::SliceRandom};

use crate::models::{
    HydrationSettingsDocument, MouseActivitySnapshot, ReminderPayload, WorkIntensityState,
};

struct MessageCatalog {
    titles: &'static [&'static str],
    headlines: &'static [&'static str],
    openings: &'static [&'static str],
    middles: &'static [&'static str],
    closings: &'static [&'static str],
}

const PRAISE_FRAGMENTS: &[&str] = &[
    "你今天这状态已经够能扛了，别连嘴巴也一起硬扛。",
    "你现在这份专注感挺像样，顺手喝口水会更像样。",
    "像你这样认真推进事情的人，没必要在喝水这步上装酷。",
    "你这会儿的节奏挺顺，别让一口水把自己卡住。",
    "你今天这份执行力挺在线，喝口水会更完整。",
];

const PLAYFUL_FRAGMENTS: &[&str] = &[
    "水杯已经在旁边站岗很久了，你多少理它一下。",
    "再不喝，它都快默认自己是桌面摆件了。",
    "这不是催你，这是水杯在进行最后的礼貌提醒。",
    "工位空气已经看出你有点该喝水了。",
    "抬手喝两口这件事，不会耽误你继续当大忙人。",
];

const WORKDAY_FRAGMENTS: &[&str] = &[
    "工作日的节奏本来就紧，补水这件事更该被排进主线。",
    "工作日容易一路忙到底，这口水就是你的中途保养。",
    "今天的安排不一定轻松，但补水可以先轻松完成。",
    "工作日里能记得喝水的人，通常状态都更稳。",
];

const WEEKEND_FRAGMENTS: &[&str] = &[
    "周末可以松弛，但补水这件事别一起放假。",
    "周末节奏更自由，刚好适合优雅喝一口。",
    "周末主打一个舒服，补水当然也得跟上。",
    "周末就该轻轻松松把水喝掉，不必等提醒太久。",
];

const MORNING_CATALOG: MessageCatalog = MessageCatalog {
    titles: &[
        "晨间补水",
        "早安续航",
        "清晨续杯",
        "早起补给",
        "晨光续杯",
        "晨间提醒",
        "状态开机",
    ],
    headlines: &[
        "早上先让嗓子别走旱路",
        "今天第一口水值得认真对待",
        "电脑开机了，你也顺手开个补水模式",
        "嘴巴别一早就进入节能状态",
        "晨间状态值，建议先靠喝水拉满",
    ],
    openings: &[
        "今天的第一段状态条，建议先靠喝水补满。",
        "早上的你很适合来一口温和启动。",
        "清晨补一点，往后说话和思路都会更舒服。",
        "先喝两口，再去处理今天的安排，会更稳。",
        "先续一口水，今天的开局会更丝滑。",
    ],
    middles: &[
        "你当前的提醒节奏是每 {0} 分钟一次，这一轮先优雅完成。",
        "就把这次当成今天第一个轻量级成就。",
        "动作越简单越容易坚持，拿起杯子就是胜利。",
        "把身体带上节奏，今天做事会更轻盈一点。",
    ],
    closings: &[
        "喝完这口，我们再继续发光。",
        "现在喝，刚刚好。",
        "补完这口，出发。",
        "今天先从一口舒服的水开始。",
    ],
};

const NOON_CATALOG: MessageCatalog = MessageCatalog {
    titles: &[
        "午间续杯",
        "中场补给",
        "中午续杯",
        "午休补水",
        "午间提醒",
        "午后前置",
    ],
    headlines: &[
        "中午别只想着吃，也别忘了喝",
        "午间这口水，能给下午省很多劲",
        "饭点附近最适合顺手补一口",
        "中午补水，是给下半场做准备",
    ],
    openings: &[
        "如果上午已经忙了一轮，这会儿更适合补点水。",
        "中午这口水，常常决定你下午开局够不够顺。",
        "先来一口温和补给，再决定下一步是开会还是干饭。",
        "饭前饭后都行，重点是别把补水这件事漏掉。",
    ],
    middles: &[
        "当前的提醒节奏是每 {0} 分钟一次，刚好轮到你和水杯见一面。",
        "现在喝两口，下午更不容易出现低电量表情包。",
        "把它当成一次中场维护，顺手就能完成。",
        "先来一口，下午再继续做效率担当。",
    ],
    closings: &[
        "喝完这口，再继续安排下午。",
        "就现在，刚刚合适。",
        "先润一下，下午更顺滑。",
        "轻轻一口，后面更从容。",
    ],
};

const AFTERNOON_CATALOG: MessageCatalog = MessageCatalog {
    titles: &[
        "下午续杯",
        "工位补水",
        "三点续航",
        "下午提醒",
        "状态补丁",
        "工位续杯",
    ],
    headlines: &[
        "别让自己在下午变成沙漠模式",
        "你不是没电，你只是该补水了",
        "下午最值得做的轻量动作之一，就是喝水",
        "先补水，再继续把事情稳稳往前推",
    ],
    openings: &[
        "下午最容易把补水忘掉，所以这条提醒来得很及时。",
        "现在补一点，能有效降低后面干巴巴工作的概率。",
        "这会儿抬手喝两口，性价比很高。",
        "喝水不耽误事，反而经常能让后面更顺。",
    ],
    middles: &[
        "你可以把它理解成一次给状态回稳的小维护。",
        "这不是中断流程，这是给流程做保养。",
        "如果继续忙下去，这口水只会显得越来越值。",
        "这一口下去，通常能把下午的体验往回拉一截。",
    ],
    closings: &[
        "喝完这口，再继续漂亮输出。",
        "这不是分神，是在帮状态回稳。",
        "现在来一口，刚刚好。",
        "轻轻一口，继续在线。",
    ],
};

const EVENING_CATALOG: MessageCatalog = MessageCatalog {
    titles: &[
        "傍晚续航",
        "收尾补给",
        "晚间续杯",
        "傍晚提醒",
        "收工前续杯",
        "舒服收工",
    ],
    headlines: &[
        "天快黑了，状态别先黑屏",
        "收尾阶段也值得拥有一口舒服的水",
        "傍晚这口水，能让今天收得更从容",
        "下班前补一口，体验通常会更完整",
    ],
    openings: &[
        "到了傍晚，身体对水分支持会更有感知。",
        "今天忙到现在，先给自己一点柔和续航。",
        "收尾阶段喝口水，会让整个人松快不少。",
        "先润一润，再继续做最后一段安排。",
    ],
    middles: &[
        "当前提醒间隔是 {0} 分钟一次，这轮刚好轮到水杯露脸。",
        "这一口很小，但经常能显著改善收尾体验。",
        "事情可以慢慢收，水分先补上通常更明智。",
        "这不是打断节奏，而是在帮你把节奏托住。",
    ],
    closings: &[
        "喝完这口，今天就能更体面地收尾。",
        "收工前这口水，值得。",
        "现在补，后面更从容。",
        "先续杯，再决定下一步。",
    ],
};

const NIGHT_CATALOG: MessageCatalog = MessageCatalog {
    titles: &[
        "深夜巡航",
        "夜猫子补给",
        "夜间续杯",
        "深夜提醒",
        "安静补水",
        "低打扰续杯",
    ],
    headlines: &[
        "还没休息的话，先给自己补点水",
        "深夜可以忙，但别干着忙",
        "这是一条很安静但很认真的补水提醒",
        "水杯虽然不说话，但它确实在等你",
    ],
    openings: &[
        "如果你现在还在忙，补一点水会比继续硬扛更划算。",
        "深夜时段更容易忘记补水，所以这条提醒尽量说得温柔一点。",
        "夜里补水不需要隆重，轻轻一口就很好。",
        "低打扰、不煽情，只提醒你现在适合喝口水。",
    ],
    middles: &[
        "当前提醒节奏是每 {0} 分钟一次，水杯准时来打卡了。",
        "深夜也要补水，别让嘴唇先进入加班模式。",
        "这一步不会打断你太久，但对状态很友好。",
        "你可以把它当成一份安静的夜间补丁。",
    ],
    closings: &[
        "轻轻一口，继续保持舒服。",
        "不急，喝完再继续。",
        "深夜这口，往往特别值。",
        "这一口不求热血，只求舒服。",
    ],
};

const PAUSED_PREVIEW_CATALOG: MessageCatalog = MessageCatalog {
    titles: &[
        "预览提醒",
        "暂停模式预览",
        "小窗演示",
        "静音预览",
        "弹窗彩排",
    ],
    headlines: &[
        "虽然你暂停了，但水杯没记仇",
        "系统没有催你，这次只是演示一下",
        "现在是预览时间，不是强制任务",
        "当前属于演示场次，请轻松围观",
    ],
    openings: &[
        "当前提醒处于暂停状态，所以这次只是一个温柔的界面演示。",
        "你现在看到的是预览版本，不会改变正式提醒节奏。",
        "这次出现的主要目标，是帮你看看弹窗效果顺不顺眼。",
        "就把它当成一次轻量试播，不用有任何压力。",
    ],
    middles: &[
        "正式提醒仍然保持暂停，不会因为这次预览偷偷恢复。",
        "看看样式、感受一下文案，就算完成任务了。",
        "预览结束后，系统仍旧会尊重你当前的暂停选择。",
    ],
    closings: &[
        "想喝就喝，看看效果也完全可以。",
        "这波主打一个轻松出现。",
        "看完效果，我们就安静退场。",
        "保持轻松，继续你的节奏。",
    ],
};

pub fn build_reminder_payload(
    document: &HydrationSettingsDocument,
    activity_snapshot: &MouseActivitySnapshot,
) -> ReminderPayload {
    build_reminder_payload_at(document, activity_snapshot, Local::now())
}

pub(crate) fn build_reminder_payload_at(
    document: &HydrationSettingsDocument,
    activity_snapshot: &MouseActivitySnapshot,
    now: DateTime<Local>,
) -> ReminderPayload {
    let catalog = if document.is_paused {
        &PAUSED_PREVIEW_CATALOG
    } else {
        select_catalog(now)
    };
    let mut rng = rand::thread_rng();

    ReminderPayload {
        title: pick(catalog.titles, &mut rng).to_string(),
        headline: pick(catalog.headlines, &mut rng).to_string(),
        message: build_body(catalog, document, activity_snapshot, now, &mut rng),
        auto_close_seconds: document.auto_close_seconds,
    }
}

fn select_catalog(now: DateTime<Local>) -> &'static MessageCatalog {
    match now.hour() {
        5..=9 => &MORNING_CATALOG,
        10..=12 => &NOON_CATALOG,
        13..=17 => &AFTERNOON_CATALOG,
        18..=21 => &EVENING_CATALOG,
        _ => &NIGHT_CATALOG,
    }
}

fn build_body(
    catalog: &MessageCatalog,
    document: &HydrationSettingsDocument,
    activity_snapshot: &MouseActivitySnapshot,
    now: DateTime<Local>,
    rng: &mut ThreadRng,
) -> String {
    if document.is_paused {
        return build_paused_body(catalog, rng);
    }

    let first_candidates = [
        pick_short_sentence(catalog.openings, 28, rng),
        interval_context(document.reminder_interval_minutes),
        day_context(now, rng),
        activity_context(activity_snapshot, rng),
    ];
    let first_sentence = pick(&first_candidates, rng).clone();

    let second_candidates = [
        pick_short_sentence(catalog.middles, 26, rng),
        pick_short_sentence(catalog.closings, 18, rng),
        pick_short_sentence(PRAISE_FRAGMENTS, 18, rng),
    ];
    let second_sentence =
        pick_different_sentence(&second_candidates, &[first_sentence.as_str()], rng);

    let third_candidates = [
        pick_short_sentence(catalog.closings, 16, rng),
        pick_short_sentence(PLAYFUL_FRAGMENTS, 18, rng),
        pick_short_sentence(PRAISE_FRAGMENTS, 16, rng),
    ];
    let third_sentence = pick_different_sentence(
        &third_candidates,
        &[first_sentence.as_str(), second_sentence.as_str()],
        rng,
    );

    let mut body = format!("{}{}", first_sentence, second_sentence);
    if body.chars().count() + third_sentence.chars().count() <= 52 {
        body.push_str(&third_sentence);
    }

    body
}

fn build_paused_body(catalog: &MessageCatalog, rng: &mut ThreadRng) -> String {
    let first_sentence = pick_short_sentence(catalog.openings, 28, rng);
    let second_candidates = [
        pick_short_sentence(catalog.middles, 22, rng),
        pick_short_sentence(catalog.closings, 18, rng),
    ];
    let second_sentence =
        pick_different_sentence(&second_candidates, &[first_sentence.as_str()], rng);

    format!("{}{}", first_sentence, second_sentence)
}

fn interval_context(reminder_interval_minutes: i64) -> String {
    normalize_sentence(format!(
        "到了每 {} 分钟的补水节点。",
        reminder_interval_minutes
    ))
}

fn day_context(now: DateTime<Local>, rng: &mut ThreadRng) -> String {
    let primary = if is_weekend(now) {
        *pick(WEEKEND_FRAGMENTS, rng)
    } else {
        *pick(WORKDAY_FRAGMENTS, rng)
    };

    normalize_sentence(pick(&[primary, weekday_fragment(now.weekday())], rng))
}

fn activity_context(snapshot: &MouseActivitySnapshot, rng: &mut ThreadRng) -> String {
    let sentence = match snapshot.work_state {
        WorkIntensityState::Unavailable => "当前没有拿到鼠标活跃度信号，这轮先按温和提醒处理。",
        WorkIntensityState::DeepFocus => pick(
            &[
                "看起来你刚刚点得不多，更像是在沉浸推进事情，倒是挺会闷头干活。",
                "最近这段时间点击很少，像是认真进了专注区，顺手喝口水不过分吧。",
                "你的鼠标最近很安静，这通常意味着脑袋在稳定输出，也意味着你又容易忘喝水。",
                "你这会儿像在深度模式里，补水一下刚好能把状态托住。",
            ],
            rng,
        ),
        WorkIntensityState::SteadyFlow => pick(
            &[
                "最近的点击节奏挺稳，像是在有条不紊地推进工作，别稳到把喝水也一起省了。",
                "你这会儿的操作频率很均匀，属于稳稳往前推的状态，顺手补一口正合适。",
                "这类平稳工作状态最适合顺手补一口，不会打乱节奏。",
            ],
            rng,
        ),
        WorkIntensityState::ActiveHandling => pick(
            &[
                "最近这段时间点击挺密，像是在来回处理不少事项，忙得很认真。",
                "你这会儿的点击活跃度挺在线，像是在密集处理事情，顺手补水刚好降点燥。",
                "最近操作频率不低，这口水很适合给节奏降一点燥。",
            ],
            rng,
        ),
        WorkIntensityState::RapidFire => pick(
            &[
                "最近这阵点击非常密集，像是在高强度连轴转，水杯都快插不上话了。",
                "你这会儿操作很密，像是在集中处理一串事情，但也别忙到把喝水开除。",
                "刚刚的点击密度很高，像是在处理一串连发任务，这时候更需要顺手润一下。",
            ],
            rng,
        ),
    };

    normalize_sentence(sentence)
}

fn pick_short_sentence(pool: &[&str], preferred_max_len: usize, rng: &mut ThreadRng) -> String {
    let mut candidates = Vec::new();
    let mut shortest: Option<String> = None;

    for item in pool {
        let sentence = normalize_sentence(*item);
        let is_shorter = shortest
            .as_ref()
            .map(|current| sentence.chars().count() < current.chars().count())
            .unwrap_or(true);
        if is_shorter {
            shortest = Some(sentence.clone());
        }
        if sentence.chars().count() <= preferred_max_len {
            candidates.push(sentence);
        }
    }

    if candidates.is_empty() {
        shortest.unwrap_or_default()
    } else {
        pick(&candidates, rng).to_string()
    }
}

fn pick_different_sentence(pool: &[String], excluded: &[&str], rng: &mut ThreadRng) -> String {
    let candidates: Vec<String> = pool
        .iter()
        .filter(|candidate| !excluded.iter().any(|item| item == &candidate.as_str()))
        .cloned()
        .collect();

    if candidates.is_empty() {
        pool.first().cloned().unwrap_or_default()
    } else {
        pick(&candidates, rng).clone()
    }
}

fn normalize_sentence<T: AsRef<str>>(text: T) -> String {
    let trimmed = text.as_ref().trim();
    if trimmed.is_empty() {
        return String::new();
    }

    let no_dot = trimmed.strip_suffix('.').unwrap_or(trimmed);
    if no_dot.ends_with('。') || no_dot.ends_with('！') || no_dot.ends_with('？') {
        no_dot.to_string()
    } else {
        format!("{no_dot}。")
    }
}

fn weekday_fragment(weekday: chrono::Weekday) -> &'static str {
    match weekday {
        chrono::Weekday::Mon => "周一先别把自己忙干了，补一口会更好开局。",
        chrono::Weekday::Tue => "周二适合继续稳扎稳打，也适合顺手喝水。",
        chrono::Weekday::Wed => "周三这种中场位置，补水尤其显得聪明。",
        chrono::Weekday::Thu => "周四往往容易闷头赶进度，更要记得润一下。",
        chrono::Weekday::Fri => "周五的快乐在路上，水也别落下。",
        chrono::Weekday::Sat => "周六如果你在放松，这口水会让舒服感更完整。",
        chrono::Weekday::Sun => "周日更适合把状态调柔和一点，补水很加分。",
    }
}

fn is_weekend(now: DateTime<Local>) -> bool {
    matches!(now.weekday(), chrono::Weekday::Sat | chrono::Weekday::Sun)
}

fn pick<'a, T>(pool: &'a [T], rng: &mut ThreadRng) -> &'a T {
    pool.choose(rng).unwrap_or(&pool[0])
}

#[cfg(test)]
mod tests {
    use super::*;
    use chrono::{LocalResult, TimeZone};

    fn sample_document() -> HydrationSettingsDocument {
        HydrationSettingsDocument {
            is_reminder_enabled: true,
            is_paused: false,
            reminder_interval_minutes: 30,
            auto_close_seconds: 15,
            last_reminder_at: None,
            next_reminder_at: None,
        }
    }

    fn sample_snapshot(state: WorkIntensityState) -> MouseActivitySnapshot {
        MouseActivitySnapshot {
            clicks_last_minute: 8,
            clicks_last_five_minutes: 24,
            work_state: state,
            work_state_text: "测试".to_string(),
        }
    }

    fn local_datetime(year: i32, month: u32, day: u32, hour: u32, minute: u32) -> DateTime<Local> {
        match Local.with_ymd_and_hms(year, month, day, hour, minute, 0) {
            LocalResult::Single(value) => value,
            _ => panic!("invalid local datetime for test"),
        }
    }

    #[test]
    fn morning_payload_uses_morning_title_pool() {
        let payload = build_reminder_payload_at(
            &sample_document(),
            &sample_snapshot(WorkIntensityState::DeepFocus),
            local_datetime(2026, 4, 20, 8, 30),
        );

        assert!(MORNING_CATALOG.titles.contains(&payload.title.as_str()));
        assert_eq!(payload.auto_close_seconds, 15);
        assert!(!payload.message.is_empty());
    }

    #[test]
    fn paused_payload_uses_preview_catalog_and_mentions_preview_tone() {
        let mut document = sample_document();
        document.is_paused = true;

        let payload = build_reminder_payload_at(
            &document,
            &sample_snapshot(WorkIntensityState::Unavailable),
            local_datetime(2026, 4, 20, 14, 0),
        );

        assert!(PAUSED_PREVIEW_CATALOG
            .titles
            .contains(&payload.title.as_str()));
        assert!(
            payload.message.contains("预览")
                || payload.message.contains("演示")
                || payload.message.contains("暂停")
                || payload.message.contains("弹窗效果")
                || payload.message.contains("试播")
        );
    }

    #[test]
    fn activity_context_reflects_work_state() {
        let deep_focus = activity_context(
            &sample_snapshot(WorkIntensityState::DeepFocus),
            &mut rand::thread_rng(),
        );
        let rapid_fire = activity_context(
            &sample_snapshot(WorkIntensityState::RapidFire),
            &mut rand::thread_rng(),
        );

        assert!(
            deep_focus.contains("专注")
                || deep_focus.contains("安静")
                || deep_focus.contains("点得不多")
                || deep_focus.contains("沉浸推进事情")
                || deep_focus.contains("点击很少")
                || deep_focus.contains("稳定输出")
                || deep_focus.contains("深度模式")
        );
        assert!(
            rapid_fire.contains("密集")
                || rapid_fire.contains("连轴")
                || rapid_fire.contains("快")
                || rapid_fire.contains("很密")
                || rapid_fire.contains("密度很高")
                || rapid_fire.contains("连发任务")
        );
    }
}
