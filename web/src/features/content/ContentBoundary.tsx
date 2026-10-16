import { Component, Suspense, type ReactNode } from 'react';
import { Link, useLocation } from 'react-router';
import { useDataSource } from '../../data/DataSourceContext';
import { AppError, ContentErrors } from '../../data/types';
import { forgetContent } from '../../data/useContent';
import { Button } from '../../ui/Button';

interface BoundaryProps {
  children: ReactNode;
  resetKey: string;
  onRetry: () => void;
}

/** Catches a failed content read. Navigating elsewhere (a new resetKey) clears the error. */
class ContentErrorBoundary extends Component<BoundaryProps, { error: unknown }> {
  state: { error: unknown } = { error: null };

  static getDerivedStateFromError(error: unknown) {
    return { error };
  }

  componentDidUpdate(previous: BoundaryProps) {
    if (previous.resetKey !== this.props.resetKey && this.state.error) this.setState({ error: null });
  }

  render() {
    if (!this.state.error) return this.props.children;
    return (
      <ContentError
        error={this.state.error}
        onRetry={() => {
          this.props.onRetry();
          this.setState({ error: null });
        }}
      />
    );
  }
}

/**
 * Wraps a screen's content: a placeholder while it loads (only shown if loading is slow) and a way out when it
 * fails. Every content screen uses it, so failures look and behave the same everywhere.
 */
export function ContentBoundary({ children, fallback }: { children: ReactNode; fallback: ReactNode }) {
  const source = useDataSource();
  const { pathname } = useLocation();
  return (
    <ContentErrorBoundary resetKey={pathname} onRetry={() => forgetContent(source)}>
      <Suspense fallback={fallback}>{children}</Suspense>
    </ContentErrorBoundary>
  );
}

function ContentError({ error, onRetry }: { error: unknown; onRetry: () => void }) {
  const notFound = error instanceof AppError && error.code === ContentErrors.notFound;
  return (
    <div className="mt-8 flex flex-col items-start gap-2">
      <h2 className="text-xl font-semibold">
        {notFound ? "This page isn't in the guide" : "Couldn't load this page"}
      </h2>
      <p className="text-muted">
        {notFound
          ? "It may have moved, or this version of the app doesn't have it yet."
          : error instanceof AppError
            ? error.message
            : 'Something went wrong while opening it.'}
      </p>
      {notFound ? (
        <Link
          to="/learn"
          className="mt-4 inline-flex h-11 items-center rounded-xl bg-surface px-4 font-medium transition-transform duration-[120ms] ease-out select-none active:scale-[0.97]"
        >
          Back to Learn
        </Link>
      ) : (
        <Button className="mt-4" onClick={onRetry}>
          Try again
        </Button>
      )}
    </div>
  );
}
