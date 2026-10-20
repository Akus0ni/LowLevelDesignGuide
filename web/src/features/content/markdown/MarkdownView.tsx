import type { Element, Root } from 'hast';
import { Children, type ComponentProps, type MouseEvent } from 'react';
import Markdown, { type Components } from 'react-markdown';
import { Link } from 'react-router';
import rehypeSlug from 'rehype-slug';
import remarkGfm from 'remark-gfm';
import { useDataSource } from '../../../data/DataSourceContext';
import { isWebLink } from '../../../data/camelCase';
import type { LessonTerm } from '../../../data/types';
import { Callout } from './Callout';
import { CodeBlock } from './CodeBlock';
import { CodeCompare } from './CodeCompare';
import { ArrowLegend } from '../uml/ArrowLegend';
import { CodeUmlPair } from '../uml/CodeUmlPair';
import { fenceOf, markKindOf, sideOf } from './fence';
import { TermsContext } from './terms';
import {
  rehypeCallouts,
  rehypeCompare,
  rehypePair,
  rehypeGoDeeper,
  rehypeInterview,
  rehypeLookalikeSlot,
  type CalloutType,
} from './rehypeContent';
import { LookalikeStrip } from '../patterns/LookalikeStrip';
import { rehypeRecallSections } from '../reader/recall';
import { RecallSection } from '../reader/RecallSection';
import { GoDeeper } from '../reader/GoDeeper';
import {
  InterviewAnswer,
  InterviewQuestion,
  InterviewQuestionHeading,
  InterviewQuestionList,
} from '../reader/InterviewQuestions';
import { MermaidDiagram } from './MermaidDiagram';
import { SectionHeading } from '../../notes/SectionHeading';
import { SectionAnnotationsContext } from '../../notes/sectionAnnotations';

/**
 * Renders lesson and cheat sheet Markdown. Raw HTML is never rendered. Heading ids come from rehype-slug
 * (github-slugger over every heading, in order), the same algorithm the C# packer uses for the outline and links.
 * The leading "# Title" is dropped after slugging: the page header already shows the title.
 */
export function MarkdownView({
  markdown,
  terms = NO_TERMS,
  textSize = 'm',
  recall = false,
}: {
  markdown: string;
  terms?: LessonTerm[];
  /** Reader setting (T14): s / m / l. */
  textSize?: 's' | 'm' | 'l';
  /** Cheat sheets: group `##` sections so recall mode can fold them (lessons fold interview answers instead). */
  recall?: boolean;
}) {
  return (
    <TermsContext value={terms}>
      <div className="reader-prose" data-text-size={textSize}>
        <Markdown
          remarkPlugins={[remarkGfm]}
          rehypePlugins={recall ? CHEAT_SHEET_PLUGINS : LESSON_PLUGINS}
          components={components}
          skipHtml
        >
          {markdown}
        </Markdown>
      </div>
    </TermsContext>
  );
}

const NO_TERMS: LessonTerm[] = [];

/**
 * A learner's own note (spec 007 §3.2 Preview, §3.6): GitHub Markdown and highlighted code only. No term callouts,
 * diagrams, look-alikes or section actions, so a note never looks like the lesson. Headings carry no ids, so they
 * can't clash with the lesson's anchors.
 */
export function NoteMarkdown({ markdown }: { markdown: string }) {
  return (
    <SectionAnnotationsContext value={null}>
      <div className="reader-prose note-prose">
        <Markdown remarkPlugins={[remarkGfm]} components={noteComponents} skipHtml>
          {markdown}
        </Markdown>
      </div>
    </SectionAnnotationsContext>
  );
}

/** Removes a first-child h1 (the title repeated in the body). Runs after rehype-slug, so its id still counts. */
function rehypeDropLeadingTitle() {
  return (tree: Root) => {
    const first = tree.children.findIndex((node) => node.type === 'element');
    const node = tree.children[first];
    if (node?.type === 'element' && node.tagName === 'h1') tree.children.splice(first, 1);
  };
}

const COMMON_PLUGINS = [rehypeSlug, rehypeDropLeadingTitle, rehypeCompare, rehypePair, rehypeCallouts];
const LESSON_PLUGINS = [...COMMON_PLUGINS, rehypeLookalikeSlot, rehypeInterview, rehypeGoDeeper];
const CHEAT_SHEET_PLUGINS = [...COMMON_PLUGINS, rehypeRecallSections];

