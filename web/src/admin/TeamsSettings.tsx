import Box from '@mui/material/Box';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, type TeamsApp } from '../api/client';
import { ApiError, installationQuery, queryKeys, teamsAppQuery } from '../api/queries';
import { Button } from '../components/Button';
import { Checkbox } from '../components/Checkbox';
import { TextField } from '../components/TextField';
import { saveFile } from '../format/saveFile';
import { useNotify } from '../notices/notify';
import { iconSourceOf, teamsPackage } from './teamsPackage';

const help = { fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' } as const;

const beside = {
  display: 'flex',
  alignItems: 'center',
  height: 'var(--control-md)',
  marginTop: 'calc(18px + var(--space-1))',
} as const;

// Below this width the button goes under the version (fp-form-row).
const narrowForm = '@media (max-width: 700px)';

/**
 * The notices by Teams in the "Microsoft 365" section of the General settings (ADR-0149): "Avisar pelo Teams", kept at
 * once, and the package of the application of the installation, with the version of the last one and "Baixar pacote
 * do Teams", which asks the server for the manifest and draws the icons and the .zip on the screen. Without the
 * sending to Teams in the configuration of the application, the section says Teams is not there.
 */
export function TeamsSettings() {
  const { t } = useTranslation();
  const notify = useNotify();
  const queryClient = useQueryClient();
  const { data: app } = useQuery(teamsAppQuery());
  const { data: installation } = useQuery(installationQuery());
  const available = app?.available === true;

  const turn = useMutation({
    mutationFn: async (noticesOn: boolean) => {
      const { error, response } = await api.PUT('/api/admin/teams-app', { body: { noticesOn } });
      if (!response.ok) {
        throw new ApiError(response.status, error);
      }
    },
    onMutate: (noticesOn) => {
      // The box follows the click; a refusal puts it back.
      queryClient.setQueryData<TeamsApp>(queryKeys.teams, (current) =>
        current === undefined ? current : { ...current, noticesOn },
      );
    },
    onError: () => {
      notify(t('settings.teams.turnFailed'), { tone: 'error' });
    },
    onSettled: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.teams });
      // Whether Teams is there for the Administrator too, in My profile.
      await queryClient.invalidateQueries({ queryKey: queryKeys.profile });
      await queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
  });

  const download = useMutation({
    mutationFn: async () => {
      const brand = {
        logoUrl: installation?.logoUrl ?? null,
        mainColor: installation?.mainColor ?? null,
      };
      const { data, error, response } = await api.POST('/api/admin/teams-app/package', {
        body: { iconSource: iconSourceOf(brand) },
      });
      if (data === undefined) {
        throw new ApiError(response.status, error);
      }
      saveFile(
        await teamsPackage(data.manifest, brand),
        t('settings.teams.fileName', { version: data.version }),
      );
      return data.version;
    },
    onSuccess: (version) => {
      notify(t('settings.teams.downloaded', { version }));
    },
    onError: (error) => {
      notify(
        error instanceof ApiError && error.status === 409
          ? t('settings.teams.unavailable')
          : t('settings.teams.downloadFailed'),
        { tone: 'error' },
      );
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.teams }),
  });

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}>
        <Checkbox
          label={t('settings.teams.noticesOn')}
          checked={app?.noticesOn ?? false}
          disabled={!available || turn.isPending}
          onChange={(noticesOn) => {
            turn.mutate(noticesOn);
          }}
        />
        <Box component="span" sx={{ ...help, paddingLeft: 'calc(18px + var(--space-2))' }}>
          {app === undefined || available
            ? t('settings.teams.noticesOnHint')
            : t('settings.teams.unavailable')}
        </Box>
      </Box>
      {available && (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'minmax(0, 1fr) auto',
            gap: 'var(--space-3)',
            alignItems: 'start',
            [narrowForm]: { gridTemplateColumns: 'auto', justifyContent: 'start' },
          }}
        >
          <Box sx={{ minWidth: 0 }}>
            <TextField
              label={t('settings.teams.package')}
              value={
                app.version === null ? '' : t('settings.teams.version', { version: app.version })
              }
              placeholder={t('settings.teams.neverDownloaded')}
              helper={t('settings.teams.packageHint')}
              readOnly
            />
          </Box>
          <Box sx={{ ...beside, [narrowForm]: { marginTop: 0 } }}>
            <Button
              type="button"
              variant="outlined"
              size="small"
              loading={download.isPending}
              disabled={installation === undefined}
              onClick={() => {
                download.mutate();
              }}
            >
              {t('settings.teams.download')}
            </Button>
          </Box>
        </Box>
      )}
    </Box>
  );
}
