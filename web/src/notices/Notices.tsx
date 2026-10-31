import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import CloseOutlined from '@mui/icons-material/CloseOutlined';
import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import Box from '@mui/material/Box';
import Portal from '@mui/material/Portal';
import useMediaQuery from '@mui/material/useMediaQuery';
import { type FocusEvent, type ReactNode, useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, IconButton } from '../components/Button';
import { fade, motion, play, reducesMotion } from '../theme/motion';
import { theme } from '../theme/theme';
import {
  type NoticeAction,
  type NoticeTone,
  type Notify,
  NotifyContext,
  noticeMs,
  noticeWithActionMs,
  ShellContext,
} from './notify';

interface Notice {
  id: number;
  text: string;
  tone: NoticeTone;
  action?: NoticeAction;
}

/**
 * The notices of the application (ADR-0133): one at a time, at the top, in the middle of the content area; a new one
 * sends the one on screen away before it comes in. The screens only call useNotify.
 */
export function NoticeProvider({ children }: { children: ReactNode }) {
  const [queue, setQueue] = useState<readonly Notice[]>([]);
  const [inShell, setInShell] = useState(false);
  const last = useRef(0);
  const notify = useCallback<Notify>((text, options) => {
    last.current += 1;
    const notice: Notice = {
      id: last.current,
      text,
      tone: options?.tone ?? 'success',
      action: options?.action,
    };
    setQueue((current) => [...current, notice]);
  }, []);
  const gone = useCallback((id: number) => {
    setQueue((current) => current.filter((notice) => notice.id !== id));
  }, []);
  const [shown] = queue;
  return (
    <NotifyContext.Provider value={notify}>
      <ShellContext.Provider value={setInShell}>
        {children}
        <NoticeArea inShell={inShell} notice={shown} superseded={queue.length > 1} onGone={gone} />
      </ShellContext.Provider>
    </NotifyContext.Provider>
  );
}

/**
 * The place of the notices (ADR-0109): on the computer, in the strip of the invisible header and in the middle of the
 * content, never over the sidebar; on the phone, just below the top bar; outside the structure, at the top, in the
 * middle. A live region, always on the page, so screen readers read each notice that comes in (role status).
 */
function NoticeArea({
  inShell,
  notice,
  superseded,
  onGone,
}: {
  inShell: boolean;
  notice: Notice | undefined;
  superseded: boolean;
  onGone: (id: number) => void;
}) {
  const { t } = useTranslation();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const beside = inShell && desktop;
  return (
    <Portal>
      <Box
        aria-live="polite"
        aria-label={t('shell.notices')}
        data-testid="notice-area"
        data-place={beside ? 'content' : inShell ? 'below-top-bar' : 'top'}
        sx={{
          position: 'fixed',
          zIndex: theme.zIndex.snackbar,
          top: beside
            ? 'calc((var(--topbar-height) - 40px) / 2)'
            : inShell
              ? 'calc(var(--topbar-height) + var(--space-2))'
              : 'var(--space-4)',
          left: beside ? 'calc(50% + var(--sidebar-width) / 2)' : '50%',
          transform: 'translateX(-50%)',
          width: 'max-content',
          maxWidth: beside
            ? 'min(420px, calc(100vw - var(--sidebar-width) - 2 * var(--space-6)))'
            : 'min(420px, calc(100vw - 2 * var(--space-4)))',
          pointerEvents: 'none',
        }}
      >
        {notice !== undefined && (
          <NoticeItem key={notice.id} notice={notice} superseded={superseded} onGone={onGone} />
        )}
      </Box>
    </Portal>
  );
}

const enterFrames: Keyframe[] = [
  { opacity: 0, transform: 'translateY(-8px)' },
  { opacity: 1, transform: 'translateY(0)' },
];
const exitFrames: Keyframe[] = [
  { opacity: 1, transform: 'translateY(0)' },
  { opacity: 0, transform: 'translateY(-4px)' },
];

