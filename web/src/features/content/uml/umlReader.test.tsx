import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { describe, expect, it, vi } from 'vitest';
import { DataSourceProvider } from '../../../data/DataSourceContext';
import type { LessonSummary, ModulePicker } from '../../../data/types';
import { fakeDataSource } from '../../../test/fakeDataSource';
import activitySvg from '../markdown/__fixtures__/uml-activity.svg?raw';
import classSvg from '../markdown/__fixtures__/uml-class-arrows.svg?raw';
import stateSvg from '../markdown/__fixtures__/uml-state.svg?raw';
import { renderMermaid } from '../markdown/mermaid';
import { MarkdownView } from '../markdown/MarkdownView';
import { PatternPicker } from '../patterns/PatternPicker';

// Mermaid needs a real layout engine: hand back the real Mermaid 12 output for each fixture source.
vi.mock('../markdown/mermaid', async (original) => ({
  ...(await original<typeof import('../markdown/mermaid')>()),
  renderMermaid: vi.fn((_id: string, source: string) => {
    if (source.includes('IEntity')) return Promise.resolve(classSvg);
    if (source.startsWith('stateDiagram')) return Promise.resolve(stateSvg);
    if (source.includes('online')) return Promise.resolve(activitySvg);
    return Promise.resolve('<svg aria-roledescription="class"><text>arrow</text></svg>');
  }),
}));

const CLASS_SOURCE = [
  'classDiagram',
  '  IEntity <|.. Order',
  '  Order *-- OrderLine',
  '  Order --> Customer',
  '  Order o-- Discount',
  '  Order ..> TaxPolicy',
  '  Customer <|-- VipCustomer',
];

const pair = [
  '```mermaid pair links="Order=1-4;Customer=6;Order-->Customer=3;IEntity<|..Order=1"',
  ...CLASS_SOURCE,
  '```',
  '```csharp paired',
  'public sealed class Order : IEntity',
  '{',
  '    public Customer Customer { get; }',
  '}',
  '',
  'public class Customer;',
  '```',
].join('\n');

const state = [
  '```mermaid',
  'stateDiagram-v2',
  '  [*] --> Draft',
  '  Draft --> InReview : submit',
  '  InReview --> Draft : reject',
  '  InReview --> Published : approve',
  '  Published --> [*]',
  '```',
].join('\n');

const activity = [
  '```mermaid steps',
  'flowchart TD',
  '  start((" ")) --> edit["Edit a note"]',
  '  edit --> online{"Online?"}',
  '  online -->|yes| fork1[" "]:::fork',
  '  online -->|no| queue["Queue the change"]',
  '  queue --> stop(((" ")))',
  '  fork1 --> save["Save on device"]',
  '  fork1 --> send["Send to server"]',
  '  save --> join1[" "]:::join',
  '  send --> join1',
  '  join1 --> stop',
  '```',
].join('\n');

async function show(markdown: string) {
  await act(async () => {
    render(
      <DataSourceProvider source={fakeDataSource()}>
        <MemoryRouter>
          <MarkdownView markdown={markdown} />
        </MemoryRouter>
      </DataSourceProvider>,
    );
  });
}

function markedLines(): string[] {
  const code = document.querySelector('.uml-pair-code')!;
  return [...code.querySelectorAll('.code-line[data-mark]')].map((line) => line.textContent ?? '');
}

describe('Code ↔ UML pair', () => {
  it('lights up the code behind a tapped class, then behind a tapped arrow', async () => {
    await show(pair);

    const figure = screen.getByRole('figure', { name: 'Code and UML' });
    expect(within(figure).getAllByRole('tab').map((tab) => tab.textContent)).toEqual(['diagram', 'code']);

    fireEvent.click(figure.querySelector('g.node[id*="classId-Order-"]')!);
    expect(markedLines()).toHaveLength(4);

    const arrow = figure.querySelector('.edge-hit[data-edge-key="Order-->Customer"]')!;
    expect(arrow.getAttribute('aria-label')).toBe('Order knows Customer');
    fireEvent.click(arrow);
    expect(markedLines()).toEqual(['    public Customer Customer { get; }']);
    expect(figure.querySelector('path[data-id="id_Order_Customer_3"]')!.hasAttribute('data-selected')).toBe(true);
  });

  it('lights up the class behind a tapped code line', async () => {
    await show(pair);

    const lines = document.querySelectorAll('.uml-pair-code .code-line');
    expect(lines[5].hasAttribute('data-linkable')).toBe(true);
    expect(lines[4].hasAttribute('data-linkable')).toBe(false);
    fireEvent.click(lines[5]);

    expect(document.querySelector('g.node[id*="classId-Customer-"]')!.getAttribute('aria-pressed')).toBe('true');
    expect(markedLines()).toEqual(['public class Customer;']);
  });

  it('switches to the code on a phone after a short pause', async () => {
    vi.useFakeTimers();
    try {
      await show(pair);
      const figure = screen.getByRole('figure', { name: 'Code and UML' });
      fireEvent.click(figure.querySelector('g.node[id*="classId-Order-"]')!);
      expect(within(figure).getByRole('tab', { name: 'diagram' })).toHaveAttribute('aria-selected', 'true');
      await act(async () => vi.advanceTimersByTime(300));
      expect(within(figure).getByRole('tab', { name: 'code' })).toHaveAttribute('aria-selected', 'true');
    } finally {
      vi.useRealTimers();
    }
  });
});

