import type { ReactNode } from 'react';
import type { Section } from '../app/sections';

export function ComingSoon({ section, children }: { section: Section; children?: ReactNode }) {
  return (
    <section className="pt-8 md:pt-12">
      <h1 className="text-3xl font-semibold md:text-4xl">{section.label}</h1>
      <p className="mt-2 text-muted">{section.summary}</p>
      <p className="mt-6 inline-flex rounded-full bg-surface px-3 py-1 text-sm text-muted">
        Coming in {section.comingIn}
      </p>
      {children}
    </section>
  );
}
