import { describe, expect, it } from 'vitest';
import { brandTokens } from '../theme/brandColors';
import { color } from '../theme/tokens';
import { badgeIcon, faviconOf } from './favicon';

// A color as the Administrator types it, without "#": colors in the code come only from tokens.json.
const typed = (value: string) => `#${value.toLowerCase()}`;

// Spec: configuracao-da-instalacao, requirement "Logo na aba do navegador" (ADR-0108).
describe('Logo na aba do navegador', () => {
  it('Selo na cor principal', () => {
    const icon = decodeURIComponent(badgeIcon(typed('0F766E')));

    expect(icon).toContain(`fill="${brandTokens(typed('0F766E')).light.action}"`);
    expect(icon).toContain(`fill="${color('on-action', 'light')}"`);
    expect(faviconOf('/api/installation/logo?v=1', null)).toBe('/api/installation/logo?v=1');
    expect(faviconOf(null, null)).toBe(badgeIcon(null));

    // Over a light color, the icon is dark, as the text of the buttons (ADR-0146).
    const light = decodeURIComponent(badgeIcon(typed('F5C400')));
    expect(light).toContain(`fill="${typed('F5C400')}"`);
    expect(light).toContain(`fill="${brandTokens(typed('F5C400')).light['on-action']}"`);
    expect(light).not.toContain(`fill="${color('on-action', 'light')}"`);
  });
});
