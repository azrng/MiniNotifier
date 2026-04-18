import type { ReactNode } from "react";
import { Button } from "./Button";

type StatePanelProps = {
  eyebrow: string;
  title: string;
  description: string;
  primaryLabel?: string;
  secondaryLabel?: string;
  onPrimaryClick?: () => void;
  onSecondaryClick?: () => void;
  footer?: ReactNode;
};

export function StatePanel({
  eyebrow,
  title,
  description,
  primaryLabel,
  secondaryLabel,
  onPrimaryClick,
  onSecondaryClick,
  footer
}: StatePanelProps) {
  return (
    <div className="flex min-h-screen items-center justify-center px-6 py-10">
      <div className="glass-panel relative w-full max-w-2xl overflow-hidden rounded-[32px] p-8">
        <div className="pointer-events-none absolute inset-x-6 top-0 h-px bg-gradient-to-r from-transparent via-brand-300 to-transparent" />
        <div className="mb-6 text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">{eyebrow}</div>
        <div className="space-y-3">
          <h1 className="text-3xl font-semibold tracking-tight text-slate-950">{title}</h1>
          <p className="max-w-xl text-sm leading-7 text-slate-600">{description}</p>
        </div>
        <div className="mt-8 flex flex-wrap gap-3">
          {primaryLabel ? <Button onClick={onPrimaryClick}>{primaryLabel}</Button> : null}
          {secondaryLabel ? (
            <Button variant="secondary" onClick={onSecondaryClick}>
              {secondaryLabel}
            </Button>
          ) : null}
        </div>
        {footer ? <div className="mt-8 text-sm text-slate-500">{footer}</div> : null}
      </div>
    </div>
  );
}
