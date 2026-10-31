import { waitFor, within } from '@testing-library/react';
import { expect } from 'vitest';

/** An answer of the simulated API held until the test lets it go, to see the button busy meanwhile (ADR-0131). */
export function heldAnswer() {
  let release: () => void = () => undefined;
  const until = new Promise<void>((done) => {
    release = done;
  });
  return { until, release };
}

/** The button busy: the loading icon in the place of the icon, the label kept and aria-busy (ADR-0131). */
export function expectBusy(button: HTMLElement, label?: string) {
  expect(button).toHaveAttribute('aria-busy', 'true');
  expect(within(button).getByTestId('button-spinner')).toBeInTheDocument();
  if (label !== undefined) {
    expect(button).toHaveTextContent(label);
  }
}

/** The button back to its rest: no loading icon nor aria-busy. */
export function expectIdle(button: HTMLElement) {
  expect(button).not.toHaveAttribute('aria-busy');
  expect(within(button).queryByTestId('button-spinner')).toBeNull();
}

/** Waits for the button to end its busy state, as a person who reads the answer before clicking again. */
export async function untilIdle(button: HTMLElement) {
  await waitFor(() => {
    expectIdle(button);
  });
}
