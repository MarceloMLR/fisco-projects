import type SvgIcon from '@mui/material/SvgIcon';
import Box from '@mui/material/Box';
import type { ReactNode } from 'react';

/** An entry of the column: its key, the text, the icon and, when known, the count beside it. */
export interface SideNavItem<Key extends string> {
  key: Key;
  label: string;
  icon: typeof SvgIcon;
  count?: number;
  /** The count stands out while it is above zero, as "Sem responsável nos meus times". */
  alert?: boolean;
}

const itemSx = {
  display: 'flex',
  width: '100%',
  boxSizing: 'border-box',
  alignItems: 'flex-start',
  gap: 'var(--space-2)',
  minHeight: 36,
  padding: 'var(--space-2) var(--space-3)',
  border: 0,
  borderRadius: 'var(--radius-md)',
  backgroundColor: 'transparent',
  color: 'var(--text)',
  fontFamily: 'var(--font-sans)',
  fontSize: 14,
  lineHeight: '20px',
  textAlign: 'left',
  cursor: 'pointer',
  '& > svg': { fontSize: 18, marginTop: '1px', color: 'var(--text-secondary)' },
  '&:hover': { backgroundColor: 'var(--bg-hover)' },
  '&:focus-visible': { outline: '2px solid var(--focus-ring)', outlineOffset: -2 },
  '&[aria-current="page"]': {
    backgroundColor: 'var(--bg-surface)',
    boxShadow: '0 0 0 1px var(--border)',
    fontWeight: 500,
    '& > svg': { color: 'var(--action-text)' },
  },
} as const;

const countSx = {
  flex: 'none',
  minWidth: 20,
  boxSizing: 'border-box',
  padding: '0 6px',
  borderRadius: 'var(--radius-full)',
  fontSize: 12,
  lineHeight: '20px',
  fontWeight: 400,
  textAlign: 'center',
  color: 'var(--text-secondary)',
  fontVariantNumeric: 'tabular-nums',
  '&[data-alert="true"]': {
    backgroundColor: 'var(--status-danger-bg)',
    color: 'var(--status-danger-text)',
    fontWeight: 500,
  },
} as const;

/**
 * A column of choices beside a list (fp-pane--nav with fp-nav2), as the views of Chamados and the folders of the
 * documents: the heading, then one entry per choice with its icon, its text and its count; the chosen one is marked
 * (aria-current). On the phone the screens show a field instead.
 */
export function SideNav<Key extends string>({
  label,
  items,
  current,
  onChoose,
  footer,
}: {
  /** The heading of the column, also its accessible name. */
  label: string;
  items: readonly SideNavItem<Key>[];
  current: Key | undefined;
  onChoose: (key: Key) => void;
  /** Something under the entries, as a note. */
  footer?: ReactNode;
}) {
  return (
    <Box
      component="nav"
      aria-label={label}
      sx={{
        minHeight: 0,
        boxSizing: 'border-box',
        padding: 'var(--space-5) var(--space-3)',
        backgroundColor: 'var(--bg-subtle)',
        borderRight: '1px solid var(--border)',
        overflow: 'auto',
      }}
    >
      <Box
        component="ul"
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: '2px',
          margin: 0,
          padding: 0,
          listStyle: 'none',
        }}
      >
        <Box
          component="li"
          aria-hidden="true"
          sx={{
            padding: '0 var(--space-3) var(--space-1)',
            fontSize: 12,
            lineHeight: '16px',
            fontWeight: 500,
            color: 'var(--text-secondary)',
          }}
        >
          {label}
        </Box>
        {items.map(({ key, label: text, icon: Icon, count, alert = false }) => (
          <li key={key}>
            <Box
              component="button"
              type="button"
              aria-current={key === current ? 'page' : undefined}
              onClick={() => {
                onChoose(key);
              }}
              sx={itemSx}
            >
              <Icon />
              <Box component="span" sx={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>
                {text}
              </Box>
              {count !== undefined && (
                <Box component="span" data-alert={alert && count > 0} sx={countSx}>
                  {count}
                </Box>
              )}
            </Box>
          </li>
        ))}
      </Box>
      {footer}
    </Box>
  );
}
