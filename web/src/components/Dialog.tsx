import CloseOutlined from '@mui/icons-material/CloseOutlined';
import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';
import MuiDialog from '@mui/material/Dialog';
import { type ReactNode, type SubmitEvent, useId } from 'react';
import { useTranslation } from 'react-i18next';

/**
 * Dialog of the design system (Dialog, fp-dialog, size sm): the title and a close button at the top, the content and
 * the actions at the bottom. With onSubmit, the dialog is a form, so Enter in a field submits it. An alert asks for a
 * confirmation (alertdialog), and its text describes it.
 */
export function Dialog({
  open,
  title,
  onClose,
  children,
  actions,
  onSubmit,
  alert = false,
}: {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  actions: ReactNode;
  onSubmit?: (event: SubmitEvent<HTMLFormElement>) => void;
  alert?: boolean;
}) {
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
          gap: 'var(--space-3)',
          padding: 'var(--space-4) var(--space-3) var(--space-3) var(--space-5)',
        }}
      >
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
      </Box>
      <Box
        id={bodyId}
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-4)',
          padding: 'var(--space-2) var(--space-5) var(--space-5)',
          color: 'var(--text)',
          fontSize: 14,
          lineHeight: '20px',
        }}
      >
        {children}
      </Box>
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
        paper: { sx: { maxWidth: 'var(--dialog-sm)', margin: 'var(--space-4)', width: '100%' } },
      }}
    >
      {onSubmit === undefined ? (
        content
      ) : (
        <Box component="form" noValidate onSubmit={onSubmit} sx={{ display: 'contents' }}>
          {content}
        </Box>
      )}
    </MuiDialog>
  );
}
