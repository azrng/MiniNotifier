use crate::services::{reminder_service::ReminderService, settings_service::SettingsService};

pub struct AppState {
    pub settings: SettingsService,
    pub reminders: ReminderService,
}

impl AppState {
    pub fn new() -> crate::errors::AppResult<Self> {
        Ok(Self {
            settings: SettingsService::new()?,
            reminders: ReminderService::new(),
        })
    }
}
