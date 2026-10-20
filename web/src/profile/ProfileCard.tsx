import Box from '@mui/material/Box';
import type { ReactNode } from 'react';

/** Below this width the account goes under the preferences, as fp-split in the design system. */
export const singleColumn = '@media (max-width: 1100px)';

/** A card of My profile (fp-card), with its title in the header. */
export function ProfileCard({
  id,
  title,
  children,
}: {
  id: string;
  title: string;
  children: ReactNode;
}) {
  return (
    <Box
      component="section"
      aria-labelledby={id}
      sx={{
        boxSizing: 'border-box',
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-lg)',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          minHeight: 48,
          boxSizing: 'border-box',
          padding: 'var(--space-2) var(--space-2) var(--space-2) var(--space-4)',
          borderBottom: '1px solid var(--border)',
        }}
      >
        <Box
          component="h2"
          id={id}
          sx={{
            margin: 0,
            fontSize: 15,
            lineHeight: '22px',
            fontWeight: 500,
            color: 'var(--text)',
          }}
        >
          {title}
        </Box>
      </Box>
      <Box
        sx={{ padding: '0 var(--space-4)', '& > * + *': { borderTop: '1px solid var(--border)' } }}
      >
        {children}
      </Box>
    </Box>
  );
}

/** A row of a card of My profile (fp-settings-row): the name of the setting and its control. */
export function ProfileRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: '200px minmax(0, 1fr)',
        gap: 'var(--space-6)',
        padding: 'var(--space-5) 0',
        [singleColumn]: { gridTemplateColumns: 'minmax(0, 1fr)', gap: 'var(--space-3)' },
      }}
    >
      <Box component="span" sx={{ fontWeight: 500, color: 'var(--text)' }}>
        {label}
      </Box>
      {children}
    </Box>
  );
}
