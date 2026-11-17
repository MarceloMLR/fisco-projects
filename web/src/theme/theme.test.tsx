import AddOutlined from '@mui/icons-material/AddOutlined';
import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import { ThemeProvider } from '@mui/material/styles';
import { render, screen } from '@testing-library/react';
import indexHtml from '../../index.html?raw';
import mainSource from '../main.tsx?raw';
import { describe, expect, it } from 'vitest';
import { Button } from '../components/Button';
import ptBR from '../locales/pt-BR.json';
import { hueOf } from './brandColors';
import { theme } from './theme';
import { color, colorSchemes, colorTokenNames, cssVariables, fontFamilies } from './tokens';

// Spec: estrutura-das-telas.
describe('Cores e fontes do design system', () => {
  it('Cores dos tokens nos dois temas', () => {
    const css = cssVariables();

    for (const scheme of colorSchemes) {
      const block = schemeBlock(css, scheme);
      for (const name of colorTokenNames) {
        expect(block, `--${name} in the ${scheme} theme`).toMatch(new RegExp(`--${name}: [^;]+;`));
      }
      const palette = theme.colorSchemes[scheme]?.palette;
      expect(palette?.primary.main).toBe(color('action', scheme));
      expect(palette?.background.default).toBe(color('bg-page', scheme));
      expect(palette?.text.primary).toBe(color('text', scheme));
      expect(palette?.divider).toBe(color('border', scheme));
    }
    expect(schemeBlock(css, 'dark')).toContain(`--bg-page: ${color('bg-page', 'dark')};`);
  });

  it('Contraste mínimo', () => {
    const pairs = [
      ['text', 'bg-page'],
      ['text-secondary', 'bg-page'],
      ['action-text', 'bg-page'],
      ['on-action', 'action'],
      ['sidebar-text', 'sidebar-bg'],
      ['sidebar-muted', 'sidebar-bg'],
      ['sidebar-text-active', 'sidebar-active-bg'],
      ['text', 'sidebar-bg'],
    ] as const;

    for (const scheme of colorSchemes) {
      for (const [foreground, background] of pairs) {
        const ratio = contrast(color(foreground, scheme), color(background, scheme));
        expect(ratio, `${foreground} on ${background}, ${scheme} theme`).toBeGreaterThanOrEqual(
          4.5,
        );
      }
    }
  });

  it('Paleta da marca', () => {
    // The values of the brand (ADR-0159), written without "#" because colors in the code come only from tokens.json.
    const brand: [string, string, string][] = [
      ['action', '047857', '047857'],
      ['action-hover', '065f46', '065f46'],
      ['action-pressed', '064e3b', '064e3b'],
      ['on-action', 'ffffff', 'ffffff'],
      ['action-text', '047857', '6ee7b7'],
      ['bg-page', 'ffffff', '17191d'],
      ['bg-surface', 'ffffff', '1c1f24'],
      ['bg-elevated', 'ffffff', '23272d'],
      ['sidebar-bg', 'f6f7f9', '121417'],
      ['border', 'e1e4e8', '2b3038'],
      ['border-control', '868d97', '6b727d'],
      ['text', '1c1f24', 'e1e4e8'],
      ['text-secondary', '4b525c', 'aeb4bc'],
      ['status-success-text', '047857', '6ee7b7'],
      ['status-success-bg', 'e3f5ec', '16332d'],
      ['success', '059669', '10b981'],
      ['status-info-text', '0369a1', '7dd3fc'],
      ['status-info-bg', 'e3f2fb', '1c3340'],
      ['signin-bg', '121417', '0c0d0f'],
      ['brand-panel-bg', '1c1f24', '17191d'],
    ];

    for (const [name, light, dark] of brand) {
      expect(color(name, 'light'), `${name} in the light theme`).toBe(`#${light}`);
      expect(color(name, 'dark'), `${name} in the dark theme`).toBe(`#${dark}`);
    }
  });

  it('Contraste dos status e dos papéis', () => {
    const grounds = [
      'bg-page',
      'bg-surface',
      'bg-subtle',
      'bg-hover',
      'bg-selected',
      'bg-elevated',
    ];
    const statuses = ['neutral', 'info', 'warning', 'success', 'danger'];
    const roles = ['requester', 'owner', 'manager', 'techlead', 'dev', 'functional'];
    const pairs: [string, string, number][] = [
      ...statuses.flatMap((status): [string, string, number][] => [
        [`status-${status}-text`, `status-${status}-bg`, 4.5],
        ...grounds.map((ground): [string, string, number] => [
          `status-${status}-text`,
          ground,
          4.5,
        ]),
      ]),
      ...roles.flatMap((role): [string, string, number][] => [
        [`role-${role}-text`, `role-${role}-bg`, 4.5],
        [`role-${role}-text`, `msg-${role}-bg`, 4.5],
        ['text', `msg-${role}-bg`, 4.5],
        ['text-secondary', `msg-${role}-bg`, 4.5],
        ['on-avatar-role', `avatar-${role}`, 4.5],
      ]),
      ...grounds.flatMap((ground): [string, string, number][] => [
        ['text', ground, 4.5],
        ['text-secondary', ground, 4.5],
        ['action-text', ground, 4.5],
      ]),
      ['on-action', 'action-hover', 4.5],
      ['on-action', 'action-pressed', 4.5],
      ['text-placeholder', 'bg-surface', 4.5],
      ['text-placeholder', 'bg-elevated', 4.5],
      ['text-inverse', 'bg-inverse', 4.5],
      ['action-inverse', 'bg-inverse', 4.5],
      ['brand-panel-text', 'brand-panel-bg', 4.5],
      ['brand-panel-text', 'brand-panel-bg-2', 4.5],
      ['brand-panel-muted', 'brand-panel-bg', 4.5],
      ['brand-panel-muted', 'brand-panel-bg-2', 4.5],
      ['brand-panel-accent', 'brand-panel-bg', 4.5],
      ['avatar-text', 'avatar-bg', 4.5],
      // Shapes and outlines: 3:1.
      ['success', 'progress-track', 3],
      ['success', 'bg-surface', 3],
      ['gantt-progress', 'gantt-bar', 3],
      ['gantt-bar-border', 'bg-surface', 3],
      ['border-control', 'bg-surface', 3],
      ['border-control', 'bg-page', 3],
      ['control-selected', 'bg-surface', 3],
      ['focus-ring', 'bg-page', 3],
      ['action', 'bg-page', 3],
    ];

    for (const scheme of colorSchemes) {
      for (const [foreground, background, minimum] of pairs) {
        const ratio = contrast(color(foreground, scheme), color(background, scheme));
        expect(ratio, `${foreground} on ${background}, ${scheme} theme`).toBeGreaterThanOrEqual(
          minimum,
        );
      }
    }
  });

  it('Papéis da conversa em cores diferentes', () => {
    const roles = ['requester', 'owner', 'manager', 'techlead', 'dev', 'functional'];

    for (const scheme of colorSchemes) {
      const hues = roles.map((role) => hueOf(color(`avatar-${role}`, scheme)));
      hues.forEach((hue, index) => {
        hues.slice(index + 1).forEach((other, offset) => {
          const apart = Math.min(Math.abs(hue - other), 360 - Math.abs(hue - other));
          const pair = [roles[index] ?? '', roles[index + 1 + offset] ?? ''];
          // The owner speaks in the emerald of the brand: the others stay further from it.
          expect(apart, `${pair.join(' and ')}, ${scheme} theme`).toBeGreaterThanOrEqual(
            pair.includes('owner') ? 40 : 25,
          );
        });
      });
    }
  });

  it('Fonte servida pela aplicação', () => {
    expect(theme.typography.fontFamily).toBe(fontFamilies.sans);
    expect(fontFamilies.sans.startsWith('"IBM Plex Sans"')).toBe(true);
    expect(fontFamilies.mono.startsWith('"IBM Plex Mono"')).toBe(true);
    // The font files come in the build (packages in the project), never from a font service (ADR-0064).
    for (const weight of ['400', '500']) {
      expect(mainSource).toContain(`import '@fontsource/ibm-plex-sans/latin-${weight}.css';`);
      expect(mainSource).toContain(`import '@fontsource/ibm-plex-mono/latin-${weight}.css';`);
    }
    for (const source of [indexHtml, mainSource]) {
      expect(source).not.toMatch(/fonts\.(googleapis|gstatic)\.com/);
    }
  });
});

