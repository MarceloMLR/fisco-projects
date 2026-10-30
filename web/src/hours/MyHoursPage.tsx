import AddOutlined from '@mui/icons-material/AddOutlined';
import ChevronLeftOutlined from '@mui/icons-material/ChevronLeftOutlined';
import ChevronRightOutlined from '@mui/icons-material/ChevronRightOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { queryKeys, timeEntriesQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { type DayValue, formatDayMonthText, formatDayText } from '../format/dates';
import { useDates } from '../format/useDates';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { addDays, formatHours, weekEnd, weekStart } from './hours';
import { TimeEntryDialog } from './TimeEntryDialog';
import { TimeEntryTable } from './TimeEntryTable';

const secondarySx = { color: 'var(--text-secondary)' } as const;
const noEntries: never[] = [];

/**
 * Minhas horas (canvas Horas do projeto, ADR-0128): the week from Monday to Sunday, the entries of the person grouped
 * by day, the newest first, with the total of each day and of the week (TimeEntryTable); the week before and after and
 * "Esta semana"; "Apontar horas", and "⋮" with "Corrigir" and "Apagar" on the entries the person may change.
 */
export function MyHoursPage() {
  const { t } = useTranslation();
  const { toDayValue } = useDates();
  const queryClient = useQueryClient();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const today = toDayValue(new Date());
  const thisWeek = weekStart(today);
  const [start, setStart] = useState<DayValue>(thisWeek);
  const end = weekEnd(start);
  const [logging, setLogging] = useState(false);
  const [message, setMessage] = useState<string>();
  const { data, isFetching, error } = useQuery({
    ...timeEntriesQuery(start, end),
    placeholderData: keepPreviousData,
  });
  const total = data?.total ?? 0;

  const changed = useCallback(
    (text: string, day?: DayValue) => {
      setMessage(text);
      if (day !== undefined) {
        setStart(weekStart(day));
      }
      void queryClient.invalidateQueries({ queryKey: queryKeys.timeEntries });
      void queryClient.invalidateQueries({ queryKey: queryKeys.projectHours });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    [queryClient, setMessage, setStart],
  );

  const subtitle = t('hours.subtitle', {
    from: formatDayMonthText(start),
    to: formatDayText(end),
    hours: formatHours(total),
  });

  const log = (block: boolean) => (
    <Button
      variant="contained"
      startIcon={<AddOutlined />}
      fullWidth={block}
      onClick={() => {
        setLogging(true);
      }}
      sx={block ? { flex: 'none' } : undefined}
    >
      {t('hours.log')}
    </Button>
  );

  const weekButtons = (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
      <IconButton
        aria-label={t('hours.previousWeek')}
        onClick={() => {
          setStart(addDays(start, -7));
        }}
        sx={{
          border: '1px solid var(--border-control)',
          borderRadius: 'var(--radius-md)',
          width: 36,
          height: 36,
        }}
      >
        <ChevronLeftOutlined />
      </IconButton>
      {desktop ? (
        <Button
          variant="outlined"
          disabled={start === thisWeek}
          onClick={() => {
            setStart(thisWeek);
          }}
        >
          {t('hours.thisWeek')}
        </Button>
      ) : (
        <Box sx={{ flex: 1, textAlign: 'center' }}>
          <Box sx={{ fontWeight: 500 }}>
            {t('hours.weekShort', { from: formatDayMonthText(start), to: formatDayMonthText(end) })}
          </Box>
          <Box
            sx={{
              ...secondarySx,
              fontSize: 12,
              lineHeight: '16px',
              fontVariantNumeric: 'tabular-nums',
            }}
          >
            {t('hours.weekTotal', { hours: formatHours(total) })}
          </Box>
        </Box>
      )}
      <IconButton
        aria-label={t('hours.nextWeek')}
        disabled={start >= thisWeek}
        onClick={() => {
          setStart(addDays(start, 7));
        }}
        sx={{
          border: '1px solid var(--border-control)',
          borderRadius: 'var(--radius-md)',
          width: 36,
          height: 36,
        }}
      >
        <ChevronRightOutlined />
      </IconButton>
    </Box>
  );

  return (
    <Page
      subtitle={subtitle}
      actions={
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
          {weekButtons}
          {log(false)}
        </Box>
      }
    >
      {!desktop && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
          {weekButtons}
          {start !== thisWeek && (
            <Button
              variant="outlined"
              onClick={() => {
                setStart(thisWeek);
              }}
            >
              {t('hours.thisWeek')}
            </Button>
          )}
          {log(true)}
        </Box>
      )}
      {error === null ? (
        <TimeEntryTable
          label={t('hours.table')}
          entries={data?.items ?? noEntries}
          first="project"
          loading={isFetching}
          emptyLabel={t('hours.empty')}
          footer={t('hours.total', { hours: formatHours(total) })}
          onChanged={changed}
        />
      ) : (
        <Alert tone="danger">{t('hours.loadFailed')}</Alert>
      )}
      {logging && (
        <TimeEntryDialog
          today={today}
          day={start === thisWeek ? today : end}
          onClose={() => {
            setLogging(false);
          }}
          onSaved={(text, day) => {
            setLogging(false);
            changed(text, day);
          }}
        />
      )}
      <Snackbar
        open={message !== undefined}
        message={message}
        autoHideDuration={6000}
        onClose={() => {
          setMessage(undefined);
        }}
      />
    </Page>
  );
}
