import { zodResolver } from '@hookform/resolvers/zod';
import AddOutlined from '@mui/icons-material/AddOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import FlagOutlined from '@mui/icons-material/FlagOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useMemo, useState } from 'react';
import type { TFunction } from 'i18next';
import { useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import {
  api,
  type Project,
  projectProblems,
  type Schedule,
  type ScheduleItem,
  type ScheduleItemKind,
  type SchedulePhase,
} from '../api/client';
import { projectScheduleQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Avatar } from '../components/Avatar';
import { DataTable, type DataTableColumn } from '../components/DataTable';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { Dialog } from '../components/Dialog';
import { DueDate } from '../components/DueDate';
import { ProgressBar } from '../components/ProgressBar';
import { SegmentedControl } from '../components/SegmentedControl';
import { formatDayText } from '../format/dates';
import { theme } from '../theme/theme';
import { problemType } from './projectForm';
import { useProjectOutlet } from './projectOutlet';
import {
  findItem,
  isPhaseRow,
  itemsOf,
  predecessorNames,
  type ScheduleRow,
  scheduleRows,
} from './schedule';
import { ScheduleItemFields } from './ScheduleItemFields';
import {
  bodyOf,
  newItemValues,
  scheduleItemSchema,
  type ScheduleItemValues,
  showRefusal,
  valuesOfItem,
} from './scheduleItemValues';
import { SchedulePhaseDialog } from './SchedulePhaseDialog';

/** Rows of a page of the list; the free Data Grid shows up to 100 (ADR-0050). */
const pageSize = 100;

const emptySchedule: Schedule = {
  today: '',
  start: null,
  end: null,
  progress: 0,
  lateTasks: 0,
  phases: [],
};

const secondarySx = { color: 'var(--text-secondary)' } as const;
const ellipsisSx = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const;

type PhaseDialogState = { phase?: SchedulePhase } | undefined;

/**
 * The tab Cronograma of a project, with the Lista (ADR-0071; the Linha do tempo comes with Cronograma-3): the phases
 * as group rows with their dates and progress, and under each one its tasks and milestones in order, with the start,
 * the end, the duration in business days, the owner, the progress and what each depends on. A late task says so by
 * icon, color and text (ADR-0065). Who edits the schedule (ADR-0043) includes phases and tasks and changes them; the
 * details of a task open in a drawer (ADR-0081). Every date comes computed from the API (ADR-0051).
 */
export function ProjectSchedulePage() {
  const { t } = useTranslation();
  const { project, notify } = useProjectOutlet();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { data, isFetching } = useQuery(projectScheduleQuery(project.id));
  const schedule = data ?? emptySchedule;
  const canEdit = project.permissions.canEditSchedule;
  const [page, setPage] = useState(1);
  const [chosenId, setChosenId] = useState<string>();
  const [phaseDialog, setPhaseDialog] = useState<PhaseDialogState>();
  const [creating, setCreating] = useState(false);
  const rows = useMemo(() => scheduleRows(schedule), [schedule]);
  const pageRows = useMemo(() => rows.slice((page - 1) * pageSize, page * pageSize), [rows, page]);
  const chosen = chosenId === undefined ? undefined : findItem(schedule, chosenId);
  const itemCount = itemsOf(schedule).length;

  const columns = useMemo<DataTableColumn<ScheduleRow>[]>(
    () => [
      {
        field: 'name',
        headerName: t('projects.schedule.columns.name'),
        flex: 2,
        minWidth: 220,
        renderCell: ({ row }) =>
          row.kind === 'phase' ? (
            <Box component="span" sx={ellipsisSx}>
              {row.phase.name}
            </Box>
          ) : (
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 'var(--space-2)',
                minWidth: 0,
                paddingLeft: 'var(--space-5)',
              }}
            >
              {row.item.kind === 'Milestone' && (
                <FlagOutlined
                  aria-label={t('projects.schedule.kinds.Milestone')}
                  sx={{ fontSize: 16, color: 'var(--text-secondary)', flex: 'none' }}
                />
              )}
              <Box component="span" sx={ellipsisSx} title={row.item.name}>
                {row.item.name}
              </Box>
            </Box>
          ),
      },
      {
        field: 'start',
        headerName: t('projects.schedule.columns.start'),
        width: 104,
        valueGetter: (_value, row) => {
          const start = row.kind === 'phase' ? row.phase.start : row.item.start;
          return start === null ? '' : formatDayText(start);
        },
      },
      {
        field: 'end',
        headerName: t('projects.schedule.columns.end'),
        width: 172,
        renderCell: ({ row }) => {
          const end =
            row.kind === 'phase'
              ? row.phase.end
              : row.item.kind === 'Milestone'
                ? null
                : row.item.end;
          const late = row.kind === 'phase' ? row.phase.late : row.item.late;
          return (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
              {end !== null && <span>{formatDayText(end)}</span>}
              {late && <DueDate state="overdue">{t('projects.schedule.late')}</DueDate>}
            </Box>
          );
        },
      },
      {
        field: 'duration',
        headerName: t('projects.schedule.columns.duration'),
        width: 112,
        valueGetter: (_value, row) =>
          row.kind === 'phase'
            ? ''
            : row.item.kind === 'Milestone'
              ? t('projects.schedule.kinds.Milestone')
              : t('projects.schedule.duration', { count: row.item.duration }),
      },
      {
        field: 'owner',
        headerName: t('projects.schedule.columns.owner'),
        width: 172,
        renderCell: ({ row }) => {
          if (row.kind === 'phase' || row.item.kind === 'Milestone') {
            return null;
          }
          const owner = row.item.owner;
          return owner === null ? (
            <Box component="span" sx={secondarySx}>
              {t('projects.schedule.noOwner')}
            </Box>
          ) : (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', minWidth: 0 }}>
              <Avatar name={owner.name} size="sm" />
              <Box component="span" sx={ellipsisSx}>
                {owner.name}
              </Box>
            </Box>
          );
        },
      },
      {
        field: 'progress',
        headerName: t('projects.schedule.columns.progress'),
        width: 148,
        renderCell: ({ row }) => {
          if (row.kind === 'phase') {
            return row.phase.start === null ? null : (
              <ProgressBar
                value={row.phase.progress}
                label={t('projects.schedule.phaseProgress', { name: row.phase.name })}
                width="100%"
              />
            );
          }
          return row.item.kind === 'Milestone' ? (
            <Box component="span" sx={secondarySx}>
              {t('projects.schedule.none')}
            </Box>
          ) : (
            <ProgressBar
              value={row.item.progress}
              label={t('projects.schedule.itemProgress', { name: row.item.name })}
              width="100%"
            />
          );
        },
      },
      {
        field: 'dependsOn',
        headerName: t('projects.schedule.columns.dependsOn'),
        flex: 1,
        minWidth: 160,
        valueGetter: (_value, row) =>
          row.kind === 'phase' ? '' : predecessorNames(row.item, schedule).join(', '),
        renderCell: ({ value }) => (
          <Box component="span" sx={{ ...ellipsisSx, ...secondarySx }} title={value as string}>
            {value as string}
          </Box>
        ),
      },
    ],
    [t, schedule],
  );

  const footer = useCallback(
    () =>
      t('projects.schedule.footer', {
        phases: t('projects.schedule.phases', { count: schedule.phases.length }),
        items: t('projects.schedule.items', { count: itemCount }),
      }),
    [t, schedule.phases.length, itemCount],
  );
  const choose = useCallback(
    (row: ScheduleRow) => {
      if (row.kind === 'item') {
        setChosenId(row.item.id);
      } else if (canEdit) {
        setPhaseDialog({ phase: row.phase });
      }
    },
    [canEdit],
  );
  const getRowId = useCallback((row: ScheduleRow) => row.id, []);

  const actions = (fullWidth: boolean) =>
    canEdit && (
      <Box
        sx={{
          display: 'flex',
          gap: 'var(--space-2)',
          ...(fullWidth ? { width: '100%', '& > *': { flex: 1 } } : { flex: 'none' }),
        }}
      >
        <Button
          variant="outlined"
          size="small"
          startIcon={<AddOutlined />}
          onClick={() => {
            setPhaseDialog({});
          }}
        >
          {t('projects.schedule.newPhase')}
        </Button>
        <Button
          variant="contained"
          size="small"
          startIcon={<AddOutlined />}
          disabled={schedule.phases.length === 0}
          onClick={() => {
            setCreating(true);
          }}
        >
          {t('projects.schedule.newItem')}
        </Button>
      </Box>
    );

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-3)',
      }}
    >
      <DataTable<ScheduleRow>
        label={t('projects.schedule.table')}
        rows={pageRows}
        columns={columns}
        rowCount={rows.length}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        loading={isFetching}
        rowHeight={48}
        emptyLabel={t('projects.schedule.empty')}
        displayedRowsLabel={footer}
        onRowChoose={choose}
        isGroupRow={isPhaseRow}
        groupRowsChoosable={canEdit}
        getRowId={getRowId}
        selectedRowId={chosen?.item.id ?? phaseDialog?.phase?.id}
        tools={
          <>
            {itemCount > 0 && (
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  flexWrap: 'wrap',
                  gap: 'var(--space-2) var(--space-4)',
                  minWidth: 0,
                }}
              >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
                  <Box
                    component="span"
                    sx={{ fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
                  >
                    {t('projects.schedule.progress')}
                  </Box>
                  <ProgressBar
                    value={schedule.progress}
                    label={t('projects.schedule.progress')}
                    width={180}
                  />
                </Box>
                {schedule.lateTasks > 0 && (
                  <DueDate state="overdue">
                    {t('projects.schedule.lateTasks', { count: schedule.lateTasks })}
                  </DueDate>
                )}
              </Box>
            )}
            {desktop && <Box sx={{ flex: 1 }} />}
            {desktop && actions(false)}
          </>
        }
      />
      {!desktop && actions(true)}
      {phaseDialog !== undefined && (
        <SchedulePhaseDialog
          project={project}
          phase={phaseDialog.phase}
          onClose={() => {
            setPhaseDialog(undefined);
          }}
          onDone={(message) => {
            setPhaseDialog(undefined);
            notify(message);
          }}
        />
      )}
      {creating && (
        <NewItemDialog
          project={project}
          schedule={schedule}
          onClose={() => {
            setCreating(false);
          }}
          onCreated={(message) => {
            setCreating(false);
            notify(message);
          }}
        />
      )}
      <DetailsDrawer
        open={chosen !== undefined}
        onClose={() => {
          setChosenId(undefined);
        }}
        label={t('projects.schedule.details.label')}
      >
        {chosen !== undefined &&
          (canEdit ? (
            <ItemEditor
              // A saved change comes back with the dates the API computed, and the form starts again from them.
              key={`${chosen.item.id}:${JSON.stringify(chosen.item)}`}
              project={project}
              schedule={schedule}
              item={chosen.item}
              onSaved={notify}
              onRemoved={(message) => {
                setChosenId(undefined);
                notify(message);
              }}
            />
          ) : (
            <ItemDetails item={chosen.item} phase={chosen.phase} schedule={schedule} />
          ))}
      </DetailsDrawer>
    </Box>
  );
}

