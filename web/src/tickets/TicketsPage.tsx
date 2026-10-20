import AccountTreeOutlined from '@mui/icons-material/AccountTreeOutlined';
import AddOutlined from '@mui/icons-material/AddOutlined';
import BusinessOutlined from '@mui/icons-material/BusinessOutlined';
import ConfirmationNumberOutlined from '@mui/icons-material/ConfirmationNumberOutlined';
import InboxOutlined from '@mui/icons-material/InboxOutlined';
import PersonOutlineOutlined from '@mui/icons-material/PersonOutlineOutlined';
import ReplyOutlined from '@mui/icons-material/ReplyOutlined';
import SearchOutlined from '@mui/icons-material/SearchOutlined';
import VisibilityOutlined from '@mui/icons-material/VisibilityOutlined';
import type SvgIcon from '@mui/material/SvgIcon';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import useMediaQuery from '@mui/material/useMediaQuery';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useSearchParams } from 'react-router';
import {
  type TicketCounts,
  type TicketSituation,
  type TicketTeam,
  type TicketView,
  ticketTeams,
} from '../api/client';
import { ticketChoicesQuery, ticketCountsQuery, ticketsQuery } from '../api/queries';
import { Select } from '../components/Select';
import { TextField } from '../components/TextField';
import { useDebounced } from '../hooks/useDebounced';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { TicketTable, ticketPageSize } from './TicketTable';

type PageView = Exclude<TicketView, 'Unassigned'>;

const views: readonly {
  view: PageView;
  icon: typeof SvgIcon;
  count: keyof TicketCounts;
  alert?: boolean;
}[] = [
  { view: 'AssignedToMe', icon: PersonOutlineOutlined, count: 'assignedToMe' },
  { view: 'UnassignedInMyTeams', icon: InboxOutlined, count: 'unassignedInMyTeams', alert: true },
  { view: 'OpenedByMe', icon: ReplyOutlined, count: 'openedByMe' },
  { view: 'Following', icon: VisibilityOutlined, count: 'following' },
  { view: 'WithVendor', icon: BusinessOutlined, count: 'withVendor' },
  { view: 'All', icon: ConfirmationNumberOutlined, count: 'open' },
];

const isPageView = (value: string | null): value is PageView =>
  views.some((item) => item.view === value);

const everything = 'all';
const searchDelay = 300;
const noTickets = { items: [], total: 0 };

/**
 * Chamados (canvas Chamados): the views on the left, with how many open tickets each has ("Sem responsável nos meus
 * times" stands out while it has any), and the list with the search and the filters of project, team and situation
 * (ADR-0029, ADR-0068). Everyone in a project sees its tickets (ADR-0044). "Abrir chamado" shows only to who may open a
 * ticket in some project; on the phone the views are a field above the list.
 */
