import { useId, type InputHTMLAttributes, type TextareaHTMLAttributes } from 'react';
import { cn } from './cn';

const fieldClass =
  'block w-full rounded-xl bg-surface px-4 text-base text-fg placeholder:text-muted ' +
  'outline-none ring-accent transition-[box-shadow] duration-150 ease-out focus-visible:ring-2 ' +
  'aria-[invalid=true]:ring-2 aria-[invalid=true]:ring-red-500';

interface FieldProps {
  label: string;
  error?: string;
  hint?: string;
}

function FieldShell({
  id,
  label,
  error,
  hint,
  children,
}: FieldProps & { id: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">
        {label}
      </label>
      {children}
      {error ? (
        <p id={`${id}-error`} className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      ) : hint ? (
        <p id={`${id}-hint`} className="text-sm text-muted">
          {hint}
        </p>
      ) : null}
    </div>
  );
}

/** Labelled input. Base text is 16px (text-base), so iOS never zooms on focus. */
export function TextField({
  label,
  error,
  hint,
  className,
  ...input
}: FieldProps & InputHTMLAttributes<HTMLInputElement>) {
  const id = useId();
  return (
    <FieldShell id={id} label={label} error={error} hint={hint}>
      <input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${id}-error` : hint ? `${id}-hint` : undefined}
        className={cn(fieldClass, 'h-12', className)}
        {...input}
      />
    </FieldShell>
  );
}

export function TextAreaField({
  label,
  error,
  hint,
  className,
  ...textarea
}: FieldProps & TextareaHTMLAttributes<HTMLTextAreaElement>) {
  const id = useId();
  return (
    <FieldShell id={id} label={label} error={error} hint={hint}>
      <textarea
        id={id}
        aria-invalid={error ? true : undefined}
        className={cn(fieldClass, 'min-h-24 py-3', className)}
        {...textarea}
      />
    </FieldShell>
  );
}
