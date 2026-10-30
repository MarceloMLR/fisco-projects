import { zodResolver } from '@hookform/resolvers/zod';
import AddOutlined from '@mui/icons-material/AddOutlined';
import CloseOutlined from '@mui/icons-material/CloseOutlined';
import UploadFileOutlined from '@mui/icons-material/UploadFileOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import { useQuery } from '@tanstack/react-query';
import { useId, useRef } from 'react';
import { Controller, useFieldArray, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
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
  type AcceptanceMode,
  type AcceptanceValues as Values,
  isAcceptanceError,
  maxSigners,
  sendAcceptance,
} from './acceptances';

/** The fields of the API, as the dialog names them. */
const apiFields = ['milestoneId', 'title', 'message', 'file'] as const;
const fieldErrors = {
  milestoneId: 'acceptances.errors.milestoneRequired',
  title: 'acceptances.errors.titleRequired',
  message: 'acceptances.errors.messageTooLong',
  file: 'acceptances.errors.fileNotPdf',
} as const;

const emptySigner = { name: '', email: '' };

/** The field of a signer in the form, as React Hook Form names the fields of a list. */
const signerField = (index: number, part: 'name' | 'email') =>
  `signers.${String(index)}.${part}` as `signers.${number}.${typeof part}`;

/**
 * "Pedir assinatura", "Anexar termo assinado" without a request and, in a pending request, "Anexar termo assinado"
 * (ADR-0129): the milestone of the schedule, the title, the message of a request, the PDF and the signers by name and
 * e-mail, 1 to 10, inside or outside the company; a term attached without a request has no person, and one attached to
 * a request has only the PDF.
 */
export function AcceptanceDialog({
  projectId,
  mode,
  term,
  onClose,
  onSaved,
}: {
  projectId: string;
  mode: AcceptanceMode;
  /** The pending request, for a signed term attached to it: the person of a request asked before, if any. */
  term?: { id: string; title: string; recipient: string | null };
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
    resolver: zodResolver(acceptanceSchema(mode)),
    defaultValues: {
      milestoneId: '',
      title: '',
      message: '',
      signers: mode === 'request' ? [emptySigner] : [],
      file: null,
    },
  });
  const signers = useFieldArray({ control, name: 'signers' });
  const errorText = (message: string | undefined) =>
    isAcceptanceError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const answer = await sendAcceptance(projectId, mode, { ...values, termId: term?.id }, t);
    if (answer.kind === 'done') {
      onSaved(
        mode === 'request'
          ? t('acceptances.messages.requested', { count: values.signers.length })
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
      if (answer.fields.includes('signers')) {
        setError('root', { message: t('acceptances.errors.signersRequired') });
      }
      return;
    }
    setError('root', { message: answer.message });
  });

  const noMilestones = milestones?.length === 0;
  const listError = invalid.signers?.root?.message ?? invalid.signers?.message;

  return (
    <Dialog
      open
      title={t(
        mode === 'request' ? 'acceptances.dialog.requestTitle' : 'acceptances.dialog.attachTitle',
      )}
      subtitle={
        term === undefined
          ? undefined
          : term.recipient === null
            ? t('acceptances.dialog.onBehalfRequestSubtitle', { title: term.title })
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
              : term.recipient === null
                ? t('acceptances.dialog.onBehalfRequestHelp')
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
        {mode === 'request' && (
          <Box
            component="fieldset"
            aria-describedby={`${ids}-signers-help`}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: 'var(--space-3)',
              margin: 0,
              padding: 0,
              border: 0,
              minWidth: 0,
            }}
          >
            <Box
              component="legend"
              sx={{ padding: 0, fontSize: 13, lineHeight: '20px', fontWeight: 500 }}
            >
              {t('acceptances.dialog.signers')}
            </Box>
            <Box
              component="p"
              id={`${ids}-signers-help`}
              sx={{ margin: 0, fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
            >
              {t('acceptances.dialog.signersHelp')}
            </Box>
            {signers.fields.map((item, index) => {
              const number = index + 1;
              const errors = invalid.signers?.[index];
              return (
                <Box
                  key={item.id}
                  data-testid="acceptance-signer"
                  sx={{
                    display: 'grid',
                    gridTemplateColumns: {
                      xs: 'minmax(0, 1fr) auto',
                      md: 'minmax(0, 1fr) minmax(0, 1.2fr) auto',
                    },
                    alignItems: 'start',
                    gap: 'var(--space-2)',
                  }}
                >
                  <TextField
                    label={t('acceptances.dialog.signerName', { index: number })}
                    hideLabel
                    placeholder={t('acceptances.dialog.name')}
                    autoComplete="off"
                    maxLength={130}
                    error={errorText(errors?.name?.message)}
                    {...register(signerField(index, 'name'))}
                  />
                  <Box
                    sx={{ gridColumn: { xs: '1', md: 'auto' }, gridRow: { xs: '2', md: 'auto' } }}
                  >
                    <TextField
                      label={t('acceptances.dialog.signerEmail', { index: number })}
                      hideLabel
                      placeholder={t('acceptances.dialog.email')}
                      type="email"
                      autoComplete="off"
                      maxLength={260}
                      error={errorText(errors?.email?.message)}
                      {...register(signerField(index, 'email'))}
                    />
                  </Box>
                  <IconButton
                    aria-label={t('acceptances.dialog.removeSigner', { index: number })}
                    disabled={signers.fields.length === 1}
                    onClick={() => {
                      signers.remove(index);
                    }}
                  >
                    <CloseOutlined fontSize="small" />
                  </IconButton>
                </Box>
              );
            })}
            {listError !== undefined && <Alert tone="danger">{errorText(listError)}</Alert>}
            <Button
              variant="text"
              size="small"
              startIcon={<AddOutlined />}
              disabled={signers.fields.length >= maxSigners}
              onClick={() => {
                signers.append(emptySigner);
              }}
              sx={{ alignSelf: 'flex-start' }}
            >
              {t('acceptances.dialog.addSigner')}
            </Button>
          </Box>
        )}
      </Box>
    </Dialog>
  );
}
