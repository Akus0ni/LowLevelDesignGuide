/** 24px stroke icon drawn from SVG path data. */
export const Icon = ({ d }: { d: string[] }) => (
  <svg
    viewBox="0 0 24 24"
    width="24"
    height="24"
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
