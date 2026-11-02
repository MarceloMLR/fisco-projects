import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api } from '../api/client';
import { generalRulesQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { TextField } from '../components/TextField';
import { Page } from '../shell/Page';
import { hoursFieldText, parseHoursUpTo } from '../hours/hours';
import { EmailSettings } from './EmailSettings';
import { TeamsSettings } from './TeamsSettings';
import { TimeZoneField } from './TimeZoneField';
import { Button } from '../components/Button';
import { Form } from '../components/Form';
import { useNotify } from '../notices/notify';

const fieldErrors = {
  timeZoneRequired: 'settings.errors.timeZoneRequired',
  timeZoneInvalid: 'settings.errors.timeZoneInvalid',
  dailySummaryRequired: 'settings.errors.dailySummaryRequired',
  retentionRange: 'settings.errors.retentionRange',
  auditRetentionRange: 'settings.errors.auditRetentionRange',
  workHoursRange: 'settings.errors.workHoursRange',
} as const;

type FieldError = (typeof fieldErrors)[keyof typeof fieldErrors];

/** The most hours of a working day, as the API takes them (ADR-0142). */
const maxWorkHoursPerDay = 12;

const form = z.object({
  timeZone: z.string().min(1, { error: fieldErrors.timeZoneRequired }),
  // Hours and minutes, in the time zone of the installation (ADR-0037).
  dailySummaryAt: z.string().regex(/^\d{2}:\d{2}$/, { error: fieldErrors.dailySummaryRequired }),
  // From 1 to 365 days (ADR-0038).
  notificationRetentionDays: z.coerce
    .number({ error: fieldErrors.retentionRange })
    .int({ error: fieldErrors.retentionRange })
    .min(1, { error: fieldErrors.retentionRange })
    .max(365, { error: fieldErrors.retentionRange }),
  // From 1 to 10 years; the monthly cleanup removes what is older (ADR-0058, ADR-0110).
  auditRetentionYears: z.coerce
    .number({ error: fieldErrors.auditRetentionRange })
    .int({ error: fieldErrors.auditRetentionRange })
    .min(1, { error: fieldErrors.auditRetentionRange })
    .max(10, { error: fieldErrors.auditRetentionRange }),
  // From 1 to 12 hours, in quarters, typed as "8" or "7,5", for the capacity of the load of the team (ADR-0142).
  workHoursPerDay: z
    .string()
    .transform((value) => parseHoursUpTo(value, maxWorkHoursPerDay))
    .refine((value) => value !== undefined && value >= 1, { error: fieldErrors.workHoursRange })
    .transform((value) => value ?? 0),
});

// Below 700 px the fields of a row stack (fp-form-row).
const narrowForm = '@media (max-width: 700px)';

/** A section of the form (fp-form-section) and its title (fp-h2). */
const sectionSx = {
  display: 'flex',
  flexDirection: 'column',
  gap: 'var(--space-4)',
  padding: 'var(--space-6)',
  backgroundColor: 'var(--bg-surface)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-lg)',
} as const;

const sectionTitleSx = {
  margin: 0,
  fontSize: 18,
  lineHeight: '26px',
  fontWeight: 500,
  letterSpacing: '-0.005em',
  color: 'var(--text)',
} as const;

/** The fields a validation problem of the API names (RFC 9457 with the errors of ASP.NET Core). */
function invalidFields(problem: unknown): string[] {
  return typeof problem === 'object' &&
    problem !== null &&
    'errors' in problem &&
    typeof problem.errors === 'object' &&
    problem.errors !== null
    ? Object.keys(problem.errors)
    : [];
}

/**
 * General settings of the Administration (ADR-0150): only what is general to the installation, in one column. The
 * General rules, with Discard and Save changes at the bottom (fp-actionbar), and Microsoft 365, with the test of the
 * e-mails and Teams, which do not wait for Save. The brand is in Visual and the folder of the projects in Default
 * folders. Only the Administrator reaches it.
 */
export function SettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  // The shell already read the rules, before showing any screen.
  const { data: rules, isError } = useQuery(generalRulesQuery());
  const notify = useNotify();

  const {
    register,
    handleSubmit,
    reset,
    setError,
    control,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<z.input<typeof form>, unknown, z.output<typeof form>>({
    resolver: zodResolver(form),
    defaultValues: {
      timeZone: '',
      dailySummaryAt: '',
      notificationRetentionDays: '',
      auditRetentionYears: '',
      workHoursPerDay: '',
    },
    values:
      rules === undefined
        ? undefined
        : {
            timeZone: rules.timeZone,
            dailySummaryAt: rules.dailySummaryAt.slice(0, 5),
            notificationRetentionDays: String(rules.notificationRetentionDays),
            auditRetentionYears: String(rules.auditRetentionYears),
            workHoursPerDay: hoursFieldText(rules.workHoursPerDay),
          },
  });

  const save = handleSubmit(
    async ({
      timeZone,
      dailySummaryAt,
      notificationRetentionDays,
      auditRetentionYears,
      workHoursPerDay,
    }) => {
      try {
        const { error, response } = await api.PUT('/api/installation', {
          body: {
            timeZone,
            dailySummaryAt: `${dailySummaryAt}:00`,
            notificationRetentionDays,
            auditRetentionYears,
            workHoursPerDay,
          },
        });
        if (response.status === 400) {
          // The time zone only the server can check.
          const invalid = invalidFields(error);
          if (invalid.includes('dailySummaryAt')) {
            setError('dailySummaryAt', { message: fieldErrors.dailySummaryRequired });
          }
          if (invalid.includes('notificationRetentionDays')) {
            setError('notificationRetentionDays', { message: fieldErrors.retentionRange });
          }
          if (invalid.includes('auditRetentionYears')) {
            setError('auditRetentionYears', { message: fieldErrors.auditRetentionRange });
          }
          if (invalid.includes('workHoursPerDay')) {
            setError('workHoursPerDay', { message: fieldErrors.workHoursRange });
          }
          if (invalid.includes('timeZone') || invalid.length === 0) {
            setError('timeZone', { message: fieldErrors.timeZoneInvalid });
          }
          return;
        }
        if (!response.ok) {
          throw new Error(`The API answered ${String(response.status)}.`);
        }
        notify(t('settings.saved'));
      } catch {
        notify(t('settings.saveFailed'), { tone: 'error' });
      } finally {
        await Promise.all([
          queryClient.invalidateQueries({ queryKey: queryKeys.installation }),
          queryClient.invalidateQueries({ queryKey: queryKeys.audit }),
        ]);
      }
    },
  );

  const timeZoneError = errors.timeZone?.message as FieldError | undefined;
  const summaryError = errors.dailySummaryAt?.message as FieldError | undefined;
  const retentionError = errors.notificationRetentionDays?.message as FieldError | undefined;
  const auditRetentionError = errors.auditRetentionYears?.message as FieldError | undefined;
  const workHoursError = errors.workHoursPerDay?.message as FieldError | undefined;

  return (
    <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
      <Page>
        <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          {isError && rules === undefined ? (
            <Alert tone="danger">{t('settings.loadFailed')}</Alert>
          ) : (
            <Box
              sx={{
                display: 'flex',
                flexDirection: 'column',
                gap: 'var(--space-5)',
                maxWidth: 880,
                minWidth: 0,
              }}
            >
              <Form id="settings-form" onSubmit={(event) => save(event)} sx={{ minWidth: 0 }}>
                <Box component="section" aria-labelledby="settings-rules" sx={sectionSx}>
                  <Box component="h2" id="settings-rules" sx={sectionTitleSx}>
                    {t('settings.rules')}
                  </Box>
                  {/* The names of the time zones are long: the field takes more than half of the row. */}
                  <Box sx={{ maxWidth: 520 }}>
                    <Controller
                      control={control}
                      name="timeZone"
                      render={({ field }) => (
                        <TimeZoneField
                          label={t('settings.timeZone')}
                          value={field.value}
                          onChange={field.onChange}
                          onBlur={field.onBlur}
                          error={timeZoneError === undefined ? undefined : t(timeZoneError)}
                        />
                      )}
                    />
                  </Box>
                  <Box
                    sx={{
                      display: 'grid',
                      gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
                      gap: 'var(--space-4)',
                      [narrowForm]: { gridTemplateColumns: 'minmax(0, 1fr)' },
                    }}
                  >
                    <TextField
                      label={t('settings.dailySummaryAt')}
                      type="time"
                      step={60}
                      helper={t('settings.dailySummaryHint')}
                      error={summaryError === undefined ? undefined : t(summaryError)}
                      {...register('dailySummaryAt')}
                    />
                    <TextField
                      label={t('settings.notificationRetention')}
                      type="number"
                      inputMode="numeric"
                      min={1}
                      max={365}
                      helper={t('settings.notificationRetentionHint')}
                      error={retentionError === undefined ? undefined : t(retentionError)}
                      {...register('notificationRetentionDays')}
                    />
                    <TextField
                      label={t('settings.auditRetention')}
                      type="number"
                      inputMode="numeric"
                      min={1}
                      max={10}
                      helper={t('settings.auditRetentionHint')}
                      error={auditRetentionError === undefined ? undefined : t(auditRetentionError)}
                      {...register('auditRetentionYears')}
                    />
                    <TextField
                      label={t('settings.workHoursPerDay')}
                      inputMode="decimal"
                      autoComplete="off"
                      helper={t('settings.workHoursPerDayHint')}
                      error={workHoursError === undefined ? undefined : t(workHoursError)}
                      {...register('workHoursPerDay')}
                    />
                  </Box>
                </Box>
              </Form>
              <Box component="section" aria-labelledby="settings-m365" sx={sectionSx}>
                <Box component="h2" id="settings-m365" sx={sectionTitleSx}>
                  {t('settings.m365')}
                </Box>
                <EmailSettings />
                <TeamsSettings />
              </Box>
            </Box>
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
            reset();
          }}
        >
          {t('settings.discard')}
        </Button>
        <Button variant="contained" type="submit" form="settings-form" disabled={!isDirty}>
          {t('settings.save')}
        </Button>
      </Box>
    </Box>
  );
}
