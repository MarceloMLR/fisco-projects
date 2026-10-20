import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import SaveOutlined from '@mui/icons-material/SaveOutlined';
import VisibilityOutlined from '@mui/icons-material/VisibilityOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import { useQueryClient } from '@tanstack/react-query';
import type { TFunction } from 'i18next';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type Project, type Schedule, type ScheduleItem } from '../api/client';
import { queryKeys } from '../api/queries';
import { Checkbox, CheckboxMark } from '../components/Checkbox';
import { Dialog } from '../components/Dialog';
import { DueDate } from '../components/DueDate';
import { formatDayText } from '../format/dates';
import type { ScheduleDisplay } from './schedule';
import { DetailsSection } from './ScheduleItemProgress';
import { failureOf } from './scheduleItemValues';

const displayOptions = ['baseline', 'critical'] as const;

/**
 * "Exibir" in the bar of the tab (ADR-0071): a menu that turns the baseline and the critical path on and off, each on
 * its own.
 */
export function DisplayMenu({
  display,
  onChange,
}: {
  display: ScheduleDisplay;
  onChange: (display: ScheduleDisplay) => void;
}) {
  const { t } = useTranslation();
  const [anchor, setAnchor] = useState<HTMLElement>();
  return (
    <>
      <Button
        variant="outlined"
        size="small"
        aria-haspopup="menu"
        aria-expanded={anchor !== undefined}
        startIcon={<VisibilityOutlined />}
        endIcon={<ExpandMoreOutlined />}
        onClick={(event) => {
          setAnchor(event.currentTarget);
        }}
      >
        {t('projects.baseline.display')}
      </Button>
      <Menu
        anchorEl={anchor}
        open={anchor !== undefined}
        onClose={() => {
          setAnchor(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
        slotProps={{
          paper: { sx: { minWidth: 200, marginTop: 'var(--space-1)' } },
          list: { 'aria-label': t('projects.baseline.displayLabel') },
        }}
      >
        {displayOptions.map((option) => (
          <MenuItem
            key={option}
            role="menuitemcheckbox"
            aria-checked={display[option]}
            onClick={() => {
              onChange({ ...display, [option]: !display[option] });
            }}
          >
            <CheckboxMark checked={display[option]} />
            {t(`projects.baseline.show.${option}`)}
          </MenuItem>
        ))}
      </Menu>
    </>
  );
}

/** The same two options right in the header of Expandir, as the canvas shows. */
export function DisplayChecks({
  display,
  onChange,
}: {
  display: ScheduleDisplay;
  onChange: (display: ScheduleDisplay) => void;
}) {
  const { t } = useTranslation();
  return (
    <Box
      role="group"
      aria-label={t('projects.baseline.displayLabel')}
      sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-4)' }}
    >
      {displayOptions.map((option) => (
        <Checkbox
          key={option}
          label={t(`projects.baseline.show.${option}`)}
          checked={display[option]}
          onChange={(checked) => {
            onChange({ ...display, [option]: checked });
          }}
        />
      ))}
    </Box>
  );
}

/**
 * The "⋮" of the tab, for who edits the schedule (ADR-0071): "Salvar linha de base" saves it right away, and, with one
 * saved, "Atualizar linha de base" asks first, saying the day of the baseline it replaces (ADR-0021).
 */
export function BaselineMenu({
  project,
  schedule,
  notify,
}: {
  project: Project;
  schedule: Schedule;
  notify: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [anchor, setAnchor] = useState<HTMLElement>();
  const [confirming, setConfirming] = useState(false);
  const [pending, setPending] = useState(false);
  const saved = schedule.baseline;

  const save = async () => {
    setPending(true);
    try {
      const { error, response } = await api.PUT('/api/projects/{id}/schedule/baseline', {
        params: { path: { id: project.id } },
      });
      setConfirming(false);
      void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
      void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
      notify(
        response.ok
          ? t(
              saved === null
                ? 'projects.baseline.messages.saved'
                : 'projects.baseline.messages.updated',
            )
          : response.status === 400
            ? t('projects.baseline.messages.noItems')
            : failureOf(error, response.status, t),
      );
    } catch {
      setConfirming(false);
      notify(t('projects.schedule.messages.failed'));
    } finally {
      setPending(false);
    }
  };

  return (
    <>
      <IconButton
        size="small"
        aria-label={t('projects.baseline.more')}
        title={t('projects.baseline.more')}
        aria-haspopup="menu"
        aria-expanded={anchor !== undefined}
        onClick={(event) => {
          setAnchor(event.currentTarget);
        }}
        sx={{ color: 'var(--text-secondary)' }}
      >
        <MoreVertOutlined />
      </IconButton>
      <Menu
        anchorEl={anchor}
        open={anchor !== undefined}
        onClose={() => {
          setAnchor(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { minWidth: 240, marginTop: 'var(--space-1)' } },
          list: { 'aria-label': t('projects.baseline.more') },
        }}
      >
        <MenuItem
          disabled={schedule.start === null || pending}
          onClick={() => {
            setAnchor(undefined);
            if (saved === null) {
              void save();
            } else {
              setConfirming(true);
            }
          }}
        >
          <SaveOutlined aria-hidden />
          {t(saved === null ? 'projects.baseline.save' : 'projects.baseline.update')}
        </MenuItem>
      </Menu>
      <Dialog
        open={confirming}
        alert
        title={t('projects.baseline.confirm.title')}
        onClose={() => {
          setConfirming(false);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setConfirming(false);
              }}
            >
              {t('projects.baseline.confirm.back')}
            </Button>
            <Button
              variant="contained"
              disabled={pending}
              onClick={() => {
                void save();
              }}
            >
              {t('projects.baseline.update')}
            </Button>
          </>
        }
      >
        {saved !== null &&
          t('projects.baseline.confirm.text', { day: formatDayText(saved.savedOn) })}
      </Dialog>
    </>
  );
}

/**
 * How the end of the project or of an item stands against the baseline (ADR-0021): after it is a slip against the plan,
 * in amber with the hourglass (soon), not a delay against today, which keeps the red; on it or before, as met.
 */
function deviationOf(deviation: number, kind: 'projectEnd' | 'item', t: TFunction) {
  if (deviation > 0) {
    return {
      state: 'soon' as const,
      text: t(`projects.baseline.${kind}.later`, { count: deviation }),
    };
  }
  if (deviation < 0) {
    return {
      state: 'met' as const,
      text: t(`projects.baseline.${kind}.earlier`, { count: -deviation }),
    };
  }
  return { state: 'met' as const, text: t(`projects.baseline.${kind}.same`) };
}

/** "Linha de base de 16/10/2026" in the summary of the tab, with how the end of the project stands against it. */
export function BaselineSummary({ schedule }: { schedule: Schedule }) {
  const { t } = useTranslation();
  const baseline = schedule.baseline;
  if (baseline === null) {
    return null;
  }
  const deviation =
    baseline.deviation === null ? undefined : deviationOf(baseline.deviation, 'projectEnd', t);
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 'var(--space-2)' }}>
      <Box
        component="span"
        sx={{ fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
      >
        {t('projects.baseline.summary', { day: formatDayText(baseline.savedOn) })}
      </Box>
      {deviation !== undefined && <DueDate state={deviation.state}>{deviation.text}</DueDate>}
    </Box>
  );
}

/**
 * The Linha de base of a task or milestone, in its details, while the schedule has one: the dates saved (for a
 * milestone, its day) and how its end stands against them; an item included later says so.
 */
export function BaselineSection({ item, schedule }: { item: ScheduleItem; schedule: Schedule }) {
  const { t } = useTranslation();
  if (schedule.baseline === null) {
    return null;
  }
  const task = item.kind === 'Task';
  const { baselineStart: start, baselineEnd: end, baselineDeviation: days } = item;
  const deviation = days === null ? undefined : deviationOf(days, 'item', t);
  return (
    <DetailsSection title={t('projects.baseline.details.title')}>
      {start === null || end === null || deviation === undefined ? (
        <Box
          component="span"
          sx={{ fontSize: 14, lineHeight: '20px', color: 'var(--text-secondary)' }}
        >
          {t(`projects.baseline.details.notIn.${item.kind}`)}
        </Box>
      ) : (
        <Box
          component="dl"
          sx={{
            display: 'grid',
            gridTemplateColumns: '128px minmax(0, 1fr)',
            gap: 'var(--space-2) var(--space-3)',
            alignSelf: 'stretch',
            margin: 0,
            fontSize: 14,
            lineHeight: '20px',
            '& dt': { fontSize: 13, lineHeight: '20px', color: 'var(--text-secondary)' },
            '& dd': { margin: 0, minWidth: 0 },
          }}
        >
          <dt>{t(task ? 'projects.baseline.details.start' : 'projects.baseline.details.day')}</dt>
          <dd>{formatDayText(start)}</dd>
          {task && (
            <>
              <dt>{t('projects.baseline.details.end')}</dt>
              <dd>{formatDayText(end)}</dd>
            </>
          )}
          <dt>{t('projects.baseline.details.deviation')}</dt>
          <dd>
            <DueDate state={deviation.state}>{deviation.text}</DueDate>
          </dd>
        </Box>
      )}
    </DetailsSection>
  );
}
