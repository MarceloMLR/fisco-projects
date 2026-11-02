import { describe, expect, it } from 'vitest';
import { colorFunction, formatColor, hexOfHsv, hsvOf, parseColor } from './colorFormats';

// Colors as the Administrator types them: the "#" and the functions are put together here, because colors in the code
// come only from tokens.json, and these are what someone types, not colors of the screens.
const typed = (value: string) => `#${value}`;

// Spec: configuracao-da-instalacao, requirement "Seletor de cores" (ADR-0146).
describe('Formatos de cor', () => {
  it('Cor em vários formatos', () => {
    const teal = typed('0F766E');
    for (const text of [
      typed('0F766E'),
      '0f766e',
      ` ${typed('0f766e')} `,
      colorFunction('rgb', '15, 118, 110'),
      colorFunction('rgb', '15 118 110'),
      `${colorFunction('rgb', '15, 118, 110').replace('rgb', 'rgba').slice(0, -1)}, 0.5)`,
      '15, 118, 110',
      'RGB(15,118,110)'.toLowerCase(),
      colorFunction('hsl', '175, 77%, 26%'),
      colorFunction('hsl', '175deg 77% 26%'),
    ]) {
      const parsed = parseColor(text);
      expect(parsed, text).toBeDefined();
      // HSL rounds to whole percents: the color comes back within a step of each channel.
      const [r, g, b] = [1, 3, 5].map((start) =>
        parseInt((parsed ?? '').slice(start, start + 2), 16),
      );
      expect(
        Math.abs((r ?? 0) - 15) + Math.abs((g ?? 0) - 118) + Math.abs((b ?? 0) - 110),
        text,
      ).toBeLessThanOrEqual(4);
    }
    expect(parseColor(typed('0f7'))).toBe(typed('00FF77'));
    expect(parseColor('0f766e')).toBe(teal);
  });

  it('Valor que não é cor', () => {
    for (const text of [
      '',
      'verde',
      typed('0F766'),
      typed('GGGGGG'),
      colorFunction('rgb', '300, 0, 0'),
      colorFunction('hsl', '175, 120%, 26%'),
      '15, 118',
      colorFunction('rgb', '15, 118, 110').slice(0, -1),
      '15, 118, 110)',
    ]) {
      expect(parseColor(text), text).toBeUndefined();
    }
  });

  it('Cor escrita em cada formato', () => {
    expect(formatColor(typed('0f766e'), 'hex')).toBe(typed('0F766E'));
    expect(formatColor(typed('0F766E'), 'rgb')).toBe(colorFunction('rgb', '15, 118, 110'));
    expect(formatColor(typed('0F766E'), 'hsl')).toBe(colorFunction('hsl', '175, 77%, 26%'));
    for (const hex of [
      typed('0F766E'),
      typed('F5C400'),
      typed('FFFFFF'),
      typed('000000'),
      typed('8E1B3A'),
    ]) {
      expect(parseColor(formatColor(hex, 'rgb'))).toBe(hex);
    }
  });

  it('Cor pela área e pela matiz', () => {
    for (const hex of [
      typed('0F766E'),
      typed('F5C400'),
      typed('FFFFFF'),
      typed('000000'),
      typed('8E1B3A'),
    ]) {
      expect(hexOfHsv(hsvOf(hex))).toBe(hex);
    }
    expect(hsvOf(typed('FF0000'))).toEqual({ h: 0, s: 1, v: 1 });
  });
});
