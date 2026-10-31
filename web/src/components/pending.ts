import { createContext, useEffect, useRef, useState } from 'react';

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
