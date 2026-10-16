import { Link } from 'react-router';
import type { LessonSummary, Mnemonic } from '../../data/types';

/**
 * Module mnemonics (spec 004 §3.8) drawn as acrostics: the big first letters, read top to bottom, spell the word
 * (A·P·I·E, S·O·L·I·D). Each row is the lesson that letter stands for, with its icon and hook, and opens it.
 */
export function ModuleMnemonics({ mnemonics, lessons }: { mnemonics: Mnemonic[]; lessons: LessonSummary[] }) {
  const bySlug = new Map(lessons.map((lesson) => [lesson.slug, lesson]));
  return (
    <section aria-label="Remember the module" className="mnemonics">
      {mnemonics.map((mnemonic) => {
        const letters = [...mnemonic.word].filter((c) => /\p{L}/u.test(c));
        const spelled = mnemonic.lessons.map((slug) => bySlug.get(slug)?.title ?? slug).join(', ');
        return (
          <figure key={mnemonic.word} className="mnemonic" aria-label={`${mnemonic.word}: ${spelled}`}>
            <figcaption className="mnemonic-word">
              <span className="mnemonic-stereotype">«remember»</span>
              {mnemonic.word}
            </figcaption>
            <ol>
              {mnemonic.lessons.map((slug, index) => {
                const lesson = bySlug.get(slug);
                if (!lesson) return null;
                return (
                  <li key={slug}>
                    <Link to={`/learn/${lesson.moduleSlug}/${lesson.slug}`} className="mnemonic-row">
                      <span aria-hidden="true" className="mnemonic-letter">
                        {letters[index]}
                      </span>
                      <span aria-hidden="true" className="lesson-icon mnemonic-icon">
                        {lesson.icon}
                      </span>
                      <span className="min-w-0 flex-1">
                        <span className="mnemonic-title">{lesson.title}</span>
                        {lesson.hook && <span className="mnemonic-hook">{lesson.hook}</span>}
                      </span>
                    </Link>
                  </li>
                );
              })}
            </ol>
          </figure>
        );
      })}
    </section>
  );
}
