import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import Snackbar from '@mui/material/Snackbar';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type TicketDeadlineSetting, ticketPriorities } from '../api/client';
import { queryKeys, ticketDeadlinesQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { TextField } from '../components/TextField';
import { Page } from '../shell/Page';
import { PriorityTag } from '../tickets/TicketTags';
import { Button } from '../components/Button';
import { Form } from '../components/Form';

const days = z.number().int().min(1).max(365);

const form = z.object({
  deadlines: z.array(
    z.object({
      priority: z.enum(ticketPriorities),
      firstResponseDays: days,
      resolutionDays: days,
    }),
  ),
});

type Values = z.infer<typeof form>;

/** A section of the form (fp-form-section) and its title (fp-h2), as in Configurações gerais. */
const sectionSx = {
  display: 'flex',
  flexDirection: 'column',
  gap: 'var(--space-4)',
  maxWidth: 720,
  padding: 'var(--space-6)',
  backgroundColor: 'var(--bg-surface)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-lg)',
} as const;

const rowSx = {
  display: 'grid',
  gridTemplateColumns: 'minmax(120px, 1fr) 160px 160px',
  alignItems: 'start',
  gap: 'var(--space-4)',
  '@media (max-width: 600px)': { gridTemplateColumns: 'minmax(0, 1fr) 1fr 1fr' },
} as const;

const headSx = {
  fontSize: 13,
  lineHeight: '18px',
  fontWeight: 500,
  color: 'var(--text-secondary)',
} as const;

const valuesOf = (settings: readonly TicketDeadlineSetting[]): Values => ({
  deadlines: settings.map(({ priority, firstResponseDays, resolutionDays }) => ({
    priority,
    firstResponseDays,
    resolutionDays,
  })),
});

/**
 * Prazos dos chamados (ADR-0025, ADR-0046): the business days of each priority for the first answer and for the
 * resolution, from 1 to 365, as the Administrator sets them; they count for the tickets opened afterwards. No screen was
 * drawn for it: it follows Configurações gerais, a section of the form and the bar of Salvar at the bottom.
 */
export function TicketDeadlinesPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: settings, isError } = useQuery(ticketDeadlinesQuery());
  const [message, setMessage] = useState<string>();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(form),
    defaultValues: { deadlines: [] },
  });

  useEffect(() => {
    if (settings !== undefined) {
      reset(valuesOf(settings));
    }
  }, [settings, reset]);

  const save = handleSubmit(async (values) => {
    try {
      const { response } = await api.PUT('/api/admin/ticket-deadlines', { body: values });
      if (!response.ok) {
        throw new Error(String(response.status));
      }
      reset(values);
      setMessage(t('ticketDeadlines.saved'));
      void queryClient.invalidateQueries({ queryKey: queryKeys.ticketDeadlines });
    } catch {
      setMessage(t('ticketDeadlines.saveFailed'));
    }
  });

  return (
    <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
      <Page>
        <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          {isError && settings === undefined ? (
            <Alert tone="danger">{t('ticketDeadlines.loadFailed')}</Alert>
          ) : (
            <Form id="ticket-deadlines-form" onSubmit={(event) => save(event)}>
              <Box component="section" aria-labelledby="ticket-deadlines-title" sx={sectionSx}>
                <Box
                  component="h2"
                  id="ticket-deadlines-title"
                  sx={{
                    margin: 0,
                    fontSize: 18,
                    lineHeight: '26px',
                    fontWeight: 500,
                    letterSpacing: '-0.005em',
                  }}
                >
                  {t('ticketDeadlines.section')}
                </Box>
                <Box
                  component="p"
                  sx={{
                    margin: 0,
                    fontSize: 14,
                    lineHeight: '20px',
                    color: 'var(--text-secondary)',
                  }}
                >
                  {t('ticketDeadlines.intro')}
                </Box>
                <Box sx={rowSx} aria-hidden>
                  <Box sx={headSx}>{t('ticketDeadlines.priority')}</Box>
                  <Box sx={headSx}>{t('ticketDeadlines.firstResponse')}</Box>
                  <Box sx={headSx}>{t('ticketDeadlines.resolution')}</Box>
                </Box>
                {(settings ?? []).map((setting, index) => {
                  const priority = t(`tickets.priorities.${setting.priority}`);
                  const rowErrors = errors.deadlines?.[index];
                  return (
                    <Box key={setting.priority} sx={rowSx}>
                      <Box sx={{ minHeight: 40, display: 'flex', alignItems: 'center' }}>
                        <PriorityTag priority={setting.priority} />
                      </Box>
                      <TextField
                        label={t('ticketDeadlines.firstResponseOf', { priority })}
                        hideLabel
                        type="number"
                        inputMode="numeric"
                        min={1}
                        max={365}
                        helper={t('ticketDeadlines.days')}
                        error={
                          rowErrors?.firstResponseDays === undefined
                            ? undefined
                            : t('ticketDeadlines.invalid')
                        }
                        {...register(
                          `deadlines.${String(index)}.firstResponseDays` as `deadlines.${number}.firstResponseDays`,
                          {
                            valueAsNumber: true,
                          },
                        )}
                      />
                      <TextField
                        label={t('ticketDeadlines.resolutionOf', { priority })}
                        hideLabel
                        type="number"
                        inputMode="numeric"
                        min={1}
                        max={365}
                        helper={t('ticketDeadlines.days')}
                        error={
                          rowErrors?.resolutionDays === undefined
                            ? undefined
                            : t('ticketDeadlines.invalid')
                        }
                        {...register(
                          `deadlines.${String(index)}.resolutionDays` as `deadlines.${number}.resolutionDays`,
                          { valueAsNumber: true },
                        )}
                      />
                    </Box>
                  );
                })}
                <Alert tone="info">{t('ticketDeadlines.help')}</Alert>
              </Box>
            </Form>
          )}
        </Box>
      </Page>
      <Box
        sx={{
          flex: 'none',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'flex-end',
          gap: 'var(--space-2)',
          padding: 'var(--space-3) var(--space-6)',
          backgroundColor: 'var(--bg-page)',
          borderTop: '1px solid var(--border)',
        }}
      >
        <Button
          variant="outlined"
          disabled={!isDirty || isSubmitting}
          onClick={() => {
            if (settings !== undefined) {
              reset(valuesOf(settings));
            }
          }}
        >
          {t('ticketDeadlines.discard')}
        </Button>
        <Button
          variant="contained"
          type="submit"
          form="ticket-deadlines-form"
          disabled={!isDirty}
          loading={isSubmitting}
        >
          {t('ticketDeadlines.save')}
        </Button>
      </Box>
      <Snackbar
        open={message !== undefined}
        message={message}
        autoHideDuration={6000}
        onClose={() => {
          setMessage(undefined);
        }}
      />
    </Box>
  );
}
