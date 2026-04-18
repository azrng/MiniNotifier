import { invoke } from "@tauri-apps/api/core";
import { commandErrorSchema, hydrationSettingsSchema, reminderPayloadSchema } from "../../schemas/hydration";
import type { CommandError, HydrationSettings, ReminderPayload } from "../../schemas/hydration";

type SaveHydrationSettingsInput = {
  isReminderEnabled: boolean;
  isPaused: boolean;
  reminderIntervalMinutes: number;
  autoCloseSeconds: number;
  startupEnabled: boolean;
};

function parseCommandError(error: unknown): CommandError {
  if (typeof error === "string") {
    try {
      return commandErrorSchema.parse(JSON.parse(error));
    } catch {
      return { code: "UNKNOWN", message: error };
    }
  }

  try {
    return commandErrorSchema.parse(error);
  } catch {
    return { code: "UNKNOWN", message: "发生了未识别的本地错误。" };
  }
}

async function invokeWithSchema<T>(
  command: string,
  parser: { parse: (value: unknown) => T },
  args?: Record<string, unknown>
) {
  try {
    const result = await invoke(command, args);
    return parser.parse(result);
  } catch (error) {
    throw parseCommandError(error);
  }
}

export async function loadHydrationSettings() {
  return invokeWithSchema("get_hydration_settings", hydrationSettingsSchema);
}

export async function saveHydrationSettings(input: SaveHydrationSettingsInput) {
  return invokeWithSchema("save_hydration_settings", hydrationSettingsSchema, { input });
}

export async function toggleHydrationPause() {
  return invokeWithSchema("toggle_hydration_pause", hydrationSettingsSchema);
}

export async function showHydrationPreview() {
  return invokeWithSchema("show_hydration_preview", hydrationSettingsSchema);
}

export async function getCurrentReminderPayload() {
  return invokeWithSchema("get_current_reminder_payload", reminderPayloadSchema);
}

export async function dismissReminder() {
  try {
    await invoke("dismiss_reminder");
  } catch (error) {
    throw parseCommandError(error);
  }
}
