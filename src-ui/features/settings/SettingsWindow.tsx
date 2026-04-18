import { useEffect, useMemo, useState } from "react";
import { listen } from "@tauri-apps/api/event";
import { useMutation, useQuery } from "@tanstack/react-query";
import type { QueryClient } from "@tanstack/react-query";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "../../components/ui/Button";
import { StatePanel } from "../../components/ui/StatePanel";
import { MetricCard } from "../../components/business/MetricCard";
import {
  loadHydrationSettings,
  saveHydrationSettings,
  showHydrationPreview,
  toggleHydrationPause
} from "../../lib/tauri/hydration";
import { hydrationSettingsFormSchema } from "../../schemas/hydration";
import type { CommandError, HydrationSettings, HydrationSettingsFormValues } from "../../schemas/hydration";

const SETTINGS_QUERY_KEY = ["hydration-settings"];

type SettingsWindowProps = {
  queryClient: QueryClient;
};

function formatTime(value: string | null, fallback: string) {
  if (!value) {
    return fallback;
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return fallback;
  }

  return new Intl.DateTimeFormat("zh-CN", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false
  }).format(date);
}

function formatRelativeLabel(value: string | null) {
  if (!value) {
    return "等待下一次计算";
  }

  const diffMs = new Date(value).getTime() - Date.now();
  const diffMinutes = Math.max(0, Math.round(diffMs / 60000));

  if (diffMinutes <= 1) {
    return "即将提醒";
  }

  return `还有 ${diffMinutes} 分钟`;
}

function createDefaultValues(settings?: HydrationSettings): HydrationSettingsFormValues {
  return {
    isReminderEnabled: settings?.isReminderEnabled ?? true,
    reminderIntervalMinutes: settings?.reminderIntervalMinutes ?? 30,
    autoCloseSeconds: settings?.autoCloseSeconds ?? 15,
    startupEnabled: settings?.startupSettings.isEnabled ?? false
  };
}

