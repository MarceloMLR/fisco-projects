import EditOutlined from '@mui/icons-material/EditOutlined';
import Box from '@mui/material/Box';
import { visuallyHidden } from '@mui/utils';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { HoursBalanceState, TaskHours } from '../api/client';
import { Button, IconButton } from '../components/Button';
import { ProgressBar } from '../components/ProgressBar';
import { StatusBadge } from '../components/StatusBadge';
import { formatHours } from '../hours/hours';
import { DetailsSection } from '../projects/ScheduleItemProgress';
import { hoursTableSx } from './hoursTable';
import { PlannedHoursDialog } from './ProjectHoursDialogs';
import { balanceLook } from './projectHours';
import { needsAttention, taskBalanceText, taskHoursText } from './taskHours';

/** The color of the text of each state of the hours of a task, from the tokens of the status (ADR-0065). */
const stateColors: Record<HoursBalanceState, string> = {
  Over: 'var(--status-danger-text)',
  NearLimit: 'var(--status-warning-text)',
  Within: 'var(--status-success-text)',
  NoContract: 'var(--text-secondary)',
};

const inlineSx = {
  display: 'inline-flex',
  alignItems: 'center',
  gap: 'var(--space-1)',
  minWidth: 0,
  whiteSpace: 'nowrap',
  fontVariantNumeric: 'tabular-nums',
} as const;

/**
 * The balance of a task in words, by icon, color and text (ADR-0065, ADR-0138): "12 h acima", "Restam 2 h" or "Sem
 * horas previstas".
 */
export function TaskBalance({ task }: { task: TaskHours }) {
  const { t } = useTranslation();
  const { icon: Icon } = balanceLook[task.balance.state];
  return (
    <Box
      component="span"
      data-state={task.balance.state}
      sx={{ ...inlineSx, fontSize: 12, lineHeight: '16px', color: stateColors[task.balance.state] }}
    >
      <Icon aria-hidden sx={{ fontSize: 14 }} />
      {taskBalanceText(task, t)}
    </Box>
  );
}

/**
 * The hours of a task in the column "Horas" of the Lista, only for the Administrator (ADR-0138): "52 de 40 h", with the
 * icon and the color of the state when the task is over or near the planned hours, and the balance for who reads the
 * screen; a dash for a task without planned or logged hours.
 */
export function TaskHoursCell({ task }: { task: TaskHours | undefined }) {
  const { t } = useTranslation();
  if (task === undefined) {
    return (
      <Box component="span" sx={{ color: 'var(--text-secondary)' }}>
        {t('projectHours.none')}
      </Box>
    );
  }
  const text = taskHoursText(task, t);
  const balance = taskBalanceText(task, t);
  if (!needsAttention(task)) {
    return (
      <Box component="span" title={balance} sx={inlineSx}>
        {text}
      </Box>
    );
  }
  const { icon: Icon } = balanceLook[task.balance.state];
  return (
    <Box
      component="span"
      title={balance}
      data-state={task.balance.state}
      sx={{ ...inlineSx, fontWeight: 500, color: stateColors[task.balance.state] }}
    >
      <Icon aria-hidden sx={{ fontSize: 16 }} />
      {text}
      <Box component="span" sx={visuallyHidden}>
        {`, ${balance}`}
      </Box>
    </Box>
  );
}

const detailsListSx = {
  display: 'grid',
  gridTemplateColumns: '128px minmax(0, 1fr)',
  alignItems: 'center',
  gap: 'var(--space-2) var(--space-3)',
  alignSelf: 'stretch',
  margin: 0,
  fontSize: 14,
  lineHeight: '20px',
  '& dt': { fontSize: 13, lineHeight: '20px', color: 'var(--text-secondary)' },
  '& dd': { margin: 0, minWidth: 0 },
} as const;

/**
 * The section "Horas" of the details of a task, only for the Administrator (ADR-0138): the planned hours with
 * "Alterar", the hours logged in the task, the balance and the bar of the consumption.
 */
