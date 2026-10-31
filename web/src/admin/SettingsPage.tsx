import { zodResolver } from '@hookform/resolvers/zod';
import UploadOutlined from '@mui/icons-material/UploadOutlined';
import Box from '@mui/material/Box';
import { useColorScheme } from '@mui/material/styles';
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
import {
  brandVariables,
  defaultMainColor,
  hasContrast,
  normalizeHex,
  sidebarIsDark,
} from '../theme/brandColors';
import { BrandPreview } from './BrandPreview';
import { EmailSettings } from './EmailSettings';
import { MainColorField } from './MainColorField';
import { TimeZoneField } from './TimeZoneField';
import { Button } from '../components/Button';
import { Form } from '../components/Form';
import { useNotify } from '../notices/notify';

/** Up to 1 MB, PNG or SVG, as the API checks (LogoFile in the Administration module). */
const logoMaxBytes = 1024 * 1024;
const logoTypes = ['image/png', 'image/svg+xml'];

const fieldErrors = {
  nameRequired: 'settings.errors.nameRequired',
  nameTooLong: 'settings.errors.nameTooLong',
  timeZoneRequired: 'settings.errors.timeZoneRequired',
  timeZoneInvalid: 'settings.errors.timeZoneInvalid',
  colorFormat: 'settings.errors.colorFormat',
  colorContrast: 'settings.errors.colorContrast',
  dailySummaryRequired: 'settings.errors.dailySummaryRequired',
  retentionRange: 'settings.errors.retentionRange',
  auditRetentionRange: 'settings.errors.auditRetentionRange',
  projectsFolderInvalid: 'settings.errors.projectsFolderInvalid',
} as const;

type FieldError = (typeof fieldErrors)[keyof typeof fieldErrors];

const form = z.object({
  name: z
    .string()
    .trim()
    .min(1, { error: fieldErrors.nameRequired })
    .max(60, { error: fieldErrors.nameTooLong }),
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
  // Where the folders of the new projects go in the file storage; the server checks the names (ADR-0121).
  projectsFolder: z.string().trim(),
  // Empty is the default color; typed with or without #, in any case, kept as #RRGGBB (ADR-0082).
  mainColor: z
    .string()
    .trim()
    .refine((value) => value === '' || normalizeHex(value) !== undefined, {
      error: fieldErrors.colorFormat,
    })
    .refine((value) => value === '' || hasContrast(value), { error: fieldErrors.colorContrast })
    .transform((value) => (value === '' ? null : (normalizeHex(value) ?? null))),
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

/** A logo of the brand in the form (ADR-0100): the tile with the logo, send or replace it and remove it. */
function LogoField({
  label,
  hint,
  shown,
  error,
  dark,
  testId,
  sendLabel,
  replaceLabel,
  removeLabel,
  onChoose,
  onRemove,
}: {
  label: string;
  hint: string;
  shown: string | null;
  error?: LogoError;
  /** The tile is dark, as the sidebar that shows the logo for the dark theme. */
  dark: boolean;
  testId: string;
  sendLabel: string;
  replaceLabel: string;
  removeLabel: string;
  onChoose: (event: ChangeEvent<HTMLInputElement>) => void;
  onRemove: () => void;
}) {
  const { t } = useTranslation();
  const labelId = useId();
  const helpId = useId();
  const fileInput = useRef<HTMLInputElement>(null);
  return (
    <Box
      role="group"
      aria-labelledby={labelId}
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)', minWidth: 0 }}
    >
      <Box
        component="span"
        id={labelId}
        sx={{ fontSize: 13, lineHeight: '18px', fontWeight: 500, color: 'var(--text)' }}
      >
        {label}
        <Box
          component="span"
          sx={{ marginLeft: 'var(--space-1)', fontWeight: 400, color: 'var(--text-secondary)' }}
        >
          {t('settings.optional')}
        </Box>
      </Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', flexWrap: 'wrap' }}>
        <Box
          component="span"
          data-testid={`${testId}-tile`}
          data-theme={dark ? 'dark' : undefined}
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
            backgroundColor: dark ? 'var(--sidebar-bg)' : 'var(--bg-surface)',
            color: 'var(--text-secondary)',
            fontSize: 11,
            fontWeight: 500,
          }}
        >
          {shown === null ? (
            t('settings.noLogo')
          ) : (
            <InstallationLogo src={shown} maxHeight={28} maxWidth={28} />
          )}
        </Box>
        <Button
          variant="outlined"
          size="small"
          startIcon={<UploadOutlined />}
          aria-describedby={helpId}
          onClick={() => fileInput.current?.click()}
        >
          {shown === null ? sendLabel : replaceLabel}
        </Button>
        {shown !== null && (
          <Button variant="text" size="small" onClick={onRemove}>
            {removeLabel}
          </Button>
        )}
        <input
          ref={fileInput}
          type="file"
          accept=".png,.svg,image/png,image/svg+xml"
          hidden
          data-testid={`${testId}-file`}
          onChange={onChoose}
        />
      </Box>
      <Box
        component="span"
        id={helpId}
        role={error === undefined ? undefined : 'alert'}
        sx={{
          fontSize: 12,
          lineHeight: '16px',
          color: error === undefined ? 'var(--text-secondary)' : 'var(--status-danger-text)',
        }}
      >
        {error === undefined ? hint : t(`settings.errors.${error}`)}
      </Box>
    </Box>
  );
}