// Spec: estrutura-das-telas, requirement "Botões com ícone".
describe('Botões com ícone', () => {
  it('Ícone perto do texto', () => {
    render(
      <ThemeProvider theme={theme}>
        <Button variant="contained" startIcon={<AddOutlined />}>
          {ptBR.settings.save}
        </Button>
        <Button variant="outlined" size="small" endIcon={<ExpandMoreOutlined />}>
          {ptBR.settings.discard}
        </Button>
      </ThemeProvider>,
    );

    // Only the gap of the button (8px) separates the icon from the label, as in the design system (fp-btn).
    const start = screen.getByRole('button', { name: ptBR.settings.save });
    expect(getComputedStyle(start).gap).toBe('var(--space-2)');
    const startIcon = start.querySelector('.MuiButton-startIcon');
    if (startIcon === null) {
      throw new Error('No start icon.');
    }
    expect(Number.parseFloat(getComputedStyle(startIcon).marginRight)).toBe(0);
    const endIcon = screen
      .getByRole('button', { name: ptBR.settings.discard })
      .querySelector('.MuiButton-endIcon');
    if (endIcon === null) {
      throw new Error('No end icon.');
    }
    expect(Number.parseFloat(getComputedStyle(endIcon).marginLeft)).toBe(0);
  });
});

function schemeBlock(css: string, scheme: 'light' | 'dark'): string {
  const start = css.indexOf(
    scheme === 'light' ? ':root, [data-theme="light"] {' : '[data-theme="dark"] {',
  );
  return css.slice(start, css.indexOf('}', start));
}

// WCAG 2 contrast ratio between two opaque colors written as #rrggbb.
function contrast(foreground: string, background: string): number {
  const [lighter, darker] = [luminance(foreground), luminance(background)].sort(
    (a, b) => b - a,
  ) as [number, number];
  return (lighter + 0.05) / (darker + 0.05);
}

function luminance(hex: string): number {
  const channels = [1, 3, 5].map((start) => Number.parseInt(hex.slice(start, start + 2), 16) / 255);
  const [r = 0, g = 0, b = 0] = channels.map((value) =>
    value <= 0.03928 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4,
  );
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}
