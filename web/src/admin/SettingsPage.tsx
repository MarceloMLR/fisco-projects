import { zodResolver } from '@hookform/resolvers/zod';
import UploadOutlined from '@mui/icons-material/UploadOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Snackbar from '@mui/material/Snackbar';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { type ChangeEvent, useId, useRef, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, installationProblems } from '../api/client';
import { generalRulesQuery, installationQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { TextField } from '../components/TextField';
import { InstallationLogo } from '../installation/InstallationLogo';
import { Page } from '../shell/Page';
import { BrandPreview } from './BrandPreview';
import { TimeZoneField } from './TimeZoneField';

/** Up to 1 MB, PNG or SVG, as the API checks (LogoFile in the Administration module). */
const logoMaxBytes = 1024 * 1024;
const logoTypes = ['image/png', 'image/svg+xml'];

const fieldErrors = {
  nameRequired: 'settings.errors.nameRequired',
  nameTooLong: 'settings.errors.nameTooLong',
  timeZoneRequired: 'settings.errors.timeZoneRequired',
  timeZoneInvalid: 'settings.errors.timeZoneInvalid',
} as const;

type FieldError = (typeof fieldErrors)[keyof typeof fieldErrors];

const form = z.object({
  name: z
    .string()
    .trim()
    .min(1, { error: fieldErrors.nameRequired })
    .max(60, { error: fieldErrors.nameTooLong }),
  timeZone: z.string().min(1, { error: fieldErrors.timeZoneRequired }),
});

type LogoError = 'logoFormat' | 'logoTooLarge' | 'logoUnsafe';

/** The logo in the form: the one saved, a new file with its picture, or removed. */
type LogoChoice =
  { kind: 'saved' } | { kind: 'new'; file: File; picture: string } | { kind: 'removed' };

// Below this width the Preview goes under the form (fp-cols); below 700 px the fields of a row stack (fp-form-row).
const singleColumn = '@media (max-width: 1100px)';
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

/** Two fields side by side (fp-form-row), one under the other on a narrow screen. */
const rowSx = {
  display: 'grid',
  gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
  gap: 'var(--space-4)',
  [narrowForm]: { gridTemplateColumns: 'minmax(0, 1fr)' },
} as const;

function isLogoFile(file: File): boolean {
  const name = file.name.toLowerCase();
  return logoTypes.includes(file.type) || name.endsWith('.png') || name.endsWith('.svg');
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

function logoErrorOf(type: unknown): LogoError {
  if (type === installationProblems.logoTooLarge) {
    return 'logoTooLarge';
  }
  return type === installationProblems.logoUnsafe ? 'logoUnsafe' : 'logoFormat';
}

/**
 * General settings of the Administration, as in the canvas (AdminConfiguracoes): the Brand (name shown and logo) and
 * the Preview, with Discard and Save changes at the bottom (fp-actionbar). Only the Administrator reaches it.
 */
export function SettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: installation, isError } = useQuery(installationQuery());
  // The shell already read the rules, before showing any screen.
  const { data: rules } = useQuery(generalRulesQuery());
  const [logo, setLogo] = useState<LogoChoice>({ kind: 'saved' });
  const [logoError, setLogoError] = useState<LogoError>();
  const [message, setMessage] = useState<string>();
  const fileInput = useRef<HTMLInputElement>(null);
  const logoHelpId = useId();

  const {
    register,
    handleSubmit,
    reset,
    setError,
    control,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<z.input<typeof form>, unknown, z.output<typeof form>>({
    resolver: zodResolver(form),
    defaultValues: { name: '', timeZone: '' },
    values:
      installation === undefined || rules === undefined
        ? undefined
        : { name: installation.name, timeZone: rules.timeZone },
  });

  const savedLogo = installation?.logoUrl ?? null;
  const shownLogo = logo.kind === 'new' ? logo.picture : logo.kind === 'removed' ? null : savedLogo;
  const changed = isDirty || logo.kind !== 'saved';
  const typedName = useWatch({ control, name: 'name' });

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

  const discard = () => {
    reset();
    setLogo({ kind: 'saved' });
    setLogoError(undefined);
  };

  const save = handleSubmit(async ({ name, timeZone }) => {
    // The new logo stays on screen until the brand is read again, so the old one never flashes back.
    let logoSaved = false;
    try {
      if (logo.kind === 'new') {
        const body = new FormData();
        body.append('file', logo.file, logo.file.name);
        const { error, response } = await api.PUT('/api/installation/logo', {
          body: {},
          bodySerializer: () => body,
        });
        if (response.status === 400 || response.status === 413) {
          setLogoError(response.status === 413 ? 'logoTooLarge' : logoErrorOf(error?.type));
          return;
        }
        if (!response.ok) {
          throw new Error(`The API answered ${String(response.status)}.`);
        }
      } else if (logo.kind === 'removed') {
        const { response } = await api.DELETE('/api/installation/logo');
        if (!response.ok) {
          throw new Error(`The API answered ${String(response.status)}.`);
        }
      }
      logoSaved = logo.kind !== 'saved';

      if (isDirty) {
        const { error, response } = await api.PUT('/api/installation', {
          body: { name, timeZone },
        });
        if (response.status === 400) {
          // The screen checks the name before sending; the time zone only the server can check.
          const invalid = invalidFields(error);
          if (invalid.includes('name')) {
            setError('name', { message: fieldErrors.nameRequired });
          }
          if (invalid.includes('timeZone') || invalid.length === 0) {
            setError('timeZone', { message: fieldErrors.timeZoneInvalid });
          }
          return;
        }
        if (!response.ok) {
          throw new Error(`The API answered ${String(response.status)}.`);
        }
      }
      setMessage(t('settings.saved'));
    } catch {
      setMessage(t('settings.saveFailed'));
    } finally {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.installation }),
        queryClient.invalidateQueries({ queryKey: queryKeys.audit }),
      ]);
      if (logoSaved) {
        setLogo({ kind: 'saved' });
      }
    }
  });

  const nameError = errors.name?.message as FieldError | undefined;
  const timeZoneError = errors.timeZone?.message as FieldError | undefined;

  return (
    <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
      <Page>
        <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          {isError && installation === undefined ? (
            <Alert tone="danger">{t('settings.loadFailed')}</Alert>
          ) : (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: 'minmax(0, 1fr) 320px',
                gap: 'var(--space-5)',
                alignItems: 'start',
                [singleColumn]: { gridTemplateColumns: 'minmax(0, 1fr)' },
              }}
            >
              <Box
                component="form"
                id="settings-form"
                noValidate
                onSubmit={(event) => {
                  void save(event);
                }}
                sx={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 'var(--space-5)',
                  minWidth: 0,
                }}
              >
                <Box component="section" aria-labelledby="settings-brand" sx={sectionSx}>
                  <Box component="h2" id="settings-brand" sx={sectionTitleSx}>
                    {t('settings.brand')}
                  </Box>
                  <Box
                    sx={{
                      display: 'grid',
                      // The logo takes what its buttons need, and the name the rest (fp-form-row).
                      gridTemplateColumns: 'minmax(0, 1fr) auto',
                      gap: 'var(--space-4)',
                      [narrowForm]: { gridTemplateColumns: 'minmax(0, 1fr)' },
                    }}
                  >
                    <TextField
                      label={t('settings.name')}
                      autoComplete="off"
                      error={nameError === undefined ? undefined : t(nameError)}
                      {...register('name')}
                    />
                    <Box
                      role="group"
                      aria-labelledby="settings-logo"
                      sx={{
                        display: 'flex',
                        flexDirection: 'column',
                        gap: 'var(--space-1)',
                        minWidth: 0,
                      }}
                    >
                      <Box
                        component="span"
                        id="settings-logo"
                        sx={{
                          fontSize: 13,
                          lineHeight: '18px',
                          fontWeight: 500,
                          color: 'var(--text)',
                        }}
                      >
                        {t('settings.logo')}
                        <Box
                          component="span"
                          sx={{
                            marginLeft: 'var(--space-1)',
                            fontWeight: 400,
                            color: 'var(--text-secondary)',
                          }}
                        >
                          {t('settings.optional')}
                        </Box>
                      </Box>
                      <Box
                        sx={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: 'var(--space-2)',
                          flexWrap: 'wrap',
                        }}
                      >
                        <Box
                          component="span"
                          data-testid="logo-tile"
                          sx={{
                            display: 'inline-grid',
                            placeItems: 'center',
                            flex: 'none',
                            boxSizing: 'border-box',
                            width: 36,
                            height: 36,
                            padding: '3px',
                            border: '1px solid var(--border)',
                            borderRadius: 'var(--radius-md)',
                            backgroundColor: 'var(--bg-surface)',
                            color: 'var(--text-secondary)',
                            fontSize: 11,
                            fontWeight: 500,
                          }}
                        >
                          {shownLogo === null ? (
                            t('settings.noLogo')
                          ) : (
                            <InstallationLogo src={shownLogo} maxHeight={28} maxWidth={28} />
                          )}
                        </Box>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<UploadOutlined />}
                          aria-describedby={logoHelpId}
                          onClick={() => fileInput.current?.click()}
                        >
                          {shownLogo === null ? t('settings.sendLogo') : t('settings.replaceLogo')}
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
                            {t('settings.removeLogo')}
                          </Button>
                        )}
                        <input
                          ref={fileInput}
                          type="file"
                          accept=".png,.svg,image/png,image/svg+xml"
                          hidden
                          data-testid="logo-file"
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
                            logoError === undefined
                              ? 'var(--text-secondary)'
                              : 'var(--status-danger-text)',
                        }}
                      >
                        {logoError === undefined
                          ? t('settings.logoHint')
                          : t(`settings.errors.${logoError}`)}
                      </Box>
                    </Box>
                  </Box>
                </Box>
                <Box component="section" aria-labelledby="settings-rules" sx={sectionSx}>
                  <Box component="h2" id="settings-rules" sx={sectionTitleSx}>
                    {t('settings.rules')}
                  </Box>
                  <Box sx={rowSx}>
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
                </Box>
              </Box>
              <BrandPreview
                name={typedName.trim() === '' ? (installation?.name ?? '') : typedName.trim()}
                logo={shownLogo}
              />
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
        <Button variant="outlined" disabled={!changed || isSubmitting} onClick={discard}>
          {t('settings.discard')}
        </Button>
        <Button
          variant="contained"
          type="submit"
          form="settings-form"
          disabled={!changed || isSubmitting}
        >
          {t('settings.save')}
        </Button>
      </Box>
      <Snackbar
        open={message !== undefined}
        message={message}
        autoHideDuration={6000}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        onClose={() => {
          setMessage(undefined);
        }}
      />
    </Box>
  );
}
