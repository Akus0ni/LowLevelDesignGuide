import { useSearchParams } from 'react-router';

/**
 * True when the page was opened from a search result (`?q=`, spec 009 §3.6). Folds then keep their closed content in
 * the page (`hidden="until-found"`), so a match inside one can be marked and opened; otherwise closed folds stay
 * unmounted and cost nothing.
 */
export function useSearchableFolds(): boolean {
  const [params] = useSearchParams();
  return params.has('q');
}
