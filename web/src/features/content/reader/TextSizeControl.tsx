import { Popover } from '@base-ui/react/popover';
import { cn } from '../../../ui/cn';
import { setTextSize, type TextSize } from './readerStore';
import { useTextSize } from './useTextSize';

const SIZES: { value: TextSize; label: string; sample: string }[] = [
  { value: 's', label: 'Small', sample: 'text-[13px]' },
  { value: 'm', label: 'Medium', sample: 'text-[16px]' },
  { value: 'l', label: 'Large', sample: 'text-[20px]' },
];

/** "Aa": reading text size for this device. A setting, so it changes instantly (no animation). */
export function TextSizeControl() {
  const size = useTextSize();
  return (
    <Popover.Root>
      <Popover.Trigger
        aria-label="Text size"
        className={cn(
          'inline-flex h-10 items-center rounded-full border border-rule bg-surface px-3.5 font-medium select-none',
          'transition-transform duration-[120ms] ease-out active:scale-[0.97]',
          'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
        )}
      >
        <span className="text-[13px]">A</span>
        <span className="text-[17px]">a</span>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Positioner side="bottom" align="start" sideOffset={8} className="z-40">
          <Popover.Popup className="term-card">
            <Popover.Title className="font-mono text-[12px] tracking-[0.08em] text-muted">
              text size
            </Popover.Title>
            <div role="radiogroup" aria-label="Text size" className="mt-2 mb-1 flex gap-1">
              {SIZES.map((option) => (
                <button
                  key={option.value}
                  type="button"
                  role="radio"
                  aria-checked={size === option.value}
                  aria-label={option.label}
                  onClick={() => setTextSize(option.value)}
                  className={cn(
                    'flex size-11 items-center justify-center rounded-lg border font-semibold select-none',
                    'transition-[transform,background-color,border-color] duration-[120ms] ease-out active:scale-[0.95]',
                    'focus-visible:outline-2 focus-visible:outline-accent',
                    option.sample,
                    size === option.value
                      ? 'border-accent bg-accent/10 text-accent'
                      : 'border-rule text-fg hover:bg-fg/[0.04]',
                  )}
                >
                  A
                </button>
              ))}
            </div>
          </Popover.Popup>
        </Popover.Positioner>
      </Popover.Portal>
    </Popover.Root>
  );
}
