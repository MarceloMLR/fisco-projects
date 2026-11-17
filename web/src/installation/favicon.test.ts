import { describe, expect, it } from 'vitest';
import { productFavicon } from './brand';
import { faviconOf, showFavicon } from './favicon';

// Spec: configuracao-da-instalacao, requirement "Ícone da aba do navegador" (ADR-0108, ADR-0160).
describe('Ícone da aba do navegador', () => {
  it('Símbolo na aba sem logo', () => {
    expect(faviconOf(null)).toBe(productFavicon);
    expect(faviconOf('/api/installation/logo?v=1')).toBe('/api/installation/logo?v=1');
  });

  it('Logo no lugar do ícone da página', () => {
    document.head.innerHTML = '<link rel="icon" href="/favicon.svg" type="image/svg+xml" />';

    showFavicon('/api/installation/logo?v=1');

    const links = document.head.querySelectorAll('link[rel="icon"]');
    expect(links).toHaveLength(1);
    expect(links[0]?.getAttribute('href')).toBe('/api/installation/logo?v=1');
    // The logo may be a PNG: the type of the symbol does not stay.
    expect(links[0]?.hasAttribute('type')).toBe(false);
  });
});
