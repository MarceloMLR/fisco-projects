import { color, type ColorScheme, colorSchemes } from './tokens';

// The colors of the brand derived from the main color of the installation (ADR-0082). Every token starts from its
// value in the design system: the hue moves by the difference of hue between the new color and the default one, and
// the chroma scales by their ratio, in OKLCH. The lightness stays, except for the tokens that are the main color itself
// in the light theme. Then the pairs of text and background of the design system are pushed to their minimum
// contrast. With the default color, every token keeps its value.

/** The tokens that follow the main color (design system, "Marca da instalação"). */
export const brandTokenNames = [
  'action',
  'action-hover',
  'action-pressed',
  'action-text',
  'control-selected',
  'focus-ring',
  'sidebar-active-bg',
  'sidebar-text-active',
  'sidebar-indicator',
  'avatar-owner',
  'brand-panel-bg',
  'brand-panel-bg-2',
  'brand-panel-muted',
  'brand-panel-accent',
] as const;

export type BrandTokenName = (typeof brandTokenNames)[number];
export type BrandTokens = Record<BrandTokenName, string>;

/** In the light theme, these are the main color, with the difference of lightness each has to the default one. */
const followLightness: ReadonlySet<BrandTokenName> = new Set([
  'action',
  'action-hover',
  'action-pressed',
  'action-text',
  'control-selected',
  'focus-ring',
  'sidebar-indicator',
  'avatar-owner',
  'brand-panel-bg-2',
]);

/** Contrast the main color needs with white, the label of the main button and the background of links (ADR-0082). */
export const minimumContrastWithWhite = 4.5;

interface Oklch {
  l: number;
  c: number;
  h: number;
}

type Rgb = [number, number, number];

const hexPattern = /^#?([0-9a-fA-F]{6})$/;

/** A #RRGGBB color in capitals, or undefined for anything else (with or without #, any case). */
export function normalizeHex(value: string): string | undefined {
  const match = hexPattern.exec(value.trim());
  return match?.[1] === undefined ? undefined : `#${match[1].toUpperCase()}`;
}

function rgbOf(hex: string): Rgb {
  const value = hex.slice(1, 7);
  return [0, 2, 4].map((start) => parseInt(value.slice(start, start + 2), 16) / 255) as Rgb;
}

function hexOf([r, g, b]: Rgb): string {
  return `#${[r, g, b]
    .map((channel) =>
      Math.round(Math.min(1, Math.max(0, channel)) * 255)
        .toString(16)
        .padStart(2, '0'),
    )
    .join('')}`;
}

const toLinear = (channel: number) =>
  channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
const fromLinear = (channel: number) =>
  channel <= 0.0031308 ? channel * 12.92 : 1.055 * channel ** (1 / 2.4) - 0.055;

/** OKLCH of a sRGB color (Björn Ottosson, OKLab). */
function oklchOf(hex: string): Oklch {
  const [r, g, b] = rgbOf(hex).map(toLinear) as Rgb;
  const l = Math.cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
  const m = Math.cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
  const s = Math.cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
  const lightness = 0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s;
  const a = 1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s;
  const bb = 0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s;
  const hue = (Math.atan2(bb, a) * 180) / Math.PI;
  return { l: lightness, c: Math.hypot(a, bb), h: hue < 0 ? hue + 360 : hue };
}

/** Linear sRGB of an OKLCH color, possibly outside the sRGB gamut. */
function linearOf({ l: lightness, c, h }: Oklch): Rgb {
  const a = c * Math.cos((h * Math.PI) / 180);
  const b = c * Math.sin((h * Math.PI) / 180);
  const l = (lightness + 0.3963377774 * a + 0.2158037573 * b) ** 3;
  const m = (lightness - 0.1055613458 * a - 0.0638541728 * b) ** 3;
  const s = (lightness - 0.0894841775 * a - 1.291485548 * b) ** 3;
  return [
    4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
    -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s,
  ];
}

const inGamut = (rgb: Rgb) => rgb.every((channel) => channel >= -1e-4 && channel <= 1 + 1e-4);

