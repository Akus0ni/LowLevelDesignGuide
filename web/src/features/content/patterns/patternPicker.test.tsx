import { act, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { describe, expect, it } from 'vitest';
import type { LessonSummary, ModulePicker } from '../../../data/types';
import { PatternPicker } from './PatternPicker';
import { walkPicker } from './pickerWalk';

const picker: ModulePicker = {
  start: 'create',
  questions: [
    {
      id: 'create',
      ask: 'What is hard about creating it?',
      answers: [
        { label: 'Only one', next: null, picks: ['singleton'], why: 'One instance.' },
        { label: 'Hide the class', next: 'family', picks: [], why: null },
      ],
    },
    {
      id: 'family',
      ask: 'One product or a family?',
      answers: [
        { label: 'One', next: null, picks: ['factory-method'], why: 'A subclass picks.' },
        { label: 'A family', next: null, picks: ['abstract-factory'], why: 'They match.' },
      ],
    },
  ],
};

const lessons: LessonSummary[] = [
  ['singleton', 'Singleton', 'Sg'],
  ['factory-method', 'Factory Method', 'Fm'],
  ['abstract-factory', 'Abstract Factory', 'Af'],
].map(([slug, title, symbol], index) => ({
  id: slug,
  moduleSlug: 'gof-patterns',
  slug,
  title,
  summary: '',
  order: index + 1,
  readingMinutes: 3,
  sectionCount: 6,
  tags: [],
  hook: `${title} hook`,
  symbol,
}));

describe('walkPicker', () => {
  it('starts at the start question, follows answers, and stops at the answer that picks', () => {
    expect(walkPicker(picker, []).step).toMatchObject({ kind: 'question', question: { id: 'create' } });
    expect(walkPicker(picker, [1]).step).toMatchObject({ kind: 'question', question: { id: 'family' } });

    const done = walkPicker(picker, [1, 1]);
    expect(done.step).toMatchObject({ kind: 'result', answer: { picks: ['abstract-factory'] } });
    expect(done.path.map((choice) => choice.answer.label)).toEqual(['Hide the class', 'A family']);
  });
});

describe('PatternPicker', () => {
  async function open() {
    await act(async () => {
      render(
        <MemoryRouter>
          <PatternPicker picker={picker} lessons={lessons} />
        </MemoryRouter>,
      );
    });
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Which pattern?' })));
  }

  it('asks one question at a time and ends at the pattern with its why', async () => {
    await open();

    expect(await screen.findByText('What is hard about creating it?')).toBeInTheDocument();
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Hide the class' })));
    expect(screen.getByText('One product or a family?')).toBeInTheDocument();
    expect(screen.getByLabelText('Your answers')).toHaveTextContent('Hide the class');
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'A family' })));

    expect(screen.getByText('Try Abstract Factory')).toBeInTheDocument();
    expect(screen.getByText('They match.')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Abstract Factory/ })).toHaveAttribute('href', '/learn/gof-patterns/abstract-factory');
  });

  it('steps back one question with the Back button and with Esc, and closes on Esc at the start', async () => {
    await open();
    await act(async () => fireEvent.click(await screen.findByRole('button', { name: 'Hide the class' })));
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'One' })));

    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Back' })));
    expect(screen.getByText('One product or a family?')).toBeInTheDocument();

    await act(async () => fireEvent.keyDown(document.activeElement ?? document.body, { key: 'Escape' }));
    expect(screen.getByText('What is hard about creating it?')).toBeInTheDocument();

    await act(async () => fireEvent.keyDown(document.activeElement ?? document.body, { key: 'Escape' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});
