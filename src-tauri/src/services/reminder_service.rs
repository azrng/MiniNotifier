use std::sync::Mutex;

use tauri::{
    AppHandle, Emitter, LogicalPosition, LogicalSize, Manager, Position, Size, WebviewWindow,
};

use crate::{
    errors::{AppError, AppResult},
    models::{HydrationSettingsDto, ReminderPayload},
    services::{
        message_service::build_reminder_payload, mouse_activity_service::MouseActivityService,
        settings_service::SettingsService,
    },
};

pub struct ReminderService {
    payload: Mutex<ReminderPayload>,
}

impl ReminderService {
    pub fn new() -> Self {
        Self {
            payload: Mutex::new(ReminderPayload {
                title: "Drink Water".to_string(),
                headline: "提醒窗口准备中".to_string(),
                message: "当提醒触发后，这里会展示新的喝水文案。".to_string(),
                auto_close_seconds: 15,
            }),
        }
    }

    pub fn current_payload(&self) -> ReminderPayload {
        self.payload
            .lock()
            .expect("reminder payload mutex poisoned")
            .clone()
    }

    pub fn dismiss(&self, app: &AppHandle) -> AppResult<()> {
        let reminder_window = app
            .get_webview_window("reminder")
            .ok_or(AppError::WindowUnavailable)?;

        reminder_window
            .hide()
            .map_err(|_| AppError::WindowUnavailable)
    }

    pub fn show_preview(
        &self,
        app: &AppHandle,
        settings: &SettingsService,
        mouse_activity: &MouseActivityService,
    ) -> AppResult<HydrationSettingsDto> {
        self.show(app, settings, mouse_activity, true)
    }

    pub fn show_scheduled(
        &self,
        app: &AppHandle,
        settings: &SettingsService,
        mouse_activity: &MouseActivityService,
    ) -> AppResult<HydrationSettingsDto> {
        self.show(app, settings, mouse_activity, false)
    }

    fn show(
        &self,
        app: &AppHandle,
        settings: &SettingsService,
        mouse_activity: &MouseActivityService,
        preserve_next_reminder: bool,
    ) -> AppResult<HydrationSettingsDto> {
        let document = settings.current_document();
        let activity_snapshot = mouse_activity.get_snapshot();
        let payload = build_reminder_payload(&document, &activity_snapshot);
        {
            let mut current_payload = self
                .payload
                .lock()
                .expect("reminder payload mutex poisoned");
            *current_payload = payload.clone();
        }

        let reminder_window = app
            .get_webview_window("reminder")
            .ok_or(AppError::WindowUnavailable)?;

        prepare_reminder_window(app, &reminder_window)?;
        reminder_window
            .emit("reminder-updated", payload)
            .map_err(|_| AppError::WindowUnavailable)?;
        reminder_window
            .show()
            .map_err(|_| AppError::WindowUnavailable)?;

        settings.record_reminder_shown(app, preserve_next_reminder)
    }
}

fn prepare_reminder_window(app: &AppHandle, reminder_window: &WebviewWindow) -> AppResult<()> {
    let monitor = app
        .primary_monitor()
        .map_err(|_| AppError::WindowUnavailable)?
        .ok_or(AppError::WindowUnavailable)?;

    let scale_factor = monitor.scale_factor();
    let monitor_width = f64::from(monitor.size().width) / scale_factor;
    let monitor_height = f64::from(monitor.size().height) / scale_factor;
    let width = 460.0;
    let height = 320.0;
    let x = (monitor_width - width - 24.0).max(24.0);
    let y = (monitor_height - height - 48.0).max(24.0);

    reminder_window
        .set_size(Size::Logical(LogicalSize::new(width, height)))
        .map_err(|_| AppError::WindowUnavailable)?;
    reminder_window
        .set_position(Position::Logical(LogicalPosition::new(x, y)))
        .map_err(|_| AppError::WindowUnavailable)?;
    reminder_window
        .set_always_on_top(true)
        .map_err(|_| AppError::WindowUnavailable)?;
    reminder_window
        .set_skip_taskbar(true)
        .map_err(|_| AppError::WindowUnavailable)?;

    Ok(())
}
