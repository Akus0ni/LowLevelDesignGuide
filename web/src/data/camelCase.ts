/**
 * C# answers over the bridge with PascalCase keys (`ModuleSlug`) while the API uses camelCase (`moduleSlug`).
 * Converting once, deeply, gives React the same shape from both data sources.
 */
export function toCamelCaseDeep(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(toCamelCaseDeep);
  if (value === null || typeof value !== 'object') return value;
  return Object.fromEntries(
    Object.entries(value).map(([key, inner]) => [
      key.charAt(0).toLowerCase() + key.slice(1),
      toCamelCaseDeep(inner),
    ]),
  );
}

/** Only web links leave the app: never `javascript:`, `file:` or app-specific schemes. Mirrors the C# check. */
export function isWebLink(url: string): boolean {
  try {
    const { protocol } = new URL(url);
    return protocol === 'https:' || protocol === 'http:';
  } catch {
    return false;
  }
}
