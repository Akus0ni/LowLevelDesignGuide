import type { StatusMessage } from './serverStatusTracker';

export type StatusRejection = 'malformed' | 'bad_signature' | 'unsupported_version';

export type StatusVerification =
  { ok: true; message: StatusMessage } | { ok: false; rejection: StatusRejection };

export interface StatusVerifier {
  verify(envelopeJson: string): Promise<StatusVerification>;
}

const CURRENT_VERSION = 1;
const ALGORITHM = { name: 'ECDSA', namedCurve: 'P-256' } as const;
const SIGNATURE = { name: 'ECDSA', hash: 'SHA-256' } as const;

/**
 * Verifies status envelopes with the PC's public key (SPKI, base64) using WebCrypto. Mirrors
 * Lld.ServerStatus.StatusVerifier: the signature is checked on the raw payload bytes before anything is parsed.
 */
export async function createStatusVerifier(
  publicKeySpki: string,
  subtle: SubtleCrypto = globalThis.crypto.subtle,
): Promise<StatusVerifier> {
  const key = await subtle.importKey('spki', base64ToBytes(publicKeySpki), ALGORITHM, false, ['verify']);

  return {
    async verify(envelopeJson) {
      let payload: Uint8Array<ArrayBuffer>;
      let signature: Uint8Array<ArrayBuffer>;
      try {
        const envelope = JSON.parse(envelopeJson) as { payload?: unknown; sig?: unknown };
        if (typeof envelope.payload !== 'string' || typeof envelope.sig !== 'string') {
          return { ok: false, rejection: 'malformed' };
        }
        payload = base64UrlToBytes(envelope.payload);
        signature = base64UrlToBytes(envelope.sig);
      } catch {
        return { ok: false, rejection: 'malformed' };
      }

      if (!(await subtle.verify(SIGNATURE, key, signature, payload))) {
        return { ok: false, rejection: 'bad_signature' };
      }

      const message = parseMessage(payload);
      if (!message) return { ok: false, rejection: 'malformed' };
      return message.v === CURRENT_VERSION
        ? { ok: true, message }
        : { ok: false, rejection: 'unsupported_version' };
    },
  };
}

function parseMessage(payload: Uint8Array): StatusMessage | null {
  try {
    const message = JSON.parse(new TextDecoder().decode(payload)) as Partial<StatusMessage>;
    const valid =
      typeof message.v === 'number' &&
      (message.status === 'online' || message.status === 'offline') &&
      typeof message.reason === 'string' &&
      typeof message.at === 'string';
    return valid ? (message as StatusMessage) : null;
  } catch {
    return null;
  }
}

function base64ToBytes(base64: string): Uint8Array<ArrayBuffer> {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return bytes;
}

function base64UrlToBytes(value: string): Uint8Array<ArrayBuffer> {
  if (!/^[A-Za-z0-9_-]*$/.test(value)) throw new Error('Not base64url');
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
  return base64ToBytes(base64.padEnd(Math.ceil(base64.length / 4) * 4, '='));
}
