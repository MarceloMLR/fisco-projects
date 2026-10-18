import { render } from '@testing-library/react';
import indexHtml from '../../index.html?raw';
import mainSource from '../main.tsx?raw';
import { describe, expect, it } from 'vitest';
import { simulateDevice } from '../test/media';
import { AppThemeProvider } from './AppThemeProvider';
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

describe('Tema automático', () => {
  it('Tema segue o sistema', () => {
    for (const systemTheme of colorSchemes) {
      simulateDevice({ systemTheme });

      const { unmount } = render(<AppThemeProvider>{null}</AppThemeProvider>);

      expect(document.documentElement).toHaveAttribute('data-theme', systemTheme);
      unmount();
    }
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
