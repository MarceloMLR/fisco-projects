import ChevronLeftOutlined from '@mui/icons-material/ChevronLeftOutlined';
import ChevronRightOutlined from '@mui/icons-material/ChevronRightOutlined';
import CloseOutlined from '@mui/icons-material/CloseOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { projectEntriesQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { type DayValue, formatMonthText } from '../format/dates';
import { useDates } from '../format/useDates';
import { addMonths, formatHours, monthEnd, monthStart } from '../hours/hours';
import { TimeEntryTable } from '../hours/TimeEntryTable';
import type { HoursProject } from './ProjectHoursDialogs';

const noEntries: never[] = [];

const monthButtonSx = {
  border: '1px solid var(--border-control)',
  borderRadius: 'var(--radius-md)',
  width: 32,
  height: 32,
} as const;

/**
 * "Ver apontamentos" of the hours of a project (ADR-0128): the entries of every person in the project, a month at a
 * time, grouped by day as in Minhas horas, with the person of each one and the "⋮" to correct or delete it, with the
 * rules of an entry. The month before and after, never past the current one, and "Este mês" are in the header.
 */
export function ProjectEntriesDialog({
  project,
  onClose,
  onChanged,
}: {
  project: HoursProject;
  onClose: () => void;
  /** An entry was corrected or deleted, with the message. */
  onChanged: (message: string) => void;
}) {
  const { t } = useTranslation();
  const { toDayValue } = useDates();
  const thisMonth = monthStart(toDayValue(new Date()));
  const [month, setMonth] = useState<DayValue>(thisMonth);
  const { data, isFetching, error } = useQuery({
    ...projectEntriesQuery(project.id, month, monthEnd(month)),
    placeholderData: keepPreviousData,
  });
  const total = data?.total ?? 0;

  return (
    <Dialog
      open
      size="full"
      title={t('projectHours.entries.title')}
      subtitle={t('projectHours.entries.subtitle', { code: project.code, name: project.name })}
      onClose={onClose}
      headerTools={
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: 'var(--space-2)',
            flex: 1,
          }}
        >
          <IconButton
            aria-label={t('projectHours.entries.previousMonth')}
            onClick={() => {
              setMonth(addMonths(month, -1));
            }}
            sx={monthButtonSx}
          >
            <ChevronLeftOutlined />
          </IconButton>
          <Box
            component="span"
            aria-live="polite"
            sx={{ minWidth: 160, textAlign: 'center', fontWeight: 500 }}
          >
            {formatMonthText(month)}
          </Box>
          <IconButton
            aria-label={t('projectHours.entries.nextMonth')}
            disabled={month >= thisMonth}
            onClick={() => {
              setMonth(addMonths(month, 1));
            }}
            sx={monthButtonSx}
          >
            <ChevronRightOutlined />
          </IconButton>
          <Button
            variant="outlined"
            size="small"
            disabled={month === thisMonth}
            onClick={() => {
              setMonth(thisMonth);
            }}
          >
            {t('projectHours.entries.thisMonth')}
          </Button>
          <Box sx={{ flex: 1 }} />
          <IconButton
            aria-label={t('dialog.close')}
            onClick={onClose}
            sx={{ color: 'var(--text-secondary)' }}
          >
            <CloseOutlined sx={{ fontSize: 'var(--icon-md)' }} />
          </IconButton>
        </Box>
      }
    >
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          height: '100%',
          boxSizing: 'border-box',
          display: 'flex',
          flexDirection: 'column',
          padding: 'var(--space-4)',
        }}
      >
        {error === null ? (
          <TimeEntryTable
            label={t('projectHours.entries.table')}
            entries={data?.items ?? noEntries}
            first="person"
            loading={isFetching}
            emptyLabel={t('projectHours.entries.empty')}
            footer={t('projectHours.entries.total', { hours: formatHours(total) })}
            onChanged={onChanged}
          />
        ) : (
          <Alert tone="danger">{t('projectHours.entries.loadFailed')}</Alert>
        )}
      </Box>
    </Dialog>
  );
}
