import { describe, expect, it } from 'vitest';
import {
  brandStyleSheet,
  brandTokenNames,
  brandTokens,
  brandVariables,
  contrast,
  defaultMainColor,
  hasContrast,
  normalizeHex,
  defaultMessageColor,
  messageParts,
  messageTokens,
  secondaryTokenNames,
  secondaryTokens,
  sidebarIsDark,
} from './brandColors';
import { color, colorSchemes } from './tokens';

// Colors as the Administrator types them, without "#": colors in the code come only from tokens.json, and these are
// what someone types in General settings, not colors of the screens.
const typed = (value: string) => `#${value.toLowerCase()}`;

/** Main colors with contrast with white, of many hues: teal, wine, violet, brown, graphite, black and red. */
const accepted = ['0F766E', '7A1F3D', '5B21B6', '8A4B08', '374151', '000000', 'B4232A'];

// Spec: configuracao-da-instalacao, requirement "Cor principal" (ADR-0082).
describe('Cores da marca', () => {
  it('Cor padrão', () => {
    for (const mainColor of [null, defaultMainColor(), defaultMainColor().toLowerCase()]) {
      const values = brandTokens(mainColor);
      for (const scheme of colorSchemes) {
        for (const name of brandTokenNames) {
          expect(values[scheme][name], `${scheme} ${name}`).toBe(color(name, scheme));
        }
      }
    }
  });

  it('A cor escolhida no tema claro', () => {
    const light = brandTokens('0f766e').light;

    for (const name of [
      'action',
      'action-text',
      'control-selected',
      'focus-ring',
      'sidebar-indicator',
      'avatar-owner',
      'brand-panel-bg-2',
    ] as const) {
      expect(light[name], name).toBe(typed('0f766e'));
    }
    // The hover is darker than the color, as the default one is darker than the default color.
    expect(contrast(light['action-hover'], color('on-action', 'light'))).toBeGreaterThan(
      contrast(light.action, color('on-action', 'light')),
    );
  });

  it('Contraste nos dois temas', () => {
    for (const mainColor of accepted) {
      expect(hasContrast(mainColor), mainColor).toBe(true);
      const values = brandTokens(mainColor);
      for (const scheme of colorSchemes) {
        const token = (name: string) => color(name, scheme);
        const brand = values[scheme];
        const pairs: [string, string, string, number][] = [
          ['on-action / action', token('on-action'), brand.action, 4.5],
          ['on-action / action-hover', token('on-action'), brand['action-hover'], 4.5],
          ['on-action / action-pressed', token('on-action'), brand['action-pressed'], 4.5],
          ['action-text / bg-surface', brand['action-text'], token('bg-surface'), 4.5],
          ['action-text / bg-page', brand['action-text'], token('bg-page'), 4.5],
          [
            'sidebar-text-active / sidebar-active-bg',
            brand['sidebar-text-active'],
            brand['sidebar-active-bg'],
            4.5,
          ],
          ['control-selected / bg-surface', brand['control-selected'], token('bg-surface'), 3],
          ['focus-ring / bg-page', brand['focus-ring'], token('bg-page'), 3],
          ['on-avatar-role / avatar-owner', token('on-avatar-role'), brand['avatar-owner'], 4.5],
          [
            'brand-panel-text / brand-panel-bg',
            token('brand-panel-text'),
            brand['brand-panel-bg'],
            4.5,
          ],
          [
            'brand-panel-muted / brand-panel-bg',
            brand['brand-panel-muted'],
            brand['brand-panel-bg'],
            4.5,
          ],
        ];
        for (const [pair, text, background, minimum] of pairs) {
          expect(
            contrast(text, background),
            `${mainColor} ${scheme}: ${pair}`,
          ).toBeGreaterThanOrEqual(minimum);
        }
      }
    }
  });

  it('Tokens sempre hexadecimais', () => {
    for (const mainColor of [...accepted, '00FF66', '2200FF']) {
      for (const scheme of colorSchemes) {
        for (const value of Object.values(brandTokens(mainColor)[scheme])) {
          expect(value, `${mainColor} ${scheme}`).toMatch(/^#[0-9a-f]{6}$/);
        }
      }
    }
  });

  it('Cores de status iguais', () => {
    const sheet = brandStyleSheet('7A1F3D');

    expect(sheet).not.toMatch(/--(status|success|warning|danger|notify|text|bg|border)-/);
    expect(Object.keys(brandVariables('7A1F3D', 'dark'))).toEqual([
      ...brandTokenNames.map((name) => `--${name}`),
      '--mui-palette-primary-main',
      '--mui-palette-primary-dark',
      '--mui-palette-primary-mainChannel',
      '--mui-palette-primary-darkChannel',
    ]);
  });

  it('Cor sem contraste', () => {
    expect(hasContrast('FFD400')).toBe(false);
    expect(hasContrast('808080')).toBe(false);
    expect(hasContrast('767676')).toBe(true);
    expect(hasContrast('azul')).toBe(false);
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

/** Secondary colors of every kind: navy, a light yellow, white, black, a red and a light gray. */
const secondaries = ['0B2545', 'FFD400', 'FFFFFF', '000000', 'B4232A', 'E5E7EB'];

// Spec: configuracao-da-instalacao, requirement "Cor secundária" (ADR-0100).
describe('Cor secundária', () => {
  it('Sem cor secundária', () => {
    expect(secondaryTokens(null)).toBeNull();
    expect(secondaryTokens('azul')).toBeNull();
    const variables = brandVariables(null, 'light');
    expect(Object.keys(variables)).not.toContain('--sidebar-bg');
    expect(Object.keys(variables)).not.toContain('--sidebar-title');
  });

  it('Trocar a cor secundária', () => {
    const tokens = secondaryTokens(typed('0B2545'));
    if (tokens === null) {
      throw new Error('No tokens.');
    }
    // The sidebar is the color itself in the light theme, and a dark shade of its hue in the dark one.
    expect(tokens.light['sidebar-bg']).toBe(typed('0B2545'));
    expect(contrast(tokens.dark['sidebar-bg'], color('sidebar-bg', 'dark'))).toBeLessThan(1.3);
    // The highlight keeps the lightness of the design system.
    expect(contrast(tokens.light['bg-selected'], color('bg-selected', 'light'))).toBeLessThan(1.1);
    const variables = brandVariables(null, 'light', typed('0B2545'));
    for (const name of [...secondaryTokenNames, 'sidebar-title']) {
      expect(variables[`--${name}`], name).toMatch(/^#[0-9a-f]{6}$/);
    }
    expect(brandStyleSheet(null, typed('0B2545'))).toContain(`--sidebar-bg: ${typed('0B2545')};`);
  });

  it('Contraste na barra lateral', () => {
    for (const secondary of secondaries) {
      const tokens = secondaryTokens(typed(secondary), typed('0F766E'));
      if (tokens === null) {
        throw new Error('No tokens.');
      }
      for (const scheme of colorSchemes) {
        const value = tokens[scheme];
        const label = `${secondary} ${scheme}`;
        expect(contrast(value['sidebar-text'], value['sidebar-bg']), label).toBeGreaterThanOrEqual(
          4.5,
        );
        expect(
          contrast(value['sidebar-text'], value['sidebar-hover-bg']),
          label,
        ).toBeGreaterThanOrEqual(4.5);
        expect(contrast(value['sidebar-muted'], value['sidebar-bg']), label).toBeGreaterThanOrEqual(
          4.5,
        );
        expect(contrast(value['sidebar-title'], value['sidebar-bg']), label).toBeGreaterThanOrEqual(
          4.5,
        );
        expect(
          contrast(value['sidebar-text-active'], value['sidebar-active-bg']),
          label,
        ).toBeGreaterThanOrEqual(4.5);
        expect(
          contrast(value['sidebar-indicator'], value['sidebar-active-bg']),
          label,
        ).toBeGreaterThanOrEqual(3);
        const panelText = color('brand-panel-text', scheme);
        expect(contrast(panelText, value['brand-panel-bg']), label).toBeGreaterThanOrEqual(4.5);
        expect(contrast(panelText, value['brand-panel-bg-2']), label).toBeGreaterThanOrEqual(4.5);
        expect(
          contrast(value['brand-panel-muted'], value['brand-panel-bg']),
          label,
        ).toBeGreaterThanOrEqual(4.5);
      }
    }
  });

  it('Logo escuro na barra lateral escura', () => {
    expect(sidebarIsDark(null, 'light')).toBe(false);
    expect(sidebarIsDark(null, 'dark')).toBe(true);
    expect(sidebarIsDark(typed('0B2545'), 'light')).toBe(true);
    expect(sidebarIsDark(typed('FFD400'), 'light')).toBe(false);
    expect(sidebarIsDark(typed('FFD400'), 'dark')).toBe(true);
  });
});

/** The tone of the tokens of each part of the conversation. */
const tones = {
  requester: 'requester',
  owner: 'owner',
  manager: 'manager',
  techLead: 'techlead',
  development: 'dev',
  functional: 'functional',
} as const;

// Spec: configuracao-da-instalacao, requirement "Cores das mensagens" (ADR-0101).
describe('Cores das mensagens', () => {
  it('Cores padrão das mensagens', () => {
    for (const scheme of colorSchemes) {
      expect(messageTokens({}, scheme)).toEqual({});
      expect(messageTokens(null, scheme)).toEqual({});
    }
    expect(Object.keys(brandVariables(null, 'light'))).not.toContain('--msg-owner-bg');
    expect(defaultMessageColor('owner')).toBe(normalizeHex(color('avatar-owner', 'light')));
  });

  it('Contraste das mensagens', () => {
    for (const chosen of [...secondaries, '7A1F3D', '0F766E']) {
      const colors = Object.fromEntries(messageParts.map((part) => [part, typed(chosen)]));
      for (const scheme of colorSchemes) {
        const tokens = messageTokens(colors, scheme);
        for (const part of messageParts) {
          const tone = tones[part];
          const label = `${chosen} ${scheme} ${part}`;
          expect(tokens[`msg-${tone}-bg`], label).toMatch(/^#[0-9a-f]{6}$/);
          // The cards stay light in the light theme and dark in the dark one, as in the design system.
          expect(
            contrast(tokens[`msg-${tone}-bg`] ?? '', color(`msg-${tone}-bg`, scheme)),
            label,
          ).toBeLessThan(1.15);
          if (part === 'requester') {
            expect(tokens['role-requester-text'], label).toBeUndefined();
            continue;
          }
          const text = tokens[`role-${tone}-text`] ?? '';
          expect(contrast(text, tokens[`role-${tone}-bg`] ?? ''), label).toBeGreaterThanOrEqual(
            4.5,
          );
          expect(contrast(text, tokens[`msg-${tone}-bg`] ?? ''), label).toBeGreaterThanOrEqual(4.5);
          expect(
            contrast(tokens[`avatar-${tone}`] ?? '', color('on-avatar-role', scheme)),
            label,
          ).toBeGreaterThanOrEqual(4.5);
        }
      }
    }
    // A color with contrast with white is the avatar itself in the light theme.
    expect(messageTokens({ owner: typed('7A1F3D') }, 'light')['avatar-owner']).toBe(
      typed('7A1F3D'),
    );
  });
});
