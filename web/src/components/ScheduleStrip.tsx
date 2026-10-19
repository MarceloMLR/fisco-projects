import Box from '@mui/material/Box';
import type { DayValue } from '../format/dates';
import { clamp, type StripPhase, stripLayout } from './scheduleStripLayout';

const percent = (value: number) => `${String(value)}%`;

/**
 * The strip of phases of the design system (ScheduleStrip, fp-strip): the phases of the project in sequence, the part
 * of each that is done in green, a late phase in the colors of the critical bars, the milestones as diamonds and today
 * as a line (ADR-0071). No label goes inside it: the name of each phase is its title, and the text beside the strip
 * says the progress, so the color is never the only information (ADR-0065).
 */
export function ScheduleStrip({
  start,
  end,
  phases,
  milestones,
  today,
  label,
  size = 'md',
}: {
  start: DayValue;
  end: DayValue;
  phases: readonly StripPhase[];
  milestones: readonly DayValue[];
  today: DayValue;
  /** Accessible name, such as "Cronograma de 05/10 a 13/11/2026". */
  label: string;
  size?: 'md' | 'lg';
}) {
  const layout = stripLayout(start, end, phases, milestones, today);
  const large = size === 'lg';
  const barTop = large ? 4 : 3;
  const barHeight = large ? 12 : 8;
  return (
    <Box
      role="img"
      aria-label={label}
      sx={{ position: 'relative', boxSizing: 'border-box', height: large ? 20 : 14, minWidth: 120 }}
    >
      {phases.map((phase, index) => {
        const place = layout.phases[index] ?? { left: 0, width: 0 };
        return (
          <Box
            key={`${phase.name}-${String(index)}`}
            title={phase.name}
            data-late={phase.late}
            sx={{
              position: 'absolute',
              top: barTop,
              height: barHeight,
              left: percent(place.left),
              width: percent(place.width),
              boxSizing: 'border-box',
              overflow: 'hidden',
              borderRadius: 'var(--radius-sm)',
              backgroundColor: phase.late ? 'var(--gantt-critical-bar)' : 'var(--gantt-bar)',
              boxShadow: phase.late
                ? 'inset 0 0 0 1px var(--gantt-critical-border)'
                : 'inset 0 0 0 1px var(--gantt-bar-border)',
            }}
          >
            <Box
              component="span"
              sx={{
                display: 'block',
                height: '100%',
                width: percent(clamp(phase.progress)),
                backgroundColor: 'var(--success)',
              }}
            />
          </Box>
        );
      })}
      {layout.milestones.map((left, index) => (
        <Box
          // Two milestones may fall on the same day, so the place alone is not a key.
          key={`${String(left)}-${String(index)}`}
          data-testid="strip-milestone"
          sx={{
            position: 'absolute',
            top: large ? 6 : 3,
            width: 8,
            height: 8,
            marginLeft: '-4px',
            left: percent(left),
            backgroundColor: 'var(--gantt-milestone)',
            transform: 'rotate(45deg)',
          }}
        />
      ))}
      <Box
        data-testid="strip-today"
        sx={{
          position: 'absolute',
          top: 0,
          bottom: 0,
          width: 2,
          marginLeft: '-1px',
          left: percent(layout.today),
          borderRadius: '1px',
          backgroundColor: 'var(--gantt-today)',
        }}
      />
    </Box>
  );
}
