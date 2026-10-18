import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';

/**
 * Invisible header over the content (TopBar, fp-appbar): same color as the content, no line below, with the
 * page trail. The bell and the account menu join it with their own changes.
 */
export function TopBar({ title }: { title: string }) {
  const { t } = useTranslation();

  return (
    <Box
      component="header"
      sx={{
        gridColumn: 2,
        gridRow: 1,
        position: 'relative',
        zIndex: 20,
        display: 'flex',
        alignItems: 'center',
        boxSizing: 'border-box',
        height: 'var(--topbar-height)',
        padding: '0 var(--space-4) 0 var(--space-2)',
        backgroundColor: 'var(--topbar-bg)',
        color: 'var(--text)',
      }}
    >
      <Box
        component="nav"
        aria-label={t('shell.breadcrumb')}
        sx={{ flex: 1, minWidth: 0, paddingLeft: 'var(--space-2)' }}
      >
        <Box
          component="ol"
          sx={{
            display: 'flex',
            alignItems: 'center',
            margin: 0,
            padding: 0,
            listStyle: 'none',
            fontSize: 14,
            lineHeight: '20px',
            whiteSpace: 'nowrap',
            overflow: 'hidden',
            '& li + li::before': {
              content: '"/"',
              padding: '0 2px',
              color: 'var(--text-disabled)',
            },
          }}
        >
          <Box component="li" sx={{ display: 'flex', alignItems: 'center', minWidth: 0 }}>
            <Box
              component="span"
              aria-current="page"
              sx={{
                display: 'inline-flex',
                alignItems: 'center',
                height: 32,
                padding: '0 var(--space-2)',
                borderRadius: 'var(--radius-md)',
                color: 'var(--text)',
                fontWeight: 500,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
              }}
            >
              {title}
            </Box>
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
