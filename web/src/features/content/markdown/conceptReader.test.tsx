import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { describe, expect, it } from 'vitest';
import { DataSourceProvider } from '../../../data/DataSourceContext';
import { fakeDataSource } from '../../../test/fakeDataSource';
import { parseFenceMeta, parseLineRanges } from './fence';
import { MarkdownView } from './MarkdownView';

/** Spec 004 reader features, rendered from Markdown exactly as the packer leaves it. */
async function show(markdown: string, recall = false) {
  await act(async () => {
    render(
      <DataSourceProvider source={fakeDataSource()}>
        <MemoryRouter>
          <MarkdownView markdown={markdown} recall={recall} />
        </MemoryRouter>
      </DataSourceProvider>,
    );
  });
}

const compare = [
  '```csharp before title="Before: one class, two jobs" mark="1,5"',
  'public sealed class Report',
  '{',
  '    public string Render() => "report";',
  '',
  '    public void Save() { }',
  '}',
  '```',
  '```csharp after title="After" mark="1"',
  'public sealed class ReportRenderer',
  '{',
  '    public string Render() => "report";',
  '}',
  '```',
].join('\n');

describe('fence meta', () => {
  it('reads flags, the title and marked lines', () => {
    const meta = parseFenceMeta('before title="Before: a, b" mark="3-5,9"');

    expect(meta.flags).toEqual(['before']);
    expect(meta.title).toBe('Before: a, b');
    expect([...meta.marks]).toEqual([3, 4, 5, 9]);
  });

  it('skips what it cannot read', () => {
    expect([...parseLineRanges('2, x, 4-')]).toEqual([2]);
    expect(parseFenceMeta(undefined)).toEqual({ flags: [], title: undefined, marks: new Set() });
  });
});

describe('Before / After', () => {
  it('groups the pair into one box that starts on Before', async () => {
    await show(compare);

    const figure = screen.getByRole('figure', { name: /Before and after/ });
    const tabs = within(figure).getAllByRole('tab');
    expect(tabs.map((tab) => tab.textContent)).toEqual(['before', 'after']);
    expect(tabs[0]).toHaveAttribute('aria-selected', 'true');
    expect(within(figure).getByRole('tabpanel')).toHaveTextContent('public sealed class Report');
    expect(document.querySelectorAll('figure.code-block')).toHaveLength(1);
  });

  it('marks the changed lines on each side', async () => {
    await show(compare);

    const removed = [...document.querySelectorAll('.compare-pane-before [data-mark="del"]')].map((l) => l.textContent);
    const added = [...document.querySelectorAll('.compare-pane-after [data-mark="add"]')].map((l) => l.textContent);
    expect(removed).toEqual(['public sealed class Report', '    public void Save() { }']);
    expect(added).toEqual(['public sealed class ReportRenderer']);
  });

  it('switches with a tap and with the arrow keys, keeping both panes rendered', async () => {
    await show(compare);
    const [before, after] = screen.getAllByRole('tab');

    fireEvent.click(after);
    expect(after).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tabpanel')).toHaveTextContent('ReportRenderer');
    expect(document.querySelector('.compare-pane-before')).toHaveAttribute('inert');

    fireEvent.keyDown(after, { key: 'ArrowLeft' });
    expect(before).toHaveAttribute('aria-selected', 'true');
    expect(before).toHaveFocus();
    expect(document.querySelectorAll('.compare-pane')).toHaveLength(2);
  });

  it('leaves a lone before fence as a normal block', async () => {
    await show('```csharp before\nvar a = 1;\n```\n\nText.\n\n```csharp after\nvar a = 2;\n```');

    expect(screen.queryByRole('tab')).not.toBeInTheDocument();
    expect(document.querySelectorAll('figure.code-block')).toHaveLength(2);
  });

  it('marks lines in a normal block with mark', async () => {
    await show('```csharp mark="2"\nvar a = 1;\nvar b = 2;\n```');

    expect(document.querySelector('[data-mark="mark"]')).toHaveTextContent('var b = 2;');
  });
});

