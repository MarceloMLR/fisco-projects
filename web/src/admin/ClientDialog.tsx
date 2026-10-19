import { zodResolver } from '@hookform/resolvers/zod';
import UploadOutlined from '@mui/icons-material/UploadOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useQueryClient } from '@tanstack/react-query';
import { type ChangeEvent, useId, useRef, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, type ClientRow, clientProblems } from '../api/client';
import { queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { LogoTile } from '../components/LogoTile';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { cleanCnpj, formatCnpj, isValidCnpj, maskTypedCnpj } from '../format/cnpj';

/** Up to 1 MB, PNG or JPG, as the API checks by the content (ClientLogo in the Projects module). */
const logoMaxBytes = 1024 * 1024;
const logoTypes = ['image/png', 'image/jpeg'];
const logoExtensions = ['.png', '.jpg', '.jpeg'];

const fieldErrors = {
  nameRequired: 'clients.errors.nameRequired',
  nameTooLong: 'clients.errors.nameTooLong',
  cnpjInvalid: 'clients.errors.cnpjInvalid',
  observationTooLong: 'clients.errors.observationTooLong',
} as const;

type FieldError = (typeof fieldErrors)[keyof typeof fieldErrors];
type LogoError = 'logoFormat' | 'logoTooLarge';

/** The logo in the form: the one saved, a new file with its picture, or removed. */
type LogoChoice =
  { kind: 'saved' } | { kind: 'new'; file: File; picture: string } | { kind: 'removed' };

// The data as the API keeps them: the name up to 150 characters, the CNPJ empty or by the rule of the Receita, and
// the observation up to 500 characters, all without the spaces at the ends.
const form = z.object({
  name: z
    .string()
    .trim()
    .min(1, { error: fieldErrors.nameRequired })
    .max(150, { error: fieldErrors.nameTooLong }),
  cnpj: z.string().refine(
    (value) => {
      const cnpj = cleanCnpj(value);
      return cnpj === '' || isValidCnpj(cnpj);
    },
    { error: fieldErrors.cnpjInvalid },
  ),
  observation: z.string().trim().max(500, { error: fieldErrors.observationTooLong }),
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

/** The name of the client that already has the CNPJ, from the 409 of the API. */
function ownerOf(problem: unknown): string {
  return typeof problem === 'object' &&
    problem !== null &&
    'clientName' in problem &&
    typeof problem.clientName === 'string'
    ? problem.clientName
    : '';
}

function problemTypeOf(problem: unknown): unknown {
  return typeof problem === 'object' && problem !== null && 'type' in problem
    ? problem.type
    : undefined;
}

function isLogoFile(file: File): boolean {
  const name = file.name.toLowerCase();
  return logoTypes.includes(file.type) || logoExtensions.some((end) => name.endsWith(end));
}

function readPicture(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      resolve(typeof reader.result === 'string' ? reader.result : '');
    };
    reader.onerror = () => {
      reject(reader.error ?? new Error('The file could not be read.'));
    };
    reader.readAsDataURL(file);
  });
}

/**
 * "Novo cliente" and "Editar cliente", as the "Novo feriado" of the canvas: the name, the CNPJ with its mask and the
 * observation, and, for the Administrator, the logo (ADR-0046). Saving sends the client and then the logo. A CNPJ of
 * another client shows in the field, with the name of that client.
 */
