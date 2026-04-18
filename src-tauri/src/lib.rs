mod commands;
mod errors;
mod models;
mod services;
mod state;

use std::time::Duration;

use tauri::{
    include_image,
    menu::{MenuBuilder, MenuItemBuilder, PredefinedMenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    AppHandle, Emitter, Manager, WindowEvent,
};
use tauri_plugin_autostart::MacosLauncher;
use tokio::time::sleep;

use crate::{
    commands::{
        dismiss_reminder, get_current_reminder_payload, get_hydration_settings,
        get_mouse_activity_snapshot, save_hydration_settings, show_hydration_preview,
        toggle_hydration_pause,
    },
    state::AppState,
};

pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_autostart::init(
            MacosLauncher::LaunchAgent,
            Some(vec!["--background"]),
        ))
        .setup(|app| {
            let state = AppState::new()
                .map_err(|error| -> Box<dyn std::error::Error> { Box::new(error) })?;
            app.manage(state);
            build_tray(app)?;
            spawn_scheduler(app.handle().clone());
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            get_hydration_settings,
            save_hydration_settings,
            toggle_hydration_pause,
            show_hydration_preview,
            get_current_reminder_payload,
            get_mouse_activity_snapshot,
            dismiss_reminder
        ])
        .on_window_event(|window, event| {
            if let WindowEvent::CloseRequested { api, .. } = event {
                if matches!(window.label(), "main" | "reminder") {
                    api.prevent_close();
                    let _ = window.hide();
                }
            }
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

fn build_tray(app: &tauri::App) -> tauri::Result<()> {
    let open = MenuItemBuilder::with_id("open", "打开设置").build(app)?;
    let preview = MenuItemBuilder::with_id("preview", "立即提醒一次").build(app)?;
    let toggle_pause = MenuItemBuilder::with_id("toggle_pause", "暂停 / 恢复提醒").build(app)?;
    let quit = MenuItemBuilder::with_id("quit", "退出应用").build(app)?;
    let separator = PredefinedMenuItem::separator(app)?;

    let menu = MenuBuilder::new(app)
        .item(&open)
        .item(&preview)
        .item(&toggle_pause)
        .item(&separator)
        .item(&quit)
        .build()?;

    TrayIconBuilder::with_id("main-tray")
        .icon(include_image!("icons/icon.ico"))
        .tooltip("MiniNotifier")
        .show_menu_on_left_click(false)
        .menu(&menu)
        .on_menu_event(|app, event| {
            handle_tray_action(app, event.id.as_ref());
        })
        .on_tray_icon_event(|tray, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                button_state: MouseButtonState::Up,
                ..
            } = event
            {
                let app = tray.app_handle();
                handle_tray_action(&app, "open");
            }
        })
        .build(app)?;

    update_tray_tooltip(app.handle().clone());
    Ok(())
}

fn handle_tray_action(app: &AppHandle, action_id: &str) {
    match action_id {
        "open" => {
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.show();
                let _ = window.set_focus();
            }
        }
        "preview" => {
            let state = app.state::<AppState>();
            let state = state.inner();
            let _ = state
                .reminders
                .show_preview(app, &state.settings, &state.mouse_activity);
            update_tray_tooltip(app.clone());
        }
        "toggle_pause" => {
            let state = app.state::<AppState>();
            let state = state.inner();
            let _ = state.settings.toggle_pause(app);
            update_tray_tooltip(app.clone());
        }
        "quit" => {
            app.exit(0);
        }
        _ => {}
    }
}

fn update_tray_tooltip(app: AppHandle) {
    let state = app.state::<AppState>();
    let state = state.inner();
    let tooltip = if let Ok(settings) = state.settings.get_current(&app) {
        if !settings.is_reminder_enabled {
            "MiniNotifier · 已关闭".to_string()
        } else if settings.is_paused {
            "MiniNotifier · 已暂停".to_string()
        } else {
            "MiniNotifier · 运行中".to_string()
        }
    } else {
        "MiniNotifier".to_string()
    };

    if let Some(tray) = app.tray_by_id("main-tray") {
        let _ = tray.set_tooltip(Some(tooltip.clone()));
    }

    let _ = app.emit("tray-tooltip-updated", tooltip);
}

fn spawn_scheduler(app: AppHandle) {
    tauri::async_runtime::spawn(async move {
        loop {
            sleep(Duration::from_secs(15)).await;
            let state = app.state::<AppState>();
            let state = state.inner();
            if state.settings.is_due() {
                let _ =
                    state
                        .reminders
                        .show_scheduled(&app, &state.settings, &state.mouse_activity);
                update_tray_tooltip(app.clone());
            }
        }
    });
}