/** The OKLCH color as #RRGGBB, with the chroma reduced until it fits in sRGB. */
function hexOfOklch(color: Oklch): string {
  const target = { ...color, l: Math.min(1, Math.max(0, color.l)) };
  if (!inGamut(linearOf(target))) {
    let low = 0;
    let high = target.c;
    for (let step = 0; step < 24; step += 1) {
      const middle = (low + high) / 2;
      if (inGamut(linearOf({ ...target, c: middle }))) {
        low = middle;
      } else {
        high = middle;
      }
    }
    target.c = low;
  }
  return hexOf(linearOf(target).map(fromLinear) as Rgb);
}

function luminance(hex: string): number {
  const [r, g, b] = rgbOf(hex).map(toLinear) as Rgb;
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

/** Contrast ratio of two opaque #RRGGBB colors (WCAG 2.x). */
export function contrast(first: string, second: string): number {
  const [light, dark] = [luminance(first), luminance(second)].sort((a, b) => b - a) as [
    number,
    number,
  ];
  return (light + 0.05) / (dark + 0.05);
}

/** The default main color: the action of the light theme in the design system. */
export function defaultMainColor(): string {
  return normalizeHex(color('action', 'light')) ?? color('action', 'light');
}

/**
 * Whether a color may be the main color: #RRGGBB with 4.5:1 with white (the label of the main button), the same rule
 * the server checks (InstallationSettings.TryNormalizeMainColor).
 */
export function hasContrast(mainColor: string): boolean {
  const hex = normalizeHex(mainColor);
  return (
    hex !== undefined && contrast(hex, color('on-action', 'light')) >= minimumContrastWithWhite
  );
}

/** Moves the lightness of a color away from the backgrounds until it has the contrast with each of them. */
function withContrast(hex: string, backgrounds: readonly string[], minimum: number): string {
  if (backgrounds.every((background) => contrast(hex, background) >= minimum)) {
    return hex;
  }
  const lighter = luminance(backgrounds[0] ?? hex) < 0.18;
  const start = oklchOf(hex);
  let candidate = hex;
  for (let step = 1; step <= 100; step += 1) {
    candidate = hexOfOklch({ ...start, l: start.l + (lighter ? step : -step) * 0.01 });
    if (backgrounds.every((background) => contrast(candidate, background) >= minimum)) {
      break;
    }
  }
  return candidate;
}

function derive(mainColor: string, scheme: ColorScheme): BrandTokens {
  const base = oklchOf(defaultMainColor());
  const chosen = oklchOf(mainColor);
  const ratio = base.c > 0 ? chosen.c / base.c : 0;
  const tokens = {} as BrandTokens;
  for (const name of brandTokenNames) {
    const original = color(name, scheme);
    if (scheme === 'light' && original.toUpperCase() === defaultMainColor()) {
      // The main color itself, as the Administrator chose it.
      tokens[name] = mainColor.toLowerCase();
      continue;
    }
    const value = oklchOf(original);
    tokens[name] = hexOfOklch({
      l: scheme === 'light' && followLightness.has(name) ? value.l + (chosen.l - base.l) : value.l,
      c: value.c * ratio,
      h: value.h + (chosen.h - base.h),
    });
  }

  // The pairs of the design system, at their minimum contrast; never the main color in the light theme.
  const token = (name: string) => color(name, scheme);
  const keep = (name: BrandTokenName) =>
    scheme === 'light' && tokens[name] === mainColor.toLowerCase();
  const adjust = (name: BrandTokenName, backgrounds: string[], minimum: number) => {
    if (!keep(name)) {
      tokens[name] = withContrast(tokens[name], backgrounds, minimum);
    }
  };
  for (const name of ['action', 'action-hover', 'action-pressed'] as const) {
    // The background of the white label: darker until the label has its contrast.
    if (!keep(name)) {
      tokens[name] = darkerUntil(tokens[name], token('on-action'), 4.5);
    }
  }
  if (!keep('avatar-owner')) {
    tokens['avatar-owner'] = darkerUntil(tokens['avatar-owner'], token('on-avatar-role'), 4.5);
  }
  adjust('action-text', [token('bg-surface'), token('bg-page')], 4.5);
  adjust('control-selected', [token('bg-surface')], 3);
  adjust('focus-ring', [token('bg-page')], 3);
  adjust('sidebar-text-active', [tokens['sidebar-active-bg']], 4.5);
  adjust('brand-panel-muted', [tokens['brand-panel-bg']], 4.5);
  return tokens;
}

/** Makes a background darker until a light text over it has the contrast. */
function darkerUntil(background: string, text: string, minimum: number): string {
  const start = oklchOf(background);
  let candidate = background;
  for (let step = 1; contrast(candidate, text) < minimum && step <= 100; step += 1) {
    candidate = hexOfOklch({ ...start, l: start.l - step * 0.01 });
  }
  return candidate;
}

/**
 * The tokens that follow the secondary color, when the installation has one (ADR-0100): the sidebar, the support
 * highlights (the selected line, the unread notification and the mention) and the panel of the sign-in page.
 */
export const secondaryTokenNames = [
  'sidebar-bg',
  'sidebar-hover-bg',
  'sidebar-active-bg',
  'sidebar-text',
  'sidebar-text-active',
  'sidebar-muted',
  'sidebar-indicator',
  'sidebar-divider',
  'bg-selected',
  'brand-panel-bg',
  'brand-panel-bg-2',
  'brand-panel-muted',
  'brand-panel-accent',
] as const;

export type SecondaryTokenName = (typeof secondaryTokenNames)[number];

/**
 * The tokens of a secondary color, with the color of the name of the installation at the top of the sidebar
 * (sidebar-title), which is the text color of the design system without a secondary color.
 */
export type SecondaryTokens = Record<SecondaryTokenName | 'sidebar-title', string>;

/** Whether a background is dark, so the texts over it are light. */
const isDark = (hex: string) => luminance(hex) < 0.18;

/** The background of the sidebar for a secondary color: the color itself in the light theme, a dark shade of it in the dark one. */
function sidebarBackground(secondary: string, scheme: ColorScheme): string {
  if (scheme === 'light') {
    return secondary.toLowerCase();
  }
  const chosen = oklchOf(secondary);
  return hexOfOklch({
    l: oklchOf(color('sidebar-bg', 'dark')).l,
    c: Math.min(chosen.c, 0.04),
    h: chosen.h,
  });
}

function deriveSecondary(
  secondary: string,
  mainColor: string | null,
  scheme: ColorScheme,
): SecondaryTokens {
  const background = sidebarBackground(secondary, scheme);
  const dark = isDark(background);
  // Over a dark sidebar the steps are lighter, over a light one darker, as the design system does in each theme.
  const step = (hex: string, amount: number) => {
    const value = oklchOf(hex);
    return hexOfOklch({ ...value, l: value.l + (dark ? amount : -amount) });
  };
  const polarity: ColorScheme = dark ? 'dark' : 'light';
  const hover = step(background, 0.05);
  const active = step(background, 0.1);
  // The texts of the design system of the theme that matches the sidebar, pushed to their contrast over it.
  const strong = dark ? color('on-action', 'light') : color('text', 'light');
  const textActive = withContrast(strong, [active], 4.5);
  const mainIndicator = brandTokens(mainColor)[scheme]['sidebar-indicator'];
  const panel = brandTokens(secondary)[scheme];
  // The text of the panel is light over its whole gradient, whatever the secondary color.
  const panelText = color('brand-panel-text', scheme);
  const panelBackground = darkerUntil(panel['brand-panel-bg'], panelText, 4.5);
  const chosen = oklchOf(secondary);
  const base = oklchOf(defaultMainColor());
  const selected = oklchOf(color('bg-selected', scheme));
  return {
    'sidebar-bg': background,
    'sidebar-hover-bg': hover,
    'sidebar-active-bg': active,
    'sidebar-text': withContrast(color('sidebar-text', polarity), [background, hover], 4.5),
    'sidebar-text-active': textActive,
    'sidebar-muted': withContrast(color('sidebar-muted', polarity), [background], 4.5),
    // The main color marks the current page while it shows over the sidebar; otherwise, its text.
    'sidebar-indicator':
      contrast(mainIndicator, active) >= 3 && contrast(mainIndicator, background) >= 3
        ? mainIndicator
        : textActive,
    'sidebar-divider': step(background, 0.08),
    'sidebar-title': withContrast(strong, [background], 4.5),
    // The highlight keeps its lightness and takes the hue of the secondary color.
    'bg-selected': hexOfOklch({
      l: selected.l,
      c: base.c > 0 ? (selected.c * chosen.c) / base.c : 0,
      h: selected.h + (chosen.h - base.h),
    }),
    'brand-panel-bg': panelBackground,
    'brand-panel-bg-2': darkerUntil(panel['brand-panel-bg-2'], panelText, 4.5),
    'brand-panel-muted': withContrast(panel['brand-panel-muted'], [panelBackground], 4.5),
    'brand-panel-accent': panel['brand-panel-accent'],
  };
}

/** The tokens of a secondary color in each theme; null without one (ADR-0100). */
export function secondaryTokens(
  secondaryColor: string | null,
  mainColor: string | null = null,
): Record<ColorScheme, SecondaryTokens> | null {
  const hex = secondaryColor === null ? undefined : normalizeHex(secondaryColor);
  if (hex === undefined) {
    return null;
  }
  return {
    light: deriveSecondary(hex, mainColor, 'light'),
    dark: deriveSecondary(hex, mainColor, 'dark'),
  };
}

/** The parts of the conversation of a ticket whose messages have a color (ADR-0069, ADR-0101), as the API names them. */
export const messageParts = [
  'requester',
  'owner',
  'manager',
  'techLead',
  'development',
  'functional',
] as const;

export type MessagePart = (typeof messageParts)[number];

/** The colors of the messages the installation chose, each #RRGGBB or null for the design system's. */
export type MessageColors = Partial<Record<MessagePart, string | null>>;

/** The tone of the tokens msg-*, role-* and avatar-* of each part. */
const toneOfPart: Record<MessagePart, string> = {
  requester: 'requester',
  owner: 'owner',
  manager: 'manager',
  techLead: 'techlead',
  development: 'dev',
  functional: 'functional',
};

/**
 * The color of a part while the installation keeps the design system's (ADR-0101): the avatar of the part, the strong
 * color of its messages, or, for the requester, whose messages are neutral, the secondary text.
 */
export function defaultMessageColor(part: MessagePart): string {
  const name = part === 'requester' ? 'text-secondary' : `avatar-${toneOfPart[part]}`;
  return normalizeHex(color(name, 'light')) ?? color(name, 'light');
}

/**
 * The tokens of the messages of a part for a color (ADR-0101): each keeps the lightness of the design system and takes
 * the hue and the strength of the color, measured against the avatar of the part (of the assignee, for the neutral
 * requester); the text of the tag and of the name keeps 4.5:1, and the avatar has its white initials.
 */
function deriveMessage(
  part: MessagePart,
  chosenColor: string,
  scheme: ColorScheme,
): Record<string, string> {
  const tone = toneOfPart[part];
  const profile = part === 'requester' ? 'owner' : tone;
  const reference = oklchOf(color(`avatar-${profile}`, 'light'));
  const chosen = oklchOf(chosenColor);
  // A very strong color is held a little, so the cards stay sober (ADR-0065).
  const ratio = reference.c > 0 ? Math.min(chosen.c / reference.c, 1.4) : 0;
  const make = (name: string, shapeName: string) => {
    const own = oklchOf(color(name, scheme));
    const shape = oklchOf(color(shapeName, scheme));
    return hexOfOklch({ l: own.l, c: shape.c * ratio, h: shape.h + (chosen.h - reference.h) });
  };
  const messageBackground = make(`msg-${tone}-bg`, `msg-${profile}-bg`);
  const tokens: Record<string, string> = {
    [`msg-${tone}-bg`]: messageBackground,
    [`msg-${tone}-border`]: make(`msg-${tone}-border`, `msg-${profile}-border`),
  };
  if (part !== 'requester') {
    const tagBackground = make(`role-${tone}-bg`, `role-${tone}-bg`);
    tokens[`role-${tone}-bg`] = tagBackground;
    tokens[`role-${tone}-text`] = withContrast(
      make(`role-${tone}-text`, `role-${tone}-text`),
      [tagBackground, messageBackground, color('bg-surface', scheme)],
      4.5,
    );
    const avatar =
      scheme === 'light' ? chosenColor.toLowerCase() : make(`avatar-${tone}`, `avatar-${tone}`);
    tokens[`avatar-${tone}`] = darkerUntil(avatar, color('on-avatar-role', scheme), 4.5);
  }
  return tokens;
}

/** The tokens of the messages of every part with a color of its own, in a theme; empty without colors (ADR-0101). */
export function messageTokens(
  colors: MessageColors | null | undefined,
  scheme: ColorScheme,
): Record<string, string> {
  const tokens: Record<string, string> = {};
  for (const part of messageParts) {
    const hex = normalizeHex(colors?.[part] ?? '');
    if (hex !== undefined) {
      Object.assign(tokens, deriveMessage(part, hex, scheme));
    }
  }
  return tokens;
}

/** Whether the sidebar is dark in a theme, so it shows the logo for the dark theme (ADR-0100). */
export function sidebarIsDark(secondaryColor: string | null, scheme: ColorScheme): boolean {
  const tokens = secondaryTokens(secondaryColor);
  return isDark(tokens === null ? color('sidebar-bg', scheme) : tokens[scheme]['sidebar-bg']);
}

/**
 * The values of the brand tokens for a main color in each theme; null (or the default color) gives the values of the
 * design system.
 */
export function brandTokens(mainColor: string | null): Record<ColorScheme, BrandTokens> {
  const hex = mainColor === null ? undefined : normalizeHex(mainColor);
  const values = {} as Record<ColorScheme, BrandTokens>;
  for (const scheme of colorSchemes) {
    if (hex === undefined || hex === defaultMainColor()) {
      values[scheme] = Object.fromEntries(
        brandTokenNames.map((name) => [name, color(name, scheme)]),
      ) as BrandTokens;
    } else {
      values[scheme] = derive(hex, scheme);
    }
  }
  return values;
}

function channels(hex: string): string {
  return rgbOf(hex)
    .map((channel) => String(Math.round(channel * 255)))
    .join(' ');
}

/**
 * The CSS custom properties of a theme for a main color, also those of the primary color of MUI, and, with a secondary
 * color, the ones that follow it, which take the place of the main color in the sidebar and in the panel (ADR-0100).
 */
export function brandVariables(
  mainColor: string | null,
  scheme: ColorScheme,
  secondaryColor: string | null = null,
  messageColors: MessageColors | null = null,
): Record<string, string> {
  const tokens = brandTokens(mainColor)[scheme];
  const secondary = secondaryTokens(secondaryColor, mainColor)?.[scheme];
  return {
    ...Object.fromEntries(brandTokenNames.map((name) => [`--${name}`, tokens[name]])),
    '--mui-palette-primary-main': tokens.action,
    '--mui-palette-primary-dark': tokens['action-hover'],
    '--mui-palette-primary-mainChannel': channels(tokens.action),
    '--mui-palette-primary-darkChannel': channels(tokens['action-hover']),
    ...(secondary === undefined
      ? {}
      : Object.fromEntries(Object.entries(secondary).map(([name, value]) => [`--${name}`, value]))),
    // The colors of the messages win over the main color in the avatar of the assignee (ADR-0101).
    ...Object.fromEntries(
      Object.entries(messageTokens(messageColors, scheme)).map(([name, value]) => [
        `--${name}`,
        value,
      ]),
    ),
  };
}

/**
 * The style sheet of a main color for both themes. Its selectors weigh more than the ones of the design system
 * (0,2,0), so it wins whatever the order, also on elements that set their own theme with data-theme.
 */
export function brandStyleSheet(
  mainColor: string | null,
  secondaryColor: string | null = null,
  messageColors: MessageColors | null = null,
): string {
  const block = (selector: string, scheme: ColorScheme) =>
    `${selector} {\n${Object.entries(
      brandVariables(mainColor, scheme, secondaryColor, messageColors),
    )
      .map(([name, value]) => `  ${name}: ${value};`)
      .join('\n')}\n}`;
  return [
    block(':root:not([data-theme="dark"]), [data-theme="light"][data-theme="light"]', 'light'),
    block('[data-theme="dark"][data-theme="dark"]', 'dark'),
  ].join('\n');
}
