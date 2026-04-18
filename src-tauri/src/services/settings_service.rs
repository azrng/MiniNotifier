use std::sync::Mutex;

use chrono::{DateTime, Duration, Local};
use tauri::{AppHandle, Emitter};
use tauri_plugin_autostart::ManagerExt;

use crate::{
    errors::{AppError, AppResult},
    models::{
        HydrationSettingsDocument, HydrationSettingsDto, SaveHydrationSettingsInput,
        StartupSettingsDto,
    },
    services::settings_store::SettingsStore,
};

pub struct SettingsService {
    store: SettingsStore,
    runtime: Mutex<RuntimeSettingsState>,
}

struct RuntimeSettingsState {
    document: HydrationSettingsDocument,
    save_state_text: String,
}

impl SettingsService {
    pub fn new() -> AppResult<Self> {
        let store = SettingsStore::new()?;
        let loaded = store.load()?;

        let (document, save_state_text) = match loaded {
            Some(document) => (normalize_document(document), "配置已载入".to_string()),
            None => {
                let document = default_document();
                store.save(&document)?;
                (document, "已加载默认配置".to_string())
            }
        };

        Ok(Self {
            store,
            runtime: Mutex::new(RuntimeSettingsState {
                document,
                save_state_text,
            }),
        })
    }

    pub fn get_current(&self, app: &AppHandle) -> AppResult<HydrationSettingsDto> {
        let state = self.runtime.lock().expect("settings mutex poisoned");
        self.build_dto(app, &state.document, &state.save_state_text)
    }

    pub fn current_document(&self) -> HydrationSettingsDocument {
        self.runtime
            .lock()
            .expect("settings mutex poisoned")
            .document
            .clone()
    }

    pub fn is_due(&self) -> bool {
        let state = self.runtime.lock().expect("settings mutex poisoned");
        if !state.document.is_reminder_enabled || state.document.is_paused {
            return false;
        }

        let Some(next_reminder_at) = state.document.next_reminder_at.as_deref() else {
            return false;
        };

        parse_timestamp(next_reminder_at)
            .map(|value| value <= Local::now())
            .unwrap_or(false)
    }

    pub fn save(
        &self,
        app: &AppHandle,
        input: SaveHydrationSettingsInput,
    ) -> AppResult<HydrationSettingsDto> {
        let previous_autostart_enabled = self.is_autostart_enabled(app)?;
        if previous_autostart_enabled != input.startup_enabled {
            self.set_autostart_enabled(app, input.startup_enabled)?;
        }

        let result = (|| {
            let now = Local::now();
            let mut state = self.runtime.lock().expect("settings mutex poisoned");
            state.document = normalize_document(HydrationSettingsDocument {
                is_reminder_enabled: input.is_reminder_enabled,
                is_paused: input.is_paused,
                reminder_interval_minutes: input.reminder_interval_minutes,
                auto_close_seconds: input.auto_close_seconds,
                last_reminder_at: state.document.last_reminder_at.clone(),
                next_reminder_at: build_next_reminder(
                    input.is_reminder_enabled,
                    input.is_paused,
                    input.reminder_interval_minutes,
                    now,
                ),
            });
            self.store.save(&state.document)?;
            state.save_state_text = "刚刚保存".to_string();
            self.build_dto(app, &state.document, &state.save_state_text)
        })();

        if result.is_err() && previous_autostart_enabled != input.startup_enabled {
            let _ = self.set_autostart_enabled(app, previous_autostart_enabled);
        }

        let dto = result?;
        let _ = app.emit("settings-updated", dto.clone());
        Ok(dto)
    }

    pub fn toggle_pause(&self, app: &AppHandle) -> AppResult<HydrationSettingsDto> {
        let dto = {
            let now = Local::now();
            let mut state = self.runtime.lock().expect("settings mutex poisoned");
            state.document.is_paused = !state.document.is_paused;
            state.document.next_reminder_at = build_next_reminder(
                state.document.is_reminder_enabled,
                state.document.is_paused,
                state.document.reminder_interval_minutes,
                now,
            );
            self.store.save(&state.document)?;
            state.save_state_text = if state.document.is_paused {
                "已暂停提醒".to_string()
            } else {
                "已恢复提醒".to_string()
            };
            self.build_dto(app, &state.document, &state.save_state_text)?
        };

        let _ = app.emit("settings-updated", dto.clone());
        Ok(dto)
    }

