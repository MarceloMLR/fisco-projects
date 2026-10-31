import { useEventCallback, useForkRef } from '@mui/material/utils';
import {
  cloneElement,
  type HTMLAttributes,
  type ReactElement,
  type Ref,
  useLayoutEffect,
  useRef,
} from 'react';
import { fade, play, type WindowKind, windowMove } from '../theme/motion';

interface WindowMotionProps extends Omit<HTMLAttributes<HTMLElement>, 'children'> {
  kind: WindowKind;
  in?: boolean;
  appear?: boolean;
  children: ReactElement<{ ref?: Ref<HTMLElement> }>;
  ref?: Ref<HTMLElement>;
  onEnter?: (node: HTMLElement, isAppearing: boolean) => void;
  onEntered?: (node: HTMLElement, isAppearing: boolean) => void;
  onExit?: (node: HTMLElement) => void;
  onExited?: (node: HTMLElement) => void;
  // What the transitions of MUI take and this one leaves out.
  timeout?: unknown;
  direction?: unknown;
  easing?: unknown;
  container?: unknown;
  addEndListener?: unknown;
  onEntering?: unknown;
  onExiting?: unknown;
}

/** What only the transitions of MUI take, never the window. */
const transitionOnly = new Set([
  'timeout',
  'direction',
  'easing',
  'container',
  'addEndListener',
  'onEntering',
  'onExiting',
]);

/**
 * The transition of a dialog or a drawer for MUI (slots.transition): it plays the way in and the way out of the design
 * system by the Web Animations API and tells MUI when they end, so the window stays on the screen until its way out
 * ends. The rest goes to the window, as the focus handler of the focus trap, which brings the focus back where it was;
 * what only the transitions of MUI take (timeout, direction) is left out.
 */
export function WindowMotion({
  kind,
  in: shown = false,
  appear = true,
  children,
  ref,
  onEnter,
  onEntered,
  onExit,
  onExited,
  ...rest
}: WindowMotionProps) {
  const element = useRef<HTMLElement>(null);
  const handleRef = useForkRef(element, ref, children.props.ref);
  const started = useRef(false);
  const entering = useEventCallback((node: HTMLElement, first: boolean) => {
    onEnter?.(node, first);
  });
  const entered = useEventCallback((node: HTMLElement, first: boolean) => {
    onEntered?.(node, first);
  });
  const exiting = useEventCallback((node: HTMLElement) => {
    onExit?.(node);
  });
  const exited = useEventCallback((node: HTMLElement) => {
    onExited?.(node);
  });

  useLayoutEffect(() => {
    const node = element.current;
    if (node === null) {
      return undefined;
    }
    const first = !started.current;
    started.current = true;
    if (first && !shown) {
      return undefined;
    }
    let current = true;
    if (shown) {
      entering(node, first);
      // Opened with the screen, as a drawer by the address, it is there at once (appear of MUI).
      if (first && !appear) {
        entered(node, first);
        return undefined;
      }
    } else {
      exiting(node);
    }
    const move = windowMove(kind, shown);
    void play(node, move.frames, move.duration, move.easing, shown ? 'none' : 'forwards').then(
      () => {
        if (!current) {
          return;
        }
        if (shown) {
          entered(node, first);
        } else {
          exited(node);
        }
      },
    );
    return () => {
      current = false;
    };
  }, [shown, kind, appear, entering, entered, exiting, exited]);

  const windowProps = Object.fromEntries(
    Object.entries(rest).filter(([name]) => !transitionOnly.has(name)),
  );
  return cloneElement(children, { ...windowProps, ref: handleRef });
}

/** The dialog of the design system, for the transition slot of the Dialog of MUI. */
export function DialogMotion(props: Omit<WindowMotionProps, 'kind'>) {
  return <WindowMotion kind="dialog" {...props} />;
}

/** The drawer of the details, for the transition slot of the Drawer of MUI. */
export function DrawerMotion(props: Omit<WindowMotionProps, 'kind'>) {
  return <WindowMotion kind="drawer" {...props} />;
}

/**
 * The way out of a window the screen takes away while it is open, as a dialog shown only while a state lasts
 * (ADR-0133): placed inside the window, it copies the window when it leaves the page and plays the way out on the copy,
 * which takes no click and no screen reader, and is gone at the end. Without animations (the tests), nothing is copied.
 */
export function LeavesWithMotion({ kind, open }: { kind: WindowKind; open: boolean }) {
  const marker = useRef<HTMLSpanElement>(null);
  const openNow = useRef(open);
  useLayoutEffect(() => {
    openNow.current = open;
  }, [open]);
  useLayoutEffect(() => {
    const root = marker.current?.closest<HTMLElement>('.MuiModal-root') ?? null;
    const lastOpen = openNow;
    return () => {
      // Closed as usual, its way out already ended; open, the screen took it away.
      if (root === null || !lastOpen.current) {
        return;
      }
      // Only once it really left the page: the second mounting of the development mode keeps it.
      queueMicrotask(() => {
        if (!root.isConnected) {
          leaveCopy(root, kind);
        }
      });
    };
  }, [kind]);
  return <span ref={marker} hidden />;
}

function leaveCopy(root: HTMLElement, kind: WindowKind) {
  if (typeof root.animate !== 'function') {
    return;
  }
  const copy = root.cloneNode(true) as HTMLElement;
  copy.setAttribute('aria-hidden', 'true');
  copy.inert = true;
  copy.style.pointerEvents = 'none';
  copy.dataset.leaving = kind;
  document.body.append(copy);
  const move = windowMove(kind, false);
  const backdrop = copy.querySelector('.MuiBackdrop-root');
  const surface = copy.querySelector(
    kind === 'dialog' ? '.MuiDialog-container' : '.MuiDrawer-paper',
  );
  void Promise.all([
    play(backdrop, fade(false), move.duration, move.easing, 'forwards'),
    play(surface, move.frames, move.duration, move.easing, 'forwards'),
  ]).then(() => {
    copy.remove();
  });
}
