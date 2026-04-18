use std::{
    collections::VecDeque,
    ptr::null,
    sync::{Arc, Mutex, OnceLock},
    time::{Duration, Instant},
};

use crate::models::{MouseActivitySnapshot, WorkIntensityState};

#[cfg(windows)]
use windows_sys::Win32::{
    System::LibraryLoader::GetModuleHandleW,
    UI::WindowsAndMessaging::{
        CallNextHookEx, SetWindowsHookExW, UnhookWindowsHookEx, HC_ACTION, HHOOK, WH_MOUSE_LL,
        WM_LBUTTONDOWN, WM_MBUTTONDOWN, WM_RBUTTONDOWN, WM_XBUTTONDOWN,
    },
};

static CLICK_BUFFER: OnceLock<Arc<Mutex<VecDeque<Instant>>>> = OnceLock::new();

pub struct MouseActivityService {
    clicks: Arc<Mutex<VecDeque<Instant>>>,
    available: bool,
    #[cfg(windows)]
    hook_handle: usize,
}

impl MouseActivityService {
    pub fn new() -> Self {
        let clicks = CLICK_BUFFER
            .get_or_init(|| Arc::new(Mutex::new(VecDeque::new())))
            .clone();

        #[cfg(windows)]
        let (available, hook_handle) = {
            let handle = unsafe {
                SetWindowsHookExW(
                    WH_MOUSE_LL,
                    Some(low_level_mouse_proc),
                    GetModuleHandleW(null()),
                    0,
                )
            };

            (handle != 0 as HHOOK, handle as usize)
        };

        #[cfg(not(windows))]
        let available = false;

        Self {
            clicks,
            available,
            #[cfg(windows)]
            hook_handle,
        }
    }

    pub fn get_snapshot(&self) -> MouseActivitySnapshot {
        if !self.available {
            return MouseActivitySnapshot::unavailable();
        }

        let now = Instant::now();
        let mut clicks = self.clicks.lock().expect("mouse activity mutex poisoned");
        prune_expired_clicks(&mut clicks, now);

        let last_minute_threshold = now - Duration::from_secs(60);
        let clicks_last_minute = clicks
            .iter()
            .filter(|recorded_at| **recorded_at >= last_minute_threshold)
            .count();
        let clicks_last_five_minutes = clicks.len();
        let work_state = resolve_work_state(clicks_last_minute, clicks_last_five_minutes);

        MouseActivitySnapshot {
            clicks_last_minute,
            clicks_last_five_minutes,
            work_state,
            work_state_text: work_state_label(work_state).to_string(),
        }
    }
}

impl Drop for MouseActivityService {
    fn drop(&mut self) {
        #[cfg(windows)]
        if self.hook_handle != 0 {
            unsafe {
                UnhookWindowsHookEx(self.hook_handle as HHOOK);
            }
        }
    }
}

#[cfg(windows)]
unsafe extern "system" fn low_level_mouse_proc(ncode: i32, wparam: usize, lparam: isize) -> isize {
    if ncode == HC_ACTION as i32 && is_click_message(wparam as u32) {
        if let Some(clicks) = CLICK_BUFFER.get() {
            let now = Instant::now();
            if let Ok(mut buffer) = clicks.lock() {
                buffer.push_back(now);
                prune_expired_clicks(&mut buffer, now);
            }
        }
    }

    unsafe { CallNextHookEx(std::ptr::null_mut(), ncode, wparam, lparam) }
}

#[cfg(windows)]
fn is_click_message(message: u32) -> bool {
    matches!(
        message,
        WM_LBUTTONDOWN | WM_RBUTTONDOWN | WM_MBUTTONDOWN | WM_XBUTTONDOWN
    )
}

fn prune_expired_clicks(clicks: &mut VecDeque<Instant>, now: Instant) {
    let threshold = now - Duration::from_secs(60 * 5);
    while let Some(front) = clicks.front() {
        if *front < threshold {
            clicks.pop_front();
        } else {
            break;
        }
    }
}

fn resolve_work_state(
    clicks_last_minute: usize,
    clicks_last_five_minutes: usize,
) -> WorkIntensityState {
    if clicks_last_minute >= 30 || clicks_last_five_minutes >= 110 {
        return WorkIntensityState::RapidFire;
    }

    if clicks_last_minute >= 12 || clicks_last_five_minutes >= 45 {
        return WorkIntensityState::ActiveHandling;
    }

    if clicks_last_minute <= 2 && clicks_last_five_minutes <= 10 {
        return WorkIntensityState::DeepFocus;
    }

    WorkIntensityState::SteadyFlow
}

fn work_state_label(work_state: WorkIntensityState) -> &'static str {
    match work_state {
        WorkIntensityState::Unavailable => "监听未连接",
        WorkIntensityState::DeepFocus => "深度专注",
        WorkIntensityState::SteadyFlow => "平稳推进",
        WorkIntensityState::ActiveHandling => "高频处理",
        WorkIntensityState::RapidFire => "高速切换",
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn resolve_work_state_maps_deep_focus() {
        assert!(matches!(
            resolve_work_state(0, 0),
            WorkIntensityState::DeepFocus
        ));
        assert!(matches!(
            resolve_work_state(2, 10),
            WorkIntensityState::DeepFocus
        ));
    }

    #[test]
    fn resolve_work_state_maps_active_handling() {
        assert!(matches!(
            resolve_work_state(12, 20),
            WorkIntensityState::ActiveHandling
        ));
        assert!(matches!(
            resolve_work_state(8, 45),
            WorkIntensityState::ActiveHandling
        ));
    }

    #[test]
    fn resolve_work_state_maps_rapid_fire() {
        assert!(matches!(
            resolve_work_state(30, 20),
            WorkIntensityState::RapidFire
        ));
        assert!(matches!(
            resolve_work_state(10, 110),
            WorkIntensityState::RapidFire
        ));
    }

    #[test]
    fn resolve_work_state_maps_steady_flow() {
        assert!(matches!(
            resolve_work_state(5, 20),
            WorkIntensityState::SteadyFlow
        ));
    }

    #[test]
    fn work_state_label_matches_ui_copy() {
        assert_eq!(work_state_label(WorkIntensityState::DeepFocus), "深度专注");
        assert_eq!(work_state_label(WorkIntensityState::SteadyFlow), "平稳推进");
        assert_eq!(
            work_state_label(WorkIntensityState::ActiveHandling),
            "高频处理"
        );
        assert_eq!(work_state_label(WorkIntensityState::RapidFire), "高速切换");
    }
}
