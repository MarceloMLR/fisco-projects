import Box from '@mui/material/Box';
import { useTranslation } from 'react-i18next';
import type { FolderRole } from '../api/client';
import { Checkbox } from '../components/Checkbox';
import { folderRoles, inOrder } from './folderVisibility';

/**
 * "Quem vê" of a folder (ADR-0148): one box per role of the project, with the note that the Administrator and the
 * manager always see every folder. The folder of the acceptance terms shows only that it is of the managers.
 */
export function FolderVisibilityFields({
  value,
  onChange,
  isAcceptance = false,
}: {
  value: readonly FolderRole[];
  onChange: (roles: FolderRole[]) => void;
  isAcceptance?: boolean;
}) {
  const { t } = useTranslation();
  return (
    <Box
      component="fieldset"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-2)',
        margin: 0,
        padding: 0,
        border: 0,
        minWidth: 0,
      }}
    >
      <Box
        component="legend"
        sx={{
          padding: 0,
          marginBottom: 'var(--space-1)',
          fontSize: 13,
          lineHeight: '18px',
          fontWeight: 500,
          color: 'var(--text)',
        }}
      >
        {t('folderVisibility.label')}
      </Box>
      {isAcceptance ? (
        <Box component="p" sx={{ margin: 0, color: 'var(--text-secondary)' }}>
          {t('folderVisibility.termsFixed')}
        </Box>
      ) : (
        <>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: 'minmax(0, 1fr)', sm: 'repeat(2, minmax(0, 1fr))' },
              gap: 'var(--space-2) var(--space-4)',
            }}
          >
            {folderRoles.map((role) => (
              <Checkbox
                key={role}
                label={t(`projects.roles.${role}`)}
                checked={value.includes(role)}
                onChange={(checked) => {
                  onChange(
                    inOrder(checked ? [...value, role] : value.filter((item) => item !== role)),
                  );
                }}
              />
            ))}
          </Box>
          <Box
            component="p"
            sx={{ margin: 0, fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
          >
            {t('folderVisibility.help')}
          </Box>
        </>
      )}
    </Box>
  );
}
