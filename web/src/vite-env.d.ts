/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Browser dev mode only (web/.env.local): the hosted server's ntfy topic and public key (spec 002). */
  readonly VITE_NTFY_TOPIC?: string;
  readonly VITE_STATUS_PUBLIC_KEY?: string;
  readonly VITE_NTFY_BASE_URL?: string;
}
