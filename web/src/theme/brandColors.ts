import { color, type ColorScheme, colorSchemes } from './tokens';

// The colors of the installation (ADR-0082, ADR-0146): the main one, the secondary one and the one of the sidebar, each
// optional. Any color is accepted: the text over it is white or dark, whichever reads, and only what is text or an
// outline moves its lightness until the contrast of WCAG AA. The tokens that are no color of the installation start
// from their value in the design system, with the hue and the chroma of the color, in OKLCH. Without any color, every
// token keeps its value in the design system, and the status colors never change.

/** The tokens the colors of the installation change (design system, "Marca da instalação", and ADR-0146). */
export const brandTokenNames = [
  'action',
  'action-hover',
  'action-pressed',
  'on-action',
  'action-text',
  'control-selected',
  'on-control-selected',
  'focus-ring',
  'avatar-owner',
  'bg-selected',
  'sidebar-bg',
  'sidebar-hover-bg',
  'sidebar-active-bg',
  'sidebar-text',
  'sidebar-text-active',
  'sidebar-muted',
  'sidebar-indicator',
  'sidebar-divider',
  'sidebar-border',
  'brand-panel-bg',
  'brand-panel-bg-2',
  'brand-panel-muted',
  'brand-panel-accent',
] as const;

export type BrandTokenName = (typeof brandTokenNames)[number];
export type BrandTokens = Record<BrandTokenName, string>;

/** The three colors of the installation, each #RRGGBB (any case, with or without #) or null for the default one. */
export interface BrandColorChoice {
  main: string | null;
  secondary: string | null;
  sidebar: string | null;
}

/** No color chosen: the design system as it is. */
export const defaultColors: BrandColorChoice = { main: null, secondary: null, sidebar: null };

/** The colors of the brand as the API gives them. */
export function colorsOf(
  brand:
    | { mainColor: string | null; secondaryColor: string | null; sidebarColor: string | null }
    | undefined,
): BrandColorChoice {
  return {
    main: brand?.mainColor ?? null,
    secondary: brand?.secondaryColor ?? null,
    sidebar: brand?.sidebarColor ?? null,
  };
}

/** Contrast of a text over its background (WCAG AA), and of an outline or a large shape. */
const textContrast = 4.5;
const outlineContrast = 3;

/** Below this relative luminance a background is dark: the sidebar shows the logo for the dark theme over it. */
const darkLuminance = 0.18;

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

/** The lightness of a color moved by a step, in OKLCH. */
function shifted(hex: string, step: number): string {
  const value = oklchOf(hex);
  return hexOfOklch({ ...value, l: value.l + step });
}

