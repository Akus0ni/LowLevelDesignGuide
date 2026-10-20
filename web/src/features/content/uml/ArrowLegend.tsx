import { Drawer } from '@base-ui/react/drawer';
import { Popover } from '@base-ui/react/popover';
import { useEffect, useState, useSyncExternalStore } from 'react';
import { Icon } from '../../../ui/Icon';
import { useMediaQuery } from '../../../ui/useMediaQuery';
import { currentColorScheme, renderMermaid } from '../markdown/mermaid';
import { stripButton } from './stripButton';
import { LEGEND_ARROWS, LEGEND_HINT, legendSource, type LegendArrow } from './legendArrows';

/** Wide enough for a popover beside the diagram; narrower screens get a bottom sheet. */
const WIDE = '(min-width: 48rem)';
const arrowsIcon = ['M4 7h12', 'm12 3 4 4-4 4', 'M20 17H8', 'm12 13-4 4 4 4'];

/**
 * The arrow legend (spec 008 §3.5): what each of the six class-diagram arrows means and the C# behind it. Inline
 * where a lesson asks for it (```` ```uml-arrows ````), and one tap away from every class diagram.
 */
export function ArrowLegend() {
  return (
    <figure className="arrow-legend" aria-label="Class diagram arrows">
      <figcaption className="arrow-legend-hint">{LEGEND_HINT}</figcaption>
      <ul className="arrow-legend-list">
        {LEGEND_ARROWS.map((arrow) => (
          <LegendRow key={arrow.name} arrow={arrow} />
        ))}
      </ul>
    </figure>
  );
}

function LegendRow({ arrow }: { arrow: LegendArrow }) {
  const svg = useLegendSvg(arrow);
  return (
    <li className="arrow-legend-row">
      <div className="arrow-legend-drawing" aria-hidden="true">
        {svg ? (
          // Mermaid's strict security level sanitises the SVG.
          <div dangerouslySetInnerHTML={{ __html: svg }} />
        ) : (
          <span className="font-mono text-[12px] text-muted">{arrow.diagram}</span>
        )}
      </div>
      <div className="min-w-0">
        <p className="arrow-legend-name">
          {arrow.name} <span className="arrow-legend-meaning">{arrow.meaning}</span>
        </p>
        <code className="arrow-legend-code">{arrow.code}</code>
      </div>
    </li>
  );
}

function subscribeToScheme(onChange: () => void): () => void {
  const query = window.matchMedia('(prefers-color-scheme: dark)');
  query.addEventListener('change', onChange);
  return () => query.removeEventListener('change', onChange);
}

function useLegendSvg(arrow: LegendArrow): string | null {
  const scheme = useSyncExternalStore(subscribeToScheme, currentColorScheme, () => 'light');
  const [svg, setSvg] = useState<string | null>(null);
  useEffect(() => {
    let current = true;
    const id = `legend-${arrow.name.toLowerCase()}-${scheme}`;
    renderMermaid(id, legendSource(arrow)).then(
      (markup) => current && setSvg(markup),
      () => {}, // The text form stays.
    );
    return () => {
      current = false;
    };
  }, [arrow, scheme]);
  return svg;
}

/** The "arrows" button in a class diagram's strip: a popover on wide screens, a bottom sheet on phones. */
export function ArrowLegendButton() {
  const wide = useMediaQuery(WIDE);
  const trigger = (
    <>
      <Icon d={arrowsIcon} size={14} />
      arrows
    </>
  );

  if (wide) {
    return (
      <Popover.Root>
        <Popover.Trigger className={stripButton} aria-label="What the arrows mean">
          {trigger}
        </Popover.Trigger>
        <Popover.Portal>
          <Popover.Positioner side="bottom" align="end" sideOffset={6} collisionPadding={12} className="z-50">
            <Popover.Popup className="legend-popover">
              <Popover.Title className="legend-title">The six arrows</Popover.Title>
              <ArrowLegend />
            </Popover.Popup>
          </Popover.Positioner>
        </Popover.Portal>
      </Popover.Root>
    );
  }

  return (
    <Drawer.Root swipeDirection="down">
      <Drawer.Trigger className={stripButton} aria-label="What the arrows mean">
        {trigger}
      </Drawer.Trigger>
      <Drawer.Portal>
        <Drawer.Backdrop className="sheet-backdrop" />
        <Drawer.Viewport className="fixed inset-0 z-50 flex items-end justify-center">
          <Drawer.Popup className="sheet">
            <div aria-hidden="true" className="mx-auto mt-2.5 mb-1 h-1 w-9 rounded-full bg-fg/15" />
            <Drawer.Title className="legend-title px-5 pt-2">The six arrows</Drawer.Title>
            <Drawer.Description className="sr-only">What each class diagram arrow means, with the C# behind it.</Drawer.Description>
            <Drawer.Content className="max-h-[75dvh] overflow-y-auto overscroll-contain px-5 pt-1 pb-[calc(1.25rem+env(safe-area-inset-bottom,0px))]">
              <ArrowLegend />
            </Drawer.Content>
          </Drawer.Popup>
        </Drawer.Viewport>
      </Drawer.Portal>
    </Drawer.Root>
  );
}
