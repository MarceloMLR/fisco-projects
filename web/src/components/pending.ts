import { createContext, useEffect, useRef, useState, useSyncExternalStore } from 'react';
import { requestsStarted } from '../api/activity';

/** The shortest time a button shows it is busy, so the loading icon is seen even with a quick answer (ADR-0132). */
export const minimumBusyMs = 500;

/**
 * Whether the form around is sending, for its submit buttons (Form, ADR-0131), and whether its last sending called the
 * API (ADR-0132).
 */
export const FormPending = createContext({ sending: false, waited: false });

export interface FormSending {
  sending: boolean;
  waited: boolean;
}

const idle: FormSending = { sending: false, waited: false };
const formsById = new Map<string, FormSending>();
const formListeners = new Set<() => void>();

function subscribeForms(listener: () => void) {
  formListeners.add(listener);
  return () => {
    formListeners.delete(listener);
  };
}

/** What the form with this id tells its submit buttons outside it, tied by the attribute form (ADR-0131). */
export function publishFormSending(id: string, state: FormSending | undefined) {
  if (state === undefined) {
    formsById.delete(id);
  } else {
    formsById.set(id, state);
  }
  for (const listener of formListeners) {
    listener();
  }
}

/** The sending of the form with this id, for a submit button outside it, as a Salvar in the header of a page. */
export function useFormSending(id: string | undefined): FormSending {
  return useSyncExternalStore(subscribeForms, () =>
    id === undefined ? idle : (formsById.get(id) ?? idle),
  );
}

function isThenable(value: unknown): value is PromiseLike<unknown> {
  return (
    typeof value === 'object' &&
    value !== null &&
    typeof (value as { then?: unknown }).then === 'function'
  );
}

/**
 * An action that sends something (ADR-0131): while the promise it gives runs, it is pending, and a new call does
 * nothing; an action that gives no promise is never pending. The end of the promise, with success or error, ends it,
 * and tells whether the action called the API meanwhile (ADR-0132).
 */
export function usePendingAction<Event extends { preventDefault: () => void }>(
  action: ((event: Event) => unknown) | undefined,
): [boolean, (event: Event) => void, boolean] {
  const [pending, setPending] = useState(false);
  const [waited, setWaited] = useState(false);
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
    const before = requestsStarted();
    const result = action?.(event);
    if (!isThenable(result)) {
      return;
    }
    running.current = true;
    setPending(true);
    setWaited(false);
    const done = () => {
      running.current = false;
      if (mounted.current) {
        setWaited(requestsStarted() !== before);
        setPending(false);
      }
    };
    // The action shows its own error; the button only ends.
    result.then(done, done);
  };

  return [pending, run, waited];
}

/**
 * The busy state as the button shows it (ADR-0132): it starts with the busy state and, when this ends before
 * minimumBusyMs from its start after waiting for the server, lasts until then. A busy state that did not wait, as a
 * form refused by its own checks, ends at once, so the errors show and a new click works. A new start while it lasts
 * goes on as one.
 */
export function useMinimumBusy(busy: boolean, waited: boolean): boolean {
  const [wasBusy, setWasBusy] = useState(busy);
  // Whether the busy state now going waited for the server at some moment.
  const [hasWaited, setHasWaited] = useState(busy && waited);
  // The end of a busy state still shown, a new object at each end; null when none.
  const [ending, setEnding] = useState<object | null>(null);
  const started = useRef(0);
  if (busy && waited && !hasWaited) {
    setHasWaited(true);
  }
  if (busy !== wasBusy) {
    setWasBusy(busy);
    if (busy) {
      setEnding(null);
      setHasWaited(waited);
    } else {
      setEnding(hasWaited || waited ? {} : null);
      setHasWaited(false);
    }
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
