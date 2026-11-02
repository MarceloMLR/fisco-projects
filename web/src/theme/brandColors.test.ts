import { describe, expect, it } from 'vitest';
import {
  type BrandColorChoice,
  brandStyleSheet,
  brandTokenNames,
  brandTokens,
  brandVariables,
  colorNotes,
  contrast,
  defaultColors,
  defaultMainColor,
  isDefaultChoice,
  normalizeHex,
  sidebarIsDark,
  textOn,
} from './brandColors';
import { color, colorSchemes } from './tokens';

// Colors as the Administrator types them, without "#": colors in the code come only from tokens.json, and these are
// what someone types in Colors, not colors of the screens.
const typed = (value: string) => `#${value.toLowerCase()}`;

/**
 * Colors of every kind, as a company brand may have them: dark (teal, wine, violet, brown, graphite, black, red), light
 * (yellow, lime, sky, white), mid (gray, orange) and very saturated (green, blue).
 */
const colors = [
  '0F766E',
  '7A1F3D',
  '5B21B6',
  '8A4B08',
  '374151',
  '000000',
  'B4232A',
  'F5C400',
  'C6F432',
  '9FD3FF',
  'FFFFFF',
  '808080',
  'F07A12',
  '00FF66',
  '2200FF',
];

const choice = (
  main: string | null,
  secondary: string | null = null,
  sidebar: string | null = null,
) => ({ main, secondary, sidebar }) satisfies BrandColorChoice;

/** Every pair of text and background the colors of the installation touch, with its minimum. */
function pairsOf(colorsChosen: BrandColorChoice) {
  const values = brandTokens(colorsChosen);
  return colorSchemes.flatMap((scheme) => {
    const token = (name: string) => color(name, scheme);
    const brand = values[scheme];
    const pairs: [string, string, string, number][] = [
      ['on-action / action', brand['on-action'], brand.action, 4.5],
      ['on-action / action-hover', brand['on-action'], brand['action-hover'], 4.5],
      ['on-action / action-pressed', brand['on-action'], brand['action-pressed'], 4.5],
      ['action-text / bg-surface', brand['action-text'], token('bg-surface'), 4.5],
      ['action-text / bg-page', brand['action-text'], token('bg-page'), 4.5],
      ['control-selected / bg-surface', brand['control-selected'], token('bg-surface'), 3],
      [
        'on-control-selected / control-selected',
        brand['on-control-selected'],
        brand['control-selected'],
        4.5,
      ],
      ['focus-ring / bg-page', brand['focus-ring'], token('bg-page'), 3],
      ['on-avatar-role / avatar-owner', token('on-avatar-role'), brand['avatar-owner'], 4.5],
      ['sidebar-text / sidebar-bg', brand['sidebar-text'], brand['sidebar-bg'], 4.5],
      ['sidebar-muted / sidebar-bg', brand['sidebar-muted'], brand['sidebar-bg'], 4.5],
      ['sidebar-text / sidebar-hover-bg', brand['sidebar-text'], brand['sidebar-hover-bg'], 4.5],
      [
        'sidebar-text-active / sidebar-active-bg',
        brand['sidebar-text-active'],
        brand['sidebar-active-bg'],
        4.5,
      ],
      ['sidebar-indicator / sidebar-bg', brand['sidebar-indicator'], brand['sidebar-bg'], 3],
      [
        'brand-panel-text / brand-panel-bg',
        token('brand-panel-text'),
        brand['brand-panel-bg'],
        4.5,
      ],
      [
        'brand-panel-text / brand-panel-bg-2',
        token('brand-panel-text'),
        brand['brand-panel-bg-2'],
        4.5,
      ],
      [
        'brand-panel-muted / brand-panel-bg',
        brand['brand-panel-muted'],
        brand['brand-panel-bg'],
        4.5,
      ],
      ['text / bg-selected', token('text'), brand['bg-selected'], 4.5],
    ];
    return pairs.map(([pair, text, background, minimum]) => ({
      scheme,
      pair,
      text,
      background,
      minimum,
    }));
  });
}

function expectReadable(colorsChosen: BrandColorChoice) {
  for (const { scheme, pair, text, background, minimum } of pairsOf(colorsChosen)) {
    expect(
      contrast(text, background),
      `${JSON.stringify(colorsChosen)} ${scheme}: ${pair}`,
    ).toBeGreaterThanOrEqual(minimum);
  }
}

