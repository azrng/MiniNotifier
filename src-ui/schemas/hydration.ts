import { z } from "zod";

export const startupSettingsSchema = z.object({
  isEnabled: z.boolean(),
  statusText: z.string()
});

export const hydrationSettingsSchema = z.object({
  isReminderEnabled: z.boolean(),
  isPaused: z.boolean(),
  reminderIntervalMinutes: z.number().int().min(5).max(240),
  autoCloseSeconds: z.number().int().min(3).max(15),
  lastReminderAt: z.string().nullable(),
  nextReminderAt: z.string().nullable(),
  saveStateText: z.string(),
  startupSettings: startupSettingsSchema
});

export const mouseActivitySnapshotSchema = z.object({
  clicksLastMinute: z.number().int().min(0),
  clicksLastFiveMinutes: z.number().int().min(0),
  workState: z.enum(["unavailable", "deepFocus", "steadyFlow", "activeHandling", "rapidFire"]),
  workStateText: z.string()
});

export const hydrationSettingsFormSchema = z.object({
  isReminderEnabled: z.boolean(),
  reminderIntervalMinutes: z.coerce.number().int().min(5).max(240),
  autoCloseSeconds: z.coerce.number().int().min(3).max(15),
  startupEnabled: z.boolean()
});

export const reminderPayloadSchema = z.object({
  title: z.string(),
  headline: z.string(),
  message: z.string(),
  autoCloseSeconds: z.number().int().min(3).max(15)
});

export const commandErrorSchema = z.object({
  code: z.string(),
  message: z.string()
});

export type HydrationSettings = z.infer<typeof hydrationSettingsSchema>;
export type HydrationSettingsFormValues = z.infer<typeof hydrationSettingsFormSchema>;
export type MouseActivitySnapshot = z.infer<typeof mouseActivitySnapshotSchema>;
export type ReminderPayload = z.infer<typeof reminderPayloadSchema>;
export type CommandError = z.infer<typeof commandErrorSchema>;
