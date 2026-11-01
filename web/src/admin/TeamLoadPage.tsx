import ChevronLeftOutlined from '@mui/icons-material/ChevronLeftOutlined';
import ChevronRightOutlined from '@mui/icons-material/ChevronRightOutlined';
import EventBusyOutlined from '@mui/icons-material/EventBusyOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import Box from '@mui/material/Box';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import type { PersonLoad, PersonWeek, TeamLoadWeek } from '../api/client';
import { teamLoadQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Button, IconButton } from '../components/Button';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { SegmentedControl } from '../components/SegmentedControl';
import { TextField } from '../components/TextField';
import { formatHours } from '../hours/hours';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { TeamLoadDrawer } from './TeamLoadDrawer';
import {
  loadLook,
  loadText,
  shownPeople,
  teamLoadMeta,
  type TeamLoadView,
  weekAfter,
  weekText,
} from './teamLoad';

/** A page of the list; an installation has a few dozen people in its projects. */
const pageSize = 50;
const noPeople: PersonLoad[] = [];
const noWeeks: TeamLoadWeek[] = [];

const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;
const toneColor = {
  neutral: 'var(--status-neutral-text)',
  info: 'var(--status-info-text)',
  warning: 'var(--status-warning-text)',
  success: 'var(--status-success-text)',
  danger: 'var(--status-danger-text)',
} as const;

const getRowId = (row: PersonLoad) => row.id;

/**
 * A week of a person in the table: the load against the capacity and, under it, the state by icon, color and text,
 * with the mark of the days away (ADR-0065, ADR-0142).
 */
function TeamLoadCell({ week }: { week: PersonWeek }) {
  const { t } = useTranslation();
  const { tone, icon: Icon } = loadLook[week.state];
  const away = t('teamLoad.absenceDays', { count: week.absenceDays });
  return (
    <Box sx={{ minWidth: 0, display: 'flex', flexDirection: 'column', gap: '2px' }}>
      <Box
        component="span"
        sx={{
          ...ellipsisSx,
          fontVariantNumeric: 'tabular-nums',
          color: week.state === 'Free' ? 'var(--text-secondary)' : 'var(--text)',
        }}
      >
        {loadText(week, t)}
      </Box>
      <Box
        component="span"
        data-state={week.state}
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-1)',
          fontSize: 12,
          lineHeight: '16px',
          fontWeight: 500,
          color: toneColor[tone],
          whiteSpace: 'nowrap',
        }}
      >
        <Icon aria-hidden sx={{ fontSize: 14 }} />
        {t(`teamLoad.states.${week.state}`)}
        {week.absenceDays > 0 && (
          <Box
            component="span"
            title={away}
            sx={{ display: 'inline-flex', color: 'var(--text-secondary)' }}
          >
            <EventBusyOutlined
              role="img"
              aria-hidden={false}
              aria-label={away}
              sx={{ fontSize: 14 }}
            />
          </Box>
        )}
      </Box>
    </Box>
  );
}

/**
 * Carga da equipe, in the section Acompanhamento of the Administration (ADR-0142): eight weeks from the current one,
 * with the earlier and the later ones and "Esta semana", how many people and how many over the capacity, the search
 * by name, the views Todas and Com sobrecarga, and each person with the load against the capacity of each week. A
 * person chosen opens the tasks and the absences of each week in the drawer, by the ?person= of the address, without
 * entering the history (ADR-0105).
 */
