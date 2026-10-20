/** A note's first words as plain text: no Markdown marks, no #tags (the cards show tags as chips). */
export function excerpt(body: string, maxChars: number): string {
  const plain = body
    .replace(/(^|\s)#[A-Za-z][A-Za-z0-9-]{0,29}(?=\s|$|[.,;:!?)])/g, '$1')
    .replace(/[*_`>#~]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
  return plain.length > maxChars ? `${plain.slice(0, maxChars)}…` : plain;
}
