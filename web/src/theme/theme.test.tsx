import AddOutlined from '@mui/icons-material/AddOutlined';
import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import Button from '@mui/material/Button';
import Snackbar from '@mui/material/Snackbar';
import { ThemeProvider } from '@mui/material/styles';
import { render, screen } from '@testing-library/react';
import indexHtml from '../../index.html?raw';
import mainSource from '../main.tsx?raw';
import { describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
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

// Spec: estrutura-das-telas, requirement "Confirmações no alto da tela" (ADR-0109).
describe('Confirmações no alto da tela', () => {
  it('Confirmação no alto, no meio', () => {
    render(
      <ThemeProvider theme={theme}>
        <Snackbar open message={ptBR.settings.saved} />
      </ThemeProvider>,
    );

    // With no position of its own, a confirmation goes to the top, in the middle, below the top bar of the phone.
    const root = screen.getByText(ptBR.settings.saved).closest('.MuiSnackbar-root');
    if (!(root instanceof HTMLElement)) {
      throw new Error('No snackbar.');
    }
    expect(root).toHaveClass('MuiSnackbar-anchorOriginTopCenter');
    expect(getComputedStyle(root).top).toBe('calc(var(--topbar-height) + var(--space-2))');
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
