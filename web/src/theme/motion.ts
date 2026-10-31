import { useLayoutEffect, useRef } from 'react';

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
  dialog: { enter: 200, exit: 150 },
  drawer: { enter: 250, exit: 200 },
  /** The content of a page, a tab of a project or a section of the Administration that comes in. */
  page: 200,
  /** The counts of the tabs of a project, all together. */
  count: 150,
} as const;

/** A dialog grows from 98% and rises 8px as it fades in; it goes back the same way. */
export const dialogFrames: Keyframe[] = [
  { opacity: 0, transform: 'translateY(8px) scale(0.98)' },
  { opacity: 1, transform: 'none' },
];

/** The drawer of the details slides in from the right; it goes back the same way. */
export const drawerFrames: Keyframe[] = [{ transform: 'translateX(100%)' }, { transform: 'none' }];

/** The content of a page rises 4px as it fades in. */
export const pageFrames: Keyframe[] = [
  { opacity: 0, transform: 'translateY(4px)' },
  { opacity: 1, transform: 'none' },
];

/** The frames of a way in, played backwards for the way out. */
export function backwards(frames: Keyframe[]): Keyframe[] {
  return [...frames].reverse();
}

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
  // A way out that comes before the way in ended, or the other way round, takes its place.
  for (const running of element.getAnimations()) {
    if (running instanceof CSSAnimation || running instanceof CSSTransition) {
      continue;
    }
    running.cancel();
  }
  return element.animate(frames, { duration, easing, fill }).finished.then(
    () => undefined,
    () => undefined,
  );
}

/** The windows that move as the design system says (ADR-0133). */
export type WindowKind = 'dialog' | 'drawer';

interface WindowMove {
  frames: Keyframe[];
  duration: number;
  easing: string;
}

/** How a window comes in or goes away; with less motion, only a fade. */
export function windowMove(kind: WindowKind, entering: boolean): WindowMove {
  const easing = entering ? motion.enter : motion.exit;
  if (reducesMotion()) {
    return { frames: fade(entering), duration: motion.reduced, easing };
  }
  const frames = kind === 'dialog' ? dialogFrames : drawerFrames;
  const times = kind === 'dialog' ? motion.dialog : motion.drawer;
  return {
    frames: entering ? frames : backwards(frames),
    duration: entering ? times.enter : times.exit,
    easing,
  };
}

/** How long the dimmed background of a window takes, as the window itself. */
export function backdropDuration(kind: WindowKind, reduced: boolean) {
  if (reduced) {
    return motion.reduced;
  }
  return kind === 'dialog' ? motion.dialog : motion.drawer;
}

/**
 * Plays the way in of a page when the address of its route changes (ADR-0133): the content rises 4px as it fades in,
 * before the first frame, and the page before has already gone. Not on the first opening, nor when only the search of
 * the address changes, as when a drawer opens.
 */
export function useEnterOnChange<T extends HTMLElement>(key: string) {
  const element = useRef<T>(null);
  const shown = useRef(key);
  useLayoutEffect(() => {
    if (shown.current === key) {
      return;
    }
    shown.current = key;
    const reduced = reducesMotion();
    void play(
      element.current,
      reduced ? fade(true) : pageFrames,
      reduced ? motion.reduced : motion.page,
      motion.enter,
    );
  }, [key]);
  return element;
}
