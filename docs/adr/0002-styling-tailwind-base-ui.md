# ADR 0002: Styling with Tailwind v4 + base-ui, guarded by skill rules

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-14 |
| **Spec** | 000 |

## Context
The owner chose Tailwind + base-ui (from the `pick-ui-library` skill) and asked for the choice
to be critiqued against the installed design skills: `emil-design-eng`, `mobile-native` and
`apple-design`.

## Decision
Use **Tailwind CSS v4 + `clsx` + `cva` + `@base-ui/react`**, with a guardrail layer that
overrides Tailwind defaults the skills reject. The guardrails are enforced by **theme tokens**
and by a **lint step that fails the build**.

## Critique summary
| Tailwind default | Guardrail | Skill |
|---|---|---|
| Weak built-in easing curves; an accelerating curve is available | Custom `--ease-out`, `--ease-in-out`, `--ease-drawer`; the accelerating curve is removed from the theme and banned by lint | emil-design-eng |
| `transition-all` | Banned by lint; name properties explicitly | emil-design-eng |
| `hover:` only checks `(hover: hover)` | Redefined to `(hover: hover) and (pointer: fine)` | mobile-native §1 |
| `h-screen` = `100vh` | Banned by lint; use `h-dvh` | mobile-native §3 |
| 14px input text | 16px inputs on coarse pointers | mobile-native §4 |
| No tap-highlight, overscroll, touch-action or user-select defaults | Baseline layer in `web/src/styles/index.css` | mobile-native baseline |
| No safe-area utilities | `pt-safe`, `pb-safe`, `pl-safe`, `pr-safe` | mobile-native §7 |
| Hard `border` dividers under chrome | Translucent tab bar/sidebar + soft scroll-edge fade | apple-design §12 |
| One tracking value everywhere | Tight tracking on headings, ~0 on body | apple-design §15 |
| No press feedback | `active:scale-[0.97]` ~120 ms in the cva base | emil-design-eng, mobile-native §5 |

## WebView-specific notes
- The web `theme-color` meta does not colour the status bar inside MAUI; the native page sets
  it with `CommunityToolkit.Maui` `StatusBarBehavior`, matching the `bg` token.
- The MAUI page is full-bleed (`SafeAreaEdges="None"`) and CSS pads with `env(safe-area-inset-*)`.
  Whether each platform's WebView reports insets is recorded in spec 000 `plan.md` §4.

## Consequences
- `npm run lint` fails on `transition-all`, `h-screen`, the accelerating easing class and
  zoom-locking viewports (`web/scripts/check-guardrails.mjs`, tested by fixtures).
- New components get press feedback and correct hover behaviour by default.
- Hardware-only behaviour (tap feel, keyboard, real safe areas) still needs a physical device.
