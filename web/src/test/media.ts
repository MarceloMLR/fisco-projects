// jsdom has no matchMedia: this stand-in answers the queries MUI uses (width and system theme),
// from a window size and a system theme each test can set.

interface SimulatedDevice {
  width: number;
  systemTheme: 'light' | 'dark';
}

const desktop: SimulatedDevice = { width: 1440, systemTheme: 'light' };
let device: SimulatedDevice = { ...desktop };

function matches(query: string): boolean {
  return query
    .replace(/^@media\s*/, '')
    .split(/\s+and\s+/)
    .every((part) => {
      const condition = /\(\s*([a-z-]+)\s*:\s*([^)]+?)\s*\)/.exec(part);
      if (condition === null) {
        return true;
      }
      const [, feature, value = ''] = condition;
      switch (feature) {
        case 'min-width':
          return device.width >= Number.parseFloat(value);
        case 'max-width':
          return device.width <= Number.parseFloat(value);
        case 'prefers-color-scheme':
          return device.systemTheme === value;
        default:
          return false;
      }
    });
}

export function simulateDevice(changes: Partial<SimulatedDevice>): void {
  device = { ...device, ...changes };
}

export function resetDevice(): void {
  device = { ...desktop };
}

export function installMatchMedia(): void {
  // Tests that run in the Node environment (no DOM) have no window to patch.
  if (typeof window === 'undefined') {
    return;
  }
  window.matchMedia = (query: string): MediaQueryList => ({
    media: query,
    matches: matches(query),
    onchange: null,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    addListener: () => undefined,
    removeListener: () => undefined,
    dispatchEvent: () => false,
  });
}
