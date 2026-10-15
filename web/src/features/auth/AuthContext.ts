import { createContext, useContext } from 'react';
import type { AccessRequestInput, AccessRequestOutcome, UserSummary } from '../../data/types';

export type AuthState =
  { status: 'loading' } | { status: 'signedOut' } | { status: 'signedIn'; user: UserSummary };

export interface Auth {
  state: AuthState;
  /** `queued: true` when saved on the device until the server is online. */
  requestAccess(input: AccessRequestInput): Promise<AccessRequestOutcome>;
  login(email: string, password: string): Promise<UserSummary>;
  changePassword(currentPassword: string, newPassword: string): Promise<UserSummary>;
  logout(): Promise<void>;
  logoutAll(): Promise<void>;
}

export const AuthContext = createContext<Auth | null>(null);

export function useAuth(): Auth {
  const auth = useContext(AuthContext);
  if (!auth) throw new Error('useAuth must be used inside <AuthProvider>');
  return auth;
}
