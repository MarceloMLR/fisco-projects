/**
 * The colors the Material 3 theme of Syncfusion takes, from the tokens of the design system (ADR-0051, ADR-0067).
 * The theme writes each color as the three numbers of its red, green and blue, so each token is read as the page draws
 * it, in the light or the dark theme and with the brand color of the installation (ADR-0082), and turned into them.
 */
const palette: Record<string, string> = {
  primary: 'action',
  'on-primary': 'on-action',
  'primary-container': 'bg-selected',
  'on-primary-container': 'action-text',
  secondary: 'text-secondary',
  'secondary-container': 'bg-selected',
  'on-secondary-container': 'text',
  surface: 'bg-surface',
  'surface-variant': 'bg-subtle',
  background: 'bg-surface',
  'secondary-bg-color': 'bg-surface',
  'on-surface': 'text',
  'on-surface-variant': 'text-secondary',
  'on-background': 'text',
  outline: 'border-strong',
  'outline-variant': 'border',
  'inverse-surface': 'bg-inverse',
  'inverse-on-surface': 'text-inverse',
  success: 'success',
  'success-container': 'status-success-bg',
  'success-text': 'status-success-text',
  error: 'danger',
  'on-error': 'on-danger',
  'error-container': 'status-danger-bg',
  'danger-text': 'status-danger-text',
  warning: 'warning',
  'warning-container': 'status-warning-bg',
  'warning-text': 'status-warning-text',
};

/** The three numbers of a color as the browser computes it ("29, 78, 216"); none when it cannot read one. */
export function colorNumbers(computed: string): string | undefined {
  const numbers = computed.match(/[\d.]+/g);
  return numbers === null || numbers.length < 3 ? undefined : numbers.slice(0, 3).join(', ');
}

/** Sets the colors of the theme of Syncfusion on the element, from the tokens it inherits. */
export function applySyncfusionPalette(element: HTMLElement): void {
  const probe = document.createElement('span');
  probe.hidden = true;
  element.appendChild(probe);
  for (const [name, token] of Object.entries(palette)) {
    probe.style.color = `var(--${token})`;
    const numbers = colorNumbers(getComputedStyle(probe).color);
    if (numbers !== undefined) {
      element.style.setProperty(`--color-sf-${name}`, numbers);
    }
  }
  probe.remove();
}
