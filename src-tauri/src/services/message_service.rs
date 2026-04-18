use chrono::{DateTime, Local};
use rand::seq::SliceRandom;

use crate::models::{HydrationSettingsDocument, ReminderPayload};

const MORNING_TITLES: &[&str] = &["晨间补水", "早安续航", "清晨续杯"];
const NOON_TITLES: &[&str] = &["午间续杯", "中场补给", "午间续航"];
const AFTERNOON_TITLES: &[&str] = &["下午续杯", "工位补水", "状态补丁"];
const EVENING_TITLES: &[&str] = &["傍晚续航", "收尾补给", "晚间续杯"];
const NIGHT_TITLES: &[&str] = &["深夜巡航", "夜间续杯", "安静补水"];

const HEADLINES: &[&str] = &[
    "水杯已经等你很久了。",
    "别让今天的状态被嘴硬拖后腿。",
    "顺手喝两口，后面的节奏会更稳。",
    "这是一条低打扰但很认真的补水提醒。",
    "继续忙之前，先给自己一点水分支持。",
];

const BODY_FRAGMENTS: &[&str] = &[
    "这一步很小，但通常能把后面的专注感稳住。",
    "你已经很会推进事情了，喝水这步也别省。",
    "先润一下，再继续把今天的安排往前推。",
    "别等身体先发出抗议，现在补一点刚刚好。",
    "这口水不会耽误你太久，却会让体验柔和很多。",
];

pub fn build_reminder_payload(document: &HydrationSettingsDocument) -> ReminderPayload {
    let now = Local::now();
    let title_pool = select_title_pool(now);
    let mut rng = rand::thread_rng();

    let title = title_pool
        .choose(&mut rng)
        .copied()
        .unwrap_or("Drink Water")
        .to_string();
    let headline = HEADLINES
        .choose(&mut rng)
        .copied()
        .unwrap_or("该喝水了。")
        .to_string();
    let body = format!(
        "{} 当前提醒间隔是每 {} 分钟一次。",
        BODY_FRAGMENTS
            .choose(&mut rng)
            .copied()
            .unwrap_or("顺手喝几口水，让状态重新上线。"),
        document.reminder_interval_minutes
    );

    ReminderPayload {
        title,
        headline,
        message: body,
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

trait HourValue {
    fn hour(&self) -> u32;
}

impl HourValue for DateTime<Local> {
    fn hour(&self) -> u32 {
        chrono::Timelike::hour(self)
    }
}
