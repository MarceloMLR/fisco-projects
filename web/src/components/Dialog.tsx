import CloseOutlined from '@mui/icons-material/CloseOutlined';
import Box from '@mui/material/Box';
import MuiDialog from '@mui/material/Dialog';
import { type ReactNode, useId } from 'react';
import { useTranslation } from 'react-i18next';
import { IconButton } from './Button';
import { Form, type FormSubmit } from './Form';

/**
 * Dialog of the design system (Dialog, fp-dialog, size sm): the title and a close button at the top, the content and
 * the actions at the bottom. The large size (dialog-lg) takes a longer content, as a list of a few columns. With onSubmit, the dialog is a form (Form), so Enter in a field submits it, and its submit button is busy while it sends (ADR-0131). An alert asks for a
 * confirmation (alertdialog), and its text describes it. The full size (fp-dialog--full) covers almost the whole
 * screen, with a subtitle and the tools in the header, which closes it too, and the content to the edges.
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
}) {
  const full = size === 'full';
  const { t } = useTranslation();
  const titleId = useId();
  const bodyId = useId();

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
            {title}
          </Box>
          {subtitle !== undefined && (
            <Box sx={{ fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}>
              {subtitle}
            </Box>
          )}
        </Box>
        {headerTools}
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
        {children}
      </Box>
      {actions !== undefined && (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'flex-end',
            gap: 'var(--space-2)',
            padding: 'var(--space-3) var(--space-5) var(--space-5)',
          }}
        >
          {actions}
        </Box>
      )}
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
      slotProps={{
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
