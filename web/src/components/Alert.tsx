import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import InfoOutlined from '@mui/icons-material/InfoOutlined';
import WarningAmberOutlined from '@mui/icons-material/WarningAmberOutlined';
import Box from '@mui/material/Box';
import type { ReactNode } from 'react';

const tones = {
  info: {
    background: 'var(--status-info-bg)',
    accent: 'var(--status-info-text)',
    Icon: InfoOutlined,
  },
  warning: {
    background: 'var(--status-warning-bg)',
    accent: 'var(--status-warning-text)',
    Icon: WarningAmberOutlined,
  },
  danger: {
    background: 'var(--status-danger-bg)',
    accent: 'var(--status-danger-text)',
    Icon: ErrorOutlineOutlined,
  },
} as const;

/**
 * Message box of the design system (Alert, fp-alert): icon, color and title together, never color alone
 * (ADR-0065), and the actions at the right (fp-alert__actions), below the text when it is narrow. A danger alert is
 * announced as soon as it appears.
 */
export function Alert({
  tone,
  title,
  actions,
  children,
}: {
  tone: keyof typeof tones;
  title?: string;
  actions?: ReactNode;
  children?: ReactNode;
}) {
  const { background, accent, Icon } = tones[tone];

  return (
    <Box
      role={tone === 'danger' ? 'alert' : 'status'}
      sx={{
        display: 'flex',
        flexWrap: actions === undefined ? undefined : 'wrap',
        alignItems: 'flex-start',
        gap: 'var(--space-3)',
        boxSizing: 'border-box',
        padding: 'var(--space-3) var(--space-4)',
        borderRadius: 'var(--radius-md)',
        backgroundColor: background,
        color: 'var(--text)',
      }}
    >
      <Icon aria-hidden sx={{ fontSize: 'var(--icon-md)', color: accent }} />
      <Box sx={{ flex: 1, minWidth: actions === undefined ? 0 : 'min(100%, 200px)' }}>
        {title !== undefined && <Box sx={{ fontWeight: 500, color: accent }}>{title}</Box>}
        {children !== undefined && <Box sx={{ color: 'var(--text)' }}>{children}</Box>}
      </Box>
      {actions !== undefined && (
        <Box
          sx={{
            flex: 'none',
            display: 'flex',
            gap: 'var(--space-2)',
            marginLeft: 'auto',
            alignSelf: 'center',
          }}
        >
          {actions}
        </Box>
      )}
    </Box>
  );
}
