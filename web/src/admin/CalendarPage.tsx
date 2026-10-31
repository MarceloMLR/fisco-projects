import AddOutlined from '@mui/icons-material/AddOutlined';
import ChevronLeftOutlined from '@mui/icons-material/ChevronLeftOutlined';
import ChevronRightOutlined from '@mui/icons-material/ChevronRightOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import DoneAllOutlined from '@mui/icons-material/DoneAllOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { type MouseEvent, useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  api,
  type CalendarDay,
  type CalendarDayKind,
  calendarDayKinds,
  type CalendarYear,
  calendarYears,
  type DayOfWeek,
  weekdays,
} from '../api/client';
import { ApiError, calendarYearQuery, queryKeys, scheduleRecalculationQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { CodeChip } from '../components/Card';
import { Checkbox } from '../components/Checkbox';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { detailLineSx } from '../components/detailLineSx';
import { Dialog } from '../components/Dialog';
import { SegmentedControl } from '../components/SegmentedControl';
import { Select } from '../components/Select';
import { useDates } from '../format/useDates';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { calendarDayId, holidayName } from './calendarText';
import { HolidayDialog } from './HolidayDialog';
import { Button, IconButton } from '../components/Button';

type KindFilter = 'all' | CalendarDayKind;

/** Every date of a year in one page: there are fewer than 50. */
const pageSize = 100;
const noPageChange = () => undefined;
/** A disregarded date shows dimmed, as an inactive person in Users. */
const isDisregarded = (day: CalendarDay) => !day.isConsidered;

const cardSx = {
  boxSizing: 'border-box',
  backgroundColor: 'var(--bg-surface)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-lg)',
} as const;

const cardTitleSx = {
  margin: 0,
  fontSize: 15,
  lineHeight: '22px',
  fontWeight: 500,
  color: 'var(--text)',
} as const;

const iconButtonSx = {
  width: 'var(--control-sm)',
  height: 'var(--control-sm)',
  borderRadius: 'var(--radius-md)',
  color: 'var(--text-secondary)',
  '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
} as const;

/** A change of the dates saved at once: their keys and ids go in one call. */
interface Consideration {
  year: number;
  days: readonly CalendarDay[];
  isConsidered: boolean;
}

/** The calendar in the cache with some dates considered or disregarded, before the API answers. */
function withConsideration(
  calendar: CalendarYear | undefined,
  { days, isConsidered }: Consideration,
): CalendarYear | undefined {
  const chosen = new Set(days.map(calendarDayId));
  return calendar === undefined
    ? undefined
    : {
        ...calendar,
        days: calendar.days.map((day) =>
          chosen.has(calendarDayId(day)) ? { ...day, isConsidered } : day,
        ),
      };
}

/**
 * Calendar and holidays of the Administration, as in the canvas (AdminCalendario, ADR-0018, ADR-0083): the working
 * days at the top and the dates of a year below, with the kind, the repetition and "Considerado". Checking a box saves
 * at once; "Marcar todos" considers every date shown, and "Desmarcar todos" disregards them when all are considered.
 * Local holidays are included, changed and removed in dialogs. On the phone the dates are a list. While tasks and
 * milestones of open projects still follow the calendar before, a warning at the top offers "Recalcular as datas",
 * asked again after every change (ADR-0126).
 */
export function CalendarPage() {
  const { t } = useTranslation();
  const dates = useDates();
  const { formatDayValue, formatWeekday } = dates;
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const queryClient = useQueryClient();
  const [year, setYear] = useState(() => Number(dates.toDayValue(new Date()).slice(0, 4)));
  const [kind, setKind] = useState<KindFilter>('all');
  const [message, setMessage] = useState<string>();
  const [menu, setMenu] = useState<{ anchor: HTMLElement; day: CalendarDay }>();
  const [editing, setEditing] = useState<{ holiday?: CalendarDay }>();
  const [removing, setRemoving] = useState<CalendarDay>();
  const [recalculating, setRecalculating] = useState(false);

  const { data, isFetching, isError } = useQuery({
    ...calendarYearQuery(year),
    placeholderData: keepPreviousData,
  });
  const { data: recalculation } = useQuery(scheduleRecalculationQuery());

  // After the last of the changes in a row, the calendar and the Audit are asked again; before, a refresh could
  // bring back a box the person just changed.
  const askAgain = useCallback(() => {
    if (queryClient.isMutating({ mutationKey: queryKeys.calendar }) <= 1) {
      void queryClient.invalidateQueries({ queryKey: queryKeys.calendar });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    }
  }, [queryClient]);

  const consider = useMutation({
    mutationKey: queryKeys.calendar,
    mutationFn: async ({ days, isConsidered }: Consideration) => {
      const { response } = await api.PUT('/api/calendar/considered', {
        body: {
          isConsidered,
          calculatedHolidays: days.flatMap((day) => (day.key === null ? [] : [day.key])),
          localHolidays: days.flatMap((day) => (day.id === null ? [] : [day.id])),
        },
      });
      if (!response.ok) {
        throw new ApiError(response.status, null);
      }
    },
    onMutate: async (change) => {
      const key = calendarYearQuery(change.year).queryKey;
      await queryClient.cancelQueries({ queryKey: key });
      const previous = queryClient.getQueryData(key);
      queryClient.setQueryData(key, withConsideration(previous, change));
      return { previous };
    },
    onError: (_error, change, result) => {
      queryClient.setQueryData(calendarYearQuery(change.year).queryKey, result?.previous);
      setMessage(t('calendar.messages.failed'));
    },
    onSuccess: () => {
      setMessage(t('calendar.messages.saved'));
    },
    onSettled: askAgain,
  });

  const changeWorkingDays = useMutation({
    mutationKey: queryKeys.calendar,
    mutationFn: async ({ workingDays }: { year: number; workingDays: DayOfWeek[] }) => {
      const { response } = await api.PUT('/api/calendar/working-days', { body: { workingDays } });
      if (!response.ok) {
        throw new ApiError(response.status, null);
      }
    },
    onMutate: async ({ year: shown, workingDays }) => {
      const key = calendarYearQuery(shown).queryKey;
      await queryClient.cancelQueries({ queryKey: key });
      const previous = queryClient.getQueryData(key);
      queryClient.setQueryData(
        key,
        previous === undefined ? undefined : { ...previous, workingDays },
      );
      return { previous };
    },
    onError: (_error, { year: shown }, result) => {
      queryClient.setQueryData(calendarYearQuery(shown).queryKey, result?.previous);
      setMessage(t('calendar.messages.failed'));
    },
    onSuccess: () => {
      setMessage(t('calendar.messages.saved'));
    },
    onSettled: askAgain,
  });

  const remove = useMutation({
    mutationFn: async (holiday: CalendarDay) => {
      const { response } = await api.DELETE('/api/calendar/local-holidays/{id}', {
        params: { path: { id: holiday.id ?? '' } },
      });
      if (!response.ok) {
        throw new ApiError(response.status, null);
      }
    },
    onSuccess: (_result, holiday) => {
      setMessage(t('calendar.messages.removed', { name: holidayName(holiday, t) }));
      void queryClient.invalidateQueries({ queryKey: queryKeys.calendar });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    onError: () => {
      setMessage(t('calendar.messages.failed'));
    },
    onSettled: () => {
      setRemoving(undefined);
    },
  });

  const recalculate = useMutation({
    mutationFn: async () => {
      const { data: done, response } = await api.POST('/api/admin/schedule-recalculation');
      if (done === undefined) {
        throw new ApiError(response.status, null);
      }
      return done;
    },
    onSuccess: (done) => {
      setMessage(
        done.projects.length === 0
          ? t('calendar.recalculation.nothing')
          : t('calendar.recalculation.done', { count: done.projects.length }),
      );
      // The dates of the schedules changed: every screen that shows them asks again.
      void queryClient.invalidateQueries({ queryKey: queryKeys.calendar });
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
      void queryClient.invalidateQueries({ queryKey: queryKeys.myWork });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
    },
    onError: () => {
      setMessage(t('calendar.recalculation.failed'));
    },
    onSettled: () => {
      setRecalculating(false);
    },
  });

  const workingDays = data?.workingDays ?? [];
  const allDays = data?.days;
  const shown = useMemo(
    () => (allDays ?? []).filter((day) => kind === 'all' || day.kind === kind),
    [allDays, kind],
  );
  const allConsidered = shown.length > 0 && shown.every((day) => day.isConsidered);
  const consideredCount = shown.filter((day) => day.isConsidered).length;

  const { mutate: considerDays } = consider;
  const toggle = useCallback(
    (day: CalendarDay, isConsidered: boolean) => {
      considerDays({ year, days: [day], isConsidered });
    },
    [considerDays, year],
  );
  const openMenu = useCallback((event: MouseEvent<HTMLElement>, day: CalendarDay) => {
    setMenu({ anchor: event.currentTarget, day });
  }, []);

  const toggleWorkingDay = (day: DayOfWeek, working: boolean) => {
    const next = weekdays.filter((weekday) =>
      weekday === day ? working : workingDays.includes(weekday),
    );
    changeWorkingDays.mutate({ year, workingDays: next });
  };

  const markAll = () => {
    consider.mutate({ year, days: shown, isConsidered: !allConsidered });
  };

  const kindOf = useCallback((day: CalendarDay) => t(`calendar.kinds.${day.kind}`), [t]);
  const repeatsOf = useCallback(
    (day: CalendarDay) =>
      day.repeatsEveryYear
        ? t('calendar.repeats.everyYear')
        : t('calendar.repeats.onlyIn', { year: Number(day.date.slice(0, 4)) }),
    [t],
  );

  const columns = useMemo<DataTableColumn<CalendarDay>[]>(
    () => [
      {
        field: 'date',
        headerName: t('calendar.columns.date'),
        width: 150,
        renderCell: ({ row }) => (
          <Box sx={{ minWidth: 0 }}>
            <Box component="span" sx={{ display: 'block' }}>
              {formatDayValue(row.date)}
            </Box>
            <Box
              component="span"
              sx={{
                display: 'block',
                marginTop: '2px',
                fontSize: 12,
                lineHeight: '16px',
                color: 'var(--text-secondary)',
              }}
            >
              {formatWeekday(row.date)}
            </Box>
          </Box>
        ),
      },
      {
        field: 'name',
        headerName: t('calendar.columns.name'),
        flex: 1,
        minWidth: 240,
        renderCell: ({ row }) => (
          <Box
            component="span"
            sx={{
              fontWeight: 500,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {holidayName(row, t)}
          </Box>
        ),
      },
      {
        field: 'kind',
        headerName: t('calendar.columns.kind'),
        width: 160,
        valueGetter: (_value, row) => kindOf(row),
      },
      {
        field: 'repeatsEveryYear',
        headerName: t('calendar.columns.repeats'),
        width: 120,
        valueGetter: (_value, row) => repeatsOf(row),
      },
      {
        field: 'isConsidered',
        headerName: t('calendar.columns.considered'),
        width: 130,
        renderCell: ({ row }) => (
          <Checkbox
            hideLabel
            label={t('calendar.consider', { name: holidayName(row, t) })}
            checked={row.isConsidered}
            onChange={(checked) => {
              toggle(row, checked);
            }}
          />
        ),
      },
      {
        field: 'actions',
        headerName: t('calendar.columns.actions'),
        renderHeader: () => null,
        width: 52,
        align: 'right',
        renderCell: ({ row }) =>
          row.kind === 'LocalHoliday' && (
            <IconButton
              size="small"
              aria-label={t('calendar.menu.button', { name: holidayName(row, t) })}
              aria-haspopup="menu"
              onClick={(event) => {
                openMenu(event, row);
              }}
              sx={iconButtonSx}
            >
              <MoreVertOutlined sx={{ fontSize: 'var(--icon-md)' }} />
            </IconButton>
          ),
      },
    ],
    [t, formatDayValue, formatWeekday, kindOf, repeatsOf, toggle, openMenu],
  );

  const footer = useCallback(
    () =>
      `${t('calendar.dates', { count: shown.length, year })} · ${t('calendar.considered', { count: consideredCount })}`,
    [t, shown.length, year, consideredCount],
  );

  const yearStepper = (
    <Box
      role="group"
      aria-label={t('calendar.year')}
      sx={{ display: 'flex', alignItems: 'center', gap: '2px' }}
    >
      <IconButton
        size="small"
        aria-label={t('calendar.previousYear')}
        disabled={year <= calendarYears.first}
        onClick={() => {
          setYear(year - 1);
        }}
        sx={iconButtonSx}
      >
        <ChevronLeftOutlined sx={{ fontSize: 'var(--icon-md)' }} />
      </IconButton>
      <Box
        component="span"
        aria-live="polite"
        sx={{
          minWidth: 48,
          textAlign: 'center',
          fontSize: 15,
          lineHeight: '22px',
          fontWeight: 500,
          fontVariantNumeric: 'tabular-nums',
          color: 'var(--text)',
        }}
      >
        {year}
      </Box>
      <IconButton
        size="small"
        aria-label={t('calendar.nextYear')}
        disabled={year >= calendarYears.last}
        onClick={() => {
          setYear(year + 1);
        }}
        sx={iconButtonSx}
      >
        <ChevronRightOutlined sx={{ fontSize: 'var(--icon-md)' }} />
      </IconButton>
    </Box>
  );

  const markAllButton = (
    <Button
      variant="text"
      size="small"
      startIcon={<DoneAllOutlined />}
      disabled={shown.length === 0}
      onClick={markAll}
    >
      {t(allConsidered ? 'calendar.unmarkAll' : 'calendar.markAll')}
    </Button>
  );

  const newHoliday = (block: boolean) => (
    <Button
      variant="contained"
      startIcon={<AddOutlined />}
      fullWidth={block}
      onClick={() => {
        setEditing({});
      }}
      sx={block ? { flex: 'none' } : undefined}
    >
      {t('calendar.newHoliday')}
    </Button>
  );

  const workingDaysCard = (
    <Box
      component="section"
      aria-labelledby="calendar-working-days"
      sx={{
        ...cardSx,
        flex: 'none',
        display: 'flex',
        flexDirection: desktop ? 'row' : 'column',
        alignItems: desktop ? 'center' : 'stretch',
        gap: desktop ? 'var(--space-8)' : 'var(--space-3)',
        padding: desktop ? '14px var(--space-4)' : 'var(--space-3) var(--space-4)',
      }}
    >
      <Box component="h2" id="calendar-working-days" sx={cardTitleSx}>
        {t('calendar.workingDays')}
      </Box>
      <Box
        role="group"
        aria-labelledby="calendar-working-days"
        sx={
          desktop
            ? { display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 'var(--space-6)' }
            : {
                display: 'grid',
                gridTemplateColumns: 'repeat(4, minmax(0, 1fr))',
                gap: 'var(--space-3) var(--space-2)',
              }
        }
      >
        {weekdays.map((day) => {
          const working = workingDays.includes(day);
          return (
            <Checkbox
              key={day}
              label={t(`calendar.weekdays.${day}`)}
              checked={working}
              // The week keeps at least one working day (ADR-0018).
              disabled={data === undefined || (working && workingDays.length === 1)}
              onChange={(checked) => {
                toggleWorkingDay(day, checked);
              }}
            />
          );
        })}
      </Box>
      {workingDays.length === 1 && (
        <Box
          component="span"
          sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
        >
          {t('calendar.lastWorkingDay')}
        </Box>
      )}
    </Box>
  );

  const kindOptions = [
    {
      value: 'all' as const,
      label: t(desktop ? 'calendar.filters.all' : 'calendar.filters.allKinds'),
    },
    ...calendarDayKinds.map((value) => ({ value, label: t(`calendar.filters.${value}`) })),
  ];

  return (
    <Page actions={newHoliday(false)}>
      {isError && data === undefined && <Alert tone="danger">{t('calendar.loadFailed')}</Alert>}
      {recalculation !== undefined && recalculation.items > 0 && (
        <Alert
          tone="warning"
          title={t('calendar.recalculation.title')}
          actions={
            <Button
              variant="outlined"
              size="small"
              onClick={() => {
                setRecalculating(true);
              }}
            >
              {t('calendar.recalculation.action')}
            </Button>
          }
        >
          {t('calendar.recalculation.text', {
            count: recalculation.items,
            projects: t('calendar.recalculation.projects', {
              count: recalculation.projects.length,
            }),
          })}
        </Alert>
      )}
      {workingDaysCard}
      {desktop ? (
        <DataTable
          label={t('calendar.table', { year })}
          rows={shown}
          columns={columns}
          rowCount={shown.length}
          page={1}
          pageSize={pageSize}
          onPageChange={noPageChange}
          loading={isFetching}
          isRowMuted={isDisregarded}
          emptyLabel={t('calendar.empty', { year })}
          displayedRowsLabel={footer}
          getRowId={calendarDayId}
          tools={
            <>
              {yearStepper}
              <SegmentedControl<KindFilter>
                label={t('calendar.kind')}
                value={kind}
                options={kindOptions}
                onChange={setKind}
              />
              <Box sx={{ flex: 1 }} />
              {markAllButton}
            </>
          }
        />
      ) : (
        <>
          <Box sx={{ flex: 'none', display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
            {yearStepper}
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Select<KindFilter>
                label={t('calendar.kind')}
                size="sm"
                value={kind}
                options={kindOptions}
                onChange={setKind}
              />
            </Box>
          </Box>
          <Box
            component="section"
            aria-labelledby="calendar-list"
            sx={{
              ...cardSx,
              flex: 1,
              minHeight: 0,
              display: 'flex',
              flexDirection: 'column',
              overflow: 'hidden',
            }}
          >
            <Box
              sx={{
                flex: 'none',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: 'var(--space-3)',
                minHeight: 48,
                padding: 'var(--space-2) var(--space-2) var(--space-2) var(--space-4)',
                borderBottom: '1px solid var(--border)',
              }}
            >
              <Box component="h2" id="calendar-list" sx={cardTitleSx}>
                {t('calendar.list')}
                <Box
                  component="span"
                  sx={{
                    marginLeft: 'var(--space-2)',
                    fontWeight: 400,
                    color: 'var(--text-secondary)',
                    fontVariantNumeric: 'tabular-nums',
                  }}
                >
                  {shown.length}
                </Box>
              </Box>
              {markAllButton}
            </Box>
            <Box
              component="ul"
              aria-label={t('calendar.table', { year })}
              sx={{
                flex: 1,
                minHeight: 0,
                overflow: 'auto',
                margin: 0,
                padding: 0,
                listStyle: 'none',
              }}
            >
              {shown.length === 0 && data !== undefined && (
                <Box
                  component="li"
                  sx={{ padding: 'var(--space-4)', color: 'var(--text-secondary)' }}
                >
                  {t('calendar.empty', { year })}
                </Box>
              )}
              {shown.map((day) => (
                <Box
                  component="li"
                  key={calendarDayId(day)}
                  sx={{
                    display: 'grid',
                    gridTemplateColumns: 'minmax(0, 1fr) 24px 32px',
                    alignItems: 'center',
                    gap: 'var(--space-2)',
                    padding: '10px var(--space-3)',
                    borderBottom: '1px solid var(--border)',
                    '&:last-of-type': { borderBottom: 0 },
                  }}
                >
                  <Box sx={{ minWidth: 0 }}>
                    <Box
                      component="span"
                      sx={{
                        display: 'block',
                        fontWeight: 500,
                        color: day.isConsidered ? 'var(--text)' : 'var(--text-secondary)',
                      }}
                    >
                      {holidayName(day, t)}
                    </Box>
                    <Box
                      component="span"
                      sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
                    >
                      {`${formatDayValue(day.date).slice(0, 5)}, ${formatWeekday(day.date)} · ${kindOf(day)}`}
                    </Box>
                  </Box>
                  <Checkbox
                    hideLabel
                    label={t('calendar.consider', { name: holidayName(day, t) })}
                    checked={day.isConsidered}
                    onChange={(checked) => {
                      toggle(day, checked);
                    }}
                  />
                  <Box component="span">
                    {day.kind === 'LocalHoliday' && (
                      <IconButton
                        size="small"
                        aria-label={t('calendar.menu.button', { name: holidayName(day, t) })}
                        aria-haspopup="menu"
                        onClick={(event) => {
                          openMenu(event, day);
                        }}
                        sx={iconButtonSx}
                      >
                        <MoreVertOutlined sx={{ fontSize: 'var(--icon-md)' }} />
                      </IconButton>
                    )}
                  </Box>
                </Box>
              ))}
            </Box>
          </Box>
          {newHoliday(true)}
        </>
      )}
      <Menu
        anchorEl={menu?.anchor}
        open={menu !== undefined}
        onClose={() => {
          setMenu(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { width: 200, marginTop: 'var(--space-1)' } },
          list: {
            'aria-label':
              menu === undefined
                ? undefined
                : t('calendar.menu.button', { name: holidayName(menu.day, t) }),
          },
        }}
      >
        <MenuItem
          onClick={() => {
            setEditing({ holiday: menu?.day });
            setMenu(undefined);
          }}
        >
          <EditOutlined aria-hidden />
          {t('calendar.menu.edit')}
        </MenuItem>
        <Divider sx={{ margin: 'var(--space-1) 0' }} />
        <MenuItem
          onClick={() => {
            setRemoving(menu?.day);
            setMenu(undefined);
          }}
          sx={{
            color: 'var(--status-danger-text)',
            '& > .MuiSvgIcon-root': { color: 'var(--status-danger-text)' },
          }}
        >
          <DeleteOutlined aria-hidden />
          {t('calendar.menu.remove')}
        </MenuItem>
      </Menu>
      {editing !== undefined && (
        <HolidayDialog
          holiday={editing.holiday}
          onClose={() => {
            setEditing(undefined);
          }}
          onSaved={(saved, savedYear) => {
            setEditing(undefined);
            setMessage(saved);
            setYear(savedYear);
          }}
        />
      )}
      <Dialog
        open={removing !== undefined}
        alert
        title={t('calendar.remove.title')}
        onClose={() => {
          setRemoving(undefined);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setRemoving(undefined);
              }}
            >
              {t('calendar.cancel')}
            </Button>
            <Button
              variant="contained"
              color="error"
              loading={remove.isPending}
              onClick={() => {
                if (removing !== undefined) {
                  remove.mutate(removing);
                }
              }}
            >
              {t('calendar.remove.confirm')}
            </Button>
          </>
        }
      >
        {removing !== undefined &&
          t('calendar.remove.text', {
            name: holidayName(removing, t),
            date: formatDayValue(removing.date),
          })}
      </Dialog>
      <Dialog
        open={recalculating && recalculation !== undefined}
        title={t('calendar.recalculation.dialog.title')}
        onClose={() => {
          setRecalculating(false);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setRecalculating(false);
              }}
            >
              {t('calendar.cancel')}
            </Button>
            <Button
              variant="contained"
              loading={recalculate.isPending}
              onClick={() => {
                recalculate.mutate();
              }}
            >
              {t('calendar.recalculation.dialog.confirm')}
            </Button>
          </>
        }
      >
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
          <Box component="p" sx={{ margin: 0 }}>
            {t('calendar.recalculation.dialog.text')}
          </Box>
          <Box
            component="ul"
            aria-label={t('calendar.recalculation.dialog.projects')}
            sx={{
              maxHeight: 240,
              overflow: 'auto',
              margin: 0,
              padding: 0,
              listStyle: 'none',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-md)',
            }}
          >
            {recalculation?.projects.map((project) => (
              <Box
                component="li"
                key={project.id}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 'var(--space-2)',
                  padding: 'var(--space-2) var(--space-3)',
                  borderBottom: '1px solid var(--border)',
                  '&:last-of-type': { borderBottom: 0 },
                }}
              >
                <CodeChip code={project.code} />
                <Box
                  component="span"
                  sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}
                >
                  <Box
                    component="span"
                    sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
                  >
                    {project.name}
                  </Box>
                  <Box component="span" sx={detailLineSx}>
                    {t('calendar.recalculation.dialog.items', { count: project.items })}
                  </Box>
                </Box>
              </Box>
            ))}
          </Box>
          <Box component="p" sx={{ margin: 0, color: 'var(--text-secondary)' }}>
            {t('calendar.recalculation.dialog.notice')}
          </Box>
        </Box>
      </Dialog>
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