// Spec: configuracao-da-instalacao, requirements "Cor principal" and "Contraste calculado sobre as cores" (ADR-0146).
describe('Cor principal', () => {
  it('Cor padrão', () => {
    for (const colorsChosen of [
      defaultColors,
      choice(defaultMainColor()),
      choice(defaultMainColor().toLowerCase()),
      null,
    ]) {
      const values = brandTokens(colorsChosen);
      for (const scheme of colorSchemes) {
        for (const name of brandTokenNames) {
          expect(values[scheme][name], `${scheme} ${name}`).toBe(
            normalizeHex(color(name, scheme))?.toLowerCase(),
          );
        }
      }
    }
    expect(isDefaultChoice(defaultColors)).toBe(true);
    expect(isDefaultChoice(choice(null, null, 'FFFFFF'))).toBe(false);
  });

  it('Trocar a cor principal', () => {
    const { light, dark } = brandTokens(choice('0f766e'));

    for (const name of [
      'action',
      'action-text',
      'control-selected',
      'focus-ring',
      'avatar-owner',
    ] as const) {
      expect(light[name], name).toBe(typed('0f766e'));
    }
    expect(light['on-action']).toBe(color('on-action', 'light'));
    // The hover is darker than the color, as the default one is darker than the default color.
    expect(contrast(light['action-hover'], light['on-action'])).toBeGreaterThan(
      contrast(light.action, light['on-action']),
    );
    expect(dark.action).not.toBe(color('action', 'dark'));
  });

  it('Contraste nos dois temas', () => {
    for (const main of colors) {
      expectReadable(choice(main));
    }
  });

  it('Texto escuro sobre uma cor clara', () => {
    const { light } = brandTokens(choice('F5C400'));

    expect(light.action).toBe(typed('F5C400'));
    expect(light['on-action']).not.toBe(color('on-action', 'light'));
    expect(contrast(light['on-action'], light.action)).toBeGreaterThanOrEqual(4.5);
    expect(colorNotes(choice('F5C400')).main).toContain('darkText');
  });

  it('Texto legível sobre qualquer cor', () => {
    for (const background of ['808080', '777777', '8A8A8A', 'FFFFFF', '000000']) {
      expect(
        contrast(textOn(typed(background)), typed(background)),
        background,
      ).toBeGreaterThanOrEqual(4.5);
    }
  });

  it('Links legíveis com uma cor clara', () => {
    const { light } = brandTokens(choice('F5C400'));

    expect(light['action-text']).not.toBe(typed('F5C400'));
    expect(contrast(light['action-text'], color('bg-surface', 'light'))).toBeGreaterThanOrEqual(
      4.5,
    );
    expect(colorNotes(choice('F5C400')).main).toContain('darkerLinks');
  });

  it('Cor com bom contraste', () => {
    const { light } = brandTokens(choice('0F766E'));

    expect(light['action-text']).toBe(typed('0F766E'));
    expect(colorNotes(choice('0F766E')).main).toEqual([]);
  });

  it('Botão no tema escuro', () => {
    const { dark } = brandTokens(choice('0B1F3A'));

    expect(dark.action).not.toBe(typed('0B1F3A'));
    expect(contrast(dark.action, color('bg-page', 'dark'))).toBeGreaterThanOrEqual(3);
    expect(contrast(dark['on-action'], dark.action)).toBeGreaterThanOrEqual(4.5);
    expect(colorNotes(choice('0B1F3A')).main).toContain('lighterInDark');
  });

  it('Tokens sempre hexadecimais', () => {
    for (const main of colors) {
      for (const colorsChosen of [choice(main), choice(main, main, main)]) {
        for (const scheme of colorSchemes) {
          for (const value of Object.values(brandTokens(colorsChosen)[scheme])) {
            expect(value, `${main} ${scheme}`).toMatch(/^#[0-9a-f]{6}$/);
          }
        }
      }
    }
  });

  it('Cores de status iguais', () => {
    const sheet = brandStyleSheet(choice('7A1F3D', 'C0395A', '1F2933'));

    expect(sheet).not.toMatch(/--(status|success|warning|danger|notify|text|border)[-:]/);
    expect(sheet).not.toMatch(/--bg-(page|surface|subtle|hover)/);
    expect(Object.keys(brandVariables(choice('7A1F3D'), 'dark'))).toEqual([
      ...brandTokenNames.map((name) => `--${name}`),
      '--mui-palette-primary-main',
      '--mui-palette-primary-dark',
      '--mui-palette-primary-contrastText',
      '--mui-palette-primary-mainChannel',
      '--mui-palette-primary-darkChannel',
    ]);
  });

  it('Formato da cor', () => {
    expect(normalizeHex('0f766e')).toBe(typed('0F766E').toUpperCase());
    expect(normalizeHex(' 7a1f3d ')).toBe(typed('7A1F3D').toUpperCase());
    expect(normalizeHex('7a1f3')).toBeUndefined();
    expect(normalizeHex('verde')).toBeUndefined();
  });

  it('Folha de estilo que vale sobre a do tema', () => {
    const sheet = brandStyleSheet('0F766E');

    expect(sheet).toContain(
      ':root:not([data-theme="dark"]), [data-theme="light"][data-theme="light"] {',
    );
    expect(sheet).toContain('[data-theme="dark"][data-theme="dark"] {');
    expect(sheet).toContain(`  --action: ${typed('0f766e')};`);
    expect(sheet).toContain('  --mui-palette-primary-mainChannel: 15 118 110;');
  });
});

// Spec: configuracao-da-instalacao, requirement "Cor secundária" (ADR-0146).
describe('Cor secundária', () => {
  it('Sem cor secundária', () => {
    for (const scheme of colorSchemes) {
      const brand = brandTokens(choice('0F766E'))[scheme];
      expect(brand['bg-selected'], scheme).toBe(color('bg-selected', scheme));
    }
    // The panel of the sign-in page follows the main color.
    expect(brandTokens(choice('0F766E')).light['brand-panel-bg-2']).toBe(typed('0F766E'));
  });

  it('Trocar a cor secundária', () => {
    const { light, dark } = brandTokens(choice(null, 'C0395A'));

    expect(light['bg-selected']).not.toBe(color('bg-selected', 'light'));
    expect(dark['bg-selected']).not.toBe(color('bg-selected', 'dark'));
    expect(light['brand-panel-bg-2']).toBe(typed('C0395A'));
    // The main button stays in the main color.
    expect(light.action).toBe(color('action', 'light'));
  });

  it('Painel da entrada legível', () => {
    const { light } = brandTokens(choice(null, 'F5C400'));

    expect(light['brand-panel-bg-2']).not.toBe(typed('F5C400'));
    expect(
      contrast(color('brand-panel-text', 'light'), light['brand-panel-bg-2']),
    ).toBeGreaterThanOrEqual(4.5);
    expect(colorNotes(choice(null, 'F5C400')).secondary).toEqual(['darkerPanel']);
  });

  it('Contraste de qualquer cor secundária', () => {
    for (const secondary of colors) {
      expectReadable(choice('0F766E', secondary));
    }
  });
});

// Spec: configuracao-da-instalacao, requirement "Cor da barra lateral" (ADR-0146).
describe('Cor da barra lateral', () => {
  it('Sem cor da barra lateral', () => {
    const values = brandTokens(choice('0F766E'));
    for (const scheme of colorSchemes) {
      for (const name of [
        'sidebar-bg',
        'sidebar-text',
        'sidebar-muted',
        'sidebar-hover-bg',
        'sidebar-divider',
      ] as const) {
        expect(values[scheme][name], `${scheme} ${name}`).toBe(
          normalizeHex(color(name, scheme))?.toLowerCase(),
        );
      }
    }
    expect(brandVariables(choice('0F766E'), 'light')).not.toHaveProperty('--sidebar-title');
  });

  it('Barra lateral escura no tema claro', () => {
    const { light, dark } = brandTokens(choice(null, null, '1F2933'));

    expect(light['sidebar-bg']).toBe(typed('1F2933'));
    expect(sidebarIsDark(choice(null, null, '1F2933'), 'light')).toBe(true);
    expect(contrast(light['sidebar-text'], light['sidebar-bg'])).toBeGreaterThanOrEqual(4.5);
    expect(dark['sidebar-bg']).not.toBe(typed('1F2933'));
    expect(colorNotes(choice(null, null, '1F2933')).sidebar).toContain('lightSidebarText');
    const title = brandVariables(choice(null, null, '1F2933'), 'light')['--sidebar-title'] ?? '';
    expect(contrast(title, light['sidebar-bg'])).toBeGreaterThanOrEqual(4.5);
  });

  it('Barra lateral clara', () => {
    const { light, dark } = brandTokens(choice(null, null, 'FFFFFF'));

    expect(light['sidebar-bg']).toBe(typed('FFFFFF'));
    expect(sidebarIsDark(choice(null, null, 'FFFFFF'), 'light')).toBe(false);
    // A light sidebar does not fit the dark theme: there, a dark tone of it.
    expect(sidebarIsDark(choice(null, null, 'FFFFFF'), 'dark')).toBe(true);
    expect(dark['sidebar-bg']).not.toBe(typed('FFFFFF'));
    expect(colorNotes(choice(null, null, 'FFFFFF')).sidebar).toContain('darkSidebarText');
  });

  it('Textos da barra lateral legíveis', () => {
    for (const sidebar of colors) {
      expectReadable(choice(null, null, sidebar));
      expectReadable(choice('F5C400', 'F5C400', sidebar));
    }
  });
});

// Spec: configuracao-da-instalacao, requirement "Logo para o tema escuro" (ADR-0100, ADR-0146).
describe('Logo para o tema escuro', () => {
  it('Logo escuro na barra lateral escura', () => {
    expect(sidebarIsDark(defaultColors, 'light')).toBe(false);
    expect(sidebarIsDark(defaultColors, 'dark')).toBe(true);
  });

  it('Logo escuro na barra lateral escura do tema claro', () => {
    expect(sidebarIsDark(choice(null, null, '0B3B6E'), 'light')).toBe(true);
    expect(sidebarIsDark(choice(null, null, 'F6F7F9'), 'light')).toBe(false);
  });
});
