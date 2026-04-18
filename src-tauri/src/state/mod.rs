use crate::services::{
    mouse_activity_service::MouseActivityService, reminder_service::ReminderService,
    settings_service::SettingsService,
};

pub struct AppState {
    pub mouse_activity: MouseActivityService,
    pub settings: SettingsService,
    pub reminders: ReminderService,
}

impl AppState {
    pub fn new() -> crate::errors::AppResult<Self> {
        Ok(Self {
            mouse_activity: MouseActivityService::new(),
            settings: SettingsService::new()?,
            reminders: ReminderService::new(),
        })
    }
}