export function TaskHoursSection({
  projectId,
  task,
  hours,
  onSaved,
}: {
  projectId: string;
  /** The task, by its id in the schedule and its name. */
  task: { id: string; name: string };
  /** The hours of the task; none without planned or logged hours. */
  hours: TaskHours | undefined;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const [changing, setChanging] = useState(false);
  const planned = hours?.planned ?? null;
  const consumption = hours?.balance.consumption ?? null;
  return (
    <DetailsSection title={t('projectHours.tasks.section')}>
      <Box component="dl" sx={detailsListSx}>
        <dt>{t('projectHours.tasks.planned')}</dt>
        <dd>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
            <span>{planned === null ? t('projectHours.none') : formatHours(planned)}</span>
            <Button
              variant="text"
              size="small"
              startIcon={<EditOutlined />}
              aria-label={t('projectHours.tasks.changeLabel', { name: task.name })}
              onClick={() => {
                setChanging(true);
              }}
            >
              {t('projectHours.details.change')}
            </Button>
          </Box>
        </dd>
        <dt>{t('projectHours.tasks.logged')}</dt>
        <dd>{formatHours(hours?.logged ?? 0)}</dd>
        {hours !== undefined && (
          <>
            <dt>{t('projectHours.details.balance')}</dt>
            <dd>
              <StatusBadge
                tone={balanceLook[hours.balance.state].tone}
                icon={balanceLook[hours.balance.state].icon}
              >
                {taskBalanceText(hours, t)}
              </StatusBadge>
            </dd>
          </>
        )}
      </Box>
      {hours !== undefined && consumption !== null && (
        <ProgressBar
          value={consumption}
          label={t('projectHours.tasks.consumptionLabel', { name: task.name })}
          tone={balanceLook[hours.balance.state].progress}
          caption={t('projectHours.details.consumption')}
          detail={t('projectHours.tasks.consumptionDetail', {
            hours: taskHoursText(hours, t),
            share: consumption,
          })}
          width="100%"
        />
      )}
      {changing && (
        <PlannedHoursDialog
          projectId={projectId}
          task={task}
          planned={planned}
          onClose={() => {
            setChanging(false);
          }}
          onSaved={(message) => {
            setChanging(false);
            onSaved(message);
          }}
        />
      )}
    </DetailsSection>
  );
}

/**
 * The tasks with planned or logged hours of a project, in the order of the schedule, in the drawer of Horas dos
 * projetos (ADR-0138): the task with its balance under the name, the planned and the logged hours and "Alterar".
 */
export function TaskHoursTable({
  items,
  onChange,
}: {
  items: readonly TaskHours[];
  onChange: (task: TaskHours) => void;
}) {
  const { t } = useTranslation();
  return (
    <Box component="table" sx={hoursTableSx}>
      <thead>
        <tr>
          <th scope="col">{t('projectHours.tasks.columns.task')}</th>
          <Box component="th" scope="col" className="fp-right" sx={{ width: 80 }}>
            {t('projectHours.tasks.columns.planned')}
          </Box>
          <Box component="th" scope="col" className="fp-right" sx={{ width: 84 }}>
            {t('projectHours.tasks.columns.logged')}
          </Box>
          <Box component="th" scope="col" sx={{ width: 52 }}>
            <Box component="span" sx={visuallyHidden}>
              {t('projectHours.details.columns.actions')}
            </Box>
          </Box>
        </tr>
      </thead>
      <tbody>
        {items.map((task) => (
          <tr key={task.itemId}>
            <td>
              <Box
                component="span"
                title={task.name}
                sx={{ display: 'block', overflowWrap: 'anywhere' }}
              >
                {task.name}
              </Box>
              <TaskBalance task={task} />
            </td>
            <td className="fp-right">
              {task.planned === null ? t('projectHours.none') : formatHours(task.planned)}
            </td>
            <td className="fp-right">{formatHours(task.logged)}</td>
            <td className="fp-right">
              <IconButton
                size="small"
                aria-label={t('projectHours.tasks.changeLabel', { name: task.name })}
                onClick={() => {
                  onChange(task);
                }}
              >
                <EditOutlined />
              </IconButton>
            </td>
          </tr>
        ))}
      </tbody>
    </Box>
  );
}
