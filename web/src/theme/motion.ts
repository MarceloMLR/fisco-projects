/**
 * The motion of the screens (ADR-0133): short, with the curves of the design system, and only a fade when the person
 * asks the system for less motion.
 */
export const motion = {
  /** Things that come in: fast at first, settling at the end. */
  enter: 'cubic-bezier(0.2, 0, 0, 1)',
  /** Things that go away: slow at first, gone at the end. */
  exit: 'cubic-bezier(0.4, 0, 1, 1)',
  /** The only motion with less motion: a fade. */
  reduced: 150,
  notice: { enter: 200, exit: 150 },
} as const;

/** Whether the person asked the system for less motion (prefers-reduced-motion). */
export function reducesMotion(): boolean {
  try {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  } catch {
    return false;
  }
}

/** A fade in or out, the motion kept when the person asks for less motion. */
export function fade(entering: boolean): Keyframe[] {
  return entering ? [{ opacity: 0 }, { opacity: 1 }] : [{ opacity: 1 }, { opacity: 0 }];
}

/**
 * Plays an animation on the element and tells when it ends; at once where the browser cannot animate (the tests), so
 * nothing waits for it.
 */
export function play(
  element: Element | null,
  frames: Keyframe[],
  duration: number,
  easing: string,
  fill: FillMode = 'none',
): Promise<void> {
  if (element === null || typeof element.animate !== 'function') {
    return Promise.resolve();
  }
  return element.animate(frames, { duration, easing, fill }).finished.then(
    () => undefined,
    () => undefined,
  );
}
