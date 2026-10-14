import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { describe, expect, it } from 'vitest';
import { DataSourceProvider } from '../data/DataSourceContext';
import type { DataSource } from '../data/types';
import { AuthProvider } from '../features/auth/AuthProvider';
import { fakeDataSource, learner } from '../test/fakeDataSource';
import { routes } from './routes';

function renderAt(path: string, source: DataSource = fakeDataSource()) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  render(
    <DataSourceProvider source={source}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </DataSourceProvider>,
  );
  return source;
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

describe('Auth screens', () => {
  it('shows sign in and request access when signed out', async () => {
    renderAt('/profile');

    expect(await screen.findByRole('tab', { name: 'Sign in' })).toHaveAttribute('aria-selected', 'true');
    await userEvent.click(screen.getByRole('tab', { name: 'Request access' }));
    expect(screen.getByLabelText('Name')).toBeInTheDocument();
  });

  it('shows the server error message when sign-in fails', async () => {
    renderAt('/profile');

    await userEvent.type(await screen.findByLabelText('Email'), 'learner@example.com');
    await userEvent.type(screen.getByLabelText('Password'), 'wrong-password');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Email or password is incorrect.');
  });

  it('forces a password change after signing in with a temporary password', async () => {
    const source = fakeDataSource({
      login: async () => ({ ...learner, mustChangePassword: true }),
      changePassword: async () => learner,
    });
    renderAt('/profile', source);

    await userEvent.type(await screen.findByLabelText('Email'), learner.email);
    await userEvent.type(screen.getByLabelText('Password'), 'temporary-pass');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('heading', { name: 'Set a new password' })).toBeInTheDocument();
    expect(screen.queryByRole('navigation', { name: 'Main' })).not.toBeInTheDocument();

    await userEvent.type(screen.getByLabelText('Current password'), 'temporary-pass');
    await userEvent.type(screen.getByLabelText('New password'), 'a-brand-new-password');
    await userEvent.type(screen.getByLabelText('Confirm new password'), 'a-brand-new-password');
    await userEvent.click(screen.getByRole('button', { name: 'Save and continue' }));

    expect(await screen.findByText(learner.displayName)).toBeInTheDocument();
    expect(screen.getAllByRole('navigation', { name: 'Main' }).length).toBeGreaterThan(0);
  });

  it('rejects a new password shorter than 12 characters before calling the server', async () => {
    const source = fakeDataSource({ restoreSession: async () => ({ ...learner, mustChangePassword: true }) });
    renderAt('/learn', source);

    await userEvent.type(await screen.findByLabelText('New password'), 'short');

    expect(screen.getByText('Use at least 12 characters.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save and continue' })).toBeDisabled();
  });

  it('signs out of all devices from the account panel', async () => {
    const source = fakeDataSource({ restoreSession: async () => learner });
    renderAt('/profile', source);

    await userEvent.click(await screen.findByRole('button', { name: 'Sign out of all devices' }));

    expect(source.logoutAll).toHaveBeenCalled();
    expect(await screen.findByRole('tab', { name: 'Sign in' })).toBeInTheDocument();
  });
});