/**
 * A notice (Snackbar of the design system, ADR-0133): the surface of a window, its icon, the text, the action and
 * "Fechar aviso". It comes down 8px as it fades in and fades out with its text; it stays its time, stopped while the
 * pointer or the focus is on it.
 */
function NoticeItem({
  notice,
  superseded,
  onGone,
}: {
  notice: Notice;
  superseded: boolean;
  onGone: (id: number) => void;
}) {
  const { t } = useTranslation();
  const element = useRef<HTMLDivElement>(null);
  const [closed, setClosed] = useState(false);
  const [paused, setPaused] = useState(false);
  const left = useRef(notice.action === undefined ? noticeMs : noticeWithActionMs);
  const leaving = closed || superseded;

  useEffect(() => {
    const reduced = reducesMotion();
    void play(
      element.current,
      reduced ? fade(true) : enterFrames,
      reduced ? motion.reduced : motion.notice.enter,
      motion.enter,
    );
  }, []);

  useEffect(() => {
    if (paused || leaving) {
      return undefined;
    }
    const started = Date.now();
    const timer = setTimeout(() => {
      setClosed(true);
    }, left.current);
    return () => {
      clearTimeout(timer);
      left.current = Math.max(0, left.current - (Date.now() - started));
    };
  }, [paused, leaving]);

  useEffect(() => {
    if (!leaving) {
      return undefined;
    }
    let current = true;
    const reduced = reducesMotion();
    void play(
      element.current,
      reduced ? fade(false) : exitFrames,
      reduced ? motion.reduced : motion.notice.exit,
      motion.exit,
      'forwards',
    ).then(() => {
      if (current) {
        onGone(notice.id);
      }
    });
    return () => {
      current = false;
    };
  }, [leaving, notice.id, onGone]);

  const close = () => {
    setClosed(true);
  };
  const resumeOnBlur = (event: FocusEvent<HTMLDivElement>) => {
    if (!event.currentTarget.contains(event.relatedTarget)) {
      setPaused(false);
    }
  };
  const error = notice.tone === 'error';
  const Icon = error ? ErrorOutlineOutlined : CheckCircleOutlined;
  const { action } = notice;
  return (
    <Box
      ref={element}
      role="status"
      data-testid="notice"
      data-tone={notice.tone}
      onMouseEnter={() => {
        setPaused(true);
      }}
      onMouseLeave={() => {
        setPaused(false);
      }}
      onFocus={() => {
        setPaused(true);
      }}
      onBlur={resumeOnBlur}
      sx={{
        pointerEvents: 'auto',
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--space-2)',
        boxSizing: 'border-box',
        minHeight: 40,
        padding: '6px 6px 6px var(--space-3)',
        borderRadius: 'var(--radius-lg)',
        border: '1px solid var(--border)',
        backgroundColor: 'var(--bg-elevated)',
        color: 'var(--text)',
        boxShadow: 'var(--shadow-menu)',
        fontSize: 14,
        lineHeight: '20px',
      }}
    >
      <Icon
        aria-hidden
        sx={{
          flex: 'none',
          fontSize: 'var(--icon-md)',
          color: error ? 'var(--status-danger-text)' : 'var(--status-success-text)',
        }}
      />
      <Box component="span" sx={{ flex: 1, minWidth: 0 }}>
        {notice.text}
      </Box>
      {action !== undefined && (
        <Button
          variant="text"
          size="small"
          onClick={() => {
            action.onClick();
            close();
          }}
          sx={{ flex: 'none' }}
        >
          {action.label}
        </Button>
      )}
      <IconButton
        size="small"
        aria-label={t('shell.closeNotice')}
        title={t('shell.closeNotice')}
        onClick={close}
        sx={{ flex: 'none', color: 'var(--text-secondary)' }}
      >
        <CloseOutlined sx={{ fontSize: 'var(--icon-sm)' }} />
      </IconButton>
    </Box>
  );
}
