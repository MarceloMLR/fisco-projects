import Box from '@mui/material/Box';

/** The colors of the done part, as the fp-progress modifiers of the design system. */
const fills = {
  success: 'var(--success)',
  info: 'var(--control-selected)',
  warning: 'var(--warning)',
  danger: 'var(--danger)',
} as const;

export type ProgressTone = keyof typeof fills;

const captionSx = {
  display: 'flex',
  justifyContent: 'space-between',
  gap: 'var(--space-2)',
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
  fontVariantNumeric: 'tabular-nums',
} as const;

/**
 * Thin progress bar of the design system (ProgressBar, fp-progress): the track (progress-track) and the done part in
 * green (success) or in the tone asked for, with the percentage beside it, so the color is never the only information
 * (ADR-0065), and, when asked, a line above with the caption and the detail (fp-progress__label). The progress of
 * tasks, phases and projects is weighted by duration (ADR-0020).
 */
export function ProgressBar({
  value,
  label,
  width,
  valueText,
  tone = 'success',
  caption,
  detail,
}: {
  /** From 0 to 100. */
  value: number;
  /** Accessible name, such as "Progresso da tarefa". */
  label: string;
  width?: number | string;
  /** The text beside the bar in place of the percentage, such as "1 de 2 dias úteis". */
  valueText?: string;
  /** The color of the done part (fp-progress--info, --warning and --danger); green by default. */
  tone?: ProgressTone;
  /** The text above the bar, on the left, such as "Consumo". */
  caption?: string;
  /** The text above the bar, on the right, such as "468 de 440 h (106%)". */
  detail?: string;
}) {
  const shown = Math.max(0, Math.min(100, Math.round(value)));
  // With the line above, the detail tells the number, and the bar goes alone (fp-progress__label).
  const labelled = caption !== undefined || detail !== undefined;
  const bar = (
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
        aria-valuetext={labelled ? detail : undefined}
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
            backgroundColor: fills[tone],
          }}
        />
      </Box>
      {!labelled && (
        <Box
          component="span"
          aria-hidden
          sx={{
            minWidth: 36,
            flex: 'none',
            textAlign: 'right',
            fontSize: 12,
            lineHeight: '16px',
            fontWeight: 500,
            color: 'var(--text-secondary)',
            fontVariantNumeric: 'tabular-nums',
          }}
        >
          {valueText ?? `${String(shown)}%`}
        </Box>
      )}
    </Box>
  );
  if (!labelled) {
    return bar;
  }
  return (
    <Box
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)', width, minWidth: 120 }}
    >
      <Box sx={captionSx}>
        <span>{caption}</span>
        <Box component="strong" sx={{ color: 'var(--text)', fontWeight: 500 }}>
          {detail}
        </Box>
      </Box>
      {bar}
    </Box>
  );
}
