import type { Element, ElementContent, Root } from 'hast';
import type { ComponentProps, MouseEvent } from 'react';
import Markdown, { type Components } from 'react-markdown';
import { Link } from 'react-router';
import rehypeSlug from 'rehype-slug';
import remarkGfm from 'remark-gfm';
import { useDataSource } from '../../../data/DataSourceContext';
import { isWebLink } from '../../../data/camelCase';
import { CodeBlock } from './CodeBlock';
import { MermaidDiagram } from './MermaidDiagram';

/**
 * Renders lesson and cheat sheet Markdown. Raw HTML is never rendered. Heading ids come from rehype-slug
 * (github-slugger over every heading, in order), the same algorithm the C# packer uses for the outline and links.
 * The leading "# Title" is dropped after slugging: the page header already shows the title.
 */
export function MarkdownView({ markdown }: { markdown: string }) {
  return (
    <div className="reader-prose">
      <Markdown
        remarkPlugins={[remarkGfm]}
        rehypePlugins={[rehypeSlug, rehypeDropLeadingTitle]}
        components={components}
        skipHtml
      >
        {markdown}
      </Markdown>
    </div>
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

function textOf(node: ElementContent): string {
  if (node.type === 'text') return node.value;
  return node.type === 'element' ? node.children.map(textOf).join('') : '';
}

const components: Components = {
  a: ContentLink,
  table: ({ node, ...props }) => (
    // Wide tables scroll inside their own box; the page never scrolls sideways.
    <div className="table-scroll" tabIndex={0} role="region" aria-label="Table">
      <table {...props} />
    </div>
  ),
  pre: ({ node }) => {
    const code = node?.children.find(
      (child): child is Element => child.type === 'element' && child.tagName === 'code',
    );
    if (!code) return null;
    const classes = (code.properties.className as string[] | undefined) ?? [];
    const info = classes.find((name) => name.startsWith('language-'))?.slice('language-'.length);
    const source = code.children.map(textOf).join('').replace(/\n$/, '');
    return info === 'mermaid' ? <MermaidDiagram source={source} /> : <CodeBlock code={source} info={info} />;
  },
};

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
