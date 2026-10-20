import { useAuth } from '../auth/AuthContext';

/** Whose recent searches these are: the signed-in user's, or this device's guest's (spec §3.5). */
export function useSearchOwner(): string {
  const { state } = useAuth();
  return state.status === 'signedIn' ? state.user.id : 'guest';
}
