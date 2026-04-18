use tauri::{AppHandle, State};

use crate::{
    errors::CommandError,
    models::{HydrationSettingsDto, ReminderPayload, SaveHydrationSettingsInput},
    state::AppState,
};

#[tauri::command]
pub fn get_hydration_settings(
    app: AppHandle,
    state: State<'_, AppState>,
) -> Result<HydrationSettingsDto, CommandError> {
    state.settings.get_current(&app).map_err(Into::into)
}

#[tauri::command]
pub fn save_hydration_settings(
    app: AppHandle,
    state: State<'_, AppState>,
    input: SaveHydrationSettingsInput,
) -> Result<HydrationSettingsDto, CommandError> {
    state.settings.save(&app, input).map_err(Into::into)
}

#[tauri::command]
pub fn toggle_hydration_pause(
    app: AppHandle,
    state: State<'_, AppState>,
) -> Result<HydrationSettingsDto, CommandError> {
    state.settings.toggle_pause(&app).map_err(Into::into)
}

#[tauri::command]
pub fn show_hydration_preview(
    app: AppHandle,
    state: State<'_, AppState>,
) -> Result<HydrationSettingsDto, CommandError> {
    state
        .reminders
        .show_preview(&app, &state.settings)
        .map_err(Into::into)
}

#[tauri::command]
pub fn get_current_reminder_payload(
    state: State<'_, AppState>,
) -> Result<ReminderPayload, CommandError> {
    Ok(state.reminders.current_payload())
}

#[tauri::command]
pub fn dismiss_reminder(app: AppHandle, state: State<'_, AppState>) -> Result<(), CommandError> {
    state.reminders.dismiss(&app).map_err(Into::into)
}
