import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "../../lib/utils/cn";

const buttonVariants = cva(
  "inline-flex items-center justify-center rounded-2xl border text-sm font-semibold transition duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-300 disabled:cursor-not-allowed disabled:opacity-60",
  {
    variants: {
      variant: {
        primary:
          "border-brand-700 bg-brand-700 px-4 py-3 text-white shadow-card hover:border-brand-800 hover:bg-brand-800",
        secondary:
          "border-white/70 bg-white/70 px-4 py-3 text-slate-800 shadow-[var(--mn-shadow-sm)] hover:border-brand-200 hover:bg-brand-50",
        ghost:
          "border-transparent bg-transparent px-3 py-2 text-slate-600 hover:bg-slate-100/80 hover:text-slate-900"
      },
      size: {
        md: "h-11 min-w-[104px]",
        sm: "h-9 min-w-[88px]"
      }
    },
    defaultVariants: {
      variant: "primary",
      size: "md"
    }
  }
);

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants>;

export function Button({ className, size, variant, type = "button", ...props }: ButtonProps) {
  return <button className={cn(buttonVariants({ size, variant }), className)} type={type} {...props} />;
}
