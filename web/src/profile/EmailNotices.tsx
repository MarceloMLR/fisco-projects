import Box from '@mui/material/Box';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, type ImmediateNotices, type Profile } from '../api/client';
import {
  ApiError,
  generalRulesQuery,
  immediateNoticesQuery,
  profileQuery,
  queryKeys,
} from '../api/queries';
import { Checkbox } from '../components/Checkbox';
import { useNotify } from '../notices/notify';
import { ProfileCard, ProfileRow } from './ProfileCard';

/** The channels of the notices sent now, as the API takes them. */
type Channels = Pick<ImmediateNotices, 'email' | 'teams'>;

const help = { fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' } as const;

const column = { display: 'flex', flexDirection: 'column', gap: '10px' } as const;

/**
 * The card "Avisos" of My profile (ADR-0037, ADR-0151): the daily summary, at the time of the installation, and the
 * channels of the notices that ask for an action, each turned on and off by the person and kept at once: "Por e-mail",
 * on for who never chose, and "Pelo Teams", off for who never chose and only with Teams there for the person. With both
 * off, the card says the notices stay in the bell and in My work.
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

  const chooseChannels = useMutation({
    mutationFn: async (channels: Channels) => {
      const { error, response } = await api.PUT('/api/profile/immediate-notices', {
        body: channels,
      });
      if (!response.ok) {
        throw new ApiError(response.status, error);
      }
    },
    onMutate: (channels) => {
      // The boxes follow the click; a refusal puts them back.
      queryClient.setQueryData<ImmediateNotices>(noticesKey, (current) =>
        current === undefined ? current : { ...current, ...channels },
      );
    },
    onError: () => {
      notify(t('profile.notices.channelFailed'), { tone: 'error' });
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: noticesKey }),
  });

  const channels: Channels = { email: notices?.email ?? true, teams: notices?.teams ?? false };
  const teamsShown = notices?.teamsAvailable === true;
  const busy = notices === undefined || chooseChannels.isPending;
  // Teams off for the person, or off in the installation: only the e-mail counts.
  const onlyInBell = !channels.email && !(teamsShown && channels.teams);

  return (
    <ProfileCard id="profile-notices" title={t('profile.notices.title')}>
      <ProfileRow label={t('profile.notices.summary')}>
        <Box sx={column}>
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
        <Box sx={column}>
          <Box component="span" sx={help}>
            {t('profile.notices.nowHelp')}
          </Box>
          <Checkbox
            label={t('profile.notices.byEmail')}
            checked={channels.email}
            disabled={busy}
            onChange={(email) => {
              chooseChannels.mutate({ ...channels, email });
            }}
          />
          {teamsShown && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}>
              <Checkbox
                label={t('profile.notices.byTeams')}
                checked={channels.teams}
                disabled={busy}
                onChange={(teams) => {
                  chooseChannels.mutate({ ...channels, teams });
                }}
              />
              <Box component="span" sx={{ ...help, paddingLeft: 'calc(18px + var(--space-2))' }}>
                {t('profile.notices.teamsHelp')}
              </Box>
            </Box>
          )}
          {notices !== undefined && onlyInBell && (
            <Box component="span" role="status" sx={help}>
              {t('profile.notices.onlyInBell')}
            </Box>
          )}
        </Box>
      </ProfileRow>
    </ProfileCard>
  );
}
