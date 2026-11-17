import symbolWhite from '../assets/brand/gerenware-icone-branca.svg';
import symbolFull from '../assets/brand/gerenware-icone-quadrado-cheio.svg';
import symbol from '../assets/brand/gerenware-icone.svg';
import logoOnDark from '../assets/brand/gerenware-horizontal-fundo-escuro.svg';
import logoOnLight from '../assets/brand/gerenware-horizontal-fundo-claro.svg';

// The brand of the product (ADR-0159, ADR-0160). The files are copies of docs/marca, never redrawn; brand.test.ts
// fails when a copy differs.

/** The name of the product: an installation with this name and no logo of its own shows the whole logo. */
export const productName = 'GerenWare';

/** The symbol (the G path), for any background. */
export const productSymbol = symbol;

/** The symbol with the full square, for where the system rounds the corners itself (the icon of Teams). */
export const productSymbolFull = symbolFull;

/** The symbol in white on transparent, for an outline icon. */
export const productSymbolWhite = symbolWhite;

/** The horizontal logo, for a light or a dark background. */
export function productLogo(dark: boolean): string {
  return dark ? logoOnDark : logoOnLight;
}

/** The icon of the tab of the browser, served from the root of the site (web/public). */
export const productFavicon = '/favicon.svg';

/** Whether the whole logo stands for the name, so the name is not written beside it. */
export function showsProductLogo(name: string, logoUrl: string | null): boolean {
  return logoUrl === null && name === productName;
}
