/**
 * The path of the item a notice of Teams leads to (ADR-0149), only when it is a path of the application itself: it
 * starts with a single "/", with no scheme, no "\" and no "//", also after the dot segments are read ("/.//site"). Anything
 * else, or no item, leads to the home page, so the page never sends the person to another site.
 */
export function safeItemPath(item: string | null): string {
  if (item === null || !item.startsWith('/') || item.startsWith('//') || item.includes('\\')) {
    return '/';
  }
  const origin = 'https://fiscoprojects.invalid';
  try {
    const address = new URL(item, origin);
    return address.origin === origin && !address.pathname.startsWith('//')
      ? `${address.pathname}${address.search}${address.hash}`
      : '/';
  } catch {
    return '/';
  }
}

/** The theme of Teams, as the page receives it: "default" is light, "dark" and "contrast" are dark; nothing follows the system. */
export function schemeOfTeams(theme: string | null): 'light' | 'dark' | undefined {
  if (theme === 'default') {
    return 'light';
  }
  return theme === 'dark' || theme === 'contrast' ? 'dark' : undefined;
}

/**
 * The page of passage of the links of the page in Teams (ADR-0153), with the path of the item, which it checks again:
 * it opens the item, goes straight to the sign-in of Microsoft or shows the sign-in page.
 */
export function teamsOpenPath(path: string): string {
  return `/teams/open?item=${encodeURIComponent(path)}`;
}
