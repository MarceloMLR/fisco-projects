import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { api, type Project, type StatusReportDraft } from '../api/client';
import { queryKeys, statusReportDraftQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Button } from '../components/Button';
import { Checkbox } from '../components/Checkbox';
import { Dialog } from '../components/Dialog';
import { MultiSelect } from '../components/MultiSelect';
import { SegmentedControl } from '../components/SegmentedControl';
import { TextArea } from '../components/TextArea';
import { documentFileUrl } from '../documents/documents';
import { downloadAddress } from '../format/saveFile';
import { useDates } from '../format/useDates';
import { useNotify } from '../notices/notify';
import { invalidFields } from './projectForm';
import {
  isStatusReportError,
  riskLevels,
  risksAfter,
  statusReportErrors,
  statusReportLimits,
  statusReportSchema,
  ticketLabel,
  ticketOptions,
  type StatusReportValues,
  valuesOf,
} from './statusReport';

type Texts = 'summary' | 'focus' | 'nextSteps' | 'clientNeeds';

const texts: readonly Texts[] = ['summary', 'focus', 'nextSteps', 'clientNeeds'];

/** The path of a field of a risk in the form. */
const riskField = <Part extends 'level' | 'attention'>(index: number, part: Part) =>
  `risks.${String(index)}.${part}` as `risks.${number}.${Part}`;

/**
 * "Gerar relatório de status" (ADR-0165, ADR-0166): the draft of the report, read each time the dialog opens, with the
 * texts and the risks of the last report; on "Gerar relatório", the file is generated, kept in the folder of the
 * status reports and downloaded.
 */
