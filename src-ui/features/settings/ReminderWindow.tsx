import { useEffect, useState } from "react";
import { listen } from "@tauri-apps/api/event";
import { Button } from "../../components/ui/Button";
import { dismissReminder, getCurrentReminderPayload } from "../../lib/tauri/hydration";
import type { ReminderPayload } from "../../schemas/hydration";

export function ReminderWindow() {
  const [payload, setPayload] = useState<ReminderPayload | null>(null);
  const [secondsRemaining, setSecondsRemaining] = useState(0);

  useEffect(() => {
    const loadPayload = async () => {
      const nextPayload = await getCurrentReminderPayload();
      setPayload(nextPayload);
      setSecondsRemaining(nextPayload.autoCloseSeconds);
    };

    void loadPayload();

    const unlistenPromise = listen<ReminderPayload>("reminder-updated", (event) => {
      setPayload(event.payload);
      setSecondsRemaining(event.payload.autoCloseSeconds);
    });

    return () => {
      void unlistenPromise.then((unlisten) => unlisten());
    };
  }, []);

  useEffect(() => {
    if (!payload || secondsRemaining <= 0) {
      if (payload && secondsRemaining <= 0) {
        void dismissReminder();
      }

      return;
    }

    const timer = window.setTimeout(() => {
      setSecondsRemaining((current) => current - 1);
    }, 1000);

    return () => window.clearTimeout(timer);
  }, [payload, secondsRemaining]);

  if (!payload) {
    return (
      <div className="flex min-h-screen items-center justify-center p-4">
        <div className="glass-panel w-full max-w-sm rounded-[30px] p-6 text-sm text-slate-600">
          正在准备提醒内容...
        </div>
      </div>
    );
  }

  return (
    <main className="min-h-screen bg-transparent p-4 text-slate-950">
      <section className="glass-panel relative overflow-hidden rounded-[30px] p-6 shadow-shell">
        <div className="pointer-events-none absolute inset-0 bg-gradient-to-br from-brand-100/80 via-white/35 to-sky-100/50" />
        <div className="relative">
          <div className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Drink Water</div>
          <h1 className="mt-4 text-3xl font-semibold tracking-tight text-slate-950">{payload.title}</h1>
          <p className="mt-3 text-base font-medium text-slate-700">{payload.headline}</p>
          <p className="mt-4 text-sm leading-7 text-slate-600">{payload.message}</p>
          <div className="mt-6 flex items-center justify-between gap-4">
            <div className="rounded-full border border-white/70 bg-white/75 px-4 py-2 text-sm font-medium text-slate-600">
              {secondsRemaining} 秒后自动关闭
            </div>
            <Button variant="secondary" size="sm" onClick={() => void dismissReminder()}>
              关闭
            </Button>
          </div>
        </div>
      </section>
    </main>
  );
}
