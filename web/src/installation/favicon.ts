import { brandTokens } from '../theme/brandColors';
import { color, length } from '../theme/tokens';

/** The two paths of the icon of the timeline (ViewTimelineOutlined of the Material Icons), in a box of 24. */
const timelinePaths = [
  'M6 15h6v2H6zm6-8h6v2h-6zm-3 4h6v2H9z',
  'M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2m0 16H5V5h14z',
];

/** The badge of the sidebar: 28px with the icon of 18px, drawn here in a box of 24 (ADR-0108). */
const badgeSize = 28;
const iconSize = 18;

/**
 * The badge of the product as the icon of the tab of the browser (ADR-0108): the icon of the timeline in the color of
 * the text over the action, on the main color of the installation (or the one of the design system), with the corners
 * of the badge of the sidebar. A data address, so it needs no file.
 */
export function badgeIcon(mainColor: string | null): string {
  const action = brandTokens(mainColor).light.action;
  const onAction = color('on-action', 'light');
  const radius = (Number.parseFloat(length('radius-md')) * 24) / badgeSize;
  const scale = iconSize / badgeSize;
  const offset = (24 - 24 * scale) / 2;
  const svg = [
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">',
    `<rect width="24" height="24" rx="${radius.toFixed(2)}" fill="${action}"/>`,
    `<g transform="translate(${offset.toFixed(2)} ${offset.toFixed(2)}) scale(${scale.toFixed(4)})" fill="${onAction}">`,
    ...timelinePaths.map((path) => `<path d="${path}"/>`),
    '</g></svg>',
  ].join('');
  return `data:image/svg+xml,${encodeURIComponent(svg)}`;
}

/** The icon of the tab of the browser: the logo of the installation, when it has one; otherwise the badge. */
export function faviconOf(logoUrl: string | null, mainColor: string | null): string {
  return logoUrl ?? badgeIcon(mainColor);
}

/** Puts the icon in the head of the page, in the one link of the icon. */
export function showFavicon(href: string): void {
  let link = document.head.querySelector<HTMLLinkElement>('link[rel="icon"]');
  if (link === null) {
    link = document.createElement('link');
    link.rel = 'icon';
    document.head.append(link);
  }
  link.href = href;
}
