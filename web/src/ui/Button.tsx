import { cva, type VariantProps } from 'class-variance-authority';
import type { ButtonHTMLAttributes } from 'react';
import { cn } from './cn';

// Press feedback lives in the base so every variant feels responsive (emil-design-eng, mobile-native §5).
const button = cva(
  'inline-flex items-center justify-center gap-2 rounded-xl font-medium select-none ' +
    'transition-[transform,background-color,opacity] duration-[120ms] ease-out active:scale-[0.97] ' +
    'disabled:pointer-events-none disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
  {
    variants: {
      intent: {
        primary: 'bg-accent text-on-fill hover:opacity-90',
        secondary: 'bg-surface text-fg hover:bg-surface/70',
      },
      size: {
        sm: 'h-9 px-3 text-sm',
        md: 'h-11 px-4 text-base',
      },
    },
    defaultVariants: { intent: 'primary', size: 'md' },
  },
);

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & VariantProps<typeof button>;

export function Button({ className, intent, size, type = 'button', ...props }: ButtonProps) {
  return <button type={type} className={cn(button({ intent, size }), className)} {...props} />;
}
