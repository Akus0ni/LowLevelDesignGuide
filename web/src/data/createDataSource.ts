import { BridgeDataSource } from './bridgeDataSource';
import { HttpDataSource, type HttpStatusOptions } from './httpDataSource';
import { connectNtfyStatus } from './serverStatus/ntfyStatusSource';
import { createStatusVerifier } from './serverStatus/statusVerifier';
import type { DataSource } from './types';

const DEFAULT_NTFY_BASE_URL = 'https://ntfy.sh';

/** Factory: inside the MAUI shell use the native bridge, otherwise call the API over HTTP. */
export function createDataSource(
  win: Pick<Window, 'HybridWebView'> = window,
  env: ImportMetaEnv = import.meta.env,
): DataSource {
  return win.HybridWebView
    ? new BridgeDataSource(win.HybridWebView)
    : new HttpDataSource('', undefined, browserStatusOptions(env));
}

/**
 * Browser dev mode talks to the Aspire dev API, so server status is assumed online unless web/.env.local names
 * the hosted server's topic and public key (VITE_NTFY_TOPIC, VITE_STATUS_PUBLIC_KEY).
 */
export function browserStatusOptions(env: ImportMetaEnv): HttpStatusOptions {
  const topic = env.VITE_NTFY_TOPIC;
  const publicKey = env.VITE_STATUS_PUBLIC_KEY;
  if (!topic || !publicKey) return {};

  const settings = { baseUrl: env.VITE_NTFY_BASE_URL ?? DEFAULT_NTFY_BASE_URL, topic };
  return {
    connectStatus: (tracker, onReconnected) => {
      let stop: (() => void) | null = null;
      let stopped = false;
      void createStatusVerifier(publicKey).then((verifier) => {
        if (!stopped) stop = connectNtfyStatus(settings, verifier, tracker, onReconnected);
      });
      return () => {
        stopped = true;
        stop?.();
      };
    },
  };
}
