import AddOutlined from '@mui/icons-material/AddOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import ViewListOutlined from '@mui/icons-material/ViewListOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import { visuallyHidden } from '@mui/utils';
import { useQuery } from '@tanstack/react-query';
import { type ReactNode, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type AdditionalHours, api, type ProjectHoursDetail } from '../api/client';
import { projectHoursDetailQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { Dialog } from '../components/Dialog';
import { CardAvatar } from '../components/PersonCard';
import { ProgressBar } from '../components/ProgressBar';
import { formatDayText } from '../format/dates';
import { useDates } from '../format/useDates';
import { formatHours } from '../hours/hours';
import { memberToneOf } from '../projects/memberTone';
import { ProjectStatusBadge } from '../projects/ProjectStatusBadge';
import { ProjectEntriesDialog } from './ProjectEntriesDialog';
import { AdditionalHoursDialog, ContractedHoursDialog } from './ProjectHoursDialogs';
import { HoursBalanceBadge } from './HoursBalanceBadge';
import { balanceLook, consumptionDetail, hoursOrDash } from './projectHours';

const statLabelSx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;
const statValueSx = {
  fontSize: 24,
  lineHeight: '32px',
  fontWeight: 500,
  color: 'var(--text)',
  fontVariantNumeric: 'tabular-nums',
} as const;
const statDetailSx = { fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' } as const;
const subSx = {
  display: 'block',
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
} as const;

const cell = {
  boxSizing: 'border-box',
  height: 'var(--row-height)',
  padding: '10px var(--space-3)',
  borderBottom: '1px solid var(--border)',
  textAlign: 'left',
  verticalAlign: 'middle',
} as const;

/** A small table of the drawer (fp-table in a bordered box), without choosing rows. */
const tableSx = {
  width: '100%',
  borderCollapse: 'separate',
  borderSpacing: 0,
  backgroundColor: 'var(--bg-surface)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-lg)',
  overflow: 'hidden',
  fontSize: 14,
  lineHeight: '20px',
  fontVariantNumeric: 'tabular-nums',
  '& th': {
    ...cell,
    padding: '0 var(--space-3)',
    backgroundColor: 'var(--bg-subtle)',
    color: 'var(--text-secondary)',
    fontSize: 13,
    lineHeight: '18px',
    fontWeight: 500,
    whiteSpace: 'nowrap',
  },
  '& td': { ...cell, color: 'var(--text)' },
  '& tbody tr:last-of-type td': { borderBottom: 0 },
  '& .fp-right': { textAlign: 'right' },
  '& td.fp-muted': { color: 'var(--text-secondary)' },
} as const;

const sectionHeadSx = {
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  gap: 'var(--space-3)',
  flexWrap: 'wrap',
} as const;

const sectionTitleSx = {
  margin: 0,
  fontSize: 16,
  lineHeight: '24px',
  fontWeight: 500,
  color: 'var(--text)',
} as const;

const emptySx = {
  padding: 'var(--space-4)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-lg)',
  color: 'var(--text-secondary)',
} as const;

function Stat({
  label,
  children,
  detail,
}: {
  label: string;
  children: ReactNode;
  detail?: string;
}) {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)', minWidth: 0 }}>
      <Box component="span" sx={statLabelSx}>
        {label}
      </Box>
      {children}
      {detail !== undefined && (
        <Box component="span" sx={statDetailSx}>
          {detail}
        </Box>
      )}
    </Box>
  );
}

/**
 * The hours of a project in the details drawer (canvas Horas do projeto, AdminProjeto, ADR-0081, ADR-0128): the code,
 * the name and the status; the contracted, the additional and the logged hours and the balance, with "Alterar" on the
 * contracted ones; the bar of the consumption; the additional hours, with "Incluir horas adicionais" and the "⋮" to
 * correct or delete each one; and the hours of each person, with "Ver apontamentos".
 */
export function ProjectHoursDrawer({
  projectId,
  onClose,
  onChanged,
}: {
  /** The project chosen; none while the drawer is closed. */
  projectId: string | undefined;
  onClose: () => void;
  /** Something changed, with the message. */
  onChanged: (message: string) => void;
}) {
  const { t } = useTranslation();
  const { data, isError } = useQuery({
    ...projectHoursDetailQuery(projectId ?? ''),
    enabled: projectId !== undefined,
  });

  return (
    <DetailsDrawer
      open={projectId !== undefined}
      onClose={onClose}
      label={t('projectHours.details.label')}
    >
      {isError && <Alert tone="danger">{t('projectHours.details.notLoaded')}</Alert>}
      {data !== undefined && data.project.id === projectId && (
        <ProjectHoursContent key={data.project.id} detail={data} onChanged={onChanged} />
      )}
    </DetailsDrawer>
  );
}

