import { productFavicon } from './brand';

/**
 * The icon of the tab of the browser (ADR-0108, ADR-0160): the logo of the installation, when it has one; otherwise
 * the symbol of the product, which is also the icon the page starts with.
 */
export function faviconOf(logoUrl: string | null): string {
  return logoUrl ?? productFavicon;
}

/** Puts the icon in the head of the page, in the one link of the icon. */
export function showFavicon(href: string): void {
  let link = document.head.querySelector<HTMLLinkElement>('link[rel="icon"]');
  if (link === null) {
    link = document.createElement('link');
    link.rel = 'icon';
    document.head.append(link);
  }
  // The logo of an installation may be a PNG: the type of the first icon no longer says it.
  link.removeAttribute('type');
  link.href = href;
}
