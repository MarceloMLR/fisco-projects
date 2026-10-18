import type SvgIcon from '@mui/material/SvgIcon';
import Box from '@mui/material/Box';
import type { ReactNode } from 'react';

const tones = {
  neutral: { background: 'var(--status-neutral-bg)', color: 'var(--status-neutral-text)' },
  info: { background: 'var(--status-info-bg)', color: 'var(--status-info-text)' },
  warning: { background: 'var(--status-warning-bg)', color: 'var(--status-warning-text)' },
  success: { background: 'var(--status-success-bg)', color: 'var(--status-success-text)' },
  danger: { background: 'var(--status-danger-bg)', color: 'var(--status-danger-text)' },
} as const;

export type StatusTone = keyof typeof tones;

/** Status of the design system (StatusBadge, fp-badge): always icon, color and text, never color alone (ADR-0065). */
export function StatusBadge({
  tone,
  icon: Icon,
  children,
}: {
  tone: StatusTone;
  icon: typeof SvgIcon;
  children: ReactNode;
}) {
  const { background, color } = tones[tone];

  return (
    <Box
      component="span"
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--space-1)',
        boxSizing: 'border-box',
        height: 24,
        padding: '0 var(--space-2) 0 6px',
        borderRadius: 'var(--radius-md)',
        backgroundColor: background,
        color,
        fontSize: 12,
        lineHeight: '16px',
        fontWeight: 500,
        whiteSpace: 'nowrap',
      }}
    >
      <Icon aria-hidden sx={{ fontSize: 'var(--icon-sm)' }} />
      {children}
    </Box>
  );
}
