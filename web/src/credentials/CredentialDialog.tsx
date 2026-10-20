import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useId, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { api, type Credential, credentialProblems } from '../api/client';
import { Alert } from '../components/Alert';
import { Checkbox } from '../components/Checkbox';
import { Dialog } from '../components/Dialog';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { problemType } from '../projects/projectForm';
import {
  credentialErrors as errors,
  credentialSchema,
  type CredentialValues as Values,
  isCredentialError,
} from './credentialForm';

const fieldErrors = {
  portal: errors.portalRequired,
  environment: errors.environmentRequired,
  url: errors.urlInvalid,
  username: errors.usernameRequired,
  password: errors.passwordRequired,
  notes: errors.notesTooLong,
} as const;

/** The fields the API refused, from the errors of its validation problem. */
function refusedFields(problem: unknown): string[] {
  return typeof problem === 'object' &&
    problem !== null &&
    'errors' in problem &&
    typeof problem.errors === 'object' &&
    problem.errors !== null
    ? Object.keys(problem.errors)
    : [];
}

/**
 * "Novo acesso" and "Editar acesso" (canvas ProjetoAcessos, ADR-0034): the portal, the environment, with the ones of
 * the project as suggestions, the address, the user, the password, hidden while typed unless "Mostrar senha" is marked
 * and, in a change, kept when left empty, and the notes.
 */
export function CredentialDialog({
  projectId,
  credential,
  environments,
  onClose,
  onSaved,
}: {
  projectId: string;
  /** The credential to change; none for a new one. */
  credential?: Credential;
  /** The environments the project already uses, offered while typing. */
  environments: readonly string[];
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const ids = useId();
  const [showing, setShowing] = useState(false);
  const isNew = credential === undefined;
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors: invalid, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(credentialSchema(isNew)),
    defaultValues: {
      portal: credential?.portal ?? '',
      environment: credential?.environment ?? '',
      url: credential?.url ?? '',
      username: credential?.username ?? '',
      password: '',
      notes: credential?.notes ?? '',
    },
  });
  const errorText = (message: string | undefined) =>
    isCredentialError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const body = {
      portal: values.portal.trim(),
      environment: values.environment.trim(),
      url: values.url.trim() === '' ? null : values.url.trim(),
      username: values.username.trim(),
      password: values.password === '' ? null : values.password,
      notes: values.notes.trim() === '' ? null : values.notes.trim(),
    };
    try {
      const { response, error } = isNew
        ? await api.POST('/api/projects/{projectId}/credentials', {
            params: { path: { projectId } },
            body,
          })
        : await api.PUT('/api/projects/{projectId}/credentials/{credentialId}', {
            params: { path: { projectId, credentialId: credential.id } },
            body,
          });
      if (response.ok) {
        onSaved(
          t(isNew ? 'credentials.messages.added' : 'credentials.messages.saved', {
            portal: body.portal,
          }),
        );
        return;
      }
      if (response.status === 400) {
        const refused = refusedFields(error);
        for (const [field, message] of Object.entries(fieldErrors)) {
          if (refused.includes(field)) {
            setError(field as keyof typeof fieldErrors, { message });
          }
        }
        return;
      }
      setError('root', {
        message:
          response.status === 409 && problemType(error) === credentialProblems.projectClosed
            ? t('credentials.messages.closed')
            : t('credentials.messages.failed'),
      });
    } catch {
      setError('root', { message: t('credentials.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t(isNew ? 'credentials.dialog.newTitle' : 'credentials.dialog.editTitle')}
      onClose={onClose}
      onSubmit={(event) => {
        void save(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('credentials.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t(isNew ? 'credentials.dialog.add' : 'credentials.dialog.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
            gap: 'var(--space-4)',
          }}
        >
          <TextField
            label={t('credentials.dialog.portal')}
            autoComplete="off"
            maxLength={130}
            error={errorText(invalid.portal?.message)}
            {...register('portal')}
          />
          <TextField
            label={t('credentials.dialog.environment')}
            autoComplete="off"
            maxLength={70}
            list={`${ids}-environments`}
            helper={t('credentials.dialog.environmentHelp')}
            error={errorText(invalid.environment?.message)}
            {...register('environment')}
          />
          <datalist id={`${ids}-environments`}>
            {environments.map((environment) => (
              <option key={environment} value={environment} />
            ))}
          </datalist>
        </Box>
        <TextField
          label={t('credentials.dialog.url')}
          type="url"
          autoComplete="off"
          maxLength={2010}
          helper={t('credentials.dialog.urlHelp')}
          error={errorText(invalid.url?.message)}
          {...register('url')}
        />
        <TextField
          label={t('credentials.dialog.username')}
          autoComplete="off"
          maxLength={210}
          spellCheck={false}
          error={errorText(invalid.username?.message)}
          {...register('username')}
        />
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}>
          <TextField
            label={t('credentials.dialog.password')}
            type={showing ? 'text' : 'password'}
            // Not a password of the person: the browser neither fills it in nor offers to save it.
            autoComplete="new-password"
            maxLength={510}
            spellCheck={false}
            helper={isNew ? undefined : t('credentials.dialog.passwordKeep')}
            error={errorText(invalid.password?.message)}
            {...register('password')}
          />
          <Checkbox
            label={t('credentials.dialog.showPassword')}
            checked={showing}
            onChange={setShowing}
          />
        </Box>
        <TextArea
          label={t('credentials.dialog.notes')}
          rows={3}
          maxLength={510}
          helper={t('credentials.dialog.notesHelp')}
          error={errorText(invalid.notes?.message)}
          {...register('notes')}
        />
        {invalid.root?.message !== undefined && <Alert tone="danger">{invalid.root.message}</Alert>}
      </Box>
    </Dialog>
  );
}
