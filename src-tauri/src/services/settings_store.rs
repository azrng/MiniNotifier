use std::{fs, io::ErrorKind, path::PathBuf};

use crate::{
    errors::{AppError, AppResult},
    models::HydrationSettingsDocument,
};

pub struct SettingsStore {
    file_path: PathBuf,
}

impl SettingsStore {
    pub fn new() -> AppResult<Self> {
        let local_data_dir = dirs::data_local_dir().ok_or(AppError::PathUnavailable)?;
        let app_data_dir = local_data_dir.join("MiniNotifier");
        let file_path = app_data_dir.join("hydration-settings.json");
        Ok(Self { file_path })
    }
    pub fn load(&self) -> AppResult<Option<HydrationSettingsDocument>> {
        if !self.file_path.exists() {
            return Ok(None);
        }

        let content = fs::read_to_string(&self.file_path).map_err(map_read_error)?;
        serde_json::from_str::<HydrationSettingsDocument>(&content)
            .map(Some)
            .map_err(|_| AppError::InvalidConfig)
    }

    pub fn save(&self, document: &HydrationSettingsDocument) -> AppResult<()> {
        let app_data_dir = self.file_path.parent().ok_or(AppError::PathUnavailable)?;
        fs::create_dir_all(app_data_dir).map_err(map_write_error)?;

        let temp_path = self.file_path.with_extension("json.tmp");
        let payload =
            serde_json::to_string_pretty(document).map_err(|_| AppError::ConfigWriteFailed)?;

        fs::write(&temp_path, payload).map_err(map_write_error)?;
        fs::rename(&temp_path, &self.file_path).or_else(|_| {
            fs::copy(&temp_path, &self.file_path)
                .map(|_| ())
                .map_err(map_write_error)
                .and_then(|_| fs::remove_file(&temp_path).map_err(map_write_error))
        })
    }
}

fn map_read_error(error: std::io::Error) -> AppError {
    if error.kind() == ErrorKind::PermissionDenied {
        AppError::NoPermission
    } else {
        AppError::ConfigReadFailed
    }
}

fn map_write_error(error: std::io::Error) -> AppError {
    if error.kind() == ErrorKind::PermissionDenied {
        AppError::NoPermission
    } else {
        AppError::ConfigWriteFailed
    }
}