export function StatusReportDialog({
  project,
  onClose,
}: {
  project: Pick<Project, 'id' | 'code' | 'name'>;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const { data: draft, isError } = useQuery(statusReportDraftQuery(project.id));
  if (draft !== undefined) {
    return <StatusReportForm project={project} draft={draft} onClose={onClose} />;
  }

  // The draft is read ahead when the actions of the project open, so the dialog opens with it; only a failure shows.
  return isError ? (
    <Dialog
      open
      size="lg"
      title={t('projects.statusReport.title')}
      onClose={onClose}
      actions={
        <Button variant="outlined" onClick={onClose}>
          {t('projects.statusReport.cancel')}
        </Button>
      }
    >
      <Alert tone="danger">{t('projects.statusReport.loadFailed')}</Alert>
    </Dialog>
  ) : null;
}

function StatusReportForm({
  project,
  draft,
  onClose,
}: {
  project: Pick<Project, 'id' | 'code' | 'name'>;
  draft: StatusReportDraft;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const { formatDate } = useDates();
  const notify = useNotify();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const {
    control,
    handleSubmit,
    setValue,
    setError,
    formState: { errors },
  } = useForm<StatusReportValues>({
    resolver: zodResolver(statusReportSchema),
    defaultValues: valuesOf(draft),
  });
  const risks = useWatch({ control, name: 'risks' });
  const titles = new Map(draft.tickets.map((ticket) => [ticket.number, ticket]));
  const errorText = (message: string | undefined) =>
    isStatusReportError(message) ? t(message) : undefined;

  const generate = handleSubmit(async (values) => {
    try {
      const { data, error, response } = await api.POST('/api/projects/{id}/status-report', {
        params: { path: { id: project.id } },
        body: values,
      });
      if (data !== undefined) {
        downloadAddress(documentFileUrl(project.id, data.documentId, { download: true }));
        void queryClient.invalidateQueries({ queryKey: queryKeys.documents });
        void queryClient.invalidateQueries({
          queryKey: statusReportDraftQuery(project.id).queryKey,
        });
        notify(t('projects.statusReport.saved', { folder: data.folderName }), {
          action: {
            label: t('projects.statusReport.openDocuments'),
            onClick: () => {
              void navigate(`/projects/${project.id}/documents?folder=${data.folderId}`);
            },
          },
        });
        onClose();
        return;
      }
      const fields = response.status === 400 ? invalidFields(error) : [];
      for (const field of fields) {
        if (field === 'risks') {
          setError('risks', { message: statusReportErrors.risks });
        } else if ((texts as readonly string[]).includes(field)) {
          setError(field as Texts, { message: statusReportErrors.textTooLong });
        }
      }
      if (fields.length === 0) {
        setError('root', { message: t('projects.statusReport.failed') });
      }
    } catch {
      setError('root', { message: t('projects.statusReport.failed') });
    }
  });

  return (
    <Dialog
      open
      size="lg"
      title={t('projects.statusReport.title')}
      onClose={onClose}
      onSubmit={(event) => generate(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.statusReport.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t('projects.statusReport.generate')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Box component="p" sx={{ margin: 0, fontSize: 14, lineHeight: '20px' }}>
          {t('projects.statusReport.intro', { code: project.code })}
        </Box>
        {draft.last !== null && (
          <Box
            component="p"
            sx={{ margin: 0, fontSize: 14, lineHeight: '20px', color: 'var(--text-secondary)' }}
          >
            {t('projects.statusReport.last', {
              day: formatDate(draft.last.generatedAt),
              name: draft.last.generatedBy.name,
            })}
          </Box>
        )}
        {texts.map((name) => (
          <Controller
            key={name}
            control={control}
            name={name}
            render={({ field }) => (
              <TextArea
                label={t(`projects.statusReport.${name}`)}
                rows={3}
                maxLength={statusReportLimits.text}
                helper={t('projects.statusReport.textHelp', {
                  count: statusReportLimits.text - field.value.length,
                })}
                error={errorText(errors[name]?.message)}
                {...field}
              />
            )}
          />
        ))}
        <MultiSelect
          label={t('projects.statusReport.risks')}
          value={risks.map((risk) => String(risk.number))}
          options={ticketOptions(draft.tickets, {
            threatens: t('projects.statusReport.threatens'),
            others: t('projects.statusReport.others'),
          })}
          onChange={(chosen) => {
            setValue('risks', risksAfter(risks, chosen), {
              shouldValidate: errors.risks !== undefined,
            });
          }}
          noOptionsText={t('projects.statusReport.noTickets')}
          helper={t('projects.statusReport.risksHelp')}
          error={errorText(errors.risks?.message ?? errors.risks?.root?.message)}
        />
        {risks.map((risk, index) => {
          const ticket = titles.get(risk.number);
          return (
            <Box
              key={risk.number}
              component="section"
              aria-label={ticket === undefined ? String(risk.number) : ticketLabel(ticket)}
              sx={{
                display: 'flex',
                flexDirection: 'column',
                gap: 'var(--space-3)',
                padding: 'var(--space-3) var(--space-4)',
                border: '1px solid var(--border)',
                borderRadius: 'var(--radius-md)',
              }}
            >
              <Box
                component="h3"
                sx={{ margin: 0, fontSize: 14, lineHeight: '20px', fontWeight: 600 }}
              >
                {ticket === undefined ? `#${String(risk.number)}` : ticketLabel(ticket)}
              </Box>
              <Controller
                control={control}
                name={riskField(index, 'level')}
                render={({ field }) => (
                  <SegmentedControl
                    label={t('projects.statusReport.level')}
                    value={field.value}
                    options={riskLevels.map((level) => ({
                      value: level,
                      label: t(`projects.statusReport.levels.${level}`),
                    }))}
                    onChange={field.onChange}
                  />
                )}
              />
              <Controller
                control={control}
                name={riskField(index, 'attention')}
                render={({ field }) => (
                  <TextArea
                    label={t('projects.statusReport.attention')}
                    rows={2}
                    maxLength={statusReportLimits.attention}
                    helper={t('projects.statusReport.textHelp', {
                      count: statusReportLimits.attention - field.value.length,
                    })}
                    error={errorText(errors.risks?.[index]?.attention?.message)}
                    {...field}
                  />
                )}
              />
            </Box>
          );
        })}
        <Controller
          control={control}
          name="showHours"
          render={({ field }) => (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}>
              <Checkbox
                label={t('projects.statusReport.showHours')}
                checked={field.value}
                onChange={field.onChange}
              />
              <Box
                component="span"
                sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
              >
                {t('projects.statusReport.showHoursHelp')}
              </Box>
            </Box>
          )}
        />
        {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
      </Box>
    </Dialog>
  );
}
