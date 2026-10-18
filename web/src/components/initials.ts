/** First letters of the first and the last name, as the design system Avatar shows. */
export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  const first = parts.at(0)?.at(0) ?? '';
  const last = parts.length > 1 ? (parts.at(-1)?.at(0) ?? '') : '';
  return (first + last).toUpperCase();
}