/** Moves the lightness of a color away from the backgrounds until it has the contrast with each of them. */
function withContrast(hex: string, backgrounds: readonly string[], minimum: number): string {
  if (backgrounds.every((background) => contrast(hex, background) >= minimum)) {
    return hex;
  }
  const lighter = luminance(backgrounds[0] ?? hex) < darkLuminance;
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

/** Makes a background darker until a light text over it has the contrast. */
function darkerUntil(background: string, text: string, minimum: number): string {
  const start = oklchOf(background);
  let candidate = background;
  for (let step = 1; contrast(candidate, text) < minimum && step <= 100; step += 1) {
    candidate = hexOfOklch({ ...start, l: start.l - step * 0.01 });
  }
  return candidate;
}

/** The two colors in sRGB, the second one by the share given. */
function mix(first: string, second: string, share: number): string {
  const [a, b] = [rgbOf(first), rgbOf(second)];
  return hexOf(
    a.map((channel, index) => channel + ((b[index] ?? channel) - channel) * share) as Rgb,
  );
}

/**
 * The text over a color: the white of the design system when it reads (4.5:1), otherwise its dark text, darker until
 * it reads, so any background has a readable text (ADR-0146).
 */
export function textOn(background: string): string {
  const white = color('on-action', 'light');
  if (contrast(white, background) >= textContrast) {
    return white;
  }
  return withContrast(color('text', 'light'), [background], textContrast);
}

/**
 * A token of the design system with the hue of a color and its chroma, scaled by the chroma of the default main
 * color (at most 1.6 times, so a light surface stays light); the lightness of the token stays.
 */
function recolored(token: string, chosen: string, limit = Number.POSITIVE_INFINITY): string {
  const base = oklchOf(defaultMainColor());
  const value = oklchOf(token);
  const target = oklchOf(chosen);
  const ratio = base.c > 0 ? Math.min(limit, target.c / base.c) : 0;
  return hexOfOklch({ l: value.l, c: value.c * ratio, h: value.h + (target.h - base.h) });
}

/** The tokens of a theme in the design system, as they are with no color chosen. */
function designTokens(scheme: ColorScheme): BrandTokens {
  return Object.fromEntries(
    brandTokenNames.map((name) => [name, normalizeHex(color(name, scheme))?.toLowerCase()]),
  ) as BrandTokens;
}

/** A color typed by the Administrator, or undefined for the default one. */
function chosenColor(value: string | null): string | undefined {
  return value === null ? undefined : normalizeHex(value)?.toLowerCase();
}

/** The main color (ADR-0146): the buttons, the links, the controls, the focus, the owner and the active item. */
function applyMain(tokens: BrandTokens, main: string, scheme: ColorScheme) {
  const design = designTokens(scheme);
  // The tokens that are no color of the installation: the hue and the chroma of the main color (ADR-0082).
  for (const name of [
    'sidebar-active-bg',
    'sidebar-text-active',
    'brand-panel-bg',
    'brand-panel-bg-2',
    'brand-panel-muted',
    'brand-panel-accent',
  ] as const) {
    tokens[name] = recolored(design[name], main);
  }
  const surfaces = [color('bg-surface', scheme), color('bg-page', scheme)];
  const action =
    scheme === 'light' ? main : withContrast(main, [color('bg-page', scheme)], outlineContrast);
  const onAction = textOn(action);
  const step = (name: 'action-hover' | 'action-pressed') =>
    oklchOf(design[name]).l - oklchOf(design.action).l;
  tokens.action = action;
  tokens['on-action'] = onAction;
  tokens['action-hover'] = withContrast(
    shifted(action, step('action-hover')),
    [onAction],
    textContrast,
  );
  tokens['action-pressed'] = withContrast(
    shifted(action, step('action-pressed')),
    [onAction],
    textContrast,
  );
  tokens['action-text'] = withContrast(main, surfaces, textContrast);
  tokens['control-selected'] = withContrast(main, [color('bg-surface', scheme)], outlineContrast);
  tokens['on-control-selected'] = textOn(tokens['control-selected']);
  tokens['focus-ring'] = withContrast(main, [color('bg-page', scheme)], outlineContrast);
  tokens['avatar-owner'] = darkerUntil(action, color('on-avatar-role', scheme), textContrast);
  if (scheme === 'light') {
    tokens['brand-panel-bg-2'] = main;
  }
}

/** The secondary color (ADR-0146): the selected row, the unread notification, the mention and the sign-in panel. */
function applySecondary(tokens: BrandTokens, secondary: string, scheme: ColorScheme) {
  const design = designTokens(scheme);
  tokens['bg-selected'] = recolored(design['bg-selected'], secondary, 1.6);
  tokens['brand-panel-bg'] = recolored(design['brand-panel-bg'], secondary);
  tokens['brand-panel-bg-2'] =
    scheme === 'light' ? secondary : recolored(design['brand-panel-bg-2'], secondary);
  tokens['brand-panel-muted'] = recolored(design['brand-panel-muted'], secondary);
  tokens['brand-panel-accent'] = recolored(design['brand-panel-accent'], secondary);
}

/** The color of the sidebar (ADR-0146): the background, and the texts, the hover and the dividers that follow it. */
function applySidebar(tokens: BrandTokens, sidebar: string, scheme: ColorScheme): string {
  const chosen = oklchOf(sidebar);
  // In the dark theme, a dark tone of the hue: a light sidebar does not fit the dark theme.
  const background =
    scheme === 'light'
      ? sidebar
      : hexOfOklch({ l: 0.215, c: Math.min(chosen.c, 0.04), h: chosen.h });
  const dark = luminance(background) < darkLuminance;
  // The design system's own sidebar for a background as dark as this one, then each pair to its contrast.
  const like = designTokens(dark ? 'dark' : 'light');
  const step = (name: BrandTokenName) => oklchOf(like[name]).l - oklchOf(like['sidebar-bg']).l;
  const text = withContrast(like['sidebar-text'], [background], textContrast);
  // The hover moves away from the text when the design system's way would take the contrast from it.
  const hover = [step('sidebar-hover-bg'), -step('sidebar-hover-bg')]
    .map((change) => shifted(background, change))
    .find((candidate) => contrast(text, candidate) >= textContrast);
  tokens['sidebar-bg'] = background;
  tokens['sidebar-hover-bg'] = hover ?? shifted(background, step('sidebar-hover-bg'));
  tokens['sidebar-text'] = withContrast(
    text,
    [background, tokens['sidebar-hover-bg']],
    textContrast,
  );
  tokens['sidebar-muted'] = withContrast(like['sidebar-muted'], [background], textContrast);
  tokens['sidebar-divider'] = shifted(background, step('sidebar-divider'));
  tokens['sidebar-border'] = tokens['sidebar-divider'];
  tokens['sidebar-active-bg'] = mix(
    shifted(background, dark ? 0.08 : -0.06),
    tokens.action,
    dark ? 0.22 : 0.12,
  );
  tokens['sidebar-text-active'] = dark
    ? recolored(like['sidebar-text-active'], tokens.action)
    : tokens['action-text'];
  return withContrast(color('text', dark ? 'dark' : 'light'), [background], textContrast);
}

/** The tokens of a theme for the colors chosen, and the name of the installation over the sidebar, if it changes. */
function derive(
  choice: BrandColorChoice,
  scheme: ColorScheme,
): { tokens: BrandTokens; sidebarTitle: string | null } {
  const tokens = designTokens(scheme);
  const main = chosenColor(choice.main);
  const secondary = chosenColor(choice.secondary);
  const sidebar = chosenColor(choice.sidebar);
  if (main !== undefined && main.toUpperCase() !== defaultMainColor()) {
    applyMain(tokens, main, scheme);
  }
  if (secondary !== undefined) {
    applySecondary(tokens, secondary, scheme);
  }
  const sidebarTitle = sidebar === undefined ? null : applySidebar(tokens, sidebar, scheme);
  if (main !== undefined || sidebar !== undefined) {
    // The active item and its outline over the sidebar, whatever painted them.
    tokens['sidebar-text-active'] = withContrast(
      tokens['sidebar-text-active'],
      [tokens['sidebar-active-bg']],
      textContrast,
    );
    tokens['sidebar-indicator'] = withContrast(
      main === undefined ? tokens['sidebar-indicator'] : tokens['action-text'],
      [tokens['sidebar-bg']],
      outlineContrast,
    );
  }
  if (main !== undefined || secondary !== undefined) {
    // The panel of the sign-in page: its white text and the muted one readable.
    for (const name of ['brand-panel-bg', 'brand-panel-bg-2'] as const) {
      tokens[name] = darkerUntil(tokens[name], color('brand-panel-text', scheme), textContrast);
    }
    tokens['brand-panel-muted'] = withContrast(
      tokens['brand-panel-muted'],
      [tokens['brand-panel-bg']],
      textContrast,
    );
  }
  return { tokens, sidebarTitle };
}

function choiceOf(colors: BrandColorChoice | string | null): BrandColorChoice {
  return typeof colors === 'string' || colors === null
    ? { ...defaultColors, main: colors }
    : colors;
}

/** Whether no color is chosen, so the design system stays as it is. */
export function isDefaultChoice(colors: BrandColorChoice | string | null): boolean {
  const choice = choiceOf(colors);
  return (
    chosenColor(choice.secondary) === undefined &&
    chosenColor(choice.sidebar) === undefined &&
    (chosenColor(choice.main) === undefined ||
      chosenColor(choice.main)?.toUpperCase() === defaultMainColor())
  );
}

/**
 * The values of the tokens of the installation in each theme for its colors (or only a main color); no color (or the
 * default main color) gives the values of the design system.
 */
export function brandTokens(
  colors: BrandColorChoice | string | null,
): Record<ColorScheme, BrandTokens> {
  const choice = choiceOf(colors);
  const values = {} as Record<ColorScheme, BrandTokens>;
  for (const scheme of colorSchemes) {
    values[scheme] = derive(choice, scheme).tokens;
  }
  return values;
}

/**
 * Whether the sidebar is dark in a theme, so it shows the logo for the dark theme (ADR-0100, ADR-0146): the dark
 * theme, or a dark color of the sidebar in the light one.
 */
export function sidebarIsDark(
  colors: BrandColorChoice | string | null,
  scheme: ColorScheme,
): boolean {
  return luminance(brandTokens(colors)[scheme]['sidebar-bg']) < darkLuminance;
}

/** What the screens adjusted over a color, told under its field (ADR-0146). */
export type ColorNote =
  | 'darkText'
  | 'darkerLinks'
  | 'lighterInDark'
  | 'darkerPanel'
  | 'lightSidebarText'
  | 'darkSidebarText'
  | 'darkSidebarTone'
  | 'indicator';

/** The adjustments of each color chosen; an empty list for a color used as it is, or for no color. */
export function colorNotes(colors: BrandColorChoice): Record<keyof BrandColorChoice, ColorNote[]> {
  const { light, dark } = brandTokens(colors);
  const main = chosenColor(colors.main);
  const secondary = chosenColor(colors.secondary);
  const sidebar = chosenColor(colors.sidebar);
  const notes: Record<keyof BrandColorChoice, ColorNote[]> = {
    main: [],
    secondary: [],
    sidebar: [],
  };
  if (main !== undefined && main.toUpperCase() !== defaultMainColor()) {
    if (light['on-action'] !== color('on-action', 'light')) {
      notes.main.push('darkText');
    }
    if (light['action-text'] !== main) {
      notes.main.push('darkerLinks');
    }
    if (dark.action !== main) {
      notes.main.push('lighterInDark');
    }
  }
  if (secondary !== undefined && light['brand-panel-bg-2'] !== secondary) {
    notes.secondary.push('darkerPanel');
  }
  if (sidebar !== undefined) {
    notes.sidebar.push(sidebarIsDark(colors, 'light') ? 'lightSidebarText' : 'darkSidebarText');
    notes.sidebar.push('darkSidebarTone');
    if (light['sidebar-indicator'] !== light['action-text']) {
      notes.sidebar.push('indicator');
    }
  }
  return notes;
}

function channels(hex: string): string {
  return rgbOf(hex)
    .map((channel) => String(Math.round(channel * 255)))
    .join(' ');
}

/**
 * The CSS custom properties of a theme for the colors of the installation, also those of the primary color of MUI,
 * and the color of the name of the installation over a sidebar of its own.
 */
export function brandVariables(
  colors: BrandColorChoice | string | null,
  scheme: ColorScheme,
): Record<string, string> {
  const { tokens, sidebarTitle } = derive(choiceOf(colors), scheme);
  return {
    ...Object.fromEntries(brandTokenNames.map((name) => [`--${name}`, tokens[name]])),
    ...(sidebarTitle === null ? {} : { '--sidebar-title': sidebarTitle }),
    '--mui-palette-primary-main': tokens.action,
    '--mui-palette-primary-dark': tokens['action-hover'],
    '--mui-palette-primary-contrastText': tokens['on-action'],
    '--mui-palette-primary-mainChannel': channels(tokens.action),
    '--mui-palette-primary-darkChannel': channels(tokens['action-hover']),
  };
}

/**
 * The style sheet of the colors for both themes. Its selectors weigh more than the ones of the design system (0,2,0),
 * so it wins whatever the order, also on elements that set their own theme with data-theme.
 */
export function brandStyleSheet(colors: BrandColorChoice | string | null): string {
  const block = (selector: string, scheme: ColorScheme) =>
    `${selector} {\n${Object.entries(brandVariables(colors, scheme))
      .map(([name, value]) => `  ${name}: ${value};`)
      .join('\n')}\n}`;
  return [
    block(':root:not([data-theme="dark"]), [data-theme="light"][data-theme="light"]', 'light'),
    block('[data-theme="dark"][data-theme="dark"]', 'dark'),
  ].join('\n');
}
