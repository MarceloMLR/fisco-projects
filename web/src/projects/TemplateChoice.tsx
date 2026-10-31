import AddOutlined from '@mui/icons-material/AddOutlined';
import TimelineOutlined from '@mui/icons-material/TimelineOutlined';
import UploadFileOutlined from '@mui/icons-material/UploadFileOutlined';
import WarningAmberOutlined from '@mui/icons-material/WarningAmberOutlined';
import type SvgIcon from '@mui/material/SvgIcon';
import Box from '@mui/material/Box';
import { visuallyHidden } from '@mui/utils';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import type { MsProjectPreview, ProjectTemplate, ScheduleItemKind } from '../api/client';
import { Button } from '../components/Button';
import { formatDayText, formatPeriodText } from '../format/dates';

/** The ways a project starts its schedule (canvas NovoProjeto): blank, from a model or from the MS Project (ADR-0135). */
export type ScheduleOrigin = 'blank' | 'template' | 'msProject';

/** An option of the design system (fp-opt): an icon, a title, a short text and the radio, in a card. */
function OptionCard({
  name,
  value,
  checked,
  disabled = false,
  icon: Icon,
  title,
  text,
  onChange,
}: {
  name: string;
  value: ScheduleOrigin;
  checked: boolean;
  disabled?: boolean;
  icon: typeof SvgIcon;
  title: string;
  text: string;
  onChange: (value: ScheduleOrigin) => void;
}) {
  return (
    <Box
      component="label"
      data-checked={checked}
      sx={{
        position: 'relative',
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-3)',
        boxSizing: 'border-box',
        padding: 'var(--space-4)',
        border: '1px solid var(--border-control)',
        borderRadius: 'var(--radius-lg)',
        backgroundColor: 'var(--bg-surface)',
        cursor: disabled ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.6 : 1,
        '&:hover': disabled ? undefined : { borderColor: 'var(--text-secondary)' },
        '&:focus-within': { outline: '2px solid var(--focus-ring)', outlineOffset: 2 },
        '&[data-checked="true"]': {
          borderColor: 'var(--control-selected)',
          boxShadow: 'inset 0 0 0 1px var(--control-selected)',
          backgroundColor: 'var(--bg-selected)',
        },
      }}
    >
      <Box
        component="input"
        type="radio"
        name={name}
        value={value}
        checked={checked}
        disabled={disabled}
        onChange={() => {
          onChange(value);
        }}
        sx={visuallyHidden}
      />
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)' }}>
        <Box
          aria-hidden
          sx={{
            display: 'inline-grid',
            placeItems: 'center',
            width: 36,
            height: 36,
            flex: 'none',
            borderRadius: 'var(--radius-md)',
            backgroundColor: checked ? 'var(--action)' : 'var(--bg-subtle)',
            color: checked ? 'var(--on-action)' : 'var(--text-secondary)',
          }}
        >
          <Icon />
        </Box>
        <Box component="span" sx={{ flex: 1, fontSize: 15, lineHeight: '22px', fontWeight: 500 }}>
          {title}
        </Box>
        <Box
          component="span"
          aria-hidden
          sx={{
            flex: 'none',
            width: 18,
            height: 18,
            boxSizing: 'border-box',
            border: checked
              ? '5px solid var(--control-selected)'
              : '2px solid var(--border-control)',
            borderRadius: 'var(--radius-full)',
            backgroundColor: 'var(--bg-surface)',
          }}
        />
      </Box>
      <Box
        component="span"
        sx={{ fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
      >
        {text}
      </Box>
    </Box>
  );
}

/**
 * "Em branco", "A partir de um modelo", off without active models (ADR-0017), or "A partir do MS Project" (ADR-0135).
 */
export function ScheduleOriginChoice({
  value,
  hasTemplates,
  labelledBy,
  onChange,
}: {
  value: ScheduleOrigin;
  hasTemplates: boolean;
  labelledBy: string;
  onChange: (value: ScheduleOrigin) => void;
}) {
  const { t } = useTranslation();
  return (
    <Box
      role="radiogroup"
      aria-labelledby={labelledBy}
      sx={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
        gap: 'var(--space-3)',
      }}
    >
      <OptionCard
        name="schedule-origin"
        value="blank"
        checked={value === 'blank'}
        icon={AddOutlined}
        title={t('projects.form.blank')}
        text={t('projects.form.blankHelper')}
        onChange={onChange}
      />
      <OptionCard
        name="schedule-origin"
        value="template"
        checked={value === 'template'}
        disabled={!hasTemplates}
        icon={TimelineOutlined}
        title={t('projects.form.fromTemplate')}
        text={t(hasTemplates ? 'projects.form.fromTemplateHelper' : 'projects.form.noTemplates')}
        onChange={onChange}
      />
      <OptionCard
        name="schedule-origin"
        value="msProject"
        checked={value === 'msProject'}
        icon={UploadFileOutlined}
        title={t('projects.form.fromMsProject')}
        text={t('projects.form.fromMsProjectHelper')}
        onChange={onChange}
      />
    </Box>
  );
}

/** An item of the preview: its kind, name, whether it is a subtask and, from the MS Project, its dates. */
interface PreviewItem {
  kind: ScheduleItemKind;
  name: string;
  isSubtask: boolean;
  period?: string;
}

