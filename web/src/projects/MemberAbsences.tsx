import AddOutlined from '@mui/icons-material/AddOutlined';
import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import Box from '@mui/material/Box';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useId, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type MemberAbsence, projectProblems } from '../api/client';
import { memberAbsencesQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Button, IconButton } from '../components/Button';
import { Dialog } from '../components/Dialog';
import { TextField } from '../components/TextField';
import { maskTypedDay, parseTypedDay } from '../format/dates';
import { absenceDays, absenceText, maxAbsenceDays } from './absences';
import { problemType } from './projectForm';

const errors = {
  startInvalid: 'projects.absences.errors.startInvalid',
  endInvalid: 'projects.absences.errors.endInvalid',
  tooLong: 'projects.absences.errors.tooLong',
} as const;

type AbsenceError = (typeof errors)[keyof typeof errors];
const isAbsenceError = (message: string | undefined): message is AbsenceError =>
  Object.values(errors).some((error) => error === message);

const schema = z
  .object({
    start: z.string().refine((value) => parseTypedDay(value) !== undefined, errors.startInvalid),
    end: z.string().refine((value) => parseTypedDay(value) !== undefined, errors.endInvalid),
  })
  .superRefine((values, context) => {
    const start = parseTypedDay(values.start);
    const end = parseTypedDay(values.end);
    if (start === undefined || end === undefined) {
      return;
    }
    if (end < start) {
      context.addIssue({ code: 'custom', path: ['end'], message: errors.endInvalid });
    } else if (absenceDays(start, end) > maxAbsenceDays) {
      context.addIssue({ code: 'custom', path: ['end'], message: errors.tooLong });
    }
  });

type Values = z.input<typeof schema>;

/**
 * "Incluir ausência" (ADR-0141): the first and the last day typed as dd/mm/aaaa, from 1 to 366 calendar days, never over
 * another absence of the person.
 */
