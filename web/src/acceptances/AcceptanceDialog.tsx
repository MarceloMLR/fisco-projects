import { zodResolver } from '@hookform/resolvers/zod';
import UploadFileOutlined from '@mui/icons-material/UploadFileOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useQuery } from '@tanstack/react-query';
import { useId, useRef } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import type { ProjectPerson } from '../api/client';
import { acceptanceMilestonesQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { FormField } from '../components/FormField';
import { Select } from '../components/Select';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { formatDayText } from '../format/dates';
import { sizeText } from '../format/size';
import {
  acceptanceSchema,
  type AcceptanceValues as Values,
  isAcceptanceError,
  sendAcceptance,
} from './acceptances';

/** The fields of the API, as the dialog names them. */
const apiFields = ['milestoneId', 'recipientId', 'title', 'message', 'file'] as const;
const fieldErrors = {
  milestoneId: 'acceptances.errors.milestoneRequired',
  recipientId: 'acceptances.errors.recipientRequired',
  title: 'acceptances.errors.titleRequired',
  message: 'acceptances.errors.messageTooLong',
  file: 'acceptances.errors.fileNotPdf',
} as const;

/**
 * "Pedir aceite", "Anexar termo assinado" without a request and, in a pending request, "Anexar termo assinado" in the
 * name of its person (canvas ProjetoTermos, ADR-0035, ADR-0095): the milestone of the schedule, the person of the
 * project, the title, the message of a request and the PDF; in the name of the person, only the PDF.
 */
export function AcceptanceDialog({
  projectId,
  mode,
  members,
  term,
  onClose,
  onSaved,
}: {
  projectId: string;
  mode: 'request' | 'attach' | 'onBehalf';
  /** The people of the project, any of them the person of a term. */
  members: readonly ProjectPerson[];
  /** The pending request, for a term attached in the name of its person. */
  term?: { id: string; title: string; recipient: string };
  onClose: () => void;
  onSaved: (message: string, id: string | undefined) => void;
}) {
  const { t } = useTranslation();
  const ids = useId();
  const input = useRef<HTMLInputElement>(null);
  const onBehalf = mode === 'onBehalf';
  const { data: milestones, isError } = useQuery({
    ...acceptanceMilestonesQuery(projectId),
    enabled: !onBehalf,
  });
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors: invalid, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(acceptanceSchema(onBehalf)),
    defaultValues: { milestoneId: '', recipientId: '', title: '', message: '', file: null },
  });
  const errorText = (message: string | undefined) =>
    isAcceptanceError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const answer = await sendAcceptance(
      projectId,
      mode === 'request' ? 'request' : mode === 'attach' ? 'attach' : 'onBehalf',
      { ...values, termId: term?.id },
      t,
    );
    if (answer.kind === 'done') {
      const recipient = members.find((member) => member.id === values.recipientId)?.name ?? '';
      onSaved(
        mode === 'request'
          ? t('acceptances.messages.requested', { name: recipient })
          : t('acceptances.messages.attached'),
        answer.id,
      );
      return;
    }
    if (answer.kind === 'fields') {
      for (const field of apiFields) {
        if (answer.fields.includes(field)) {
          setError(field, { message: fieldErrors[field] });
        }
      }
      return;
    }
    setError('root', { message: answer.message });
  });

  const noMilestones = milestones?.length === 0;
  const people = [...members].sort((one, other) => one.name.localeCompare(other.name, 'pt-BR'));

  return (
    <Dialog
      open
      title={t(
        mode === 'request' ? 'acceptances.dialog.requestTitle' : 'acceptances.dialog.attachTitle',
      )}
      subtitle={
        term === undefined
          ? undefined
          : t('acceptances.dialog.onBehalfSubtitle', { name: term.recipient })
      }
      onClose={onClose}
      onSubmit={(event) => {
        void save(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('acceptances.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting || noMilestones}>
            {t(
              mode === 'request'
                ? 'acceptances.dialog.submitRequest'
                : 'acceptances.dialog.submitAttach',
            )}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        {invalid.root?.message !== undefined && <Alert tone="danger">{invalid.root.message}</Alert>}
        {isError && <Alert tone="danger">{t('acceptances.messages.failed')}</Alert>}
        {noMilestones && <Alert tone="warning">{t('acceptances.dialog.noMilestones')}</Alert>}
        {mode !== 'request' && (
          <Box component="p" sx={{ margin: 0, color: 'var(--text-secondary)' }}>
            {term === undefined
              ? t('acceptances.dialog.attachHelp')
              : t('acceptances.dialog.onBehalfHelp', { name: term.recipient })}
          </Box>
        )}
        {!onBehalf && (
          <>
            <FormField
              label={t('acceptances.dialog.milestone')}
              error={errorText(invalid.milestoneId?.message)}
              helpId={`${ids}-milestone-help`}
            >
              <Controller
                control={control}
                name="milestoneId"
                render={({ field }) => (
                  <Select<string>
                    label={t('acceptances.dialog.milestone')}
                    value={field.value}
                    options={[
                      { value: '', label: t('acceptances.dialog.chooseMilestone'), disabled: true },
                      ...(milestones ?? []).map((milestone) => ({
                        value: milestone.id,
                        label:
                          milestone.day === null
                            ? milestone.name
                            : t('acceptances.milestone', {
                                name: milestone.name,
                                day: formatDayText(milestone.day),
                              }),
                      })),
                    ]}
                    invalid={invalid.milestoneId !== undefined}
                    describedBy={`${ids}-milestone-help`}
                    onChange={field.onChange}
                  />
                )}
              />
            </FormField>
            <FormField
              label={t(
                mode === 'request'
                  ? 'acceptances.dialog.recipient'
                  : 'acceptances.dialog.recipientAttached',
              )}
              error={errorText(invalid.recipientId?.message)}
              helper={t('acceptances.dialog.recipientHelp')}
              helpId={`${ids}-recipient-help`}
            >
              <Controller
                control={control}
                name="recipientId"
                render={({ field }) => (
                  <Select<string>
                    label={t(
                      mode === 'request'
                        ? 'acceptances.dialog.recipient'
                        : 'acceptances.dialog.recipientAttached',
                    )}
                    value={field.value}
                    options={[
                      { value: '', label: t('acceptances.dialog.chooseRecipient'), disabled: true },
                      ...people.map((member) => ({ value: member.id, label: member.name })),
                    ]}
                    invalid={invalid.recipientId !== undefined}
                    describedBy={`${ids}-recipient-help`}
                    onChange={field.onChange}
                  />
                )}
              />
            </FormField>
            <TextField
              label={t('acceptances.dialog.title')}
              autoComplete="off"
              maxLength={130}
              helper={t('acceptances.dialog.titleHelp')}
              error={errorText(invalid.title?.message)}
              {...register('title')}
            />
            {mode === 'request' && (
              <TextArea
                label={t('acceptances.dialog.message')}
                rows={3}
                maxLength={2010}
                helper={t('acceptances.dialog.messageHelp')}
                error={errorText(invalid.message?.message)}
                {...register('message')}
              />
            )}
          </>
        )}
        <Controller
          control={control}
          name="file"
          render={({ field }) => (
            <FormField
              label={t(
                mode === 'request' ? 'acceptances.dialog.file' : 'acceptances.dialog.signedFile',
              )}
              error={errorText(invalid.file?.message)}
              helper={t('acceptances.dialog.fileHelp')}
              helpId={`${ids}-file-help`}
            >
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 'var(--space-3)',
                  flexWrap: 'wrap',
                }}
              >
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={<UploadFileOutlined />}
                  aria-describedby={`${ids}-file-help`}
                  onClick={() => input.current?.click()}
                >
                  {t(
                    field.value === null
                      ? 'acceptances.actions.choose'
                      : 'acceptances.actions.change',
                  )}
                </Button>
                <Box
                  component="span"
                  data-testid="acceptance-file-name"
                  sx={{
                    minWidth: 0,
                    overflowWrap: 'anywhere',
                    color: field.value === null ? 'var(--text-secondary)' : 'var(--text)',
                  }}
                >
                  {field.value === null
                    ? t('acceptances.dialog.noFile')
                    : `${field.value.name} · ${sizeText(field.value.size)}`}
                </Box>
              </Box>
              <input
                ref={input}
                type="file"
                accept=".pdf,application/pdf"
                hidden
                aria-hidden
                tabIndex={-1}
                data-testid="acceptance-file"
                onChange={(event) => {
                  const picked = event.target.files?.[0];
                  event.target.value = '';
                  if (picked !== undefined) {
                    field.onChange(picked);
                  }
                }}
              />
            </FormField>
          )}
        />
      </Box>
    </Dialog>
  );
}
