import Box from '@mui/material/Box';

/**
 * Thin progress bar of the design system (ProgressBar, fp-progress): the track (progress-track) and the done part in
 * green (success), with the percentage beside it, so the color is never the only information (ADR-0065). The
 * progress of tasks, phases and projects is weighted by duration (ADR-0020).
 */
export function ProgressBar({
  value,
  label,
  width,
}: {
  /** From 0 to 100. */
  value: number;
  /** Accessible name, such as "Progresso da tarefa". */
  label: string;
  width?: number | string;
}) {
  const shown = Math.max(0, Math.min(100, Math.round(value)));
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--space-2)',
        width,
        minWidth: 96,
      }}
    >
      <Box
        role="progressbar"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={shown}
        sx={{
          flex: 1,
          height: 6,
          overflow: 'hidden',
          borderRadius: 'var(--radius-sm)',
          backgroundColor: 'var(--progress-track)',
        }}
      >
        <Box
          sx={{
            width: `${String(shown)}%`,
            height: '100%',
            borderRadius: 'var(--radius-sm)',
            backgroundColor: 'var(--success)',
          }}
        />
      </Box>
      <Box
        component="span"
        aria-hidden
        sx={{
          minWidth: 36,
          textAlign: 'right',
          fontSize: 12,
          lineHeight: '16px',
          fontWeight: 500,
          color: 'var(--text-secondary)',
          fontVariantNumeric: 'tabular-nums',
        }}
      >
        {`${String(shown)}%`}
      </Box>
    </Box>
  );
}
