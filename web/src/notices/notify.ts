import { createContext, useContext, useEffect } from 'react';

/** A notice confirms what went right, with the icon of done, or tells a failure, with the icon of error. */
export type NoticeTone = 'success' | 'error';

export interface NoticeAction {
  label: string;
  onClick: () => void;
}

export interface NoticeOptions {
  tone?: NoticeTone;
  /** One or two words, as "Ver chamado"; the notice then stays longer. */
  action?: NoticeAction;
}

/** Shows a short notice at the top of the screen (ADR-0109, ADR-0133). */
export type Notify = (text: string, options?: NoticeOptions) => void;

/** How long a notice stays: 5 seconds, 8 with an action (design system, Snackbar). */
export const noticeMs = 5000;
export const noticeWithActionMs = 8000;

export const NotifyContext = createContext<Notify>(() => undefined);
export const ShellContext = createContext<(inShell: boolean) => void>(() => undefined);

/**
 * The notice of the screens (NoticeProvider), the same for all of them: it stays while the screen that gave it closes
 * or changes. A failure goes with { tone: 'error' }.
 */
export function useNotify(): Notify {
  return useContext(NotifyContext);
}

/**
 * Tells the notices that the structure of the screens is around: on the computer they go to the middle of the content
 * area, beside the sidebar (AppShell).
 */
export function useNoticesInShell() {
  const setInShell = useContext(ShellContext);
  useEffect(() => {
    setInShell(true);
    return () => {
      setInShell(false);
    };
  }, [setInShell]);
}