function AbsenceDialog({
  projectId,
  person,
  onClose,
  onSaved,
}: {
  projectId: string;
  person: { id: string; name: string };
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors },
  } = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { start: '', end: '' } });
  const errorText = (message: string | undefined) =>
    isAbsenceError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const start = parseTypedDay(values.start) ?? '';
    const end = parseTypedDay(values.end) ?? '';
    try {
      const { error, response } = await api.POST('/api/projects/{id}/members/{userId}/absences', {
        params: { path: { id: projectId, userId: person.id } },
        body: { start, end },
      });
      if (response.ok) {
        onSaved(
          t('projects.absences.messages.added', {
            name: person.name,
            period: absenceText({ start, end }, t),
          }),
        );
        return;
      }
      if (response.status === 409 && problemType(error) === projectProblems.absenceOverlap) {
        setError('root', { message: t('projects.absences.errors.overlap') });
        return;
      }
      if (response.status === 400) {
        setError('end', { message: errors.endInvalid });
        return;
      }
      setError('root', {
        message: t(
          response.status === 409 && problemType(error) === projectProblems.closed
            ? 'projects.schedule.messages.closed'
            : 'projects.absences.messages.failed',
        ),
      });
    } catch {
      setError('root', { message: t('projects.absences.messages.failed') });
    }
  });

  const dayField = (name: 'start' | 'end', label: string) => (
    <Controller
      control={control}
      name={name}
      render={({ field }) => (
        <TextField
          label={label}
          placeholder={t('projects.schedule.form.dayPlaceholder')}
          inputMode="numeric"
          autoComplete="off"
          error={errorText(fieldErrors[name]?.message)}
          {...field}
          onChange={(event) => {
            field.onChange(maskTypedDay(event.target.value));
          }}
        />
      )}
    />
  );

  return (
    <Dialog
      open
      title={t('projects.absences.dialog.title')}
      subtitle={person.name}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.absences.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t('projects.absences.dialog.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
            gap: 'var(--space-4)',
            alignItems: 'start',
          }}
        >
          {dayField('start', t('projects.absences.dialog.start'))}
          {dayField('end', t('projects.absences.dialog.end'))}
        </Box>
        <Box
          component="p"
          sx={{ margin: 0, fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
        >
          {t('projects.absences.dialog.help')}
        </Box>
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}

/**
 * The section "Ausências" of the details of a person in Members (ADR-0141): the absences kept, the latest end first,
 * which the people of the project see; for the Administrator and the manager of the open project, "Incluir ausência" and
 * the removal of each one, confirmed.
 */
export function MemberAbsences({
  projectId,
  member,
  onDone,
}: {
  projectId: string;
  member: { id: string; name: string };
  onDone: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const headingId = useId();
  const { data, isError } = useQuery(memberAbsencesQuery(projectId, member.id));
  const [adding, setAdding] = useState(false);
  const [removing, setRemoving] = useState<MemberAbsence>();
  const [failure, setFailure] = useState<string>();
  const absences = data?.absences ?? [];
  const canManage = data?.canManage === true;

  const refreshed = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
    void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
  };

  const remove = async (absence: MemberAbsence) => {
    try {
      const { response } = await api.DELETE(
        '/api/projects/{id}/members/{userId}/absences/{absenceId}',
        { params: { path: { id: projectId, userId: member.id, absenceId: absence.id } } },
      );
      setRemoving(undefined);
      if (response.ok) {
        setFailure(undefined);
        refreshed();
        onDone(
          t('projects.absences.messages.removed', {
            name: member.name,
            period: absenceText(absence, t),
          }),
        );
        return;
      }
      setFailure(t('projects.absences.messages.failed'));
    } catch {
      setRemoving(undefined);
      setFailure(t('projects.absences.messages.failed'));
    }
  };

  return (
    <Box
      component="section"
      aria-labelledby={headingId}
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 'var(--space-3)',
        }}
      >
        <Box
          component="h3"
          id={headingId}
          sx={{ margin: 0, fontSize: 13, lineHeight: '18px', fontWeight: 500 }}
        >
          {t('projects.absences.title')}
        </Box>
        {canManage && (
          <Button
            variant="text"
            size="small"
            startIcon={<AddOutlined />}
            onClick={() => {
              setAdding(true);
            }}
          >
            {t('projects.absences.add')}
          </Button>
        )}
      </Box>
      {isError && <Alert tone="danger">{t('projects.absences.loadFailed')}</Alert>}
      {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
      {data !== undefined && absences.length === 0 && (
        <Box component="span" sx={{ fontSize: 13, color: 'var(--text-secondary)' }}>
          {t('projects.absences.none')}
        </Box>
      )}
      {absences.length > 0 && (
        <Box
          component="ul"
          aria-labelledby={headingId}
          sx={{
            listStyle: 'none',
            margin: 0,
            padding: 0,
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          {absences.map((absence) => (
            <Box
              component="li"
              key={absence.id}
              sx={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: 'var(--space-2)',
                minHeight: 36,
                borderBottom: '1px solid var(--border)',
                fontSize: 14,
                lineHeight: '20px',
                fontVariantNumeric: 'tabular-nums',
              }}
            >
              <span>{absenceText(absence, t)}</span>
              {canManage && (
                <IconButton
                  size="small"
                  aria-label={t('projects.absences.remove', { period: absenceText(absence, t) })}
                  onClick={() => {
                    setRemoving(absence);
                  }}
                >
                  <DeleteOutlined />
                </IconButton>
              )}
            </Box>
          ))}
        </Box>
      )}
      {adding && (
        <AbsenceDialog
          projectId={projectId}
          person={member}
          onClose={() => {
            setAdding(false);
          }}
          onSaved={(message) => {
            setAdding(false);
            refreshed();
            onDone(message);
          }}
        />
      )}
      <Dialog
        open={removing !== undefined}
        alert
        title={t('projects.absences.removeDialog.title')}
        onClose={() => {
          setRemoving(undefined);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setRemoving(undefined);
              }}
            >
              {t('projects.absences.removeDialog.cancel')}
            </Button>
            <Button
              variant="contained"
              color="error"
              onClick={() => (removing === undefined ? undefined : remove(removing))}
            >
              {t('projects.absences.removeDialog.confirm')}
            </Button>
          </>
        }
      >
        {removing === undefined
          ? null
          : t('projects.absences.removeDialog.text', {
              name: member.name,
              period: absenceText(removing, t),
            })}
      </Dialog>
    </Box>
  );
}
