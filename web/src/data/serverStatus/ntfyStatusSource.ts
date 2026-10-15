import type { ServerStatusTracker } from './serverStatusTracker';
import type { StatusVerifier } from './statusVerifier';

export interface NtfySettings {
  baseUrl: string;
  topic: string;
}

type EventSourceFactory = (url: string) => Pick<EventSource, 'addEventListener' | 'close'>;

/**
 * Browser status source: one EventSource on the ntfy topic. `since=12h` replays what ntfy has cached, then
 * streams live messages; EventSource reconnects by itself, and replays are ignored by the tracker.
 * Returns a function that closes the stream.
 */
export function connectNtfyStatus(
  settings: NtfySettings,
  verifier: StatusVerifier,
  tracker: ServerStatusTracker,
  onOpen: () => void = () => {},
  open: EventSourceFactory = (url) => new EventSource(url),
): () => void {
  const url = `${settings.baseUrl.replace(/\/$/, '')}/${encodeURIComponent(settings.topic)}/sse?since=12h`;
  const source = open(url);
  source.addEventListener('open', onOpen); // first connect and every automatic reconnect

  source.addEventListener('message', (event) => {
    void (async () => {
      try {
        const data = JSON.parse((event as MessageEvent<string>).data) as { event?: string; message?: string };
        if (data.event !== 'message' || typeof data.message !== 'string') return; // open / keepalive
        const result = await verifier.verify(data.message);
        if (result.ok) tracker.apply(result.message);
      } catch {
        // Not ntfy JSON: ignore, exactly like an unsigned message.
      }
    })();
  });

  return () => source.close();
}
