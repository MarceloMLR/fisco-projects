// The colors as a person types or pastes them from the manual of a brand (ADR-0146): HEX, RGB or HSL, always kept as
// #RRGGBB. And the conversions the color picker works with: HSV for its area and its hue bar, HSL for the format.

/** The formats the color picker shows the value in. */
export type ColorFormat = 'hex' | 'rgb' | 'hsl';

/** A color by its hue (0 to 360), saturation and value (0 to 1), as the area and the hue bar of the picker. */
export interface Hsv {
  h: number;
  s: number;
  v: number;
}

type Channels = [number, number, number];

const clamp = (value: number, low: number, high: number) => Math.min(high, Math.max(low, value));

function hexOf(channels: Channels): string {
  return `#${channels
    .map((channel) => clamp(Math.round(channel), 0, 255).toString(16).padStart(2, '0'))
    .join('')
    .toUpperCase()}`;
}

function channelsOf(hex: string): Channels {
  return [1, 3, 5].map((start) => parseInt(hex.slice(start, start + 2), 16)) as Channels;
}

function hslToChannels(hue: number, saturation: number, lightness: number): Channels {
  const s = saturation / 100;
  const l = lightness / 100;
  const a = s * Math.min(l, 1 - l);
  const f = (n: number) => {
    const k = (n + hue / 30) % 12;
    return (l - a * Math.max(-1, Math.min(k - 3, 9 - k, 1))) * 255;
  };
  return [f(0), f(8), f(4)];
}

function hslOf(hex: string): Channels {
  const [r, g, b] = channelsOf(hex).map((channel) => channel / 255) as Channels;
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const lightness = (max + min) / 2;
  const delta = max - min;
  if (delta === 0) {
    return [0, 0, lightness * 100];
  }
  const saturation = delta / (1 - Math.abs(2 * lightness - 1));
  const hue =
    max === r ? ((g - b) / delta) % 6 : max === g ? (b - r) / delta + 2 : (r - g) / delta + 4;
  return [(hue * 60 + 360) % 360, saturation * 100, lightness * 100];
}

const hex6 = /^#?([0-9a-f]{6})$/;
const hex3 = /^#?([0-9a-f]{3})$/;
// The RGB function (with or without the alpha, with commas or spaces) or only the three channels, as 15, 118, 110;
// the alpha is left out.
const rgb =
  /^(?:rgba?\(\s*)?(\d{1,3})\s*[,\s]\s*(\d{1,3})\s*[,\s]\s*(\d{1,3})\s*(?:[,/]\s*[\d.]+%?\s*)?\)?$/;
// The HSL function, as the RGB one, with the hue in degrees and the saturation and lightness in percent; the alpha is
// left out.
const hsl =
  /^hsla?\(\s*([\d.]+)(?:deg)?\s*[,\s]\s*([\d.]+)%?\s*[,\s]\s*([\d.]+)%?\s*(?:[,/]\s*[\d.]+%?\s*)?\)$/;

/** A color typed in HEX, RGB or HSL as #RRGGBB in capitals, or undefined when it is no color in these formats. */
export function parseColor(text: string): string | undefined {
  const value = text.trim().toLowerCase();
  const long = hex6.exec(value);
  if (long?.[1] !== undefined) {
    return `#${long[1].toUpperCase()}`;
  }
  const short = hex3.exec(value);
  if (short?.[1] !== undefined) {
    const digits = short[1];
    return `#${[0, 1, 2]
      .map((index) => digits.charAt(index).repeat(2))
      .join('')
      .toUpperCase()}`;
  }
  const byChannels = rgb.exec(value);
  // The parenthesis closes only the function: "rgb" opens it, the three channels alone have none.
  if (byChannels !== null && value.startsWith('rgb') === value.endsWith(')')) {
    const channels = byChannels.slice(1, 4).map(Number) as Channels;
    return channels.every((channel) => channel <= 255) ? hexOf(channels) : undefined;
  }
  const byHsl = hsl.exec(value);
  if (byHsl !== null) {
    const [hue, saturation, lightness] = byHsl.slice(1, 4).map(Number) as Channels;
    return saturation <= 100 && lightness <= 100
      ? hexOf(hslToChannels(hue % 360, saturation, lightness))
      : undefined;
  }
  return undefined;
}

/** A CSS color function with its arguments, as the RGB and the HSL ones. */
export function colorFunction(name: 'rgb' | 'hsl', args: string): string {
  return `${name}(${args})`;
}

/** A #RRGGBB color written in a format: in hexadecimal, or as the RGB or the HSL function. */
export function formatColor(hex: string, format: ColorFormat): string {
  if (format === 'rgb') {
    return colorFunction('rgb', channelsOf(hex).join(', '));
  }
  if (format === 'hsl') {
    const [hue, saturation, lightness] = hslOf(hex).map(Math.round) as Channels;
    return colorFunction(
      'hsl',
      `${String(hue % 360)}, ${String(saturation)}%, ${String(lightness)}%`,
    );
  }
  return hex.toUpperCase();
}

/** The hue, saturation and value of a #RRGGBB color. */
export function hsvOf(hex: string): Hsv {
  const [r, g, b] = channelsOf(hex).map((channel) => channel / 255) as Channels;
  const max = Math.max(r, g, b);
  const delta = max - Math.min(r, g, b);
  const hue =
    delta === 0
      ? 0
      : max === r
        ? ((g - b) / delta) % 6
        : max === g
          ? (b - r) / delta + 2
          : (r - g) / delta + 4;
  return { h: (hue * 60 + 360) % 360, s: max === 0 ? 0 : delta / max, v: max };
}

/** The #RRGGBB color of a hue, saturation and value. */
export function hexOfHsv({ h, s, v }: Hsv): string {
  const f = (n: number) => {
    const k = (n + h / 60) % 6;
    return (v - v * s * Math.max(0, Math.min(k, 4 - k, 1))) * 255;
  };
  return hexOf([f(5), f(3), f(1)]);
}

const hueStops = [0, 60, 120, 180, 240, 300, 360]
  .map((hue) => `${hexOfHsv({ h: hue % 360, s: 1, v: 1 })} ${String((hue / 360) * 100)}%`)
  .join(', ');

/** The colors of every hue in a line, as the hue bar of the picker and the mark of a swatch that opens it. */
export function hueGradient(direction = 'to right'): string {
  return `linear-gradient(${direction}, ${hueStops})`;
}