export function ClientDialog({
  client,
  canSendLogo,
  onClose,
  onSaved,
}: {
  /** The client being edited; none for a new one. */
  client?: ClientRow;
  /** Only the Administrator sends, replaces and removes the logo. */
  canSendLogo: boolean;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [logo, setLogo] = useState<LogoChoice>({ kind: 'saved' });
  const [logoError, setLogoError] = useState<LogoError>();
  // A new client saved whose logo was refused: saving again changes it instead of including another.
  const [savedId, setSavedId] = useState(client?.id);
  const fileInput = useRef<HTMLInputElement>(null);
  const logoLabelId = useId();
  const logoHelpId = useId();
  const editing = client !== undefined;

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(form),
    defaultValues: {
      name: client?.name ?? '',
      cnpj: client?.cnpj === null || client?.cnpj === undefined ? '' : formatCnpj(client.cnpj),
      observation: client?.observation ?? '',
    },
  });

  // The logo as it will be saved, or the initials of the name as it is typed.
  const typedName = useWatch({ control, name: 'name' });
  const savedLogo = client?.logoUrl ?? null;
  const shownLogo = logo.kind === 'new' ? logo.picture : logo.kind === 'removed' ? null : savedLogo;

  const errorText = (message: string | undefined) =>
    isFieldError(message) ? t(message) : undefined;
  const cnpjError =
    errors.cnpj?.type === 'cnpjInUse'
      ? t('clients.errors.cnpjInUse', { name: errors.cnpj.message ?? '' })
      : errorText(errors.cnpj?.message);

  const chooseLogo = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    // The same file can be chosen again after a refusal.
    event.target.value = '';
    if (file === undefined) {
      return;
    }
    if (!isLogoFile(file)) {
      setLogoError('logoFormat');
      return;
    }
    if (file.size > logoMaxBytes) {
      setLogoError('logoTooLarge');
      return;
    }
    setLogoError(undefined);
    setLogo({ kind: 'new', file, picture: await readPicture(file) });
  };

  /** Sends the logo chosen; false when the API refused it, with the reason under the logo. */
  const saveLogo = async (id: string): Promise<boolean> => {
    const path = { params: { path: { id } } };
    if (logo.kind === 'new') {
      const body = new FormData();
      body.append('file', logo.file, logo.file.name);
      const { error, response } = await api.PUT('/api/clients/{id}/logo', {
        ...path,
        body: {},
        bodySerializer: () => body,
      });
      if (response.status === 400 || response.status === 413) {
        setLogoError(
          response.status === 413 || problemTypeOf(error) === clientProblems.logoTooLarge
            ? 'logoTooLarge'
            : 'logoFormat',
        );
        return false;
      }
      if (!response.ok) {
        throw new Error(`The API answered ${String(response.status)}.`);
      }
    } else if (logo.kind === 'removed') {
      const { response } = await api.DELETE('/api/clients/{id}/logo', path);
      if (!response.ok) {
        throw new Error(`The API answered ${String(response.status)}.`);
      }
    }
    return true;
  };

  const save = handleSubmit(async (values) => {
    const name = values.name.trim();
    const cnpj = cleanCnpj(values.cnpj);
    const observation = values.observation.trim();
    const body = {
      name,
      cnpj: cnpj === '' ? null : cnpj,
      observation: observation === '' ? null : observation,
    };
    let saved = false;
    try {
      let id = savedId;
      const { response, error, data } =
        id === undefined
          ? await api.POST('/api/clients', { body })
          : await api.PUT('/api/clients/{id}', { params: { path: { id } }, body });
      if (response.status === 409) {
        setError('cnpj', { type: 'cnpjInUse', message: ownerOf(error) });
        return;
      }
      if (response.status === 400) {
        const fields = invalidFields(error);
        if (fields.includes('name')) {
          setError('name', { message: fieldErrors.nameRequired });
        }
        if (fields.includes('cnpj')) {
          setError('cnpj', { message: fieldErrors.cnpjInvalid });
        }
        if (fields.includes('observation')) {
          setError('observation', { message: fieldErrors.observationTooLong });
        }
        if (fields.length === 0) {
          setError('root', { message: t('clients.messages.failed') });
        }
        return;
      }
      if (!response.ok) {
        setError('root', { message: t('clients.messages.failed') });
        return;
      }
      id ??= data !== undefined && 'id' in data ? data.id : undefined;
      saved = true;
      setSavedId(id);
      if (id !== undefined && !(await saveLogo(id))) {
        return;
      }
      onSaved(t(editing ? 'clients.messages.changed' : 'clients.messages.included', { name }));
    } catch {
      setError('root', {
        message: saved ? t('clients.messages.logoFailed', { name }) : t('clients.messages.failed'),
      });
    } finally {
      // The lists that show the client, here and in the Audit, are asked again once it is saved.
      if (saved) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.clients });
        void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
      }
    }
  });

  return (
    <Dialog
      open
      title={t(editing ? 'clients.editClient' : 'clients.newClient')}
      onClose={onClose}
      onSubmit={(event) => {
        void save(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('clients.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t(editing ? 'clients.save' : 'clients.include')}
          </Button>
        </>
      }
    >
      <TextField
        label={t('clients.name')}
        autoComplete="off"
        maxLength={200}
        error={errorText(errors.name?.message)}
        {...register('name')}
      />
      <Controller
        control={control}
        name="cnpj"
        render={({ field }) => (
          <TextField
            label={`${t('clients.cnpj')} ${t('clients.optional')}`}
            placeholder={t('clients.cnpjPlaceholder')}
            autoComplete="off"
            autoCapitalize="characters"
            spellCheck={false}
            maxLength={18}
            width={240}
            error={cnpjError}
            name={field.name}
            ref={field.ref}
            value={field.value}
            onBlur={field.onBlur}
            onChange={(event) => {
              field.onChange(maskTypedCnpj(event.target.value));
            }}
          />
        )}
      />
      <TextArea
        label={`${t('clients.observation')} ${t('clients.optional')}`}
        maxLength={600}
        error={errorText(errors.observation?.message)}
        {...register('observation')}
      />
      {canSendLogo && (
        <Box
          role="group"
          aria-labelledby={logoLabelId}
          sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)', minWidth: 0 }}
        >
          <Box
            component="span"
            id={logoLabelId}
            sx={{ fontSize: 13, lineHeight: '18px', fontWeight: 500, color: 'var(--text)' }}
          >
            {t('clients.logo')}
            <Box
              component="span"
              sx={{
                marginLeft: 'var(--space-1)',
                fontWeight: 400,
                color: 'var(--text-secondary)',
              }}
            >
              {t('clients.optional')}
            </Box>
          </Box>
          <Box
            sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', flexWrap: 'wrap' }}
          >
            <LogoTile name={typedName} src={shownLogo} />
            <Button
              variant="outlined"
              size="small"
              startIcon={<UploadOutlined />}
              aria-describedby={logoHelpId}
              onClick={() => fileInput.current?.click()}
            >
              {shownLogo === null ? t('clients.sendLogo') : t('clients.replaceLogo')}
            </Button>
            {shownLogo !== null && (
              <Button
                variant="text"
                size="small"
                onClick={() => {
                  setLogo(savedLogo === null ? { kind: 'saved' } : { kind: 'removed' });
                  setLogoError(undefined);
                }}
              >
                {t('clients.removeLogo')}
              </Button>
            )}
            <input
              ref={fileInput}
              type="file"
              accept=".png,.jpg,.jpeg,image/png,image/jpeg"
              hidden
              data-testid="client-logo-file"
              onChange={(event) => {
                void chooseLogo(event);
              }}
            />
          </Box>
          <Box
            component="span"
            id={logoHelpId}
            role={logoError === undefined ? undefined : 'alert'}
            sx={{
              fontSize: 12,
              lineHeight: '16px',
              color:
                logoError === undefined ? 'var(--text-secondary)' : 'var(--status-danger-text)',
            }}
          >
            {logoError === undefined ? t('clients.logoHint') : t(`clients.errors.${logoError}`)}
          </Box>
        </Box>
      )}
      {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
    </Dialog>
  );
}
