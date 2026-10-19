import CloseOutlined from '@mui/icons-material/CloseOutlined';
import Box from '@mui/material/Box';
import Drawer from '@mui/material/Drawer';
import IconButton from '@mui/material/IconButton';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

/** Widest the drawer gets; on narrower windows, such as the phone, it takes the whole width. */
const maxWidth = 640;

const paperSx = {
  width: '100%',
  maxWidth,
  boxSizing: 'border-box',
  display: 'flex',
  flexDirection: 'column',
  backgroundColor: 'var(--bg-elevated)',
  borderLeft: '1px solid var(--border)',
  boxShadow: 'var(--shadow-dialog)',
} as const;

const slotProps = { backdrop: { sx: { backgroundColor: 'var(--overlay)' } } } as const;

/**
 * Details of an item chosen in a list (ADR-0081): a drawer on the right, over the screen, with the background
 * dimmed, so the list keeps its whole width. It closes by its button, by Esc or by a click outside it, and the focus
 * goes back to where it was, the row of the list. The content scrolls inside (ADR-0066).
 */
export function DetailsDrawer({
  open,
  onClose,
  label,
  children,
}: {
  open: boolean;
  onClose: () => void;
  /** Accessible name of the drawer, such as "Registro selecionado". */
  label: string;
  children: ReactNode;
}) {
  const { t } = useTranslation();

  return (
    <Drawer
      anchor="right"
      open={open}
      onClose={onClose}
      // On the level of the dialogs, so the details of an item chosen in a dialog that covers the screen, as
      // Expandir of the schedule, come over it.
      sx={{ zIndex: (theme) => theme.zIndex.modal }}
      slotProps={{
        ...slotProps,
        paper: { role: 'dialog', 'aria-modal': true, 'aria-label': label, sx: paperSx },
      }}
    >
      <Box
        sx={{
          flex: 'none',
          display: 'flex',
          justifyContent: 'flex-end',
          padding: 'var(--space-3) var(--space-3) 0',
        }}
      >
        <IconButton
          aria-label={t('details.close')}
          onClick={onClose}
          sx={{ color: 'var(--text-secondary)' }}
        >
          <CloseOutlined sx={{ fontSize: 'var(--icon-md)' }} />
        </IconButton>
      </Box>
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          overflow: 'auto',
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-5)',
          padding: '0 var(--space-6) var(--space-6)',
        }}
      >
        {children}
      </Box>
    </Drawer>
  );
}
