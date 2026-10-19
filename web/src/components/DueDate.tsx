import CalendarTodayOutlined from '@mui/icons-material/CalendarTodayOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import HourglassEmptyOutlined from '@mui/icons-material/HourglassEmptyOutlined';
import PauseCircleOutlined from '@mui/icons-material/PauseCircleOutlined';
import WarningAmberOutlined from '@mui/icons-material/WarningAmberOutlined';
import Box from '@mui/material/Box';
import type { ReactNode } from 'react';

/** How a deadline is going: on track, due soon, overdue, paused or met (DueDate of the design system). */
export type DueState = 'onTrack' | 'soon' | 'overdue' | 'paused' | 'met';

const looks = {
  onTrack: { Icon: CalendarTodayOutlined, color: 'var(--text-secondary)', weight: 400 },
  soon: { Icon: HourglassEmptyOutlined, color: 'var(--status-warning-text)', weight: 500 },
  overdue: { Icon: WarningAmberOutlined, color: 'var(--status-danger-text)', weight: 500 },
  paused: { Icon: PauseCircleOutlined, color: 'var(--text-secondary)', weight: 400 },
  met: { Icon: CheckCircleOutlined, color: 'var(--status-success-text)', weight: 400 },
} as const;

/**
 * A deadline in words (DueDate, fp-due): the icon of its state and a short text, such as "Atrasada" or "2 tarefas
 * atrasadas", in the color of the state, so the color is never the only information (ADR-0065).
 */
export function DueDate({ state, children }: { state: DueState; children: ReactNode }) {
  const { Icon, color, weight } = looks[state];
  return (
    <Box
      component="span"
      data-state={state}
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 'var(--space-1)',
        fontSize: 13,
        lineHeight: '18px',
        fontWeight: weight,
        color,
        whiteSpace: 'nowrap',
        fontVariantNumeric: 'tabular-nums',
      }}
    >
      <Icon aria-hidden sx={{ fontSize: 16 }} />
      {children}
    </Box>
  );
}