const components: Components = {
  a: ContentLink,
  h2: ({ node, ...props }) => <SectionHeading {...props} />,
  section: ({ node, children, ...props }) => {
    const recallId = node?.properties.dataRecall;
    if (typeof recallId === 'string') {
      const [heading, body] = Children.toArray(children);
      return <RecallSection id={recallId} heading={heading} body={body} />;
    }
    const qa = node?.properties.dataQa;
    if (typeof qa === 'string') return <InterviewQuestion index={Number(qa)}>{children}</InterviewQuestion>;
    return <section {...props}>{children}</section>;
  },
  div: ({ node, children, ...props }) => {
    const properties = node?.properties ?? {};
    if (properties.dataCompare !== undefined) return <Compare node={node} />;
    if (properties.dataPair !== undefined) return <Pair node={node} />;
    if (properties.dataLookalikes !== undefined) return <LookalikeStrip />;
    if (typeof properties.dataGoDeeper === 'string') {
      return <GoDeeper topics={properties.dataGoDeeper.split('\n').filter(Boolean)}>{children}</GoDeeper>;
    }
    if (typeof properties.dataQaList === 'string') {
      return <InterviewQuestionList count={Number(properties.dataQaList)}>{children}</InterviewQuestionList>;
    }
    if (properties.dataQaAnswer !== undefined) return <InterviewAnswer>{children}</InterviewAnswer>;
    return <div {...props}>{children}</div>;
  },
  h3: ({ node, children, ...props }) =>
    node?.properties.dataQaQuestion !== undefined ? (
      <InterviewQuestionHeading id={props.id}>{children}</InterviewQuestionHeading>
    ) : (
      <h3 {...props}>{children}</h3>
    ),
  blockquote: ({ node, children, ...props }) => {
    const type = node?.properties.dataCallout;
    return typeof type === 'string' ? (
      <Callout type={type as CalloutType}>{children}</Callout>
    ) : (
      <blockquote {...props}>{children}</blockquote>
    );
  },
  table: ({ node, ...props }) => (
    // Wide tables scroll inside their own box; the page never scrolls sideways.
    <div className="table-scroll" tabIndex={0} role="region" aria-label="Table">
      <table {...props} />
    </div>
  ),
  pre: ({ node }) => {
    const fence = fenceOf(node);
    if (!fence) return null;
    if (fence.language === 'mermaid') {
      return <MermaidDiagram source={fence.code} steps={fence.meta.flags.includes('steps')} />;
    }
    if (fence.language === 'uml-arrows') return <ArrowLegend />;
    return (
      <CodeBlock
        code={fence.code}
        info={fence.language}
        title={fence.meta.title}
        marks={fence.meta.marks}
        markKind={markKindOf(sideOf(fence.meta))}
      />
    );
  },
};

const noteComponents: Components = {
  a: ContentLink,
  table: components.table,
  pre: ({ node }) => {
    const fence = fenceOf(node);
    return fence ? <CodeBlock code={fence.code} info={fence.language} /> : null;
  },
};

/** The diagram and code rehypePair grouped. */
function Pair({ node }: { node?: Element }) {
  const [diagram, code] = (node?.children ?? []).filter((child): child is Element => child.type === 'element');
  const diagramFence = fenceOf(diagram);
  const codeFence = fenceOf(code);
  return diagramFence && codeFence ? <CodeUmlPair diagram={diagramFence} code={codeFence} /> : null;
}

/** The two `<pre>`s rehypeCompare grouped. */
function Compare({ node }: { node?: Element }) {
  const [before, after] = (node?.children ?? []).filter((child): child is Element => child.type === 'element');
  const beforeFence = fenceOf(before);
  const afterFence = fenceOf(after);
  return beforeFence && afterFence ? <CodeCompare before={beforeFence} after={afterFence} /> : null;
}

/**
 * Links were rewritten when content was packed: lessons and cheat sheets are app routes, headings are `?at=anchor`,
 * source files are full GitHub URLs. Web links always leave the app (system browser in MAUI, a new tab in the browser).
 */
function ContentLink({ href = '', children, node, ...props }: ComponentProps<'a'> & { node?: unknown }) {
  const source = useDataSource();

  if (href.startsWith('/')) {
    return (
      <Link to={href} {...props}>
        {children}
      </Link>
    );
  }

  if (href.startsWith('?')) {
    return (
      <Link to={{ search: href }} {...props}>
        {children}
      </Link>
    );
  }

  if (isWebLink(href)) {
    const open = (event: MouseEvent<HTMLAnchorElement>) => {
      event.preventDefault();
      void source.openExternal(href);
    };
    return (
      <a
        href={href}
        target="_blank"
        rel="noopener noreferrer"
        onClick={open}
        className="external-link"
        {...props}
      >
        {children}
        <span className="sr-only"> (opens outside the app)</span>
      </a>
    );
  }

  return <span {...props}>{children}</span>;
}