describe('callouts', () => {
  it('turns an alert blockquote into a labelled UML note without the marker', async () => {
    await show('> [!SMELL]\n> A class that renders **and** saves.\n\n> [!APP]\n> See `AccessRequest`.');

    const smell = screen.getByRole('complementary', { name: 'code smell' });
    expect(smell).toHaveAttribute('data-callout', 'smell');
    expect(smell).toHaveTextContent('«code smell»');
    expect(smell).toHaveTextContent('A class that renders and saves.');
    expect(smell).not.toHaveTextContent('[!SMELL]');
    expect(screen.getByRole('complementary', { name: 'in this app' })).toBeInTheDocument();
  });

  it('keeps plain and unknown blockquotes as quotes', async () => {
    await show('> Just a quote.\n\n> [!OOPS]\n> Unknown.');

    expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
    expect(document.querySelectorAll('blockquote')).toHaveLength(2);
  });
});

describe('memory aids', () => {
  it('draws REMEMBER as a stamp', async () => {
    await show('# E\n\n> [!REMEMBER]\n> Don\'t touch my data. Ask me to do it.\n\n## The rule\n\nText.');

    const stamp = screen.getByRole('complementary', { name: 'remember' });
    expect(stamp).toHaveAttribute('data-callout', 'remember');
    expect(stamp).toHaveTextContent("Don't touch my data. Ask me to do it.");
  });

  it('draws OVERUSE as a warning stamp that keeps its list', async () => {
    await show('> [!OVERUSE]\n> - Hidden dependencies.\n> - Shared state in tests.');

    const stamp = screen.getByRole('complementary', { name: 'when it hurts' });
    expect(stamp).toHaveAttribute('data-callout', 'overuse');
    expect(within(stamp).getAllByRole('listitem')).toHaveLength(2);
  });

  it('folds Go deeper behind a button that lists its topics', async () => {
    await show('# E\n\n## The rule\n\nShort.\n\n## Go deeper\n\n### A second example\n\nLong text.\n\n### C# traps\n\nMore.');

    const trigger = screen.getByRole('button', { name: /Open the deep dive/ });
    expect(trigger).toHaveTextContent('A second example · C# traps');
    expect(trigger).toHaveAttribute('aria-expanded', 'false');
    expect(screen.queryByText('Long text.')).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 2, name: 'Go deeper' })).toBeInTheDocument();

    fireEvent.click(trigger);
    expect(screen.getByText('Long text.')).toBeVisible();
  });
});

describe('interview questions', () => {
  const lesson = [
    '# SRP',
    '',
    '## Interview questions',
    '',
    'Answer out loud first.',
    '',
    '### What is SRP?',
    '',
    'One reason to change.',
    '',
    '### Is SRP about size?',
    '',
    'No: about who asks for changes.',
    '',
    '## Further reading',
    '',
    'Done.',
  ].join('\n');

  it('folds every answer under its question', async () => {
    await show(lesson);

    const questions = screen.getAllByRole('button', { name: /\?/ });
    expect(questions.map((q) => q.textContent)).toEqual(['Q1What is SRP?', 'Q2Is SRP about size?']);
    questions.forEach((q) => expect(q).toHaveAttribute('aria-expanded', 'false'));
    expect(screen.queryByText('One reason to change.')).not.toBeInTheDocument();
    expect(screen.getByText('Answer out loud first.')).toBeInTheDocument();
    expect(screen.getByText('Done.')).toBeInTheDocument();
  });

  it('reveals one answer on tap, and all of them with Show all', async () => {
    await show(lesson);

    fireEvent.click(screen.getByRole('button', { name: /What is SRP/ }));
    expect(screen.getByText('One reason to change.')).toBeVisible();

    fireEvent.click(screen.getByRole('button', { name: 'Show all' }));
    expect(screen.getByText('No: about who asks for changes.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Hide all' })).toBeInTheDocument();
  });

  it('keeps question headings and their ids', async () => {
    await show(lesson);

    expect(screen.getByRole('heading', { level: 3, name: /What is SRP/ })).toHaveAttribute('id', 'what-is-srp');
  });

  it('does not fold anything on cheat sheets', async () => {
    await show(lesson, true);

    expect(screen.queryByRole('button', { name: /What is SRP/ })).not.toBeInTheDocument();
  });
});
