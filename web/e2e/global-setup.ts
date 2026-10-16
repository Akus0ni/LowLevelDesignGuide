import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

/** Where the snapshot of every /api/content response is written (git-ignored). */
export const CONTENT_SNAPSHOT = fileURLToPath(new URL('./.content', import.meta.url));

/**
 * The e2e tests read the repository's real content: `lld-content snapshot` writes every content API response as a
 * file, and `routeContent` serves them. Needs the .NET SDK (CI's web job installs it).
 */
export default function globalSetup(): void {
  const repo = fileURLToPath(new URL('../..', import.meta.url)).replace(/[\\/]$/, '');
  execFileSync(
    'dotnet',
    [
      'run',
      '--project',
      `${repo}/src/Lld.Content.Cli`,
      '--',
      'snapshot',
      '--repo',
      repo,
      '--out',
      CONTENT_SNAPSHOT,
    ],
    { stdio: 'inherit' },
  );
}