    pub fn record_reminder_shown(
        &self,
        app: &AppHandle,
        preserve_next_reminder: bool,
    ) -> AppResult<HydrationSettingsDto> {
        let dto = {
            let now = Local::now();
            let mut state = self.runtime.lock().expect("settings mutex poisoned");
            state.document.last_reminder_at = Some(now.to_rfc3339());
            if !preserve_next_reminder {
                state.document.next_reminder_at = build_next_reminder(
                    state.document.is_reminder_enabled,
                    state.document.is_paused,
                    state.document.reminder_interval_minutes,
                    now,
                );
            }
            self.store.save(&state.document)?;
            state.save_state_text = "刚刚提醒过".to_string();
            self.build_dto(app, &state.document, &state.save_state_text)?
        };

        let _ = app.emit("settings-updated", dto.clone());
        Ok(dto)
    }
    fn build_dto(
        &self,
        app: &AppHandle,
        document: &HydrationSettingsDocument,
        save_state_text: &str,
    ) -> AppResult<HydrationSettingsDto> {
        Ok(HydrationSettingsDto {
            is_reminder_enabled: document.is_reminder_enabled,
            is_paused: document.is_paused,
            reminder_interval_minutes: document.reminder_interval_minutes,
            auto_close_seconds: document.auto_close_seconds,
            last_reminder_at: document.last_reminder_at.clone(),
            next_reminder_at: document.next_reminder_at.clone(),
            save_state_text: save_state_text.to_string(),
            startup_settings: self.get_autostart_status(app)?,
        })
    }

    fn get_autostart_status(&self, app: &AppHandle) -> AppResult<StartupSettingsDto> {
        let is_enabled = self.is_autostart_enabled(app)?;
        Ok(StartupSettingsDto {
            is_enabled,
            status_text: if is_enabled {
                "已开启，开机自动运行".to_string()
            } else {
                "未开启".to_string()
            },
        })
    }

    fn is_autostart_enabled(&self, app: &AppHandle) -> AppResult<bool> {
        app.autolaunch()
            .is_enabled()
            .map_err(|_| AppError::AutoStartFailed)
    }

    fn set_autostart_enabled(&self, app: &AppHandle, enabled: bool) -> AppResult<()> {
        if enabled {
            app.autolaunch()
                .enable()
                .map_err(|_| AppError::AutoStartFailed)
        } else {
            app.autolaunch()
                .disable()
                .map_err(|_| AppError::AutoStartFailed)
        }
    }
}

fn default_document() -> HydrationSettingsDocument {
    let mut document = HydrationSettingsDocument::default();
    document.next_reminder_at = build_next_reminder(
        document.is_reminder_enabled,
        document.is_paused,
        document.reminder_interval_minutes,
        Local::now(),
    );
    document
}

fn normalize_document(mut document: HydrationSettingsDocument) -> HydrationSettingsDocument {
    document.reminder_interval_minutes = document.reminder_interval_minutes.clamp(5, 240);
    document.auto_close_seconds = document.auto_close_seconds.clamp(3, 15);

    if !document.is_reminder_enabled || document.is_paused {
        document.next_reminder_at = None;
        return document;
    }

    let should_rebuild = match document.next_reminder_at.as_deref() {
        Some(value) => parse_timestamp(value)
            .map(|timestamp| timestamp <= Local::now())
            .unwrap_or(true),
        None => true,
    };

    if should_rebuild {
        document.next_reminder_at = build_next_reminder(
            document.is_reminder_enabled,
            document.is_paused,
            document.reminder_interval_minutes,
            Local::now(),
        );
    }

    document
}

fn build_next_reminder(
    is_reminder_enabled: bool,
    is_paused: bool,
    reminder_interval_minutes: i64,
    now: DateTime<Local>,
) -> Option<String> {
    if !is_reminder_enabled || is_paused {
        return None;
    }

    Some((now + Duration::minutes(reminder_interval_minutes.clamp(5, 240))).to_rfc3339())
}

fn parse_timestamp(value: &str) -> Option<DateTime<Local>> {
    chrono::DateTime::parse_from_rfc3339(value)
        .ok()
        .map(|date_time| date_time.with_timezone(&Local))
}
