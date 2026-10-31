import AddOutlined from '@mui/icons-material/AddOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import Box from '@mui/material/Box';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useNavigate } from 'react-router';
import type { ProjectRow, ProjectSituation } from '../api/client';
import { prefetchProject } from '../api/prefetch';
import { clientChoicesQuery, projectsQuery } from '../api/queries';
import { CardAvatar } from '../components/PersonCard';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { ScheduleStrip } from '../components/ScheduleStrip';
import { SegmentedControl } from '../components/SegmentedControl';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { formatDayText } from '../format/dates';
import { useDebounced } from '../hooks/useDebounced';
import { useSession } from '../session/SessionContext';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { ProjectStatusBadge } from './ProjectStatusBadge';
import { summaryText } from './schedule';
import { LateCounts } from './ScheduleStatus';
import { Button } from '../components/Button';

const pageSize = 25;
const searchDelay = 300;
const allClients = 'all';
const noProjects = { items: [], total: 0, today: '' };

const secondarySx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;

/**
 * Projetos (canvas Projetos): the projects the person sees, in alphabetical order, with the search by code or name,
 * the filter of client and the situation, the active ones by default (ADR-0016). Each project shows the strip of
 * phases with its progress and current phase, and the final deadline with the late tasks (ADR-0071); "Sem cronograma"
 * without tasks and milestones. Only the Administrator and the Project manager create projects. It shows the
 * confirmation the page before sent, as the deletion of a project (ADR-0041).
 */
