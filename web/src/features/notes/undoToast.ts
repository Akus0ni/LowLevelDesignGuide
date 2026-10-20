import { toast } from 'sonner';

/** How long Undo stays on screen (spec 007 §3.3, §3.4). */
export const UNDO_MS = 5000;

/** A short toast whose action reverses what was just done (Command with an inverse, spec 007 §6). */
export function undoToast(message: string, undo: () => void): void {
  toast(message, { duration: UNDO_MS, action: { label: 'Undo', onClick: undo } });
}
