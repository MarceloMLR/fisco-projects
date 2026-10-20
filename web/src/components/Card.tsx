import Box from '@mui/material/Box';
import { type ReactNode, useId } from 'react';

/**
 * A card of the design system (fp-card): a section with its heading, an optional count beside it (fp-card__count),
 * actions at the right of the heading and the content. With fill, the card takes the height left in its column and
 * the content scrolls inside it (fp-card--fill), so the page does not scroll (ADR-0066); with shrink, the card keeps
 * the height of its content while there is room, and scrolls inside when there is not.
 */
export function Card({
  title,
  count,
  actions,
  fill = false,
  shrink = false,
  children,
}: {
  title: string;
  count?: number;
  actions?: ReactNode;
  fill?: boolean;
  shrink?: boolean;
  children: ReactNode;
}) {
  const headingId = useId();
  return (
    <Box
      component="section"
      aria-labelledby={headingId}
      sx={{
        boxSizing: 'border-box',
        display: 'flex',
        flexDirection: 'column',
        minWidth: 0,
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-lg)',
        overflow: 'hidden',
        ...(fill
          ? { flex: 1, minHeight: 0 }
          : shrink
            ? { flex: '0 1 auto', minHeight: 0 }
            : { flex: 'none' }),
      }}
    >
      <Box
        sx={{
          flex: 'none',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 'var(--space-3)',
          minHeight: 48,
          boxSizing: 'border-box',
          padding: 'var(--space-2) var(--space-2) var(--space-2) var(--space-4)',
          borderBottom: '1px solid var(--border)',
        }}
      >
        <Box
          component="h2"
          id={headingId}
          sx={{
            margin: 0,
            fontSize: 15,
            lineHeight: '22px',
            fontWeight: 500,
            color: 'var(--text)',
          }}
        >
          {title}
          {count !== undefined && (
            <Box
              component="span"
              sx={{
                marginLeft: 'var(--space-2)',
                fontWeight: 400,
                color: 'var(--text-secondary)',
                fontVariantNumeric: 'tabular-nums',
              }}
            >
              {count}
            </Box>
          )}
        </Box>
        {actions}
      </Box>
      <Box sx={fill || shrink ? { flex: '1 1 auto', minHeight: 0, overflow: 'auto' } : undefined}>
        {children}
      </Box>
    </Box>
  );
}

/** The code of a project in a chip (fp-chip), as the lines of Meu trabalho show it. */
export function CodeChip({ code }: { code: string }) {
  return (
    <Box
      component="span"
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        flex: 'none',
        boxSizing: 'border-box',
        height: 22,
        padding: '0 var(--space-2)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-sm)',
        backgroundColor: 'var(--bg-surface)',
        color: 'var(--text-secondary)',
        fontFamily: 'var(--font-mono)',
        fontSize: 11,
        lineHeight: '16px',
        fontWeight: 500,
        whiteSpace: 'nowrap',
      }}
    >
      {code}
    </Box>
  );
}