/** The message of a refused change that is no field's: the project was closed meanwhile, or another failure. */
function failureOf(problem: unknown, status: number, t: TFunction) {
  return status === 409 && problemType(problem) === projectProblems.closed
    ? t('projects.schedule.messages.closed')
    : t('projects.schedule.messages.failed');
}

/**
 * "Nova tarefa": a task or a milestone, its name, phase and start, and, for a task, the duration and the owner, and
 * what it depends on (ADR-0018, ADR-0019). It starts at 0%.
 */
function NewItemDialog({
  project,
  schedule,
  onClose,
  onCreated,
}: {
  project: Project;
  schedule: Schedule;
  onClose: () => void;
  onCreated: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const firstPhase = schedule.phases[0]?.id ?? '';
  const {
    control,
    handleSubmit,
    setError,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<ScheduleItemValues>({
    resolver: zodResolver(scheduleItemSchema),
    defaultValues: newItemValues(firstPhase),
  });
  const kind = useWatch({ control, name: 'kind' });

  const create = handleSubmit(async (values) => {
    try {
      const { error, response } = await api.POST('/api/projects/{id}/schedule/items', {
        params: { path: { id: project.id } },
        body: { kind: values.kind, ...bodyOf(values) },
      });
      if (response.ok) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        onCreated(
          t(`projects.schedule.messages.itemAdded.${values.kind}`, { name: values.name.trim() }),
        );
        return;
      }
      if (!showRefusal(error, setError)) {
        setError('root', { message: failureOf(error, response.status, t) });
      }
    } catch {
      setError('root', { message: t('projects.schedule.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t('projects.schedule.form.title')}
      onClose={onClose}
      onSubmit={(event) => {
        void create(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.schedule.form.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t(`projects.schedule.form.create.${kind}`)}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <SegmentedControl<ScheduleItemKind>
          label={t('projects.schedule.form.kind')}
          value={kind}
          options={[
            { value: 'Task', label: t('projects.schedule.kinds.Task') },
            { value: 'Milestone', label: t('projects.schedule.kinds.Milestone') },
          ]}
          onChange={(value) => {
            setValue('kind', value);
          }}
        />
        <ScheduleItemFields
          control={control}
          errors={errors}
          kind={kind}
          schedule={schedule}
          project={project}
          withProgress={false}
        />
        {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
      </Box>
    </Dialog>
  );
}

/** The heading of the details of a task or milestone: its kind, its name and its phase. */
function ItemHeading({ item, phase }: { item: ScheduleItem; phase: string }) {
  const { t } = useTranslation();
  return (
    <Box sx={{ minWidth: 0 }}>
      <Box
        component="span"
        sx={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 'var(--space-1)',
          fontSize: 13,
          lineHeight: '18px',
          color: 'var(--text-secondary)',
        }}
      >
        {item.kind === 'Milestone' && <FlagOutlined aria-hidden sx={{ fontSize: 16 }} />}
        {t(`projects.schedule.kinds.${item.kind}`)}
        {' · '}
        {phase}
      </Box>
      <Box
        component="h2"
        sx={{
          margin: '2px 0 0',
          fontSize: 18,
          lineHeight: '26px',
          fontWeight: 500,
          color: 'var(--text)',
          overflowWrap: 'anywhere',
        }}
      >
        {item.name}
      </Box>
      {item.late && (
        <Box sx={{ marginTop: 'var(--space-1)' }}>
          <DueDate state="overdue">{t('projects.schedule.late')}</DueDate>
        </Box>
      )}
    </Box>
  );
}

/**
 * The details of a task or milestone for who edits the schedule: the same fields as "Nova tarefa", with the progress
 * and the end the API computed, "Salvar", which says how many items after it were pushed (ADR-0019), and the removal,
 * with confirmation.
 */
function ItemEditor({
  project,
  schedule,
  item,
  onSaved,
  onRemoved,
}: {
  project: Project;
  schedule: Schedule;
  item: ScheduleItem;
  onSaved: (message: string) => void;
  onRemoved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [removing, setRemoving] = useState(false);
  const [removePending, setRemovePending] = useState(false);
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ScheduleItemValues>({
    resolver: zodResolver(scheduleItemSchema),
    defaultValues: valuesOfItem(item),
  });
  const phaseName = schedule.phases.find((phase) => phase.id === item.phaseId)?.name ?? '';
  const refreshed = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
    void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
  };

  const save = handleSubmit(async (values) => {
    try {
      const { data, error, response } = await api.PUT(
        '/api/projects/{id}/schedule/items/{itemId}',
        {
          params: { path: { id: project.id, itemId: item.id } },
          body: bodyOf(values),
        },
      );
      if (response.ok) {
        refreshed();
        const saved = t(`projects.schedule.messages.itemSaved.${item.kind}`, {
          name: values.name.trim(),
        });
        const pushed = data?.pushed ?? 0;
        onSaved(
          pushed === 0 ? saved : t('projects.schedule.messages.pushed', { saved, count: pushed }),
        );
        return;
      }
      if (!showRefusal(error, setError)) {
        setError('root', { message: failureOf(error, response.status, t) });
      }
    } catch {
      setError('root', { message: t('projects.schedule.messages.failed') });
    }
  });

  const remove = async () => {
    setRemovePending(true);
    try {
      const { error, response } = await api.DELETE('/api/projects/{id}/schedule/items/{itemId}', {
        params: { path: { id: project.id, itemId: item.id } },
      });
      setRemoving(false);
      if (response.ok) {
        refreshed();
        onRemoved(t(`projects.schedule.messages.itemRemoved.${item.kind}`, { name: item.name }));
        return;
      }
      setError('root', { message: failureOf(error, response.status, t) });
    } catch {
      setRemoving(false);
      setError('root', { message: t('projects.schedule.messages.failed') });
    } finally {
      setRemovePending(false);
    }
  };

  return (
    <>
      <Box
        component="form"
        noValidate
        aria-label={item.name}
        onSubmit={(event) => {
          void save(event);
        }}
        sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}
      >
        <ItemHeading item={item} phase={phaseName} />
        <ScheduleItemFields
          control={control}
          errors={errors}
          kind={item.kind}
          schedule={schedule}
          project={project}
          itemId={item.id}
          endsOn={item.kind === 'Task' ? item.end : undefined}
          withProgress
        />
        {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
          <Button
            variant="text"
            size="small"
            startIcon={<DeleteOutlined />}
            onClick={() => {
              setRemoving(true);
            }}
            sx={{ color: 'var(--status-danger-text)' }}
          >
            {t(`projects.schedule.form.remove.${item.kind}`)}
          </Button>
          <Box sx={{ flex: 1 }} />
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t('projects.schedule.form.save')}
          </Button>
        </Box>
      </Box>
      <Dialog
        open={removing}
        alert
        title={t(`projects.schedule.removeItem.title.${item.kind}`)}
        onClose={() => {
          setRemoving(false);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setRemoving(false);
              }}
            >
              {t('projects.schedule.removeItem.back')}
            </Button>
            <Button
              variant="contained"
              color="error"
              disabled={removePending}
              onClick={() => {
                void remove();
              }}
            >
              {t(`projects.schedule.form.remove.${item.kind}`)}
            </Button>
          </>
        }
      >
        {t('projects.schedule.removeItem.text', { name: item.name })}
      </Dialog>
    </>
  );
}

/** The details of a task or milestone for who only sees the schedule (ADR-0043). */
function ItemDetails({
  item,
  phase,
  schedule,
}: {
  item: ScheduleItem;
  phase: SchedulePhase;
  schedule: Schedule;
}) {
  const { t } = useTranslation();
  const names = predecessorNames(item, schedule);
  const task = item.kind === 'Task';
  return (
    <Box
      component="section"
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}
    >
      <ItemHeading item={item} phase={phase.name} />
      <Box
        component="dl"
        sx={{
          display: 'grid',
          gridTemplateColumns: '128px minmax(0, 1fr)',
          gap: 'var(--space-3)',
          margin: 0,
          fontSize: 14,
          lineHeight: '20px',
          '& dt': { fontSize: 13, lineHeight: '20px', color: 'var(--text-secondary)' },
          '& dd': { margin: 0, minWidth: 0 },
        }}
      >
        <dt>{t(task ? 'projects.schedule.details.start' : 'projects.schedule.details.day')}</dt>
        <dd>{formatDayText(item.start)}</dd>
        {task && (
          <>
            <dt>{t('projects.schedule.details.end')}</dt>
            <dd>{formatDayText(item.end)}</dd>
            <dt>{t('projects.schedule.details.duration')}</dt>
            <dd>{t('projects.schedule.duration', { count: item.duration })}</dd>
            <dt>{t('projects.schedule.details.owner')}</dt>
            <dd>{item.owner?.name ?? t('projects.schedule.noOwner')}</dd>
            <dt>{t('projects.schedule.details.progress')}</dt>
            <dd>
              <ProgressBar
                value={item.progress}
                label={t('projects.schedule.itemProgress', { name: item.name })}
                width={200}
              />
            </dd>
            {item.completedOn !== null && (
              <>
                <dt>{t('projects.schedule.details.completedOn')}</dt>
                <dd>{formatDayText(item.completedOn)}</dd>
              </>
            )}
          </>
        )}
        <dt>{t('projects.schedule.details.dependsOn')}</dt>
        <dd>{names.length === 0 ? t('projects.schedule.none') : names.join(', ')}</dd>
      </Box>
    </Box>
  );
}
