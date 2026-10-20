import { Link } from 'react-router';
import type { LessonSummary, Mnemonic } from '../../data/types';
import { isPhrase, mnemonicLetters } from './mnemonicTiles';

/**
 * Module mnemonics (spec 004 §3.8) drawn as acrostics: the big first letters, read top to bottom, spell the word
 * (A·P·I·E, S·O·L·I·D). Each row is the lesson that letter stands for, with its icon and hook, and opens it.
 * A phrase (spec 006 §3.4, "Smart Builders Always Follow Plans") spells with each word's first letter; the caption
 * shows the phrase with those letters inked.
 */
export function ModuleMnemonics({ mnemonics, lessons }: { mnemonics: Mnemonic[]; lessons: LessonSummary[] }) {
  const bySlug = new Map(lessons.map((lesson) => [lesson.slug, lesson]));
  return (
    <section aria-label="Remember the module" className="mnemonics">
      {mnemonics.map((mnemonic) => {
        const letters = mnemonicLetters(mnemonic.word);
        const phrase = isPhrase(mnemonic.word);
        const spelled = mnemonic.lessons.map((slug) => bySlug.get(slug)?.title ?? slug).join(', ');
        return (
          <figure key={mnemonic.word} className="mnemonic" aria-label={`${mnemonic.word}: ${spelled}`}>
            <figcaption className="mnemonic-word" data-phrase={phrase || undefined}>
              <span className="mnemonic-stereotype">«remember»</span>
              {phrase ? (
                <span className="mnemonic-phrase">
                  {letters.map(({ letter, rest }, index) => (
                    <span key={index} className="mnemonic-phrase-word">
                      <b>{letter}</b>
                      {rest}
                    </span>
                  ))}
                </span>
              ) : (
                mnemonic.word
              )}
            </figcaption>
            <ol>
              {mnemonic.lessons.map((slug, index) => {
                const lesson = bySlug.get(slug);
                if (!lesson) return null;
                return (
                  <li key={slug}>
                    <Link to={`/learn/${lesson.moduleSlug}/${lesson.slug}`} className="mnemonic-row">
                      <span aria-hidden="true" className="mnemonic-letter">
                        {letters[index]?.letter}
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
