#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

fn main() {
    mininotifier_tauri_lib::run();
}
