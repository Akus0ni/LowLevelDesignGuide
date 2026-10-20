import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { describe, expect, it } from 'vitest';
import type { LessonSummary, Lookalike, ModuleMap } from '../../../data/types';
import { AppProviders } from '../../../test/AppProviders';
import { fakeDataSource } from '../../../test/fakeDataSource';
import { comparePath, findLookalike, lookalikeKey, partnersOf } from './lookalikes';
import { PeriodicTable } from './PeriodicTable';

const patterns: [string, string, string][] = [
  ['singleton', 'Singleton', 'Sg'],
  ['factory-method', 'Factory Method', 'Fm'],
  ['abstract-factory', 'Abstract Factory', 'Af'],
  ['builder', 'Builder', 'Bu'],
  ['adapter', 'Adapter', 'Ad'],
];

const lessons: LessonSummary[] = patterns.map(([slug, title, symbol], index) => ({
  id: slug,
  moduleSlug: 'gof-patterns',
  slug,
  title,
  summary: `About ${title}.`,
  order: index + 1,
  readingMinutes: 3,
  sectionCount: 6,
  tags: [],
  icon: '🧩',
  hook: `${title} hook`,
  symbol,
}));

const map: ModuleMap = {
  groups: [
    { title: 'Creational: making objects', lessons: ['singleton', 'factory-method', 'abstract-factory', 'builder'] },
    { title: 'Structural: fitting objects together', lessons: ['adapter'] },
  ],
  links: [{ from: 'abstract-factory', to: 'factory-method', label: 'is often built from' }],
};

const lookalikes: Lookalike[] = [
  { lessons: ['factory-method', 'abstract-factory', 'builder'], difference: 'One, a family, or step by step.', ask: 'Which?' },
];

async function show() {
  await act(async () => {
    render(
      <AppProviders source={fakeDataSource()}>
        <MemoryRouter>
          <button type="button">outside</button>
          <PeriodicTable map={map} lessons={lessons} lookalikes={lookalikes} />
        </MemoryRouter>
      </AppProviders>,
    );
  });
}

const tile = (title: string) => screen.getByRole('button', { name: new RegExp(`^${title} \\(`) });

describe('PeriodicTable', () => {
  it('draws one block per category, each pattern as a tile with its number and symbol', async () => {
    await show();

    expect(screen.getByText('«creational»')).toBeInTheDocument();
    expect(screen.getByText('Fitting objects together')).toBeInTheDocument();
    expect(tile('Factory Method')).toHaveTextContent('02🧩FmFactory Method');
    expect(tile('Adapter')).toHaveAccessibleName('Adapter (Ad)');
  });

  it('marks what a pattern travels with and its look-alikes, and opens a card with a compare link', async () => {
    await show();

    fireEvent.click(tile('Abstract Factory'));

    expect(tile('Abstract Factory')).toHaveAttribute('data-state', 'selected');
    expect(tile('Factory Method')).toHaveAttribute('data-state', 'related');
    expect(tile('Builder')).toHaveAttribute('data-state', 'lookalike');
    expect(tile('Singleton')).not.toHaveAttribute('data-state');
    const card = screen.getByRole('region', { name: 'Abstract Factory' });
    expect(within(card).getByText('“Abstract Factory hook”')).toBeInTheDocument();
    expect(within(card).getByRole('link', { name: /Compare/ })).toHaveAttribute(
      'href',
      '/learn/gof-patterns/compare/factory-method-vs-abstract-factory-vs-builder',
    );
    expect(within(card).getByRole('link', { name: /Open lesson/ })).toHaveAttribute('href', '/learn/gof-patterns/abstract-factory');
  });

  it('walks to a related pattern from the card, and clears on Esc or a tap outside', async () => {
    await show();

    fireEvent.click(tile('Abstract Factory'));
    const card = screen.getByRole('region', { name: 'Abstract Factory' });
    fireEvent.click(within(card).getByRole('button', { name: 'is often built from Factory Method' }));
    expect(tile('Factory Method')).toHaveAttribute('data-state', 'selected');

    fireEvent.keyDown(tile('Factory Method'), { key: 'Escape' });
    expect(tile('Factory Method')).not.toHaveAttribute('data-state');

    fireEvent.click(tile('Builder'));
    fireEvent.pointerDown(screen.getByRole('button', { name: 'outside' }));
    expect(screen.queryByRole('region', { name: 'Builder' })).not.toBeInTheDocument();
  });

  it('moves focus with the arrow keys, one tile at a time in reading order', async () => {
    await show();

    expect(tile('Singleton')).toHaveAttribute('tabindex', '0');
    tile('Singleton').focus();
    fireEvent.keyDown(tile('Singleton'), { key: 'ArrowRight' });
    expect(tile('Factory Method')).toHaveFocus();
    fireEvent.keyDown(tile('Factory Method'), { key: 'End' });
    expect(tile('Adapter')).toHaveFocus();
    expect(tile('Adapter')).toHaveAttribute('tabindex', '0');
    expect(tile('Singleton')).toHaveAttribute('tabindex', '-1');
  });
});

describe('lookalikes', () => {
  it('builds the compare key and finds the group again from it', () => {
    expect(lookalikeKey(lookalikes[0])).toBe('factory-method-vs-abstract-factory-vs-builder');
    expect(comparePath('gof-patterns', lookalikes[0])).toBe('/learn/gof-patterns/compare/factory-method-vs-abstract-factory-vs-builder');
    expect(findLookalike(lookalikes, 'factory-method-vs-abstract-factory-vs-builder')).toBe(lookalikes[0]);
    expect(findLookalike(lookalikes, 'builder-vs-singleton')).toBeUndefined();
    expect(partnersOf(lookalikes, 'builder')).toEqual(['factory-method', 'abstract-factory']);
  });
});
