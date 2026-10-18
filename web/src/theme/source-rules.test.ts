import { describe, expect, it } from 'vitest';
import indexHtml from '../../index.html?raw';

// Every source file, read as text. The only colors allowed are the ones in the copy of the design tokens.
const sources = import.meta.glob<string>(
  ['/src/**/*.{ts,tsx,css}', '!/src/theme/source-rules.test.ts'],
  {
    query: '?raw',
    import: 'default',
    eager: true,
  },
);

const colorLiteral = /#[0-9a-fA-F]{3,8}(?![\w-])|\b(?:rgba?|hsla?|oklch|oklab|lab|lch|hwb)\(/;

// Spec: estrutura-das-telas.
describe('Cores e fontes do design system', () => {
  it('Cor escrita direto no código', () => {
    const files = { ...sources, '/index.html': indexHtml };
    expect(Object.keys(files).length).toBeGreaterThan(5);

    const offenders = Object.entries(files)
      .filter(([, source]) => colorLiteral.test(source))
      .map(([path]) => path);

    expect(offenders, 'colors come only from src/theme/tokens.json').toEqual([]);
  });
});
