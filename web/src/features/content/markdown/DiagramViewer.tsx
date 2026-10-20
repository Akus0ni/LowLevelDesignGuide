import { Dialog } from '@base-ui/react/dialog';
import { cn } from '../../../ui/cn';
import { Icon } from '../../../ui/Icon';
import { InteractiveDiagram } from './InteractiveDiagram';
import { atNaturalSize } from './mermaid';

const expandIcon = ['M15 3h6v6', 'M9 21H3v-6', 'M21 3l-7 7', 'M3 21l7-7'];
const closeIcon = ['M18 6 6 18', 'm6 6 12 12'];

/**
 * A diagram fits the reading column so its whole shape is visible; "Expand" opens it full screen at natural size,
 * scrollable both ways (and pinch-zoomable: the app never disables zoom). Base UI's Dialog handles focus and Esc.
 */
export function DiagramViewer({
  svg,
  source,
  steps,
  label,
}: {
  svg: string;
  source: string;
  steps?: boolean;
  label: string;
}) {
  return (
    <Dialog.Root>
      <Dialog.Trigger
        aria-label={`Expand ${label}`}
        className={cn(
          '-my-1 -mr-2 inline-flex h-8 items-center gap-1.5 rounded-md px-2 font-mono text-[11px] tracking-[0.06em] text-muted select-none',
          'transition-[transform,color] duration-[120ms] ease-out active:scale-[0.94] hover:text-fg',
          'focus-visible:outline-2 focus-visible:outline-accent',
        )}
      >
        <Icon d={expandIcon} size={14} />
        expand
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Backdrop className="viewer-backdrop" />
        <Dialog.Popup className="viewer">
          <div className="flex items-center justify-between border-b border-rule px-4 py-2 pt-[calc(0.5rem+env(safe-area-inset-top,0px))]">
            <Dialog.Title className="font-mono text-[12px] tracking-[0.08em] text-muted">
              {label}
            </Dialog.Title>
            <Dialog.Close
              aria-label="Close"
              className={cn(
                'inline-flex size-10 items-center justify-center rounded-lg text-muted select-none',
                'transition-[transform,color] duration-[120ms] ease-out active:scale-[0.94] hover:text-fg',
                'focus-visible:outline-2 focus-visible:outline-accent',
              )}
            >
              <Icon d={closeIcon} size={20} />
            </Dialog.Close>
          </div>
          <InteractiveDiagram
            svg={atNaturalSize(svg)}
            source={source}
            steps={steps}
            label={label}
            showLabel={false}
            className="flex min-h-0 flex-1 flex-col"
            canvasClassName="viewer-canvas"
          />
        </Dialog.Popup>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
