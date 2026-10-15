import { NavLink, Outlet } from 'react-router';
import { useAuth } from '../features/auth/AuthContext';
import { ForcedPasswordChange } from '../features/auth/screens';
import { ServerOfflineBanner } from '../features/serverStatus/ServerOfflineBanner';
import { cn } from '../ui/cn';
import { SECTIONS } from './sections';

const linkTone = ({ isActive }: { isActive: boolean }) =>
  isActive ? 'text-accent' : 'text-muted hover:text-fg';

export function AppShell() {
  const { state } = useAuth();

  // A temporary password must be replaced before anything else is shown.
  if (state.status === 'signedIn' && state.user.mustChangePassword) {
    return <ForcedPasswordChange />;
  }

  return (
    <div className="min-h-dvh bg-bg text-fg md:pl-64">
      <ServerOfflineBanner />
      {/* Tablet/desktop: translucent sidebar (apple-design §12). */}
      <aside className="fixed inset-y-0 left-0 hidden w-64 flex-col gap-1 bg-chrome px-3 pt-safe backdrop-blur-xl md:flex">
        <p className="px-3 pt-6 pb-4 text-lg font-semibold tracking-tight">LLD Guide</p>
        <nav aria-label="Main" className="flex flex-col gap-1">
          {SECTIONS.map((s) => (
            <NavLink
              key={s.path}
              to={s.path}
              className={(state) =>
                cn(
                  'flex h-11 items-center gap-3 rounded-xl px-3 font-medium transition-colors duration-150',
                  linkTone(state),
                )
              }
            >
              {s.icon}
              {s.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <main className="mx-auto w-full max-w-3xl px-5 pt-safe pb-[calc(7rem+env(safe-area-inset-bottom,0px))] md:pb-12">
        <Outlet />
      </main>

      {/* Phone: translucent bottom tab bar with a soft scroll-edge fade instead of a hard border. */}
      <nav
        aria-label="Main"
        className={cn(
          'fixed inset-x-0 bottom-0 z-10 bg-chrome pb-safe backdrop-blur-xl md:hidden',
          'before:pointer-events-none before:absolute before:inset-x-0 before:-top-6 before:h-6',
          'before:bg-linear-to-t before:from-bg/70 before:to-transparent',
        )}
      >
        <ul className="grid grid-cols-5">
          {SECTIONS.map((s) => (
            <li key={s.path}>
              <NavLink
                to={s.path}
                className={(state) =>
                  cn(
                    'flex min-h-14 flex-col items-center justify-center gap-0.5 text-[11px] font-medium',
                    'transition-colors duration-150 active:opacity-70',
                    linkTone(state),
                  )
                }
              >
                {s.icon}
                {s.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  );
}
