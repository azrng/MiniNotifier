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

#[cfg(windows)]
use windows_sys::Win32::Graphics::Dwm::{
    DwmSetWindowAttribute, DWMWA_WINDOW_CORNER_PREFERENCE, DWMWCP_ROUND,
};
#[cfg(windows)]
use windows_sys::Win32::Graphics::Gdi::{CreateRoundRectRgn, SetWindowRgn};

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
            apply_window_chrome(app);
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
            if matches!(window.label(), "main" | "reminder") {
                match event {
                    WindowEvent::CloseRequested { api, .. } => {
                        api.prevent_close();
                        let _ = window.hide();
                    }
                    WindowEvent::Resized(_) | WindowEvent::ScaleFactorChanged { .. }
                        if window.label() == "reminder" =>
                    {
                        if let Some(webview_window) = window.app_handle().get_webview_window(window.label()) {
                            apply_rounded_corners(&webview_window);
                        }
                    }
                    _ => {}
                }
            }
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

fn apply_window_chrome(app: &tauri::App) {
    #[cfg(windows)]
    {
        for label in ["reminder"] {
            if let Some(window) = app.get_webview_window(label) {
                apply_rounded_corners(&window);
            }
        }
    }
}

#[cfg(windows)]
fn apply_rounded_corners(window: &tauri::WebviewWindow) {
    if let Ok(hwnd) = window.hwnd() {
        let preference = DWMWCP_ROUND;
        let _ = unsafe {
            DwmSetWindowAttribute(
                hwnd.0 as _,
                DWMWA_WINDOW_CORNER_PREFERENCE as _,
                &preference as *const _ as _,
                std::mem::size_of_val(&preference) as u32,
            )
        };

        if let Ok(size) = window.inner_size() {
            let width = size.width as i32;
            let height = size.height as i32;
            if width > 0 && height > 0 {
                let is_maximized = window.is_maximized().unwrap_or(false);
                let radius = if is_maximized { 0 } else { 26 };

                let region = unsafe {
                    if radius > 0 {
                        CreateRoundRectRgn(0, 0, width, height, radius, radius)
                    } else {
                        std::ptr::null_mut()
                    }
                };

                let _ = unsafe { SetWindowRgn(hwnd.0 as _, region, true.into()) };
            }
        }
    }
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
