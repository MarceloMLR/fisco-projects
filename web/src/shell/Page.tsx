import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { visuallyHidden } from '@mui/utils';
import useMediaQuery from '@mui/material/useMediaQuery';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { theme } from '../theme/theme';
import { useCurrentPage } from './pages';

/**
 * A screen inside the shell (fp-page): page title, with an optional count (fp-count), and content with the design
 * system margins. On the phone the title is in the top bar, so the heading stays only for screen readers.
 * With actions, the title and the actions share a row (fp-head). A screen with a header of its own, as a project,
 * leaves the title out; a screen split in panes leaves the margins to them (flush).
 */
export function Page({
  count,
  actions,
  withTitle = true,
  flush = false,
  children,
}: {
  count?: number;
  /** Actions of the page, at the right of the title on the computer (fp-head__actions); the phone places its own. */
  actions?: ReactNode;
  /** False when the screen has a header of its own, with the main heading. */
  withTitle?: boolean;
  /** Without the margins, for a screen split in panes with margins of their own (fp-split), as the documents. */
  flush?: boolean;
  children?: ReactNode;
}) {
  const { t } = useTranslation();
  const page = useCurrentPage();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });

  const heading = (
    <Typography variant="h1" sx={desktop ? { margin: 0 } : visuallyHidden}>
      {t(page.titleKey)}
      {count !== undefined && (
        <Box
          component="span"
          sx={{
            marginLeft: 'var(--space-2)',
            fontSize: 20,
            fontWeight: 400,
            color: 'var(--text-secondary)',
            fontVariantNumeric: 'tabular-nums',
          }}
        >
          {count}
        </Box>
      )}
    </Typography>
  );

  return (
    <Box
      sx={{
        flex: 1,
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-5)',
        minWidth: 0,
        minHeight: 0,
        boxSizing: 'border-box',
        padding: flush
          ? 0
          : desktop
            ? 'var(--space-5) var(--space-6) var(--space-6)'
            : 'var(--space-3)',
      }}
    >
      {!withTitle ? null : desktop && actions !== undefined ? (
        <Box
          sx={{
            display: 'flex',
            alignItems: 'flex-end',
            justifyContent: 'space-between',
            gap: 'var(--space-4)',
            flexWrap: 'wrap',
          }}
        >
          {heading}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>{actions}</Box>
        </Box>
      ) : (
        heading
      )}
      {children}
    </Box>
  );
}