describe('arrow legend', () => {
  it('opens from a class diagram and lists the six arrows', async () => {
    await show(['```mermaid', ...CLASS_SOURCE, '```'].join('\n'));

    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'What the arrows mean' })));

    const sheet = await screen.findByRole('dialog');
    expect(within(sheet).getByText('The six arrows')).toBeInTheDocument();
    for (const name of ['Association', 'Aggregation', 'Composition', 'Inheritance', 'Realization', 'Dependency']) {
      expect(within(sheet).getByText(name)).toBeInTheDocument();
    }
  });

  it('renders inline from a uml-arrows fence', async () => {
    await show('```uml-arrows\n```');
    expect(screen.getByRole('figure', { name: 'Class diagram arrows' })).toBeInTheDocument();
    expect(screen.getByText('class Circle : Shape')).toBeInTheDocument();
  });
});

describe('state diagrams', () => {
  it('traces a tapped state', async () => {
    await show(state);
    fireEvent.click(document.querySelector('g.node[id*="-state-InReview-"]')!);
    expect(screen.getByText('InReview · 2 links')).toBeInTheDocument();
  });

  it('runs from the start state to the end, one event at a time', async () => {
    await show(state);

    fireEvent.click(screen.getByRole('button', { name: 'run' }));
    const events = () => within(screen.getByRole('group', { name: 'Events' })).getAllByRole('button');
    expect(events().map((b) => b.textContent)).toEqual(['submit']);
    expect(document.querySelector('g.node[id*="-state-Draft-"]')!.hasAttribute('data-current')).toBe(true);

    fireEvent.click(events()[0]);
    expect(events().map((b) => b.textContent)).toEqual(['reject', 'approve']);
    fireEvent.click(events()[1]);
    fireEvent.click(screen.getByRole('button', { name: 'finish' }));
    expect(within(screen.getByRole('group', { name: 'Events' })).getByText('Final state reached.')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Undo the last event' }));
    expect(events().map((b) => b.textContent)).toEqual(['finish']);
    fireEvent.click(screen.getByRole('button', { name: 'stop' }));
    expect(screen.queryByRole('group', { name: 'Events' })).toBeNull();
    expect(document.querySelector('svg[data-running]')).toBeNull();
  });
});

describe('activity step-through', () => {
  it('walks a steps flowchart through its decision and fork', async () => {
    await show(activity);

    expect(vi.mocked(renderMermaid).mock.calls.slice(-1)[0][1]).toContain('classDef fork,join');
    expect(document.querySelector('g.node[id*="flowchart-fork1-"]')!.getAttribute('data-kind')).toBe('fork');

    fireEvent.click(screen.getByRole('button', { name: 'step through' }));
    const next = () => within(screen.getByRole('group', { name: 'Next step' })).getAllByRole('button');
    fireEvent.click(next()[0]); // start → edit
    fireEvent.click(next()[0]); // edit → online?
    expect(next().map((b) => b.textContent)).toEqual(['yes', 'no']);
    fireEvent.click(next()[0]); // yes → fork
    fireEvent.click(next()[0]); // fork → save + send
    const current = [...document.querySelectorAll('g.node[data-current]')].map((n) => n.id);
    expect(current.some((id) => id.includes('flowchart-save-'))).toBe(true);
    expect(current.some((id) => id.includes('flowchart-send-'))).toBe(true);
    fireEvent.click(next()[0]); // → join
    fireEvent.click(next()[0]); // → end
    expect(screen.getByText('The flow ends here.')).toBeInTheDocument();
  });

  it('leaves plain flowcharts static', async () => {
    await show(activity.replace('```mermaid steps', '```mermaid'));
    expect(screen.queryByRole('button', { name: 'step through' })).toBeNull();
  });
});

describe('UML module extras', () => {
  it('draws the Mistakes stamp', async () => {
    await show('> [!MISTAKES]\n> - Arrows the wrong way.');
    expect(screen.getByRole('complementary', { name: 'common mistakes' })).toBeInTheDocument();
  });

  it('labels the picker with the module title', () => {
    const picker: ModulePicker = {
      title: 'Which diagram?',
      start: 'what',
      questions: [{ id: 'what', ask: 'What?', answers: [{ label: 'Parts', next: null, picks: ['a'], why: 'x' }] }],
    };
    const lessons = [{ slug: 'a', id: 'a', title: 'A' }] as LessonSummary[];
    render(
      <MemoryRouter>
        <PatternPicker picker={picker} lessons={lessons} />
      </MemoryRouter>,
    );
    expect(screen.getByRole('button', { name: 'Which diagram?' })).toBeInTheDocument();
  });
});
