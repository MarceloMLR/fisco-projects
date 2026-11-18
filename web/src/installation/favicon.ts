import { productFavicon } from './brand';

/** The two paths of the icon of the timeline (ViewTimelineOutlined of the Material Icons), in a box of 24. */
export const timelinePaths = [
  'M6 15h6v2H6zm6-8h6v2h-6zm-3 4h6v2H9z',
  'M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2m0 16H5V5h14z',
];

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
