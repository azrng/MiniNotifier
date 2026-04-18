use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HydrationSettingsDocument {
    #[serde(rename = "IsReminderEnabled", default = "default_true")]
    pub is_reminder_enabled: bool,

    #[serde(rename = "IsPaused", default)]
    pub is_paused: bool,

    #[serde(
        rename = "ReminderIntervalMinutes",
        default = "default_interval_minutes"
    )]
    pub reminder_interval_minutes: i64,

    #[serde(rename = "AutoCloseSeconds", default = "default_auto_close_seconds")]
    pub auto_close_seconds: i64,

    #[serde(rename = "LastReminderAt", default)]
    pub last_reminder_at: Option<String>,

    #[serde(rename = "NextReminderAt", default)]
    pub next_reminder_at: Option<String>,
}

impl Default for HydrationSettingsDocument {
    fn default() -> Self {
        Self {
            is_reminder_enabled: true,
            is_paused: false,
            reminder_interval_minutes: default_interval_minutes(),
            auto_close_seconds: default_auto_close_seconds(),
            last_reminder_at: None,
            next_reminder_at: None,
        }
    }
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct StartupSettingsDto {
    pub is_enabled: bool,
    pub status_text: String,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct HydrationSettingsDto {
    pub is_reminder_enabled: bool,
    pub is_paused: bool,
    pub reminder_interval_minutes: i64,
    pub auto_close_seconds: i64,
    pub last_reminder_at: Option<String>,
    pub next_reminder_at: Option<String>,
    pub save_state_text: String,
    pub startup_settings: StartupSettingsDto,
}

#[derive(Debug, Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SaveHydrationSettingsInput {
    pub is_reminder_enabled: bool,
    pub is_paused: bool,
    pub reminder_interval_minutes: i64,
    pub auto_close_seconds: i64,
    pub startup_enabled: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ReminderPayload {
    pub title: String,
    pub headline: String,
    pub message: String,
    pub auto_close_seconds: i64,
}

fn default_true() -> bool {
    true
}

fn default_interval_minutes() -> i64 {
    30
}

fn default_auto_close_seconds() -> i64 {
    15
}
