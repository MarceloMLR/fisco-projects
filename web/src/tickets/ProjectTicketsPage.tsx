import AddOutlined from '@mui/icons-material/AddOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import Box from '@mui/material/Box';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { type TicketTeam, ticketTeams } from '../api/client';
import {
  type TicketsParams,
  ticketChoicesQuery,
  ticketCountsQuery,
  ticketsQuery,
} from '../api/queries';
import { SegmentedControl } from '../components/SegmentedControl';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { useDebounced } from '../hooks/useDebounced';
import { useProjectOutlet } from '../projects/projectOutlet';
import { theme } from '../theme/theme';
import { TicketTable, ticketPageSize } from './TicketTable';
import { Button } from '../components/Button';

type Segment = 'all' | 'open' | 'unassigned' | 'openedByMe';

/** The list each segment asks for: all of them in any status, and the open ones of the other three. */
const segments: Record<Segment, Pick<TicketsParams, 'view' | 'situation'>> = {
  all: { view: 'All', situation: 'All' },
  open: { view: 'All', situation: 'Open' },
  unassigned: { view: 'Unassigned', situation: 'Open' },
  openedByMe: { view: 'OpenedByMe', situation: 'Open' },
};

const everything = 'all';
const searchDelay = 300;
const noTickets = { items: [], total: 0 };

/**
 * The tab Chamados of a project (canvas ProjetoChamados): "Todos", "Em aberto", "Sem responsável" and "Abertos por
 * mim", each with its count, the search and the team. Everyone in the project sees its tickets (ADR-0044); "Abrir
 * chamado" shows to who may open one here, with the project already filled in (ADR-0068).
 */
export function ProjectTicketsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { project } = useProjectOutlet();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [segment, setSegment] = useState<Segment>('all');
  const [search, setSearch] = useState('');
  const [team, setTeam] = useState<TicketTeam | typeof everything>(everything);
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebounced(search, searchDelay).trim();

  const { data, isFetching } = useQuery({
    ...ticketsQuery({
      ...segments[segment],
      projectId: project.id,
      search: debouncedSearch === '' ? undefined : debouncedSearch,
      team: team === everything ? undefined : team,
      page,
      pageSize: ticketPageSize,
    }),
    placeholderData: keepPreviousData,
  });
  const tickets = data ?? noTickets;
  const { data: counts } = useQuery(ticketCountsQuery(project.id));
  const { data: choices } = useQuery(ticketChoicesQuery());
  const opens = (choices ?? []).some((choice) => choice.id === project.id && choice.canOpen);
  const filtered = debouncedSearch !== '' || team !== everything;

  const segmentOptions = useMemo(
    () =>
      (['all', 'open', 'unassigned', 'openedByMe'] as const).map((value) => ({
        value,
        label: t(`tickets.segments.${value}`),
        count:
          counts === undefined
            ? undefined
            : {
                all: counts.all,
                open: counts.open,
                unassigned: counts.unassigned,
                openedByMe: counts.openedByMe,
              }[value],
      })),
    [counts, t],
  );
  const teamOptions = useMemo(
    (): { value: TicketTeam | typeof everything; label: string }[] => [
      { value: everything, label: t('tickets.allTeams') },
      ...ticketTeams.map((value) => ({ value, label: t(`tickets.teams.${value}`) })),
    ],
    [t],
  );

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-4)',
      }}
    >
      <Box
        sx={{
          flex: 'none',
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          gap: 'var(--space-2)',
        }}
      >
        <SegmentedControl<Segment>
          label={t('tickets.viewLabel')}
          value={segment}
          options={segmentOptions}
          onChange={(value) => {
            setSegment(value);
            setPage(1);
          }}
        />
        <Box sx={desktop ? { flex: 'none' } : { flex: '1 1 100%', minWidth: 0 }}>
          <TextField
            label={t('tickets.search')}
            hideLabel
            placeholder={t('tickets.search')}
            type="search"
            size="sm"
            icon={SearchOutlined}
            width={desktop ? 264 : undefined}
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />
        </Box>
        <Select<TicketTeam | typeof everything>
          label={t('tickets.teamFilter')}
          size="sm"
          width={168}
          value={team}
          options={teamOptions}
          onChange={(value) => {
            setTeam(value);
            setPage(1);
          }}
        />
        {opens && (
          <>
            {desktop && <Box sx={{ flex: 1 }} />}
            <Button
              variant="contained"
              startIcon={<AddOutlined />}
              fullWidth={!desktop}
              onClick={() => {
                void navigate(`/projects/${project.id}/tickets/new`);
              }}
            >
              {t('tickets.open')}
            </Button>
          </>
        )}
      </Box>
      <TicketTable
        rows={tickets.items}
        total={tickets.total}
        page={page}
        onPageChange={setPage}
        loading={isFetching}
        inProject
        emptyLabel={t(filtered ? 'tickets.emptyFiltered' : 'tickets.empty')}
      />
    </Box>
  );
}
