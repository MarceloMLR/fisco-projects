const cell = {
  boxSizing: 'border-box',
  height: 'var(--row-height)',
  padding: '10px var(--space-3)',
  borderBottom: '1px solid var(--border)',
  textAlign: 'left',
  verticalAlign: 'middle',
} as const;

/**
 * A small table of the hours of a project (fp-table in a bordered box), without choosing rows: the additional hours and
 * the hours of each person in the drawer, and all the additional hours in their dialog.
 */
export const hoursTableSx = {
  width: '100%',
  borderCollapse: 'separate',
  borderSpacing: 0,
  backgroundColor: 'var(--bg-surface)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-lg)',
  overflow: 'hidden',
  fontSize: 14,
  lineHeight: '20px',
  fontVariantNumeric: 'tabular-nums',
  '& th': {
    ...cell,
    padding: '0 var(--space-3)',
    backgroundColor: 'var(--bg-subtle)',
    color: 'var(--text-secondary)',
    fontSize: 13,
    lineHeight: '18px',
    fontWeight: 500,
    whiteSpace: 'nowrap',
  },
  '& td': { ...cell, color: 'var(--text)' },
  '& tbody tr:last-of-type td': { borderBottom: 0 },
  '& .fp-right': { textAlign: 'right' },
  '& td.fp-muted': { color: 'var(--text-secondary)' },
} as const;

/** The line under the main text of a cell (fp-table__sub), such as who registered the additional hours. */
export const subLineSx = {
  display: 'block',
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
} as const;
