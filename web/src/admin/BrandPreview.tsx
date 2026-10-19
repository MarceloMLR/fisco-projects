import ViewTimelineOutlined from '@mui/icons-material/ViewTimelineOutlined';
import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import { InstallationLogo } from '../installation/InstallationLogo';

/** The badge of the product, as at the top of the sidebar while the installation has no logo. */
function Badge({ size }: { size: number }) {
  return (
    <Box
      component="span"
      sx={{
        display: 'inline-grid',
        placeItems: 'center',
        flex: 'none',
        width: size,
        height: size,
        borderRadius: 'var(--radius-sm)',
        backgroundColor: 'var(--action)',
        color: 'var(--on-action)',
      }}
    >
      <ViewTimelineOutlined sx={{ fontSize: Math.round(size * 0.64) }} />
    </Box>
  );
}

/** A line of the small picture, as a menu item or a title would be. */
function Bar({
  color,
  width = '100%',
  height = 6,
}: {
  color: string;
  width?: string;
  height?: number;
}) {
  return (
    <Box
      component="i"
      sx={{ display: 'block', height, width, borderRadius: '2px', backgroundColor: color }}
    />
  );
}

/**
 * The Preview of General settings (canvas AdminConfiguracoes): the brand as it will be, before saving. A small
 * picture of the application, the top of the sidebar at its real size and the panel of the sign-in page.
 */
export function BrandPreview({ name, logo }: { name: string; logo: string | null }) {
  const { t } = useTranslation();

  return (
    <Box
      component="aside"
      aria-label={t('settings.previewLabel')}
      sx={{
        minWidth: 0,
        overflow: 'hidden',
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
          sx={{
            margin: 0,
            fontSize: 15,
            lineHeight: '22px',
            fontWeight: 500,
            color: 'var(--text)',
          }}
        >
          {t('settings.preview')}
        </Box>
      </Box>
      <Box
        aria-hidden="true"
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-3)',
          padding: 'var(--space-4)',
        }}
      >
        <Box
          data-testid="preview-application"
          sx={{
            display: 'grid',
            gridTemplateColumns: '64px 1fr',
            gridTemplateRows: '22px 120px',
            borderRadius: 'var(--radius-md)',
            overflow: 'hidden',
            backgroundColor: 'var(--sidebar-bg)',
            border: '1px solid var(--sidebar-border)',
          }}
        >
          <Box
            component="span"
            sx={{
              gridColumn: '1 / -1',
              display: 'flex',
              alignItems: 'center',
              gap: '4px',
              minWidth: 0,
              padding: '0 var(--space-2)',
              color: 'var(--text)',
              fontSize: 10,
              lineHeight: '14px',
              fontWeight: 500,
              whiteSpace: 'nowrap',
            }}
          >
            {logo === null ? (
              <Badge size={12} />
            ) : (
              <InstallationLogo src={logo} maxHeight={12} maxWidth={48} />
            )}
            <Box
              component="span"
              sx={{ minWidth: 0, overflow: 'hidden', textOverflow: 'ellipsis' }}
            >
              {name}
            </Box>
          </Box>
          <Box
            component="span"
            sx={{ display: 'flex', flexDirection: 'column', gap: '5px', padding: '6px' }}
          >
            <Bar color="var(--sidebar-active-bg)" />
            <Bar color="var(--sidebar-hover-bg)" width="70%" />
            <Bar color="var(--sidebar-hover-bg)" width="80%" />
          </Box>
          <Box
            component="span"
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: '6px',
              margin: '0 6px 6px 0',
              padding: 'var(--space-2)',
              borderRadius: 'var(--radius-sm)',
              backgroundColor: 'var(--bg-page)',
            }}
          >
            <Bar color="var(--text)" width="45%" height={7} />
            <Box
              component="i"
              sx={{
                display: 'block',
                height: 30,
                borderRadius: 'var(--radius-sm)',
                border: '1px solid var(--border)',
              }}
            />
            <Bar color="var(--action)" width="36%" height={10} />
          </Box>
        </Box>
        <Box
          data-testid="preview-sidebar"
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: '10px',
            minWidth: 0,
            height: 44,
            boxSizing: 'border-box',
            padding: '0 var(--space-3)',
            borderRadius: 'var(--radius-md)',
            backgroundColor: 'var(--sidebar-bg)',
            border: '1px solid var(--sidebar-border)',
            color: 'var(--text)',
            fontSize: 16,
            lineHeight: '24px',
            fontWeight: 500,
            whiteSpace: 'nowrap',
          }}
        >
          {logo === null ? <Badge size={28} /> : <InstallationLogo src={logo} maxHeight={28} />}
          <Box component="span" sx={{ minWidth: 0, overflow: 'hidden', textOverflow: 'ellipsis' }}>
            {name}
          </Box>
        </Box>
        <Box
          data-testid="preview-sign-in"
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-3)',
            minWidth: 0,
            height: 64,
            boxSizing: 'border-box',
            padding: '0 var(--space-4)',
            borderRadius: 'var(--radius-md)',
            background:
              'linear-gradient(155deg, var(--brand-panel-bg-2) 0%, var(--brand-panel-bg) 64%)',
            color: 'var(--brand-panel-text)',
            fontSize: 15,
            lineHeight: '22px',
            fontWeight: 500,
            whiteSpace: 'nowrap',
          }}
        >
          <Box
            component="span"
            sx={{
              display: 'inline-flex',
              alignItems: 'center',
              flex: 'none',
              boxSizing: 'border-box',
              height: 28,
              padding: logo === null ? 0 : '3px 5px',
              borderRadius: 'var(--radius-md)',
              backgroundColor: 'var(--brand-panel-text)',
              color: 'var(--brand-panel-bg)',
            }}
          >
            {logo === null ? (
              <Box
                component="span"
                sx={{ display: 'inline-grid', placeItems: 'center', width: 28, height: 28 }}
              >
                <ViewTimelineOutlined sx={{ fontSize: 18 }} />
              </Box>
            ) : (
              <InstallationLogo src={logo} maxHeight={22} maxWidth={120} />
            )}
          </Box>
          <Box component="span" sx={{ minWidth: 0, overflow: 'hidden', textOverflow: 'ellipsis' }}>
            {name}
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
