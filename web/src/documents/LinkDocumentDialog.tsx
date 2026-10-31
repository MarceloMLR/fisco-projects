import Box from '@mui/material/Box';
import { useQuery } from '@tanstack/react-query';
import { type SubmitEvent, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type DocumentRow, documentProblems } from '../api/client';
import { projectScheduleQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { FormField } from '../components/FormField';
import { Select } from '../components/Select';
import { problemType } from '../projects/projectForm';
import { Button } from '../components/Button';

const none = '';

/** The value of an option: the phase or the item, told apart by the prefix. */
const phaseValue = (id: string) => `phase:${id}`;
const itemValue = (id: string) => `item:${id}`;

/**
 * "Vincular ao cronograma" (ADR-0031): a phase, a task or a milestone of the schedule of the project, in the order of
 * the schedule, or "Sem vínculo". The list and the details show the current name of the item.
 */
export function LinkDocumentDialog({
  projectId,
  document,
  onClose,
  onSaved,
}: {
  projectId: string;
  document: DocumentRow;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t } = useTranslation();
  const { data: schedule } = useQuery(projectScheduleQuery(projectId));
  const current =
    document.link === null
      ? none
      : document.link.type === 'Phase'
        ? phaseValue(document.link.id)
        : itemValue(document.link.id);
  const [value, setValue] = useState(current);
  const [failure, setFailure] = useState<string>();

  const options = useMemo(
    () => [
      { value: none, label: t('documents.link.none') },
      ...(schedule?.phases ?? []).flatMap((phase) => [
        {
          value: phaseValue(phase.id),
          label: `${phase.name} · ${t('documents.link.types.Phase')}`,
        },
        ...phase.items.map((item) => ({
          value: itemValue(item.id),
          label: `${item.name} · ${t(item.kind === 'Milestone' ? 'documents.link.types.Milestone' : 'documents.link.types.Task')}`,
        })),
      ]),
    ],
    [schedule, t],
  );

  const save = async (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFailure(undefined);
    const phaseId = value.startsWith('phase:') ? value.slice('phase:'.length) : null;
    const itemId = value.startsWith('item:') ? value.slice('item:'.length) : null;
    try {
      const { error, response } = await api.PUT(
        '/api/projects/{projectId}/documents/{documentId}/link',
        {
          params: { path: { projectId, documentId: document.id } },
          body: { phaseId, itemId },
        },
      );
      if (response.ok) {
        onSaved();
        return;
      }
      setFailure(
        problemType(error) === documentProblems.projectClosed
          ? t('documents.messages.closed')
          : t('documents.messages.failed'),
      );
    } catch {
      setFailure(t('documents.messages.failed'));
    }
  };

  return (
    <Dialog
      open
      title={t('documents.link.title')}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('documents.link.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={schedule === undefined}>
            {t('documents.link.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <FormField label={t('documents.link.field')} helper={t('documents.link.help')}>
          <Select
            label={t('documents.link.field')}
            value={value}
            options={options}
            onChange={setValue}
          />
        </FormField>
        {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
      </Box>
    </Dialog>
  );
}
