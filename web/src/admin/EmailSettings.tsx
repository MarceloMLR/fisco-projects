import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import HelpOutlineOutlined from '@mui/icons-material/HelpOutlineOutlined';
import ScheduleOutlined from '@mui/icons-material/ScheduleOutlined';
import Box from '@mui/material/Box';
import { visuallyHidden } from '@mui/utils';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, type EmailTest } from '../api/client';
import { ApiError, emailStatusQuery, queryKeys } from '../api/queries';
import { StatusBadge, type StatusTone } from '../components/StatusBadge';
import { TextField } from '../components/TextField';
import { useDates } from '../format/useDates';
import { Button } from '../components/Button';
import { useNotify } from '../notices/notify';

const looks = {
  Sent: { tone: 'success', icon: CheckCircleOutlined },
  Pending: { tone: 'info', icon: ScheduleOutlined },
  Failed: { tone: 'danger', icon: ErrorOutlineOutlined },
  none: { tone: 'neutral', icon: HelpOutlineOutlined },
} as const satisfies Record<EmailTest['status'] | 'none', { tone: StatusTone; icon: unknown }>;

const beside = {
  display: 'flex',
  alignItems: 'center',
  height: 'var(--control-md)',
  marginTop: 'calc(18px + var(--space-1))',
} as const;

// Below this width the badge and the button go under the mailbox (fp-form-row).
const narrowForm = '@media (max-width: 700px)';

/**
 * The e-mails of the installation in the "Microsoft 365" section of the General settings, as in the canvas
 * (AdminConfiguracoes): the shared mailbox, read-only, the result of the last test and "Enviar teste", which puts an
 * e-mail to the Administrator in the queue (ADR-0036). The result is read again while the test waits in the queue.
 */
export function EmailSettings() {
  const { t } = useTranslation();
  const notify = useNotify();
  const queryClient = useQueryClient();
  const { formatDateTime } = useDates();
  const { data: status } = useQuery(emailStatusQuery());

  const sendTest = useMutation({
    mutationFn: async () => {
      const { data, error, response } = await api.POST('/api/admin/email/test');
      if (data === undefined) {
        throw new ApiError(response.status, error);
      }
      return data;
    },
    onSuccess: ({ to }) => {
      notify(t('settings.emailTest.queued', { to }));
    },
    onError: (error) => {
      notify(
        error instanceof ApiError && error.status === 400
          ? t('settings.emailTest.noAddress')
          : t('settings.emailTest.failed'),
        { tone: 'error' },
      );
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.email }),
  });

  const test = status?.lastTest ?? null;
  const look = looks[test?.status ?? 'none'];
  const detail =
    test === null
      ? undefined
      : t('settings.emailTest.detail', { when: formatDateTime(test.at), to: test.to ?? '—' });

  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: 'minmax(0, 1fr) auto auto',
        gap: 'var(--space-3)',
        alignItems: 'start',
        [narrowForm]: { gridTemplateColumns: 'auto auto', justifyContent: 'start' },
      }}
    >
      <Box sx={{ minWidth: 0, [narrowForm]: { gridColumn: '1 / -1' } }}>
        <TextField
          label={t('settings.mailbox')}
          value={status?.mailbox ?? ''}
          placeholder={status === undefined ? undefined : t('settings.noMailbox')}
          helper={t('settings.mailboxHint')}
          readOnly
        />
      </Box>
      {/* Aligned with the control of the field, under its label (18 px) and the gap. */}
      <Box title={detail} sx={beside}>
        <StatusBadge tone={look.tone} icon={look.icon}>
          {t(`settings.emailTest.${test?.status ?? 'none'}`)}
        </StatusBadge>
        {detail !== undefined && (
          <Box component="span" sx={visuallyHidden}>
            {detail}
          </Box>
        )}
      </Box>
      <Box sx={beside}>
        <Button
          type="button"
          variant="outlined"
          size="small"
          loading={sendTest.isPending}
          onClick={() => {
            sendTest.mutate();
          }}
        >
          {t('settings.emailTest.send')}
        </Button>
      </Box>
    </Box>
  );
}
