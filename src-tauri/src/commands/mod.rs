use tauri::{AppHandle, State};

use crate::{
    errors::CommandError,
    models::{
        HydrationSettingsDto, MouseActivitySnapshot, ReminderPayload, SaveHydrationSettingsInput,
    },
    state::AppState,
};

#[tauri::command]
pub fn get_hydration_settings(
    app: AppHandle,
    state: State<'_, AppState>,
) -> Result<HydrationSettingsDto, CommandError> {
    state.inner().settings.get_current(&app).map_err(Into::into)
}

#[tauri::command]
pub fn save_hydration_settings(
    app: AppHandle,
    state: State<'_, AppState>,
    input: SaveHydrationSettingsInput,
) -> Result<HydrationSettingsDto, CommandError> {
    state.inner().settings.save(&app, input).map_err(Into::into)
}

#[tauri::command]
pub fn toggle_hydration_pause(
    app: AppHandle,
    state: State<'_, AppState>,
) -> Result<HydrationSettingsDto, CommandError> {
    state.inner().settings.toggle_pause(&app).map_err(Into::into)
}

#[tauri::command]
pub fn show_hydration_preview(
    app: AppHandle,
    state: State<'_, AppState>,
) -> Result<HydrationSettingsDto, CommandError> {
    state
        .inner()
        .reminders
        .show_preview(&app, &state.inner().settings, &state.inner().mouse_activity)
        .map_err(Into::into)
}

#[tauri::command]
pub fn get_current_reminder_payload(
    state: State<'_, AppState>,
) -> Result<ReminderPayload, CommandError> {
    Ok(state.inner().reminders.current_payload())
}

#[tauri::command]
pub fn get_mouse_activity_snapshot(
    state: State<'_, AppState>,
) -> Result<MouseActivitySnapshot, CommandError> {
    Ok(state.inner().mouse_activity.get_snapshot())
}

#[tauri::command]
pub fn dismiss_reminder(app: AppHandle, state: State<'_, AppState>) -> Result<(), CommandError> {
    state.inner().reminders.dismiss(&app).map_err(Into::into)
}