export function TicketsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const [params, setParams] = useSearchParams();
  const requested = params.get('view');
  const view: PageView = isPageView(requested) ? requested : 'All';
  const [search, setSearch] = useState('');
  const [project, setProject] = useState(everything);
  const [team, setTeam] = useState<TicketTeam | typeof everything>(everything);
  const [situation, setSituation] = useState<TicketSituation>('Open');
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebounced(search, searchDelay).trim();

  const { data, isFetching } = useQuery({
    ...ticketsQuery({
      view,
      search: debouncedSearch === '' ? undefined : debouncedSearch,
      projectId: project === everything ? undefined : project,
      team: team === everything ? undefined : team,
      situation,
      page,
      pageSize: ticketPageSize,
    }),
    placeholderData: keepPreviousData,
  });
  const tickets = data ?? noTickets;
  const { data: counts } = useQuery(ticketCountsQuery());
  const { data: choices } = useQuery(ticketChoicesQuery());
  const opens = (choices ?? []).some((choice) => choice.canOpen);
  const filtered = debouncedSearch !== '' || project !== everything || team !== everything;

  const choose = (next: PageView) => {
    setParams(next === 'All' ? {} : { view: next });
    setPage(1);
  };

  const projectOptions = useMemo(
    () => [
      { value: everything, label: t('tickets.allProjects') },
      ...(choices ?? []).map((choice) => ({
        value: choice.id,
        label: `${choice.code} · ${choice.name}`,
      })),
    ],
    [choices, t],
  );
  const teamOptions = useMemo(
    (): { value: TicketTeam | typeof everything; label: string }[] => [
      { value: everything, label: t('tickets.allTeams') },
      ...ticketTeams.map((value) => ({ value, label: t(`tickets.teams.${value}`) })),
    ],
    [t],
  );
  const situationOptions = useMemo(
    () =>
      (['Open', 'Finished', 'All'] as const).map((value) => ({
        value,
        label: t(`tickets.situations.${value}`),
      })),
    [t],
  );

  const openTicket = (block: boolean) =>
    opens ? (
      <Button
        variant="contained"
        startIcon={<AddOutlined />}
        fullWidth={block}
        onClick={() => {
          void navigate('/tickets/new');
        }}
        sx={block ? { flex: 'none' } : undefined}
      >
        {t('tickets.open')}
      </Button>
    ) : undefined;

  // How a ticket moves, from Aberto to Fechado (ADR-0024).
  const flowLink = (block: boolean) => (
    <Button
      variant="outlined"
      component={Link}
      to="/tickets/flow"
      startIcon={<AccountTreeOutlined />}
      fullWidth={block}
      sx={block ? { flex: 'none' } : undefined}
    >
      {t('tickets.flow.link')}
    </Button>
  );

  const list = (
    <Page
      actions={
        <>
          {flowLink(false)}
          {openTicket(false)}
        </>
      }
    >
      <TicketTable
        rows={tickets.items}
        total={tickets.total}
        page={page}
        onPageChange={setPage}
        loading={isFetching}
        emptyLabel={t(filtered ? 'tickets.emptyFiltered' : 'tickets.empty')}
        tools={
          <>
            {!desktop && (
              <Select<PageView>
                label={t('tickets.viewLabel')}
                size="sm"
                value={view}
                options={views.map((item) => ({
                  value: item.view,
                  label: t(`tickets.views.${item.view}`),
                }))}
                onChange={choose}
              />
            )}
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
            <Select
              label={t('tickets.projectFilter')}
              size="sm"
              width={200}
              value={project}
              options={projectOptions}
              onChange={(value) => {
                setProject(value);
                setPage(1);
              }}
            />
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
            <Select<TicketSituation>
              label={t('tickets.situationFilter')}
              size="sm"
              width={144}
              value={situation}
              options={situationOptions}
              onChange={(value) => {
                setSituation(value);
                setPage(1);
              }}
            />
          </>
        }
      />
      {!desktop && openTicket(true)}
      {!desktop && flowLink(true)}
    </Page>
  );

  if (!desktop) {
    return list;
  }

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'grid',
        gridTemplateColumns: '240px minmax(0, 1fr)',
        overflow: 'hidden',
      }}
    >
      <Box
        component="nav"
        aria-label={t('tickets.views.label')}
        sx={{
          minHeight: 0,
          boxSizing: 'border-box',
          padding: 'var(--space-5) var(--space-3)',
          backgroundColor: 'var(--bg-subtle)',
          borderRight: '1px solid var(--border)',
          overflow: 'auto',
        }}
      >
        <Box
          component="ul"
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: '2px',
            margin: 0,
            padding: 0,
            listStyle: 'none',
          }}
        >
          <Box
            component="li"
            aria-hidden="true"
            sx={{
              padding: '0 var(--space-3) var(--space-1)',
              fontSize: 12,
              lineHeight: '16px',
              fontWeight: 500,
              color: 'var(--text-secondary)',
            }}
          >
            {t('tickets.views.label')}
          </Box>
          {views.map(({ view: item, icon: Icon, count, alert = false }) => {
            const value = counts?.[count];
            return (
              <li key={item}>
                <Box
                  component="button"
                  type="button"
                  aria-current={item === view ? 'page' : undefined}
                  onClick={() => {
                    choose(item);
                  }}
                  sx={{
                    display: 'flex',
                    width: '100%',
                    boxSizing: 'border-box',
                    alignItems: 'flex-start',
                    gap: 'var(--space-2)',
                    minHeight: 36,
                    padding: 'var(--space-2) var(--space-3)',
                    border: 0,
                    borderRadius: 'var(--radius-md)',
                    backgroundColor: 'transparent',
                    color: 'var(--text)',
                    fontFamily: 'var(--font-sans)',
                    fontSize: 14,
                    lineHeight: '20px',
                    textAlign: 'left',
                    cursor: 'pointer',
                    '& > svg': { fontSize: 18, marginTop: '1px', color: 'var(--text-secondary)' },
                    '&:hover': { backgroundColor: 'var(--bg-hover)' },
                    '&:focus-visible': {
                      outline: '2px solid var(--focus-ring)',
                      outlineOffset: -2,
                    },
                    '&[aria-current="page"]': {
                      backgroundColor: 'var(--bg-surface)',
                      boxShadow: '0 0 0 1px var(--border)',
                      fontWeight: 500,
                      '& > svg': { color: 'var(--action-text)' },
                    },
                  }}
                >
                  <Icon />
                  <Box component="span" sx={{ flex: 1, minWidth: 0 }}>
                    {t(`tickets.views.${item}`)}
                  </Box>
                  {value !== undefined && (
                    <Box
                      component="span"
                      data-alert={alert && value > 0}
                      sx={{
                        flex: 'none',
                        minWidth: 20,
                        boxSizing: 'border-box',
                        padding: '0 6px',
                        borderRadius: 'var(--radius-full)',
                        fontSize: 12,
                        lineHeight: '20px',
                        fontWeight: 400,
                        textAlign: 'center',
                        color: 'var(--text-secondary)',
                        fontVariantNumeric: 'tabular-nums',
                        '&[data-alert="true"]': {
                          backgroundColor: 'var(--status-danger-bg)',
                          color: 'var(--status-danger-text)',
                          fontWeight: 500,
                        },
                      }}
                    >
                      {value}
                    </Box>
                  )}
                </Box>
              </li>
            );
          })}
        </Box>
      </Box>
      <Box sx={{ minWidth: 0, minHeight: 0, display: 'flex', flexDirection: 'column' }}>{list}</Box>
    </Box>
  );
}