function ProjectHoursContent({
  detail,
  onChanged,
}: {
  detail: ProjectHoursDetail;
  onChanged: (message: string) => void;
}) {
  const { t } = useTranslation();
  const { toDayValue } = useDates();
  const today = toDayValue(new Date());
  const { project, firstDay, additional, people } = detail;
  const { balance } = project;
  const look = balanceLook[balance.state];
  const [changingContracted, setChangingContracted] = useState(false);
  // Additional hours to correct, or null for new ones.
  const [editing, setEditing] = useState<AdditionalHours | null>();
  const [removing, setRemoving] = useState<AdditionalHours>();
  const [menu, setMenu] = useState<{ anchor: HTMLElement; additional: AdditionalHours }>();
  const [entries, setEntries] = useState(false);
  const [failure, setFailure] = useState<string>();

  const remove = async (item: AdditionalHours) => {
    setRemoving(undefined);
    try {
      const { response } = await api.DELETE(
        '/api/admin/project-hours/{projectId}/additional/{id}',
        { params: { path: { projectId: project.id, id: item.id } } },
      );
      if (response.ok) {
        onChanged(t('projectHours.messages.removed'));
        return;
      }
      setFailure(t('projectHours.messages.failed'));
    } catch {
      setFailure(t('projectHours.messages.failed'));
    }
  };

  const detailText = consumptionDetail(balance, t);

  return (
    <>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}>
        <Box
          component="span"
          sx={{
            fontFamily: 'var(--font-mono)',
            fontSize: 13,
            lineHeight: '20px',
            fontWeight: 500,
            color: 'var(--text-secondary)',
          }}
        >
          {project.code}
        </Box>
        <Box
          component="h2"
          sx={{
            margin: 0,
            fontSize: 18,
            lineHeight: '26px',
            fontWeight: 500,
            color: 'var(--text)',
            overflowWrap: 'anywhere',
          }}
        >
          {project.name}
        </Box>
        <Box>
          <ProjectStatusBadge status={project.status} />
        </Box>
      </Box>
      {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
          gap: 'var(--space-5)',
        }}
      >
        <Stat label={t('projectHours.details.contracted')}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
            <Box component="span" sx={statValueSx}>
              {hoursOrDash(balance.contracted, t)}
            </Box>
            <Button
              variant="text"
              size="small"
              startIcon={<EditOutlined />}
              aria-label={t('projectHours.details.changeLabel')}
              onClick={() => {
                setChangingContracted(true);
              }}
            >
              {t('projectHours.details.change')}
            </Button>
          </Box>
        </Stat>
        <Stat
          label={t('projectHours.details.additional')}
          detail={
            balance.total === null
              ? undefined
              : t('projectHours.details.total', { hours: formatHours(balance.total) })
          }
        >
          <Box component="span" sx={statValueSx}>
            {hoursOrDash(balance.additional, t)}
          </Box>
        </Stat>
        <Stat
          label={t('projectHours.details.logged')}
          detail={
            firstDay === null
              ? undefined
              : t('projectHours.details.since', { day: formatDayText(firstDay) })
          }
        >
          <Box component="span" sx={statValueSx}>
            {formatHours(balance.logged)}
          </Box>
        </Stat>
        <Stat label={t('projectHours.details.balance')}>
          <Box sx={{ paddingTop: 'var(--space-1)' }}>
            <HoursBalanceBadge balance={balance} />
          </Box>
        </Stat>
      </Box>
      {balance.consumption !== null && (
        <ProgressBar
          value={balance.consumption}
          label={t('projectHours.consumptionLabel')}
          tone={look.progress}
          caption={t('projectHours.details.consumption')}
          detail={detailText}
        />
      )}
      <Box
        component="section"
        aria-labelledby="project-hours-additional"
        sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}
      >
        <Box sx={sectionHeadSx}>
          <Box component="h3" id="project-hours-additional" sx={sectionTitleSx}>
            {t('projectHours.details.additionalTitle')}
          </Box>
          <Button
            variant="outlined"
            size="small"
            startIcon={<AddOutlined />}
            onClick={() => {
              setEditing(null);
            }}
          >
            {t('projectHours.details.addAdditional')}
          </Button>
        </Box>
        {additional.length === 0 ? (
          <Box sx={emptySx}>{t('projectHours.details.noAdditional')}</Box>
        ) : (
          <Box component="table" sx={tableSx}>
            <thead>
              <tr>
                <Box component="th" scope="col" sx={{ width: 104 }}>
                  {t('projectHours.details.columns.day')}
                </Box>
                <Box component="th" scope="col" className="fp-right" sx={{ width: 72 }}>
                  {t('projectHours.details.columns.hours')}
                </Box>
                <th scope="col">{t('projectHours.details.columns.reason')}</th>
                <Box component="th" scope="col" sx={{ width: 52 }}>
                  <Box component="span" sx={visuallyHidden}>
                    {t('projectHours.details.columns.actions')}
                  </Box>
                </Box>
              </tr>
            </thead>
            <tbody>
              {additional.map((item) => (
                <tr key={item.id}>
                  <td>{formatDayText(item.day)}</td>
                  <td className="fp-right">{formatHours(item.hours)}</td>
                  <Box component="td" sx={{ overflowWrap: 'anywhere' }}>
                    {item.reason}
                    <Box component="span" sx={subSx}>
                      {t('projectHours.details.createdBy', { name: item.createdBy.name })}
                    </Box>
                  </Box>
                  <td className="fp-right">
                    <IconButton
                      size="small"
                      aria-label={t('projectHours.additionalMenu.button', {
                        day: formatDayText(item.day),
                      })}
                      aria-haspopup="menu"
                      onClick={(event) => {
                        setMenu({ anchor: event.currentTarget, additional: item });
                      }}
                      sx={{ color: 'var(--text-secondary)' }}
                    >
                      <MoreVertOutlined sx={{ fontSize: 'var(--icon-md)' }} />
                    </IconButton>
                  </td>
                </tr>
              ))}
            </tbody>
          </Box>
        )}
      </Box>
      <Box
        component="section"
        aria-labelledby="project-hours-people"
        sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}
      >
        <Box sx={sectionHeadSx}>
          <Box component="h3" id="project-hours-people" sx={sectionTitleSx}>
            {t('projectHours.details.people')}
          </Box>
          <Button
            variant="text"
            size="small"
            startIcon={<ViewListOutlined />}
            onClick={() => {
              setEntries(true);
            }}
          >
            {t('projectHours.details.viewEntries')}
          </Button>
        </Box>
        {people.length === 0 ? (
          <Box sx={emptySx}>{t('projectHours.details.noPeople')}</Box>
        ) : (
          <Box component="table" sx={tableSx}>
            <thead>
              <tr>
                <th scope="col">{t('projectHours.details.columns.person')}</th>
                <th scope="col">{t('projectHours.details.columns.role')}</th>
                <th scope="col" className="fp-right">
                  {t('projectHours.details.columns.hours')}
                </th>
                <th scope="col" className="fp-right">
                  {t('projectHours.details.columns.share')}
                </th>
              </tr>
            </thead>
            <tbody>
              {people.map(({ person, role, hours, share }) => {
                const roleText =
                  role === null
                    ? t('projectHours.details.leftProject')
                    : t(`projects.roles.${role}`);
                return (
                  <tr key={person.id}>
                    <td>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
                        <CardAvatar
                          person={person}
                          role={role === null ? undefined : roleText}
                          size="sm"
                          tone={role === null ? undefined : memberToneOf[role]}
                        />
                        <span>{person.name}</span>
                      </Box>
                    </td>
                    <td className="fp-muted">{roleText}</td>
                    <td className="fp-right">{formatHours(hours)}</td>
                    <td className="fp-right">{`${String(share)}%`}</td>
                  </tr>
                );
              })}
            </tbody>
          </Box>
        )}
      </Box>
      <Menu
        anchorEl={menu?.anchor}
        open={menu !== undefined}
        onClose={() => {
          setMenu(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { width: 180, marginTop: 'var(--space-1)' } },
          list: {
            'aria-label':
              menu === undefined
                ? undefined
                : t('projectHours.additionalMenu.button', {
                    day: formatDayText(menu.additional.day),
                  }),
          },
        }}
      >
        <MenuItem
          onClick={() => {
            setEditing(menu?.additional);
            setMenu(undefined);
          }}
        >
          <EditOutlined aria-hidden />
          {t('projectHours.additionalMenu.edit')}
        </MenuItem>
        <MenuItem
          onClick={() => {
            setRemoving(menu?.additional);
            setMenu(undefined);
          }}
        >
          <DeleteOutlined aria-hidden />
          {t('projectHours.additionalMenu.remove')}
        </MenuItem>
      </Menu>
      {changingContracted && (
        <ContractedHoursDialog
          project={project}
          contracted={balance.contracted}
          onClose={() => {
            setChangingContracted(false);
          }}
          onSaved={(message) => {
            setChangingContracted(false);
            onChanged(message);
          }}
        />
      )}
      {editing !== undefined && (
        <AdditionalHoursDialog
          project={project}
          additional={editing ?? undefined}
          today={today}
          onClose={() => {
            setEditing(undefined);
          }}
          onSaved={(message) => {
            setEditing(undefined);
            onChanged(message);
          }}
        />
      )}
      {entries && (
        <ProjectEntriesDialog
          project={project}
          onClose={() => {
            setEntries(false);
          }}
          onChanged={onChanged}
        />
      )}
      <Dialog
        open={removing !== undefined}
        alert
        title={t('projectHours.removeAdditional.title')}
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
              {t('projectHours.removeAdditional.cancel')}
            </Button>
            <Button
              variant="contained"
              color="error"
              onClick={() => {
                if (removing !== undefined) {
                  void remove(removing);
                }
              }}
            >
              {t('projectHours.removeAdditional.confirm')}
            </Button>
          </>
        }
      >
        {t('projectHours.removeAdditional.text', {
          hours: formatHours(removing?.hours ?? 0),
          day: formatDayText(removing?.day ?? today),
          code: project.code,
        })}
      </Dialog>
    </>
  );
}