export function ProjectsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { session } = useSession();
  const state: unknown = location.state;
  const [message, setMessage] = useState<string | undefined>(
    typeof state === 'object' &&
      state !== null &&
      'message' in state &&
      typeof state.message === 'string'
      ? state.message
      : undefined,
  );
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [search, setSearch] = useState('');
  const [client, setClient] = useState(allClients);
  const [situation, setSituation] = useState<ProjectSituation>('Active');
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebounced(search, searchDelay).trim();
  const role = session.status === 'signed-in' ? session.user.role : undefined;
  const createsProjects = role === 'Administrator' || role === 'ProjectManager';

  const { data, isFetching } = useQuery({
    ...projectsQuery({
      search: debouncedSearch === '' ? undefined : debouncedSearch,
      clientId: client === allClients ? undefined : client,
      situation,
      page,
      pageSize,
    }),
    placeholderData: keepPreviousData,
  });
  const projects = data ?? noProjects;
  const { today } = projects;
  const { data: clients } = useQuery(clientChoicesQuery());

  const columns = useMemo<DataTableColumn<ProjectRow>[]>(
    () => [
      {
        field: 'name',
        headerName: t('projects.columns.project'),
        flex: 1.8,
        minWidth: 250,
        renderCell: ({ row }) => (
          <Box sx={{ minWidth: 0 }}>
            <Box
              component={Link}
              to={`/projects/${row.id}`}
              sx={{
                display: 'block',
                fontWeight: 500,
                color: 'var(--text)',
                textDecoration: 'none',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
                '&:hover': { textDecoration: 'underline' },
              }}
            >
              {row.name}
            </Box>
            <Box
              component="span"
              sx={{
                ...secondarySx,
                display: 'block',
                marginTop: '2px',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              <Box
                component="span"
                sx={{ fontFamily: 'var(--font-mono)', fontSize: 12, fontWeight: 500 }}
              >
                {row.code}
              </Box>
              {` · ${row.client?.name ?? t('projects.internal')}`}
            </Box>
          </Box>
        ),
      },
      {
        field: 'status',
        headerName: t('projects.columns.status'),
        width: 148,
        renderCell: ({ row }) => <ProjectStatusBadge status={row.status} />,
      },
      {
        field: 'schedule',
        headerName: t('projects.columns.schedule'),
        flex: 1.4,
        minWidth: 220,
        renderCell: ({ row }) => {
          const schedule = row.schedule;
          if (schedule === null) {
            return (
              <Box component="span" sx={secondarySx}>
                {t('projects.noSchedule')}
              </Box>
            );
          }
          const text = summaryText(schedule, today, t);
          return (
            <Box
              sx={{
                display: 'flex',
                flexDirection: 'column',
                gap: 'var(--space-1)',
                width: '100%',
              }}
            >
              <ScheduleStrip
                start={schedule.start}
                end={schedule.end}
                phases={schedule.phases}
                milestones={schedule.milestones}
                today={today}
                label={t('projects.schedule.strip', {
                  start: formatDayText(schedule.start),
                  end: formatDayText(schedule.end),
                })}
              />
              <Box
                component="span"
                sx={{
                  ...secondarySx,
                  fontSize: 12,
                  lineHeight: '16px',
                  fontVariantNumeric: 'tabular-nums',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {text}
              </Box>
            </Box>
          );
        },
      },
      {
        field: 'deadline',
        headerName: t('projects.columns.deadline'),
        width: 168,
        renderCell: ({ row }) =>
          row.schedule === null ? (
            <Box component="span" sx={secondarySx}>
              {t('projects.noDate')}
            </Box>
          ) : (
            <Box sx={{ minWidth: 0 }}>
              <Box component="span" sx={{ display: 'block', fontVariantNumeric: 'tabular-nums' }}>
                {formatDayText(row.schedule.end)}
              </Box>
              <LateCounts
                tasks={row.schedule.lateTasks}
                milestones={row.schedule.lateMilestones}
                stacked
              />
            </Box>
          ),
      },
      {
        field: 'manager',
        headerName: t('projects.columns.manager'),
        width: 190,
        renderCell: ({ row }) => {
          const manager = row.managers[0];
          return manager === undefined ? null : (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
              <CardAvatar person={manager} role={t(`projects.roles.${manager.role}`)} size="sm" />
              <Box
                component="span"
                sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
              >
                {manager.name}
              </Box>
            </Box>
          );
        },
      },
    ],
    [t, today],
  );

  const choose = useCallback(
    (row: ProjectRow) => {
      void navigate(`/projects/${row.id}`);
    },
    [navigate],
  );
  // The project starts loading when the mouse comes over its row (ADR-0106).
  const queryClient = useQueryClient();
  const hover = useCallback(
    (row: ProjectRow) => {
      prefetchProject(queryClient, row.id);
    },
    [queryClient],
  );

  // "8 projetos"; with more than a page, the projects shown come first: "1–25 de 30 projetos".
  const footer = useCallback(
    ({ from, to, count }: { from: number; to: number; count: number }) => {
      const total = t('projects.total', { count });
      return count > pageSize ? t('projects.range', { from, to, total }) : total;
    },
    [t],
  );

  const newProject = (block: boolean) =>
    createsProjects ? (
      <Button
        variant="contained"
        startIcon={<AddOutlined />}
        fullWidth={block}
        onClick={() => {
          void navigate('/projects/new');
        }}
        sx={block ? { flex: 'none' } : undefined}
      >
        {t('projects.newProject')}
      </Button>
    ) : undefined;

  const clientOptions = useMemo(
    () => [
      { value: allClients, label: t('projects.allClients') },
      ...(clients ?? []).map((choice) => ({ value: choice.id, label: choice.name })),
    ],
    [clients, t],
  );

  return (
    <Page actions={newProject(false)}>
      <DataTable
        label={t('projects.table')}
        rows={projects.items}
        columns={columns}
        rowCount={projects.total}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        loading={isFetching}
        rowHeight={64}
        emptyLabel={t('projects.empty')}
        displayedRowsLabel={footer}
        onRowChoose={choose}
        onRowHover={hover}
        tools={
          <>
            <Box sx={desktop ? { flex: 'none' } : { flex: '1 1 100%', minWidth: 0 }}>
              <TextField
                label={t('projects.search')}
                hideLabel
                placeholder={t('projects.search')}
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
            <Select
              label={t('projects.clientFilter')}
              size="sm"
              width={200}
              value={client}
              options={clientOptions}
              onChange={(value) => {
                setClient(value);
                setPage(1);
              }}
            />
            {desktop && <Box sx={{ flex: 1 }} />}
            <SegmentedControl<ProjectSituation>
              label={t('projects.situation')}
              value={situation}
              options={[
                { value: 'Active', label: t('projects.situations.Active') },
                { value: 'Closed', label: t('projects.situations.Closed') },
                { value: 'All', label: t('projects.situations.All') },
              ]}
              onChange={(value) => {
                setSituation(value);
                setPage(1);
              }}
            />
          </>
        }
      />
      {!desktop && newProject(true)}
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
