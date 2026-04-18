import type { ReactNode } from "react";

type MetricCardProps = {
  title: string;
  value: string;
  hint: string;
  accent?: "blue" | "green" | "amber";
  footer?: ReactNode;
};

const accentClassMap = {
  blue: "from-brand-700/14 to-brand-300/8 text-brand-900",
  green: "from-emerald-600/16 to-emerald-200/10 text-emerald-900",
  amber: "from-amber-500/18 to-amber-100/12 text-amber-900"
};

export function MetricCard({ title, value, hint, accent = "blue", footer }: MetricCardProps) {
  return (
    <section
      className={`rounded-[28px] border border-white/70 bg-gradient-to-br ${accentClassMap[accent]} p-5 shadow-card`}
    >
      <div className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">{title}</div>
      <div className="mt-4 text-3xl font-semibold tracking-tight">{value}</div>
      <div className="mt-2 text-sm text-slate-600">{hint}</div>
      {footer ? <div className="mt-5 text-sm text-slate-500">{footer}</div> : null}
    </section>
  );
}
