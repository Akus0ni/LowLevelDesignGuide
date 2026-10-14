import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { useDataSource } from '../../data/DataSourceContext';
import type { UserSummary } from '../../data/types';
import { AuthContext, type Auth, type AuthState } from './AuthContext';

/** Holds the signed-in user. Tokens stay inside the DataSource; React only ever sees the user summary. */
export function AuthProvider({ children }: { children: ReactNode }) {
  const source = useDataSource();
  const [state, setState] = useState<AuthState>({ status: 'loading' });

  useEffect(() => {
    let active = true;
    source
      .restoreSession()
      .then((user) => active && setState(user ? { status: 'signedIn', user } : { status: 'signedOut' }))
      .catch(() => active && setState({ status: 'signedOut' }));
    return () => {
      active = false;
    };
  }, [source]);

  const auth = useMemo<Auth>(() => {
    const signIn = (user: UserSummary) => {
      setState({ status: 'signedIn', user });
      return user;
    };
    const signOut = () => setState({ status: 'signedOut' });

    return {
      state,
      requestAccess: (input) => source.requestAccess(input),
      login: async (email, password) => signIn(await source.login(email, password)),
      changePassword: async (current, next) => signIn(await source.changePassword(current, next)),
      logout: () => source.logout().finally(signOut),
      logoutAll: () => source.logoutAll().finally(signOut),
    };
  }, [source, state]);

  return <AuthContext value={auth}>{children}</AuthContext>;
}
