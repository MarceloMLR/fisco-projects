import Box from '@mui/material/Box';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, type Profile } from '../api/client';
import { ApiError, generalRulesQuery, profileQuery, queryKeys } from '../api/queries';
import { Checkbox } from '../components/Checkbox';
import { useNotify } from '../notices/notify';
import { ProfileCard, ProfileRow } from './ProfileCard';

const immediate = ['messages', 'assigned', 'deadlines'] as const;

/**
 * The card "Avisos por e-mail" of My profile, as in the canvas (ADR-0037): the daily summary, at the time of the
 * installation, which the person turns on and off, kept at once in the profile; and the e-mails sent now, always on,
 * since they cannot be turned off.
 */
export function EmailNotices() {
  const { t } = useTranslation();
  const notify = useNotify();
  const queryClient = useQueryClient();
  const { data: profile } = useQuery(profileQuery());
  const { data: rules } = useQuery(generalRulesQuery());
  const time = rules?.dailySummaryAt.slice(0, 5) ?? '';

  const choose = useMutation({
    mutationFn: async (receives: boolean) => {
      const { error, response } = await api.PUT('/api/profile/daily-summary', {
        body: { receives },
      });
      if (!response.ok) {
        throw new ApiError(response.status, error);
      }
    },
    onMutate: (receives) => {
      // The box follows the click; a refusal puts it back.
      queryClient.setQueryData<Profile>(queryKeys.profile, (current) =>
        current === undefined ? current : { ...current, receivesDailySummary: receives },
      );
    },
    onError: () => {
      notify(t('profile.notices.failed'), { tone: 'error' });
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.profile }),
  });

  return (
    <ProfileCard id="profile-notices" title={t('profile.notices.title')}>
      <ProfileRow label={t('profile.notices.summary')}>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <Checkbox
            label={t('profile.notices.receiveSummary', { time })}
            checked={profile?.receivesDailySummary ?? true}
            disabled={profile === undefined || choose.isPending}
            onChange={(receives) => {
              choose.mutate(receives);
            }}
          />
        </Box>
      </ProfileRow>
      <ProfileRow label={t('profile.notices.now')}>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          {immediate.map((item) => (
            <Checkbox
              key={item}
              label={t(`profile.notices.immediate.${item}`)}
              checked
              disabled
              onChange={() => undefined}
            />
          ))}
          <Box
            component="span"
            sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
          >
            {t('profile.notices.alwaysOn')}
          </Box>
        </Box>
      </ProfileRow>
    </ProfileCard>
  );
}
