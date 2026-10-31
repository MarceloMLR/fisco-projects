import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type CalendarDay, calendarYears } from '../api/client';
import { queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Checkbox } from '../components/Checkbox';
import { Dialog } from '../components/Dialog';
import { TextField } from '../components/TextField';
import { maskTypedDay, parseTypedDay } from '../format/dates';
import { useDates } from '../format/useDates';
import { Button } from '../components/Button';

const fieldErrors = {
  nameRequired: 'calendar.errors.nameRequired',
  nameTooLong: 'calendar.errors.nameTooLong',
  dateRequired: 'calendar.errors.dateRequired',
  dateInvalid: 'calendar.errors.dateInvalid',
  dateOutOfRange: 'calendar.errors.dateOutOfRange',
} as const;

type FieldError = (typeof fieldErrors)[keyof typeof fieldErrors];

const yearOf = (day: string) => Number(day.slice(0, 4));

// The name as the API keeps it (up to 80 characters, without the spaces at the ends) and the date typed as
// dd/mm/aaaa, in the years of the calendar (ADR-0083).
const form = z.object({
  name: z
    .string()
    .trim()
    .min(1, { error: fieldErrors.nameRequired })
    .max(80, { error: fieldErrors.nameTooLong }),
  date: z
    .string()
    .trim()
    .min(1, { error: fieldErrors.dateRequired })
    .refine((value) => parseTypedDay(value) !== undefined, { error: fieldErrors.dateInvalid })
    .refine(
      (value) => {
        const day = parseTypedDay(value);
        return (
          day === undefined ||
          (yearOf(day) >= calendarYears.first && yearOf(day) <= calendarYears.last)
        );
      },
      { error: fieldErrors.dateOutOfRange },
    ),
  repeatsEveryYear: z.boolean(),
});

type FormValues = z.input<typeof form>;

const isFieldError = (message: string | undefined): message is FieldError =>
  Object.values(fieldErrors).some((error) => error === message);

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
 * "Novo feriado" and "Editar feriado" (canvas AdminCalendarioNovoFeriado): the name, the date typed as dd/mm/aaaa
 * and "Repete todo ano". Editing keeps whether the holiday is considered. After saving, the screen shows the year of
 * the date.
 */
export function HolidayDialog({
  holiday,
  onClose,
  onSaved,
}: {
  /** The local holiday being edited; none for a new one. */
  holiday?: CalendarDay;
  onClose: () => void;
  onSaved: (message: string, year: number) => void;
}) {
  const { t } = useTranslation();
  const { formatDayValue } = useDates();
  const queryClient = useQueryClient();
  const editing = holiday !== undefined;

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(form),
    defaultValues: {
      name: holiday?.name ?? '',
      date: holiday === undefined ? '' : formatDayValue(holiday.date),
      repeatsEveryYear: holiday?.repeatsEveryYear ?? false,
    },
  });

  const errorText = (message: string | undefined) =>
    isFieldError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const name = values.name.trim();
    const date = parseTypedDay(values.date) ?? '';
    const body = { name, date, repeatsEveryYear: values.repeatsEveryYear };
    try {
      const { response, error } =
        holiday?.id === null || holiday?.id === undefined
          ? await api.POST('/api/calendar/local-holidays', { body })
          : await api.PUT('/api/calendar/local-holidays/{id}', {
              params: { path: { id: holiday.id } },
              body: { ...body, isConsidered: holiday.isConsidered },
            });
      if (response.ok) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.calendar });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
        onSaved(
          t(editing ? 'calendar.messages.changed' : 'calendar.messages.included', { name }),
          yearOf(date),
        );
        return;
      }
      const fields = invalidFields(error);
      if (fields.includes('name')) {
        setError('name', { message: fieldErrors.nameRequired });
      }
      if (fields.includes('date')) {
        setError('date', { message: fieldErrors.dateInvalid });
      }
      if (fields.length === 0) {
        setError('root', { message: t('calendar.messages.failed') });
      }
    } catch {
      setError('root', { message: t('calendar.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t(editing ? 'calendar.editHoliday' : 'calendar.newHoliday')}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('calendar.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t(editing ? 'calendar.save' : 'calendar.include')}
          </Button>
        </>
      }
    >
      <TextField
        label={t('calendar.name')}
        autoComplete="off"
        maxLength={120}
        error={errorText(errors.name?.message)}
        {...register('name')}
      />
      <Controller
        control={control}
        name="date"
        render={({ field }) => (
          <TextField
            label={t('calendar.date')}
            placeholder={t('calendar.datePlaceholder')}
            inputMode="numeric"
            autoComplete="off"
            maxLength={10}
            width={200}
            error={errorText(errors.date?.message)}
            name={field.name}
            ref={field.ref}
            value={field.value}
            onBlur={field.onBlur}
            onChange={(event) => {
              field.onChange(maskTypedDay(event.target.value));
            }}
          />
        )}
      />
      <Controller
        control={control}
        name="repeatsEveryYear"
        render={({ field }) => (
          <Checkbox
            label={t('calendar.repeatsEveryYear')}
            checked={field.value}
            onChange={field.onChange}
          />
        )}
      />
      {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
    </Dialog>
  );
}
