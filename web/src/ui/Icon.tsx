/** Stroke icon drawn from SVG path data on a 24px grid (rendered at `size`, default 24). */
export const Icon = ({ d, size = 24 }: { d: string[]; size?: number }) => (
  <svg
    viewBox="0 0 24 24"
    width={size}
    height={size}
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    {d.map((path) => (
      <path key={path} d={path} />
    ))}
  </svg>
);
