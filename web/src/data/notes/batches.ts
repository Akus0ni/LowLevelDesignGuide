/** The API takes at most 500 records per write (Lld.Domain NoteLimits) and 1 MB per request (spec 007 plan §2.4). */
export const MAX_RECORDS_PER_WRITE = 500;
/** UTF-8 bytes of JSON per batch, leaving room under the 1 MB request limit. */
export const MAX_BATCH_BYTES = 900_000;

const encoder = new TextEncoder();

/**
 * Splits a write into requests the API accepts: each at most 500 records and 900 KB of JSON. A record is never split
 * (a note is at most 20,000 characters, so ≤ 80 KB), so every batch fits.
 */
export function batchesOf<T>(records: readonly T[], maxBytes = MAX_BATCH_BYTES): T[][] {
  const batches: T[][] = [];
  let current: T[] = [];
  let size = 2;
  for (const record of records) {
    const length = encoder.encode(JSON.stringify(record)).length + 1;
    if (current.length > 0 && (current.length === MAX_RECORDS_PER_WRITE || size + length > maxBytes)) {
      batches.push(current);
      current = [];
      size = 2;
    }
    current.push(record);
    size += length;
  }
  if (current.length > 0) batches.push(current);
  return batches;
}
