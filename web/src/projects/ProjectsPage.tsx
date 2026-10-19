import AddOutlined from '@mui/icons-material/AddOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router';
import type { ProjectRow, ProjectSituation } from '../api/client';
import { clientChoicesQuery, projectsQuery } from '../api/queries';
import { Avatar } from '../components/Avatar';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { SegmentedControl } from '../components/SegmentedControl';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { useDebounced } from '../hooks/useDebounced';
import { useSession } from '../session/SessionContext';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { ProjectStatusBadge } from './ProjectStatusBadge';

const pageSize = 25;
const searchDelay = 300;
const allClients = 'all';
const noProjects = { items: [], total: 0 };

const secondarySx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;

/**
 * Projetos (canvas Projetos): the projects the person sees, in alphabetical order, with the search by code or name,
 * the filter of client and the situation, the active ones by default (ADR-0016). The schedule columns say "Sem
 * cronograma" until the schedule exists (Cronograma-1). Only the Administrator and the Project manager create projects.
 */
export function ProjectsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { session } = useSession();
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
        flex: 1.2,
        minWidth: 180,
        renderCell: () => (
          <Box component="span" sx={secondarySx}>
            {t('projects.noSchedule')}
          </Box>
        ),
      },
      {
        field: 'deadline',
        headerName: t('projects.columns.deadline'),
        width: 140,
        renderCell: () => (
          <Box component="span" sx={secondarySx}>
            {t('projects.noDate')}
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
              <Avatar name={manager.name} size="sm" />
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
    [t],
  );

  const choose = useCallback(
    (row: ProjectRow) => {
      void navigate(`/projects/${row.id}`);
    },
    [navigate],
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
    </Page>
  );
}
