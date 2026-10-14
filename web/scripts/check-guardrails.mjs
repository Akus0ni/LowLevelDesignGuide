// Fails the lint step when code uses patterns the installed design skills forbid.
// See spec 000 §3.3 and docs/adr/0002-styling-tailwind-base-ui.md.
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

export const RULES = [
  {
    id: 'no-transition-all',
    pattern: /(?<![-\w])transition-all\b/,
    message: 'Name the exact properties, e.g. transition-[transform,opacity] (emil-design-eng).',
  },
  {
    id: 'no-h-screen',
    pattern: /(?<![-\w])(?:min-|max-)?h-screen\b/,
    message: 'Use h-dvh / min-h-dvh; 100vh overflows under mobile browser chrome (mobile-native).',
  },
  {
    id: 'no-ease-in',
    pattern: /(?<![-\w])ease-in\b(?!-out)/,
    message: 'Never use an accelerating curve for UI; use ease-out or ease-in-out (emil-design-eng).',
  },
  {
    id: 'no-zoom-lock',
    pattern: /user-scalable\s*=\s*no|maximum-scale\s*=/,
    message: 'Never disable zoom; use 16px inputs instead (mobile-native).',
  },
];

const SCANNED = /\.(tsx?|css|html)$/;
const SKIPPED = /\.test\.(tsx?|mjs)$/;

export function findViolations(text, file = '<inline>') {
  const violations = [];
  text.split(/\r?\n/).forEach((line, index) => {
    for (const rule of RULES) {
      if (rule.pattern.test(line)) {
        violations.push({ file, line: index + 1, rule: rule.id, message: rule.message });
      }
    }
  });
  return violations;
}

function listFiles(dir) {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name);
    return statSync(path).isDirectory() ? listFiles(path) : [path];
  });
}

function main() {
  const root = fileURLToPath(new URL('..', import.meta.url));
  const files = [join(root, 'index.html'), ...listFiles(join(root, 'src'))].filter(
    (file) => SCANNED.test(file) && !SKIPPED.test(file),
  );
  const violations = files.flatMap((file) =>
    findViolations(readFileSync(file, 'utf8'), relative(root, file)),
  );

  for (const v of violations) {
    console.error(`${v.file}:${v.line}  ${v.rule}  ${v.message}`);
  }
  if (violations.length > 0) {
    process.exitCode = 1;
  } else {
    console.log(`guardrails: ${files.length} files clean`);
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  main();
}
