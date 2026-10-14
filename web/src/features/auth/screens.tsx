import { useState } from 'react';
import { toast } from 'sonner';
import { Button } from '../../ui/Button';
import { cn } from '../../ui/cn';
import { AboutPanel } from '../AboutPanel';
import { useAuth } from './AuthContext';
import { ChangePasswordForm, RequestAccessForm, SignInForm } from './forms';

type Tab = 'sign-in' | 'request-access';

/** Profile: sign in / request access when signed out, account controls when signed in. */
export function ProfilePage() {
  const { state } = useAuth();

  return (
    <section className="pt-8 md:pt-12">
      <h1 className="text-3xl font-semibold md:text-4xl">Profile</h1>
      <div className="mt-6 max-w-md">
        {state.status === 'loading' && <p className="text-muted">Loading…</p>}
        {state.status === 'signedOut' && <SignedOut />}
        {state.status === 'signedIn' && <AccountPanel />}
      </div>
      <AboutPanel />
    </section>
  );
}

function SignedOut() {
  const [tab, setTab] = useState<Tab>('sign-in');

  return (
    <div className="flex flex-col gap-6">
      <div role="tablist" aria-label="Account" className="grid grid-cols-2 rounded-xl bg-surface p-1">
        {(
          [
            ['sign-in', 'Sign in'],
            ['request-access', 'Request access'],
          ] as const
        ).map(([value, label]) => (
          <button
            key={value}
            role="tab"
            type="button"
            aria-selected={tab === value}
            onClick={() => setTab(value)}
            className={cn(
              'h-10 rounded-lg text-sm font-medium transition-colors duration-150',
              tab === value ? 'bg-bg text-fg shadow-sm' : 'text-muted hover:text-fg',
            )}
          >
            {label}
          </button>
        ))}
      </div>
      {tab === 'sign-in' ? <SignInForm /> : <RequestAccessForm />}
    </div>
  );
}

function AccountPanel() {
  const auth = useAuth();
  const [changing, setChanging] = useState(false);
  if (auth.state.status !== 'signedIn') return null;
  const { user } = auth.state;

  async function signOutEverywhere() {
    await auth.logoutAll();
    toast.success('Signed out of all devices');
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-2xl bg-surface p-5">
        <p className="text-lg font-semibold break-words">{user.displayName}</p>
        <p className="text-muted break-all">{user.email}</p>
        <p className="mt-3 flex flex-wrap gap-2">
          {user.roles.map((role) => (
            <span key={role} className="rounded-full bg-bg px-3 py-1 text-xs font-medium">
              {role}
            </span>
          ))}
        </p>
      </div>

      {changing ? (
        <div className="flex flex-col gap-3">
          <h2 className="text-lg font-semibold">Change password</h2>
          <ChangePasswordForm onDone={() => setChanging(false)} />
          <Button intent="secondary" onClick={() => setChanging(false)}>
            Cancel
          </Button>
        </div>
      ) : (
        <div className="flex flex-col gap-3">
          <Button intent="secondary" onClick={() => setChanging(true)}>
            Change password
          </Button>
          <Button intent="secondary" onClick={() => auth.logout()}>
            Sign out
          </Button>
          <Button intent="secondary" onClick={signOutEverywhere}>
            Sign out of all devices
          </Button>
        </div>
      )}
    </div>
  );
}

/** Shown instead of the whole app until a temporary password has been replaced. */
export function ForcedPasswordChange() {
  const auth = useAuth();
  const email = auth.state.status === 'signedIn' ? auth.state.user.email : '';

  return (
    <main className="mx-auto flex min-h-dvh w-full max-w-md flex-col justify-center gap-6 px-5 pt-safe pb-safe">
      <div>
        <h1 className="text-3xl font-semibold">Set a new password</h1>
        <p className="mt-2 text-muted">
          You signed in as <span className="break-all text-fg">{email}</span> with a temporary password.
          Choose your own to continue.
        </p>
      </div>
      <ChangePasswordForm submitLabel="Save and continue" />
      <Button intent="secondary" onClick={() => auth.logout()}>
        Sign out
      </Button>
    </main>
  );
}
