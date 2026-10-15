import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { createStatusVerifier } from './statusVerifier';

interface VectorCase {
  name: string;
  envelope: string;
  expect: 'valid' | 'malformed' | 'bad_signature' | 'unsupported_version';
  status?: 'online' | 'offline';
  reason?: string;
  at?: string;
  quotaResetsAt?: string;
}

// The same file the .NET tests read (tests/fixtures/server-status), so both verifiers agree byte for byte.
const vector = JSON.parse(
  // Vitest runs from web/; jsdom's import.meta.url is not a file URL.
  readFileSync(
    resolve(process.cwd(), '..', 'tests', 'fixtures', 'server-status', 'test-vector.json'),
    'utf8',
  ),
) as { publicKey: string; cases: VectorCase[] };

describe('createStatusVerifier (shared test vector)', () => {
  it.each(vector.cases.map((c) => [c.name, c] as const))('%s', async (_, testCase) => {
    const verifier = await createStatusVerifier(vector.publicKey);

    const result = await verifier.verify(testCase.envelope);

    if (testCase.expect === 'valid') {
      expect(result.ok).toBe(true);
      if (!result.ok) return;
      expect(result.message.status).toBe(testCase.status);
      expect(result.message.reason).toBe(testCase.reason);
      expect(Date.parse(result.message.at)).toBe(Date.parse(testCase.at!));
      if (testCase.quotaResetsAt) {
        expect(Date.parse(result.message.quotaResetsAt!)).toBe(Date.parse(testCase.quotaResetsAt));
      }
    } else {
      expect(result).toEqual({ ok: false, rejection: testCase.expect });
    }
  });
});
