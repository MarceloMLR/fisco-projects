import tokens from './tokens.json';

// tokens.json is an exact copy of project/tokens.json of the FiscoProjects design system
// (version 1792263038-205c, v3.1, 17/10/2026). To update it, copy the file again; never edit values here.

export type ColorScheme = 'light' | 'dark';
export const colorSchemes: readonly ColorScheme[] = ['light', 'dark'];

type SchemeValue = string | Partial<Record<ColorScheme, string>>;
interface NamedToken {
  name: string;
  value: SchemeValue;
}

const aliasPattern = /^\{(.+)\}$/;

const colorTokens: readonly NamedToken[] = tokens.color.tokens;
const shadowTokens: readonly NamedToken[] = tokens.shadow.tokens;
const lengthTokens: readonly NamedToken[] = [
  ...tokens.spacing.tokens,
  ...tokens.radius.tokens,
  ...tokens.size.tokens,
];

function valueFor(token: NamedToken, scheme: ColorScheme): string {
  if (typeof token.value === 'string') {
    return token.value;
  }
  // A token without a value for a scheme inherits the first theme's value (design system format).
  const value = token.value[scheme] ?? token.value.light;
  if (value === undefined) {
    throw new Error(`Token ${token.name} has no value.`);
  }
  return value;
}

function find(list: readonly NamedToken[], name: string): NamedToken {
  const token = list.find((candidate) => candidate.name === name);
  if (token === undefined) {
    throw new Error(`Unknown design token: ${name}.`);
  }
  return token;
}

/** The raw value of a color token in a scheme: a color, or an alias such as `{bg-page}`. */
function rawColor(name: string, scheme: ColorScheme): string {
  return valueFor(find(colorTokens, name), scheme);
}

/** The resolved color of a token in a scheme (aliases followed). */
export function color(name: string, scheme: ColorScheme): string {
  const raw = rawColor(name, scheme);
  const alias = aliasPattern.exec(raw);
  return alias?.[1] === undefined ? raw : color(alias[1], scheme);
}

export function length(name: string): string {
  return valueFor(find(lengthTokens, name), 'light');
}

export function shadow(name: string, scheme: ColorScheme): string {
  return valueFor(find(shadowTokens, name), scheme);
}

export interface TextStyle {
  fontFamily: string;
  fontSize: string;
  lineHeight: string;
  fontWeight: number;
}

export const fontFamilies = tokens.type.families;

export function textStyle(name: string): TextStyle {
  for (const group of tokens.type.groups) {
    const style = group.styles.find((candidate) => candidate.name === name);
    if (style !== undefined) {
      return {
        fontFamily: group.family === 'mono' ? fontFamilies.mono : fontFamilies.sans,
        fontSize: style.fontSize,
        lineHeight: style.lineHeight,
        fontWeight: style.fontWeight,
      };
    }
  }
  throw new Error(`Unknown text style: ${name}.`);
}

export const colorTokenNames: readonly string[] = colorTokens.map((token) => token.name);

/** The values as CSS declarations, aliases kept as var(), as in the design system's tokens.css. */
function declarations(list: readonly NamedToken[], scheme: ColorScheme): string {
  return list
    .map((token) => {
      const raw = valueFor(token, scheme);
      const alias = aliasPattern.exec(raw);
      return `  --${token.name}: ${alias?.[1] === undefined ? raw : `var(--${alias[1]})`};`;
    })
    .join('\n');
}

/**
 * Every design system token as a CSS custom property with its design system name (--bg-page, --sidebar-bg,
 * --space-6...), switching with the data-theme attribute, the same selector the MUI theme uses.
 */
export function cssVariables(): string {
  const schemeBlock = (scheme: ColorScheme) =>
    `${declarations(colorTokens, scheme)}\n${declarations(shadowTokens, scheme)}`;
  const shared = [
    declarations(lengthTokens, 'light'),
    `  --font-sans: ${fontFamilies.sans};`,
    `  --font-mono: ${fontFamilies.mono};`,
  ].join('\n');

  return [
    `:root, [data-theme="light"] {\n${schemeBlock('light')}\n}`,
    `[data-theme="dark"] {\n${schemeBlock('dark')}\n}`,
    `:root {\n${shared}\n}`,
  ].join('\n');
}
