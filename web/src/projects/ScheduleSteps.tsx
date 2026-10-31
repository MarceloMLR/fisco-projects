import UndoOutlined from '@mui/icons-material/UndoOutlined';
import { useTranslation } from 'react-i18next';
import type { Project, Schedule } from '../api/client';
import { stepText } from './schedule';
import { useScheduleStep } from './useScheduleStep';
import { IconButton } from '../components/Button';

/**
 * "Desfazer" of the bar of the tab and of Expandir (ADR-0071), for who edits the schedule: its tip says what it undoes,
 * and it is off while there is nothing to undo.
 */
export function UndoButton({
  project,
  schedule,
  notify,
}: {
  project: Project;
  schedule: Schedule;
  notify: (message: string) => void;
}) {
  const { t } = useTranslation();
  const { run, pending } = useScheduleStep(project, notify);
  const step = schedule.undo;
  const tip =
    step === null
      ? t('projects.steps.nothingToUndo')
      : t('projects.steps.undoWhat', { what: stepText(step, t) });
  return (
    <span title={tip}>
      <IconButton
        size="small"
        aria-label={t('projects.steps.undo')}
        aria-description={tip}
        disabled={step === null}
        loading={pending}
        onClick={() => run('undo', schedule)}
        sx={{ color: 'var(--text-secondary)' }}
      >
        <UndoOutlined />
      </IconButton>
    </span>
  );
}
