import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { visuallyHidden } from '@mui/utils';
import useMediaQuery from '@mui/material/useMediaQuery';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { theme } from '../theme/theme';
import { useCurrentPage } from './pages';

/**
 * A screen inside the shell (fp-page): page title and content with the design system margins. On the phone
 * the title is in the top bar, so the heading stays only for screen readers.
 */
export function Page({ children }: { children?: ReactNode }) {
  const { t } = useTranslation();
  const page = useCurrentPage();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });

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
        padding: desktop ? 'var(--space-5) var(--space-6) var(--space-6)' : 'var(--space-3)',
      }}
    >
      <Typography variant="h1" sx={desktop ? { margin: 0 } : visuallyHidden}>
        {t(page.titleKey)}
      </Typography>
      {children}
    </Box>
  );
}
