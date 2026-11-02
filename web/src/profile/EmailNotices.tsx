import Box from '@mui/material/Box';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  api,
  type ImmediateNoticeChannel,
  type ImmediateNotices,
  type Profile,
} from '../api/client';
import {
  ApiError,
  generalRulesQuery,
  immediateNoticesQuery,
  profileQuery,
  queryKeys,
} from '../api/queries';
import { Checkbox } from '../components/Checkbox';
import { SegmentedControl } from '../components/SegmentedControl';
import { useNotify } from '../notices/notify';
import { ProfileCard, ProfileRow } from './ProfileCard';

const immediate = ['messages', 'assigned', 'deadlines'] as const;
const channels = ['Email', 'Teams', 'Both'] as const satisfies readonly ImmediateNoticeChannel[];

const help = { fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' } as const;

/**
 * The card "Avisos" of My profile, as in the canvas (ADR-0037, ADR-0149): the daily summary, at the time of the
 * installation, which the person turns on and off, kept at once in the profile; and the notices sent now, which cannot
 * be turned off: with Teams there for the person, the choice of where they arrive (E-mail, Teams or both), kept at
 * once too; without it, the e-mails always on.
 */
export function EmailNotices() {
  const { t } = useTranslation();
  const notify = useNotify();
  const queryClient = useQueryClient();
  const { data: profile } = useQuery(profileQuery());
  const { data: rules } = useQuery(generalRulesQuery());
  const { data: notices } = useQuery(immediateNoticesQuery());
  const time = rules?.dailySummaryAt.slice(0, 5) ?? '';
  const noticesKey = immediateNoticesQuery().queryKey;

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

  const chooseChannel = useMutation({
    mutationFn: async (channel: ImmediateNoticeChannel) => {
      const { error, response } = await api.PUT('/api/profile/immediate-notices', {
        body: { channel },
      });
      if (!response.ok) {
        throw new ApiError(response.status, error);
      }
    },
    onMutate: (channel) => {
      // The choice follows the click; a refusal puts it back.
      queryClient.setQueryData<ImmediateNotices>(noticesKey, (current) =>
        current === undefined ? current : { ...current, channel },
      );
    },
    onError: () => {
      notify(t('profile.notices.channelFailed'), { tone: 'error' });
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: noticesKey }),
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
        {notices?.teamsAvailable === true ? (
          <Box
            sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: '10px' }}
          >
            <SegmentedControl
              label={t('profile.notices.channel')}
              value={notices.channel}
              options={channels.map((channel) => ({
                value: channel,
                label: t(`profile.notices.channels.${channel}`),
              }))}
              onChange={(channel) => {
                chooseChannel.mutate(channel);
              }}
            />
            <Box component="span" sx={help}>
              {t('profile.notices.channelHelp')}
            </Box>
          </Box>
        ) : (
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
            <Box component="span" sx={help}>
              {t('profile.notices.alwaysOn')}
            </Box>
          </Box>
        )}
      </ProfileRow>
    </ProfileCard>
  );
}