/** Sends or removes the logo of a theme, if it changed; the error of a refused file, or nothing. */
async function saveLogo(choice: LogoChoice, dark: boolean): Promise<LogoError | undefined> {
  if (choice.kind === 'new') {
    const body = new FormData();
    body.append('file', choice.file, choice.file.name);
    const { error, response } = dark
      ? await api.PUT('/api/installation/logo/dark', { body: {}, bodySerializer: () => body })
      : await api.PUT('/api/installation/logo', { body: {}, bodySerializer: () => body });
    if (response.status === 400 || response.status === 413) {
      return response.status === 413 ? 'logoTooLarge' : logoErrorOf(error?.type);
    }
    if (!response.ok) {
      throw new Error(`The API answered ${String(response.status)}.`);
    }
  } else if (choice.kind === 'removed') {
    const { response } = dark
      ? await api.DELETE('/api/installation/logo/dark')
      : await api.DELETE('/api/installation/logo');
    if (!response.ok) {
      throw new Error(`The API answered ${String(response.status)}.`);
    }
  }
  return undefined;
}

function logoErrorOf(type: unknown): LogoError {
  if (type === installationProblems.logoTooLarge) {
    return 'logoTooLarge';
  }
  return type === installationProblems.logoUnsafe ? 'logoUnsafe' : 'logoFormat';
}

/**
 * General settings of the Administration, as in the canvas (AdminConfiguracoes): the Brand (name shown and logo), the
 * General rules and the Preview, with Discard and Save changes at the bottom (fp-actionbar), and Microsoft 365, with the
 * test of the e-mails, which does not wait for Save. Only the Administrator reaches it.
 */