export function SettingsWindow({ queryClient }: SettingsWindowProps) {
  const [showFirstRunPanel, setShowFirstRunPanel] = useState(true);

  const settingsQuery = useQuery({
    queryKey: SETTINGS_QUERY_KEY,
    queryFn: loadHydrationSettings
  });

  const form = useForm<HydrationSettingsFormValues>({
    resolver: zodResolver(hydrationSettingsFormSchema),
    defaultValues: createDefaultValues()
  });

  useEffect(() => {
    if (!settingsQuery.data) {
      return;
    }

    form.reset(createDefaultValues(settingsQuery.data));
    if (settingsQuery.data.saveStateText !== "已加载默认配置") {
      setShowFirstRunPanel(false);
    }
  }, [form, settingsQuery.data]);

  useEffect(() => {
    const unlistenPromise = listen<HydrationSettings>("settings-updated", (event) => {
      queryClient.setQueryData(SETTINGS_QUERY_KEY, event.payload);
    });

    return () => {
      void unlistenPromise.then((unlisten) => unlisten());
    };
  }, [queryClient]);

  const saveMutation = useMutation({
    mutationFn: async (values: HydrationSettingsFormValues) => {
      if (!settingsQuery.data) {
        throw { code: "UNKNOWN", message: "当前设置尚未载入。" } satisfies CommandError;
      }

      return saveHydrationSettings({
        isReminderEnabled: values.isReminderEnabled,
        isPaused: settingsQuery.data.isPaused,
        reminderIntervalMinutes: values.reminderIntervalMinutes,
        autoCloseSeconds: values.autoCloseSeconds,
        startupEnabled: values.startupEnabled
      });
    },
    onSuccess: (result) => {
      queryClient.setQueryData(SETTINGS_QUERY_KEY, result);
      form.reset(createDefaultValues(result));
    }
  });

  const previewMutation = useMutation({
    mutationFn: showHydrationPreview,
    onSuccess: (result) => {
      queryClient.setQueryData(SETTINGS_QUERY_KEY, result);
    }
  });

  const pauseMutation = useMutation({
    mutationFn: toggleHydrationPause,
    onSuccess: (result) => {
      queryClient.setQueryData(SETTINGS_QUERY_KEY, result);
      form.reset(createDefaultValues(result));
    }
  });

  const mutationError =
    (saveMutation.error as CommandError | null) ??
    (previewMutation.error as CommandError | null) ??
    (pauseMutation.error as CommandError | null);

  const viewError = settingsQuery.error as CommandError | null;

  const heroTitle = useMemo(() => {
    if (!settingsQuery.data) {
      return "等待载入";
    }

    if (!settingsQuery.data.isReminderEnabled) {
      return "提醒已关闭";
    }

    return settingsQuery.data.isPaused ? "提醒已暂停" : "提醒运行中";
  }, [settingsQuery.data]);

  const heroHint = useMemo(() => {
    if (!settingsQuery.data) {
      return "正在同步你的提醒空间。";
    }

    if (!settingsQuery.data.isReminderEnabled) {
      return "托盘仍会保留，等你随时恢复。";
    }

    return settingsQuery.data.isPaused
      ? "后台调度已经停住，但你的配置仍会保留。"
      : "托盘、配置和下一次提醒时间会保持联动。";
  }, [settingsQuery.data]);

  if (settingsQuery.isLoading) {
    return (
      <StatePanel
        eyebrow="Loading"
        title="正在同步你的提醒空间"
        description="MiniNotifier 正在读取本地配置、恢复托盘状态，并准备下一次提醒节奏。"
      />
    );
  }

  if (viewError?.code === "NO_PERMISSION") {
    return (
      <StatePanel
        eyebrow="No Permission"
        title="当前无法写入本地提醒配置"
        description="请检查 `%LocalAppData%/MiniNotifier` 的权限，确认当前用户可以写入后再重试。"
        primaryLabel="重新加载"
        secondaryLabel="关闭说明"
        onPrimaryClick={() => void settingsQuery.refetch()}
      />
    );
  }

  if (settingsQuery.isError || !settingsQuery.data) {
    return (
      <StatePanel
        eyebrow="Error"
        title="提醒配置加载失败"
        description={viewError?.message ?? "当前未能读取本地配置，请稍后重试。"}
        primaryLabel="重新加载"
        onPrimaryClick={() => void settingsQuery.refetch()}
        footer="如果问题持续存在，建议检查现有 WPF 配置文件是否损坏。"
      />
    );
  }

  if (showFirstRunPanel && settingsQuery.data.saveStateText === "已加载默认配置") {
    return (
      <StatePanel
        eyebrow="Empty"
        title="还没有发现你的本地提醒配置"
        description="已经按设计文档准备好默认节奏：每 30 分钟提醒一次，弹窗 15 秒后自动收起。你可以先直接使用，也可以继续进入设置页微调。"
        primaryLabel="打开设置表单"
        secondaryLabel="使用默认配置继续"
        onPrimaryClick={() => setShowFirstRunPanel(false)}
        onSecondaryClick={() => setShowFirstRunPanel(false)}
      />
    );
  }

  const settings = settingsQuery.data;

  return (
    <main className="min-h-screen px-4 py-6 text-slate-900 sm:px-6">
      <div className="mx-auto max-w-6xl">
        <div className="glass-panel relative overflow-hidden rounded-[36px] p-6 sm:p-8">
          <div className="pointer-events-none absolute inset-0 bg-frost-grid bg-[length:42px_42px] opacity-20" />
          <div className="relative">
            <header className="flex flex-col gap-6 border-b border-white/70 pb-6 lg:flex-row lg:items-end lg:justify-between">
              <div className="max-w-2xl">
                <div className="text-xs font-semibold uppercase tracking-[0.32em] text-brand-700">MiniNotifier</div>
                <h1 className="mt-3 text-3xl font-semibold tracking-tight sm:text-4xl">
                  Tauri 渐进迁移设置页
                </h1>
                <p className="mt-3 text-sm leading-7 text-slate-600">
                  保留原有托盘提醒节奏，同时把配置链路迁到 Rust 与 React。界面强调清晰、冷静和可扫描的桌面效率感。
                </p>
              </div>
              <div className="rounded-[24px] border border-white/70 bg-white/70 px-5 py-4 text-sm text-slate-600 shadow-[var(--mn-shadow-sm)]">
                <div className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500">保存状态</div>
                <div className="mt-2 text-lg font-semibold text-slate-900">{settings.saveStateText}</div>
              </div>
            </header>

            <section className="mt-6 grid gap-4 lg:grid-cols-[1.2fr_1fr_1fr]">
              <section className="rounded-[30px] bg-slate-950 px-6 py-6 text-white shadow-shell">
                <div className="text-xs font-semibold uppercase tracking-[0.26em] text-brand-200">当前节奏</div>
                <div className="mt-4 text-4xl font-semibold tracking-tight">{heroTitle}</div>
                <p className="mt-3 max-w-xl text-sm leading-7 text-slate-300">{heroHint}</p>
                <div className="mt-6 flex flex-wrap gap-3 text-sm text-slate-300">
                  <span className="rounded-full border border-white/10 bg-white/5 px-3 py-1.5">
                    {settings.reminderIntervalMinutes} 分钟 / 次
                  </span>
                  <span className="rounded-full border border-white/10 bg-white/5 px-3 py-1.5">
                    自动关闭 {settings.autoCloseSeconds} 秒
                  </span>
                  <span className="rounded-full border border-white/10 bg-white/5 px-3 py-1.5">
                    {settings.startupSettings.statusText}
                  </span>
                </div>
              </section>
              <MetricCard
                accent="blue"
                title="下次提醒"
                value={formatTime(settings.nextReminderAt, "--:--")}
                hint={formatRelativeLabel(settings.nextReminderAt)}
              />
              <MetricCard
                accent="green"
                title="上次提醒"
                value={formatTime(settings.lastReminderAt, "--:--")}
                hint={settings.lastReminderAt ? "最近一次弹窗已记录" : "今天还没有提醒"}
              />
            </section>

            <section className="mt-6 grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
              <form
                className="rounded-[30px] border border-white/70 bg-white/78 p-6 shadow-card"
                onSubmit={form.handleSubmit((values) => void saveMutation.mutateAsync(values))}
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="text-xs font-semibold uppercase tracking-[0.24em] text-brand-700">
                      提醒设置
                    </div>
                    <h2 className="mt-2 text-2xl font-semibold tracking-tight text-slate-950">
                      让托盘、调度和提醒弹窗保持同一份真相
                    </h2>
                  </div>
                  <div className="rounded-2xl bg-brand-50 px-4 py-3 text-right text-sm text-brand-900">
                    <div className="text-xs uppercase tracking-[0.2em] text-brand-700">状态</div>
                    <div className="mt-1 font-semibold">{settings.isPaused ? "暂停中" : "运行中"}</div>
                  </div>
                </div>

                <div className="mt-8 grid gap-5 md:grid-cols-2">
                  <label className="rounded-[24px] border border-slate-200 bg-slate-50/80 p-5">
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <div className="text-sm font-semibold text-slate-900">喝水提醒</div>
                        <div className="mt-1 text-sm leading-6 text-slate-500">
                          关闭后仍保留托盘入口，但不再触发新的提醒弹窗。
                        </div>
                      </div>
                      <input
                        className="mt-1 h-5 w-5 rounded border-slate-300 text-brand-700 focus:ring-brand-300"
                        type="checkbox"
                        {...form.register("isReminderEnabled")}
                      />
                    </div>
                  </label>

                  <label className="rounded-[24px] border border-slate-200 bg-slate-50/80 p-5">
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <div className="text-sm font-semibold text-slate-900">开机自启动</div>
                        <div className="mt-1 text-sm leading-6 text-slate-500">
                          通过 Rust 侧接入系统自启动，和提醒总开关互不耦合。
                        </div>
                      </div>
                      <input
                        className="mt-1 h-5 w-5 rounded border-slate-300 text-brand-700 focus:ring-brand-300"
                        type="checkbox"
                        {...form.register("startupEnabled")}
                      />
                    </div>
                  </label>

                  <label className="rounded-[24px] border border-slate-200 bg-slate-50/80 p-5">
                    <div className="text-sm font-semibold text-slate-900">提醒间隔</div>
                    <div className="mt-1 text-sm leading-6 text-slate-500">允许范围 5 至 240 分钟。</div>
                    <div className="mt-4 flex items-center gap-3">
                      <input
                        className="h-12 w-full rounded-2xl border border-slate-200 bg-white px-4 text-lg font-semibold text-slate-900 shadow-[var(--mn-shadow-sm)] outline-none transition focus:border-brand-300"
                        type="number"
                        min={5}
                        max={240}
                        {...form.register("reminderIntervalMinutes")}
                      />
                      <span className="text-sm font-semibold text-slate-500">分钟</span>
                    </div>
                    <div className="mt-2 text-sm text-red-600">
                      {form.formState.errors.reminderIntervalMinutes?.message}
                    </div>
                  </label>

                  <label className="rounded-[24px] border border-slate-200 bg-slate-50/80 p-5">
                    <div className="text-sm font-semibold text-slate-900">自动关闭</div>
                    <div className="mt-1 text-sm leading-6 text-slate-500">允许范围 3 至 15 秒。</div>
                    <div className="mt-4 flex items-center gap-3">
                      <input
                        className="h-12 w-full rounded-2xl border border-slate-200 bg-white px-4 text-lg font-semibold text-slate-900 shadow-[var(--mn-shadow-sm)] outline-none transition focus:border-brand-300"
                        type="number"
                        min={3}
                        max={15}
                        {...form.register("autoCloseSeconds")}
                      />
                      <span className="text-sm font-semibold text-slate-500">秒</span>
                    </div>
                    <div className="mt-2 text-sm text-red-600">
                      {form.formState.errors.autoCloseSeconds?.message}
                    </div>
                  </label>
                </div>

                {mutationError ? (
                  <div className="mt-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                    {mutationError.message}
                  </div>
                ) : null}

                <div className="mt-8 flex flex-wrap gap-3">
                  <Button
                    variant="secondary"
                    onClick={() => void previewMutation.mutateAsync()}
                    disabled={previewMutation.isPending}
                  >
                    {previewMutation.isPending ? "正在触发提醒..." : "测试提醒"}
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => void pauseMutation.mutateAsync()}
                    disabled={pauseMutation.isPending}
                  >
                    {pauseMutation.isPending
                      ? "正在同步状态..."
                      : settings.isPaused
                        ? "恢复提醒"
                        : "暂停提醒"}
                  </Button>
                  <Button type="submit" disabled={saveMutation.isPending}>
                    {saveMutation.isPending ? "保存中..." : "保存设置"}
                  </Button>
                </div>
              </form>

              <aside className="space-y-5">
                <section className="rounded-[30px] border border-white/70 bg-white/78 p-6 shadow-card">
                  <div className="text-xs font-semibold uppercase tracking-[0.24em] text-brand-700">迁移状态</div>
                  <h2 className="mt-3 text-2xl font-semibold tracking-tight text-slate-950">
                    当前切片已经把设置链路迁到 Tauri
                  </h2>
                  <ul className="mt-5 space-y-3 text-sm leading-7 text-slate-600">
                    <li>托盘菜单、暂停/恢复、立即提醒与退出动作由 Rust 托管。</li>
                    <li>配置文件继续兼容 `%LocalAppData%/MiniNotifier/hydration-settings.json`。</li>
                    <li>设置页通过 Command 读写真实配置，不再依赖 WPF ViewModel。</li>
                  </ul>
                </section>
                <MetricCard
                  accent="amber"
                  title="关闭提示"
                  value="托盘常驻"
                  hint="关闭设置窗口不会退出应用，核心调度会在后台继续运行。"
                  footer="这是沿用原 WPF 版的行为约定。"
                />
              </aside>
            </section>
          </div>
        </div>
      </div>
    </main>
  );
}
