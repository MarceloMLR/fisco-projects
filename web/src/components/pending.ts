import { createContext, useEffect, useRef, useState } from 'react';

/** The shortest time a button shows it is busy, so the loading icon is seen even with a quick answer (ADR-0132). */
export const minimumBusyMs = 500;

/** Whether the form around is sending, for its submit buttons (Form, ADR-0131). */
export const FormPending = createContext(false);

function isThenable(value: unknown): value is PromiseLike<unknown> {
  return (
    typeof value === 'object' &&
    value !== null &&
    typeof (value as { then?: unknown }).then === 'function'
  );
}

/**
 * An action that sends something (ADR-0131): while the promise it gives runs, it is pending, and a new call does
 * nothing; an action that gives no promise is never pending. The end of the promise, with success or error, ends it.
 */
export function usePendingAction<Event extends { preventDefault: () => void }>(
  action: ((event: Event) => unknown) | undefined,
): [boolean, (event: Event) => void] {
  const [pending, setPending] = useState(false);
  const running = useRef(false);
  const mounted = useRef(true);
  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);

  const run = (event: Event) => {
    if (running.current) {
      event.preventDefault();
      return;
    }
    const result = action?.(event);
    if (!isThenable(result)) {
      return;
    }
    running.current = true;
    setPending(true);
    const done = () => {
      running.current = false;
      if (mounted.current) {
        setPending(false);
      }
    };
    // The action shows its own error; the button only ends.
    result.then(done, done);
  };

  return [pending, run];
}

/**
 * The busy state as the button shows it (ADR-0132): it starts with the busy state and, when this ends before
 * minimumBusyMs from its start, lasts until then. A new start while it lasts goes on as one.
 */
export function useMinimumBusy(busy: boolean): boolean {
  const [wasBusy, setWasBusy] = useState(busy);
  // The end of a busy state still shown, a new object at each end; null when none.
  const [ending, setEnding] = useState<object | null>(null);
  const started = useRef(0);
  if (busy !== wasBusy) {
    setWasBusy(busy);
    setEnding(busy ? null : {});
  }
  useEffect(() => {
    if (busy) {
      started.current = Date.now();
    }
  }, [busy]);
  useEffect(() => {
    if (ending === null) {
      return undefined;
    }
    const timer = setTimeout(
      () => {
        setEnding(null);
      },
      Math.max(0, minimumBusyMs - (Date.now() - started.current)),
    );
    return () => {
      clearTimeout(timer);
    };
  }, [ending]);
  return busy || ending !== null;
}