export function SettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: installation, isError } = useQuery(installationQuery());
  // The shell already read the rules, before showing any screen.
  const { data: rules } = useQuery(generalRulesQuery());
  const [logo, setLogo] = useState<LogoChoice>({ kind: 'saved' });
  const [logoError, setLogoError] = useState<LogoError>();
  const [darkLogo, setDarkLogo] = useState<LogoChoice>({ kind: 'saved' });
  const [darkLogoError, setDarkLogoError] = useState<LogoError>();
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
      name: '',
      timeZone: '',
      mainColor: '',
      dailySummaryAt: '',
      notificationRetentionDays: '',
      auditRetentionYears: '',
      projectsFolder: '',
    },
    values:
      installation === undefined || rules === undefined
        ? undefined
        : {
            name: installation.name,
            timeZone: rules.timeZone,
            mainColor: installation.mainColor ?? '',
            dailySummaryAt: rules.dailySummaryAt.slice(0, 5),
            notificationRetentionDays: String(rules.notificationRetentionDays),
            auditRetentionYears: String(rules.auditRetentionYears),
            projectsFolder: rules.projectsFolder,
          },
  });

  const savedLogo = installation?.logoUrl ?? null;
  const shownLogo = logo.kind === 'new' ? logo.picture : logo.kind === 'removed' ? null : savedLogo;
  const savedDarkLogo = installation?.darkLogoUrl ?? null;
  const shownDarkLogo =
    darkLogo.kind === 'new' ? darkLogo.picture : darkLogo.kind === 'removed' ? null : savedDarkLogo;
  const changed = isDirty || logo.kind !== 'saved' || darkLogo.kind !== 'saved';
  const typedName = useWatch({ control, name: 'name' });
  const typedColor = useWatch({ control, name: 'mainColor' }).trim();
  // The Preview shows the color as it is typed once it is a color that may be saved; until then, the saved one.
  const typedHex = normalizeHex(typedColor);
  const previewColor =
    typedColor === ''
      ? null
      : typedHex !== undefined && hasContrast(typedHex)
        ? typedHex
        : (installation?.mainColor ?? null);
  const { colorScheme } = useColorScheme();
  const scheme = colorScheme ?? 'light';
  // The sidebar of the Preview shows the logo for the dark theme when it is dark, as the real one (ADR-0100).
  const previewLogo = sidebarIsDark(scheme) && shownDarkLogo !== null ? shownDarkLogo : shownLogo;

  const chooser =
    (choose: (choice: LogoChoice) => void, refuse: (error: LogoError | undefined) => void) =>
    async (event: ChangeEvent<HTMLInputElement>) => {
      const file = event.target.files?.[0];
      // The same file can be chosen again after a refusal.
      event.target.value = '';
      if (file === undefined) {
        return;
      }
      if (!isLogoFile(file)) {
        refuse('logoFormat');
        return;
      }
      if (file.size > logoMaxBytes) {
        refuse('logoTooLarge');
        return;
      }
      refuse(undefined);
      choose({ kind: 'new', file, picture: await readPicture(file) });
    };
  const chooseLogo = chooser(setLogo, setLogoError);
  const chooseDarkLogo = chooser(setDarkLogo, setDarkLogoError);

  const discard = () => {
    reset();
    setLogo({ kind: 'saved' });
    setLogoError(undefined);
    setDarkLogo({ kind: 'saved' });
    setDarkLogoError(undefined);
  };

  const save = handleSubmit(
    async ({
      name,
      timeZone,
      mainColor,
      dailySummaryAt,
      notificationRetentionDays,
      auditRetentionYears,
      projectsFolder,
    }) => {
      // The new logos stay on screen until the brand is read again, so the old ones never flash back.
      let logoSaved = false;
      let darkLogoSaved = false;
      try {
        const refused = await saveLogo(logo, false);
        if (refused !== undefined) {
          setLogoError(refused);
          return;
        }
        logoSaved = logo.kind !== 'saved';
        const darkRefused = await saveLogo(darkLogo, true);
        if (darkRefused !== undefined) {
          setDarkLogoError(darkRefused);
          return;
        }
        darkLogoSaved = darkLogo.kind !== 'saved';

        if (isDirty) {
          const { error, response } = await api.PUT('/api/installation', {
            body: {
              name,
              timeZone,
              mainColor,
              dailySummaryAt: `${dailySummaryAt}:00`,
              notificationRetentionDays,
              auditRetentionYears,
              projectsFolder,
            },
          });
          if (response.status === 400) {
            // The screen checks the name before sending; the time zone only the server can check.
            const invalid = invalidFields(error);
            if (invalid.includes('name')) {
              setError('name', { message: fieldErrors.nameRequired });
            }
            if (invalid.includes('mainColor')) {
              setError('mainColor', { message: fieldErrors.colorContrast });
            }
            if (invalid.includes('dailySummaryAt')) {
              setError('dailySummaryAt', { message: fieldErrors.dailySummaryRequired });
            }
            if (invalid.includes('notificationRetentionDays')) {
              setError('notificationRetentionDays', { message: fieldErrors.retentionRange });
            }
            if (invalid.includes('auditRetentionYears')) {
              setError('auditRetentionYears', { message: fieldErrors.auditRetentionRange });
            }
            if (invalid.includes('projectsFolder')) {
              setError('projectsFolder', { message: fieldErrors.projectsFolderInvalid });
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
        notify(t('settings.saved'));
      } catch {
        notify(t('settings.saveFailed'), { tone: 'error' });
      } finally {
        await Promise.all([
          queryClient.invalidateQueries({ queryKey: queryKeys.installation }),
          queryClient.invalidateQueries({ queryKey: queryKeys.audit }),
        ]);
        if (logoSaved) {
          setLogo({ kind: 'saved' });
        }
        if (darkLogoSaved) {
          setDarkLogo({ kind: 'saved' });
        }
      }
    },
  );

  const nameError = errors.name?.message as FieldError | undefined;
  const timeZoneError = errors.timeZone?.message as FieldError | undefined;
  const colorError = errors.mainColor?.message as FieldError | undefined;
  const summaryError = errors.dailySummaryAt?.message as FieldError | undefined;
  const retentionError = errors.notificationRetentionDays?.message as FieldError | undefined;
  const auditRetentionError = errors.auditRetentionYears?.message as FieldError | undefined;
  const projectsFolderError = errors.projectsFolder?.message as FieldError | undefined;

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
              <Form
                id="settings-form"
                onSubmit={(event) => save(event)}
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
                    <LogoField
                      label={t('settings.logo')}
                      hint={t('settings.logoHint')}
                      shown={shownLogo}
                      error={logoError}
                      dark={false}
                      testId="logo"
                      sendLabel={t('settings.sendLogo')}
                      replaceLabel={t('settings.replaceLogo')}
                      removeLabel={t('settings.removeLogo')}
                      onChoose={(event) => {
                        void chooseLogo(event);
                      }}
                      onRemove={() => {
                        setLogo(savedLogo === null ? { kind: 'saved' } : { kind: 'removed' });
                        setLogoError(undefined);
                      }}
                    />
                  </Box>
                  <LogoField
                    label={t('settings.darkLogo')}
                    hint={t('settings.darkLogoHint')}
                    shown={shownDarkLogo}
                    error={darkLogoError}
                    dark
                    testId="dark-logo"
                    sendLabel={t('settings.sendDarkLogo')}
                    replaceLabel={t('settings.replaceDarkLogo')}
                    removeLabel={t('settings.removeDarkLogo')}
                    onChoose={(event) => {
                      void chooseDarkLogo(event);
                    }}
                    onRemove={() => {
                      setDarkLogo(savedDarkLogo === null ? { kind: 'saved' } : { kind: 'removed' });
                      setDarkLogoError(undefined);
                    }}
                  />
                  <Controller
                    control={control}
                    name="mainColor"
                    render={({ field }) => (
                      <MainColorField
                        value={field.value}
                        onChange={field.onChange}
                        onBlur={field.onBlur}
                        error={
                          colorError === undefined
                            ? undefined
                            : t(colorError, { example: defaultMainColor() })
                        }
                      />
                    )}
                  />
                </Box>
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
                  </Box>
                </Box>
                <Box component="section" aria-labelledby="settings-files" sx={sectionSx}>
                  <Box component="h2" id="settings-files" sx={sectionTitleSx}>
                    {t('settings.files')}
                  </Box>
                  <Box sx={{ maxWidth: 520 }}>
                    <TextField
                      label={t('settings.projectsFolder')}
                      autoComplete="off"
                      spellCheck={false}
                      helper={t('settings.projectsFolderHint')}
                      error={projectsFolderError === undefined ? undefined : t(projectsFolderError)}
                      {...register('projectsFolder')}
                    />
                  </Box>
                </Box>
                <Box component="section" aria-labelledby="settings-m365" sx={sectionSx}>
                  <Box component="h2" id="settings-m365" sx={sectionTitleSx}>
                    {t('settings.m365')}
                  </Box>
                  <EmailSettings />
                </Box>
              </Form>
              <BrandPreview
                name={typedName.trim() === '' ? (installation?.name ?? '') : typedName.trim()}
                logo={shownLogo}
                sidebarLogo={previewLogo}
                colors={brandVariables(previewColor, scheme)}
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
        <Button variant="contained" type="submit" form="settings-form" disabled={!changed}>
          {t('settings.save')}
        </Button>
      </Box>
    </Box>
  );
}