/**
 * The preview of the schedule beside the form (canvas NovoProjeto): the name, how many tasks and milestones it has,
 * where its dates come from, an action below them, and its phases with their items.
 */
function SchedulePreview({
  title,
  note,
  action,
  phases,
}: {
  title: string;
  note: string;
  action?: ReactNode;
  phases: readonly { name: string; items: readonly PreviewItem[] }[];
}) {
  const { t } = useTranslation();
  const items = phases.flatMap((phase) => phase.items);
  const milestones = items.filter((item) => item.kind === 'Milestone').length;
  const tasks = items.length - milestones;
  return (
    <Box
      component="aside"
      aria-label={t('projects.form.preview')}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        minHeight: 0,
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-lg)',
        overflow: 'hidden',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          minHeight: 48,
          padding: 'var(--space-2) var(--space-4)',
          borderBottom: '1px solid var(--border)',
        }}
      >
        <Box component="h2" sx={{ margin: 0, fontSize: 15, lineHeight: '22px', fontWeight: 500 }}>
          {title}
        </Box>
      </Box>
      <Box
        sx={{
          padding: 'var(--space-3) var(--space-4)',
          borderBottom: '1px solid var(--border)',
          fontSize: 13,
          lineHeight: '18px',
          color: 'var(--text-secondary)',
        }}
      >
        <div>
          {t('projects.form.previewCounts', {
            tasks: t('projects.form.previewTasks', { count: tasks }),
            milestones: t('projects.form.previewMilestones', { count: milestones }),
          })}
        </div>
        <div>{note}</div>
        {action !== undefined && <Box sx={{ marginTop: 'var(--space-1)' }}>{action}</Box>}
      </Box>
      <Box
        component="ol"
        sx={{
          flex: 1,
          minHeight: 0,
          overflow: 'auto',
          margin: 0,
          padding: '0 var(--space-4)',
          listStyle: 'none',
        }}
      >
        {phases.map((phase, index) => (
          <Box
            component="li"
            // The phases of a preview have no ids of their own in the API.
            key={`${String(index)}-${phase.name}`}
            sx={{ padding: 'var(--space-3) 0', borderBottom: '1px solid var(--border)' }}
          >
            <Box component="span" sx={{ fontSize: 14, lineHeight: '20px', fontWeight: 500 }}>
              {phase.name}
            </Box>
            <Box
              component="ul"
              sx={{
                margin: '6px 0 0',
                padding: '0 0 0 var(--space-4)',
                fontSize: 13,
                lineHeight: '20px',
                color: 'var(--text-secondary)',
              }}
            >
              {phase.items.map((item, position) => (
                <Box
                  component="li"
                  key={`${String(position)}-${item.name}`}
                  sx={{
                    display: 'flex',
                    gap: 'var(--space-3)',
                    ...(item.isSubtask ? { marginLeft: 'var(--space-4)' } : {}),
                  }}
                >
                  <Box component="span" sx={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>
                    {item.kind === 'Milestone'
                      ? t('projects.form.previewMilestone', { name: item.name })
                      : item.name}
                  </Box>
                  {item.period !== undefined && (
                    <Box
                      component="span"
                      sx={{
                        flex: 'none',
                        whiteSpace: 'nowrap',
                        fontVariantNumeric: 'tabular-nums',
                      }}
                    >
                      {item.period}
                    </Box>
                  )}
                </Box>
              ))}
            </Box>
          </Box>
        ))}
      </Box>
    </Box>
  );
}

/** The preview of a model: its phases and items, the dates coming from the server on the creation (ADR-0051). */
export function TemplatePreview({ template }: { template: ProjectTemplate }) {
  const { t } = useTranslation();
  return (
    <SchedulePreview
      title={template.name}
      note={t('projects.form.previewDates')}
      phases={template.phases}
    />
  );
}

/**
 * The preview of a file of the MS Project (ADR-0135): its title, its phases and items with the dates the server
 * computed in the calendar of the installation, from the start, and the shortcut to the box of the conversion when it
 * lists something.
 */
export function MsProjectSchedulePreview({
  preview,
  adjustments,
  onShowAdjustments,
}: {
  preview: MsProjectPreview;
  adjustments: number;
  onShowAdjustments: () => void;
}) {
  const { t } = useTranslation();
  return (
    <SchedulePreview
      title={preview.title}
      note={t('projects.form.msProject.previewDates', { start: formatDayText(preview.start) })}
      action={
        adjustments === 0 ? undefined : (
          <Button
            variant="text"
            size="small"
            startIcon={<WarningAmberOutlined />}
            onClick={onShowAdjustments}
          >
            {t('projects.form.msProject.adjustments', { count: adjustments })}
          </Button>
        )
      }
      phases={preview.phases.map((phase) => ({
        name: phase.name,
        items: phase.items.map((item) => ({
          kind: item.kind,
          name: item.name,
          isSubtask: item.isSubtask,
          // A milestone, and a task of one day, on its day.
          period:
            item.start === item.end
              ? formatDayText(item.start)
              : formatPeriodText(item.start, item.end),
        })),
      }))}
    />
  );
}