export function TeamLoadPage() {
  const { t } = useTranslation();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [params, setParams] = useSearchParams();
  const [week, setWeek] = useState<string | null>(null);
  const [view, setView] = useState<TeamLoadView>('All');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const { data, isFetching, error } = useQuery({
    ...teamLoadQuery(week),
    placeholderData: keepPreviousData,
  });
  const people = data?.people ?? noPeople;
  const weeks = data?.weeks ?? noWeeks;
  const found = useMemo(() => shownPeople(people, view, search), [people, view, search]);
  const shown = useMemo(() => found.slice((page - 1) * pageSize, page * pageSize), [found, page]);
  const chosen = params.get('person') ?? undefined;
  const first = weeks[0]?.start;

  const choose = useCallback(
    (row: PersonLoad) => {
      setParams(
        (current) => {
          const next = new URLSearchParams(current);
          next.set('person', row.id);
          return next;
        },
        { replace: true },
      );
    },
    [setParams],
  );

  const close = useCallback(() => {
    setParams(
      (current) => {
        const next = new URLSearchParams(current);
        next.delete('person');
        return next;
      },
      { replace: true },
    );
  }, [setParams]);

  const columns = useMemo<DataTableColumn<PersonLoad>[]>(
    () => [
      {
        field: 'name',
        headerName: t('teamLoad.columns.person'),
        flex: 1,
        minWidth: 180,
        renderCell: ({ row }) => (
          <Box sx={{ minWidth: 0 }}>
            <Box component="span" sx={{ ...ellipsisSx, display: 'block', fontWeight: 500 }}>
              {row.name}
            </Box>
            <Box
              component="span"
              sx={{
                ...ellipsisSx,
                display: 'block',
                fontSize: 12,
                lineHeight: '16px',
                color: 'var(--text-secondary)',
              }}
            >
              {row.email}
            </Box>
          </Box>
        ),
      },
      ...weeks.map((one, index): DataTableColumn<PersonLoad> => ({
        field: `week${String(index)}`,
        headerName: weekText(one),
        width: 132,
        sortable: false,
        renderCell: ({ row }) => {
          const personWeek = row.weeks[index];
          return personWeek === undefined ? null : <TeamLoadCell week={personWeek} />;
        },
      })),
    ],
    [t, weeks],
  );

  const moveTo = (start: string | null) => {
    setWeek(start);
    setPage(1);
  };

  return (
    <Page
      subtitle={data === undefined ? undefined : teamLoadMeta(people, t)}
      actions={
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
          <IconButton
            aria-label={t('teamLoad.previous')}
            disabled={first === undefined}
            onClick={() => {
              if (first !== undefined) {
                moveTo(weekAfter(first, -1));
              }
            }}
          >
            <ChevronLeftOutlined />
          </IconButton>
          <Button
            variant="outlined"
            disabled={week === null}
            onClick={() => {
              moveTo(null);
            }}
          >
            {t('teamLoad.thisWeek')}
          </Button>
          <IconButton
            aria-label={t('teamLoad.next')}
            disabled={first === undefined}
            onClick={() => {
              if (first !== undefined) {
                moveTo(weekAfter(first, 1));
              }
            }}
          >
            <ChevronRightOutlined />
          </IconButton>
        </Box>
      }
    >
      {error === null ? (
        <Box
          sx={{
            flex: 1,
            minHeight: 0,
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-3)',
          }}
        >
          {data !== undefined && (
            <Box
              component="p"
              sx={{ margin: 0, fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
            >
              {t('teamLoad.rule', { hours: formatHours(data.hoursPerDay) })}
            </Box>
          )}
          <DataTable
            label={t('teamLoad.table')}
            rows={shown}
            columns={columns}
            rowCount={found.length}
            page={page}
            pageSize={pageSize}
            onPageChange={setPage}
            loading={isFetching}
            rowHeight={56}
            emptyLabel={t(view === 'Overloaded' ? 'teamLoad.emptyOverloaded' : 'teamLoad.empty')}
            selectedRowId={chosen}
            onRowChoose={choose}
            getRowId={getRowId}
            tools={
              <>
                <Box sx={desktop ? { flex: 'none' } : { flex: '1 1 100%', minWidth: 0 }}>
                  <TextField
                    label={t('teamLoad.search')}
                    hideLabel
                    placeholder={t('teamLoad.search')}
                    type="search"
                    size="sm"
                    icon={SearchOutlined}
                    width={desktop ? 280 : undefined}
                    value={search}
                    onChange={(event) => {
                      setSearch(event.target.value);
                      setPage(1);
                    }}
                  />
                </Box>
                <Box sx={{ flex: 1 }} />
                <SegmentedControl<TeamLoadView>
                  label={t('teamLoad.viewsLabel')}
                  value={view}
                  options={[
                    { value: 'All', label: t('teamLoad.views.All') },
                    { value: 'Overloaded', label: t('teamLoad.views.Overloaded') },
                  ]}
                  onChange={(value) => {
                    setView(value);
                    setPage(1);
                  }}
                />
              </>
            }
          />
        </Box>
      ) : (
        <Alert tone="danger">{t('teamLoad.loadFailed')}</Alert>
      )}
      <TeamLoadDrawer
        person={chosen === undefined ? undefined : people.find((one) => one.id === chosen)}
        open={chosen !== undefined}
        weeks={weeks}
        onClose={close}
      />
    </Page>
  );
}
