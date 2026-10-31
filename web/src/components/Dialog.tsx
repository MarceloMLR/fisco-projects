import CloseOutlined from '@mui/icons-material/CloseOutlined';
import Box from '@mui/material/Box';
import MuiDialog from '@mui/material/Dialog';
import useMediaQuery from '@mui/material/useMediaQuery';
import { type ReactNode, useId } from 'react';
import { useTranslation } from 'react-i18next';
import { backdropDuration, motion } from '../theme/motion';
import { IconButton } from './Button';
import { Form, type FormSubmit } from './Form';
import { useKeptWhileOpen } from './keptWhileOpen';
import { DialogMotion, LeavesWithMotion } from './WindowMotion';

const backdropEasing = { enter: motion.enter, exit: motion.exit };

/**
 * Dialog of the design system (Dialog, fp-dialog, size sm): the title and a close button at the top, the content and
 * the actions at the bottom. The large size (dialog-lg) takes a longer content, as a list of a few columns. With
 * onSubmit, the dialog is a form (Form), so Enter in a field submits it, and its submit button is busy while it sends
 * (ADR-0131). An alert asks for a confirmation (alertdialog), and its text describes it. The full size (fp-dialog--full) covers almost the whole
 * screen, with a subtitle and the tools in the header, which closes it too, and the content to the edges. It grows from
 * 98% as it fades in and goes back the same way, with what it showed until its way out ends, also when the screen takes
 * it away at once (ADR-0133).
 */
export function Dialog({
  open,
  title,
  onClose,
  children,
  actions,
  onSubmit,
  alert = false,
  size = 'sm',
  subtitle,
  headerTools,
  onExited,
}: {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  actions?: ReactNode;
  onSubmit?: FormSubmit;
  alert?: boolean;
  size?: 'sm' | 'lg' | 'full';
  subtitle?: ReactNode;
  /** In the header, after the title; the full size closes by them (its "Recolher"). */
  headerTools?: ReactNode;
  /** Once its way out ended and it left the screen. */
  onExited?: () => void;
}) {
  const full = size === 'full';
  const { t } = useTranslation();
  const titleId = useId();
  const bodyId = useId();
  const reduced = useMediaQuery('(prefers-reduced-motion: reduce)', { noSsr: true });
  // What the dialog showed, until its way out ends: the screen may have cleared the state it came from.
  const [shownTitle, shownSubtitle, shownTools, shownChildren, shownActions] = useKeptWhileOpen(
    open,
    [title, subtitle, headerTools, children, actions] as const,
  );

  const content = (
    <>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 'var(--space-3)',
          padding: full
            ? 'var(--space-4) var(--space-5)'
            : 'var(--space-4) var(--space-3) var(--space-3) var(--space-5)',
          ...(full && { borderBottom: '1px solid var(--border)' }),
        }}
      >
        <Box sx={{ minWidth: 0, ...(full && { marginRight: 'var(--space-2)' }) }}>
          <Box
            component="h2"
            id={titleId}
            sx={{
              margin: 0,
              fontSize: 18,
              lineHeight: '26px',
              fontWeight: 500,
              color: 'var(--text)',
            }}
          >
            {shownTitle}
          </Box>
          {shownSubtitle !== undefined && (
            <Box sx={{ fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}>
              {shownSubtitle}
            </Box>
          )}
        </Box>
        {shownTools}
        {!full && (
          <IconButton
            size="small"
            aria-label={t('dialog.close')}
            onClick={onClose}
            sx={{
              width: 'var(--control-sm)',
              height: 'var(--control-sm)',
              borderRadius: 'var(--radius-md)',
              color: 'var(--text-secondary)',
              '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
            }}
          >
            <CloseOutlined sx={{ fontSize: 'var(--icon-md)' }} />
          </IconButton>
        )}
      </Box>
      <Box
        id={bodyId}
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-4)',
          padding: full ? 0 : 'var(--space-2) var(--space-5) var(--space-5)',
          color: 'var(--text)',
          fontSize: 14,
          lineHeight: '20px',
          ...(full && { flex: 1, minHeight: 0, overflow: 'hidden' }),
        }}
      >
        {shownChildren}
      </Box>
      {shownActions !== undefined && (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'flex-end',
            gap: 'var(--space-2)',
            padding: 'var(--space-3) var(--space-5) var(--space-5)',
          }}
        >
          {shownActions}
        </Box>
      )}
      <LeavesWithMotion kind="dialog" open={open} />
    </>
  );

  return (
    <MuiDialog
      open={open}
      onClose={onClose}
      fullWidth
      aria-labelledby={titleId}
      aria-describedby={alert ? bodyId : undefined}
      role={alert ? 'alertdialog' : 'dialog'}
      transitionDuration={backdropDuration('dialog', reduced)}
      slots={{ transition: DialogMotion }}
      slotProps={{
        backdrop: { slotProps: { transition: { easing: backdropEasing } } },
        transition: { onExited },
        paper: {
          sx: full
            ? {
                maxWidth: 'none',
                maxHeight: 'none',
                width: 'calc(100% - 2 * var(--space-6))',
                height: 'calc(100% - 2 * var(--space-6))',
                margin: 'var(--space-6)',
              }
            : {
                maxWidth: size === 'lg' ? 'var(--dialog-lg)' : 'var(--dialog-sm)',
                margin: 'var(--space-4)',
                width: '100%',
              },
        },
      }}
    >
      {onSubmit === undefined ? (
        content
      ) : (
        <Form onSubmit={onSubmit} sx={{ display: 'contents' }}>
          {content}
        </Form>
      )}
    </MuiDialog>
  );
}
