use serde::Serialize;
use thiserror::Error;

pub type AppResult<T> = Result<T, AppError>;

#[derive(Debug, Error)]
pub enum AppError {
    #[error("当前没有权限写入提醒配置。")]
    NoPermission,
    #[error("提醒配置文件内容无效。")]
    InvalidConfig,
    #[error("无法读取本地提醒配置。")]
    ConfigReadFailed,
    #[error("无法保存本地提醒配置。")]
    ConfigWriteFailed,
    #[error("无法同步开机自启动状态。")]
    AutoStartFailed,
    #[error("提醒窗口当前不可用。")]
    WindowUnavailable,
    #[error("系统本地目录不可用。")]
    PathUnavailable,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct CommandError {
    pub code: &'static str,
    pub message: String,
}

impl From<AppError> for CommandError {
    fn from(value: AppError) -> Self {
        let code = match value {
            AppError::NoPermission => "NO_PERMISSION",
            AppError::InvalidConfig => "INVALID_CONFIG",
            AppError::ConfigReadFailed => "CONFIG_READ_FAILED",
            AppError::ConfigWriteFailed => "CONFIG_WRITE_FAILED",
            AppError::AutoStartFailed => "AUTOSTART_FAILED",
            AppError::WindowUnavailable => "WINDOW_UNAVAILABLE",
            AppError::PathUnavailable => "PATH_UNAVAILABLE",
        };

        Self {
            code,
            message: value.to_string(),
        }
    }
}
