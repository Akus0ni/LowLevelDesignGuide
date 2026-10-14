import { render, screen, within } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { describe, expect, it } from 'vitest';
import { DataSourceProvider } from '../data/DataSourceContext';
import type { DataSource } from '../data/types';
import { routes } from './routes';

const fakeSource: DataSource = {
  kind: 'bridge',
  getInfo: () => Promise.resolve({ name: 'LLD Guide', version: '0.1.0', environment: 'Test' }),
};

function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  return render(
    <DataSourceProvider source={fakeSource}>
      <RouterProvider router={router} />
    </DataSourceProvider>,
  );
}

describe('AppShell', () => {
  it('redirects / to Learn and shows all five sections in each nav', async () => {
    renderAt('/');

    expect(await screen.findByRole('heading', { name: 'Learn' })).toBeInTheDocument();
    for (const nav of screen.getAllByRole('navigation', { name: 'Main' })) {
      expect(
        within(nav)
          .getAllByRole('link')
          .map((a) => a.textContent),
      ).toEqual(['Learn', 'Revise', 'Search', 'Notes', 'Profile']);
    }
  });

  it('shows app info from the data source on Profile', async () => {
    renderAt('/profile');

    expect(await screen.findByTestId('info-name')).toHaveTextContent('LLD Guide');
    expect(screen.getByTestId('transport')).toHaveTextContent('Native bridge');
  });
});
