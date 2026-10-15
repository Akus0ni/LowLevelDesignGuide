import { useState, type FormEvent } from 'react';
import { toast } from 'sonner';
import { AppError } from '../../data/types';
import { Button } from '../../ui/Button';
import { TextAreaField, TextField } from '../../ui/TextField';
import { useAuth } from './AuthContext';

export const MIN_PASSWORD_LENGTH = 12;

function messageOf(error: unknown): string {
  return error instanceof AppError ? error.message : 'Something went wrong. Please try again.';
}

export function SignInForm() {
  const auth = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await auth.login(email, password);
    } catch (failure) {
      setError(messageOf(failure));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
      <TextField
        label="Email"
        type="email"
        inputMode="email"
        autoComplete="username"
        autoCapitalize="none"
        autoCorrect="off"
        enterKeyHint="next"
        required
        value={email}
        onChange={(e) => setEmail(e.target.value)}
      />
      <TextField
        label="Password"
        type="password"
        autoComplete="current-password"
        enterKeyHint="go"
        required
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />
      {error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}
      <Button type="submit" disabled={busy || !email || !password}>
        {busy ? 'Signing in…' : 'Sign in'}
      </Button>
    </form>
  );
}

export function RequestAccessForm({ onSent }: { onSent?: () => void }) {
  const auth = useAuth();
  const [email, setEmail] = useState('');
  const [name, setName] = useState('');
  const [note, setNote] = useState('');
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [sent, setSent] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await auth.requestAccess({ email, name, note: note || undefined });
      setSent(true);
      toast.success('Request sent', { description: 'The admin will review it and contact you.' });
      onSent?.();
    } catch (failure) {
      setError(messageOf(failure));
    } finally {
      setBusy(false);
    }
  }

  if (sent) {
    return (
      <p role="status" className="rounded-2xl bg-surface p-5">
        Thanks! Your request for <strong className="break-all">{email}</strong> is with the admin.
        They&apos;ll send you a temporary password when it&apos;s approved.
      </p>
    );
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
      <TextField
        label="Email"
        type="email"
        inputMode="email"
        autoComplete="email"
        autoCapitalize="none"
        autoCorrect="off"
        enterKeyHint="next"
        required
        hint="This will be your username."
        value={email}
        onChange={(e) => setEmail(e.target.value)}
      />
      <TextField
        label="Name"
        autoComplete="name"
        enterKeyHint="next"
        required
        maxLength={100}
        value={name}
        onChange={(e) => setName(e.target.value)}
      />
      <TextAreaField
        label="Note (optional)"
        maxLength={500}
        value={note}
        onChange={(e) => setNote(e.target.value)}
      />
      {error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}
      <Button type="submit" disabled={busy || !email || !name}>
        {busy ? 'Sending…' : 'Request access'}
      </Button>
    </form>
  );
}

export function ChangePasswordForm({
  submitLabel = 'Change password',
  onDone,
}: {
  submitLabel?: string;
  onDone?: () => void;
}) {
  const auth = useAuth();
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);

  const tooShort = next.length > 0 && next.length < MIN_PASSWORD_LENGTH;
  const mismatch = confirm.length > 0 && confirm !== next;

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (tooShort || mismatch) return;
    setBusy(true);
    setError(undefined);
    try {
      await auth.changePassword(current, next);
      toast.success('Password changed', { description: 'Other devices have been signed out.' });
      onDone?.();
    } catch (failure) {
      setError(messageOf(failure));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
      <TextField
        label="Current password"
        type="password"
        autoComplete="current-password"
        enterKeyHint="next"
        required
        value={current}
        onChange={(e) => setCurrent(e.target.value)}
      />
      <TextField
        label="New password"
        type="password"
        autoComplete="new-password"
        enterKeyHint="next"
        required
        hint={`At least ${MIN_PASSWORD_LENGTH} characters. A short sentence works well.`}
        error={tooShort ? `Use at least ${MIN_PASSWORD_LENGTH} characters.` : undefined}
        value={next}
        onChange={(e) => setNext(e.target.value)}
      />
      <TextField
        label="Confirm new password"
        type="password"
        autoComplete="new-password"
        enterKeyHint="done"
        required
        error={mismatch ? "The passwords don't match." : undefined}
        value={confirm}
        onChange={(e) => setConfirm(e.target.value)}
      />
      {error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}
      <Button type="submit" disabled={busy || !current || !next || next !== confirm || tooShort}>
        {busy ? 'Saving…' : submitLabel}
      </Button>
    </form>
  );
}
