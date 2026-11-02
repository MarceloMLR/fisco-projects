import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api } from '../api/client';
import { installationQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Button } from '../components/Button';
import { Form } from '../components/Form';
import { TextField } from '../components/TextField';
import { useNotify } from '../notices/notify';
import { Page } from '../shell/Page';
import {
  type BrandColorChoice,
  brandTokens,
  colorNotes,
  colorsOf,
  defaultMainColor,
  normalizeHex,
} from '../theme/brandColors';
import { LogoField } from './LogoField';
import { type LogoChoice, type LogoError, logoChooser, saveLogo, shownLogo } from './logos';
import { ColorField, type ColorFieldName } from './ColorField';
import { ColorsPreview } from './ColorsPreview';

const fields: readonly ColorFieldName[] = ['main', 'secondary', 'sidebar'];

const apiField: Record<ColorFieldName, 'mainColor' | 'secondaryColor' | 'sidebarColor'> = {
  main: 'mainColor',
  secondary: 'secondaryColor',
  sidebar: 'sidebarColor',
};

const nameErrors = {
  required: 'brand.errors.nameRequired',
  tooLong: 'brand.errors.nameTooLong',
} as const;

type NameError = (typeof nameErrors)[keyof typeof nameErrors];

/** The name shown, up to 60 characters, without the spaces at the ends (ADR-0003). */
const brandForm = z.object({
  name: z
    .string()
    .trim()
    .min(1, { error: nameErrors.required })
    .max(60, { error: nameErrors.tooLong }),
});

/** A card of the column of the fields (fp-form-section) and its title (fp-h2). */
const sectionSx = {
  display: 'flex',
  flexDirection: 'column',
  gap: 'var(--space-5)',
  padding: 'var(--space-5)',
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

/** The colors as kept, #RRGGBB in capitals or null, so the saved and the chosen ones compare. */
function kept(colors: BrandColorChoice): BrandColorChoice {
  const one = (value: string | null) => (value === null ? null : (normalizeHex(value) ?? null));
  return { main: one(colors.main), secondary: one(colors.secondary), sidebar: one(colors.sidebar) };
}

/** The fields the server refused in a problem of validation. */
function refusedFields(error: unknown): string[] {
  if (typeof error === 'object' && error !== null && 'errors' in error) {
    const { errors } = error as { errors?: Record<string, unknown> };
    return Object.keys(errors ?? {});
  }
  return [];
}

/**
 * Visual, in the Administration (ADR-0146, ADR-0150): the Brand (the name shown, the logo and the logo for the dark
 * theme) and the colors (the main one, the secondary one and the one of the sidebar, each optional), with the preview of
 * the screens in the light and the dark theme showing them before saving, and the bar of actions with Descartar and
 * Salvar alterações, enabled only when something changed. Saved by the Administrator: the logos, the name and the
 * colors, each only when it changed.
 */
export function VisualPage() {
  const { t } = useTranslation();
  const notify = useNotify();
  const queryClient = useQueryClient();
  const { data: installation, isError } = useQuery(installationQuery());
  const [logo, setLogo] = useState<LogoChoice>({ kind: 'saved' });
  const [logoError, setLogoError] = useState<LogoError>();
  const [darkLogo, setDarkLogo] = useState<LogoChoice>({ kind: 'saved' });
  const [darkLogoError, setDarkLogoError] = useState<LogoError>();
  const saved = useMemo(() => kept(colorsOf(installation)), [installation]);
  // The colors changed on this screen, until they are saved or discarded.
  const [chosen, setChosen] = useState<Partial<BrandColorChoice>>({});
  // The color being typed or picked in a field, shown only in the preview.
  const [trying, setTrying] = useState<Partial<Record<ColorFieldName, string>>>({});
  const [refused, setRefused] = useState<ColorFieldName[]>([]);
  const colors = useMemo(() => ({ ...saved, ...chosen }), [saved, chosen]);
  const previewed = useMemo(() => ({ ...colors, ...trying }), [colors, trying]);
  const notes = useMemo(() => colorNotes(previewed), [previewed]);
  const colorsChanged = fields.some((field) => colors[field] !== saved[field]);
  const defaults = useMemo(() => brandTokens(null).light, []);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    control,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<z.input<typeof brandForm>, unknown, z.output<typeof brandForm>>({
    resolver: zodResolver(brandForm),
    defaultValues: { name: '' },
    values: installation === undefined ? undefined : { name: installation.name },
  });
  const typedName = useWatch({ control, name: 'name' });
  const savedLogo = installation?.logoUrl ?? null;
  const savedDarkLogo = installation?.darkLogoUrl ?? null;
  const shown = shownLogo(logo, savedLogo);
  const shownDark = shownLogo(darkLogo, savedDarkLogo);
  const changed = isDirty || logo.kind !== 'saved' || darkLogo.kind !== 'saved' || colorsChanged;
  const nameError = errors.name?.message as NameError | undefined;

  // What each field shows when it has no color of its own: the color the design system (or the main color) gives.
  const defaultOf: Record<ColorFieldName, string> = {
    main: defaultMainColor(),
    secondary: previewed.main ?? defaultMainColor(),
    sidebar: normalizeHex(defaults['sidebar-bg']) ?? defaults['sidebar-bg'],
  };

  const discard = () => {
    reset();
    setLogo({ kind: 'saved' });
    setLogoError(undefined);
    setDarkLogo({ kind: 'saved' });
    setDarkLogoError(undefined);
    setChosen({});
    setTrying({});
    setRefused([]);
  };

  const save = handleSubmit(async ({ name }) => {
    // The new logos stay on screen until the brand is read again, so the old ones never flash back.
    let logoSaved = false;
    let darkLogoSaved = false;
    try {
      const logoRefused = await saveLogo(logo, false);
      if (logoRefused !== undefined) {
        setLogoError(logoRefused);
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
        const { response } = await api.PUT('/api/installation/name', { body: { name } });
        if (response.status === 400) {
          setError('name', { message: nameErrors.required });
          return;
        }
        if (!response.ok) {
          throw new Error(`The API answered ${String(response.status)}.`);
        }
      }

      if (colorsChanged) {
        const { error, response } = await api.PUT('/api/installation/colors', {
          body: {
            mainColor: colors.main,
            secondaryColor: colors.secondary,
            sidebarColor: colors.sidebar,
          },
        });
        if (response.status === 400) {
          const invalid = refusedFields(error);
          setRefused(fields.filter((field) => invalid.includes(apiField[field])));
          return;
        }
        if (!response.ok) {
          throw new Error(`The API answered ${String(response.status)}.`);
        }
        setChosen({});
        setRefused([]);
      }
      notify(t('visual.saved'));
    } catch {
      notify(t('visual.saveFailed'), { tone: 'error' });
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
  });

  return (
    <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
      <Page subtitle={t('visual.subtitle')}>
        <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          {isError && installation === undefined ? (
            <Alert tone="danger">{t('visual.loadFailed')}</Alert>
          ) : (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: 'minmax(0, 360px) minmax(0, 1fr)',
                gap: 'var(--space-5)',
                alignItems: 'start',
                '@media (max-width: 900px)': { gridTemplateColumns: 'minmax(0, 1fr)' },
              }}
            >
              <Form
                id="visual-form"
                aria-label={t('visual.formLabel')}
                onSubmit={(event) => save(event)}
                sx={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 'var(--space-5)',
                  minWidth: 0,
                }}
              >
                <Box component="section" aria-labelledby="visual-brand" sx={sectionSx}>
                  <Box component="h2" id="visual-brand" sx={sectionTitleSx}>
                    {t('brand.title')}
                  </Box>
                  <TextField
                    label={t('brand.name')}
                    autoComplete="off"
                    error={nameError === undefined ? undefined : t(nameError)}
                    {...register('name')}
                  />
                  <LogoField
                    label={t('brand.logo')}
                    hint={t('brand.logoHint')}
                    shown={shown}
                    error={logoError}
                    dark={false}
                    testId="logo"
                    sendLabel={t('brand.sendLogo')}
                    replaceLabel={t('brand.replaceLogo')}
                    removeLabel={t('brand.removeLogo')}
                    onChoose={(event) => {
                      void logoChooser(setLogo, setLogoError)(event);
                    }}
                    onRemove={() => {
                      setLogo(savedLogo === null ? { kind: 'saved' } : { kind: 'removed' });
                      setLogoError(undefined);
                    }}
                  />
                  <LogoField
                    label={t('brand.darkLogo')}
                    hint={t('brand.darkLogoHint')}
                    shown={shownDark}
                    error={darkLogoError}
                    dark
                    testId="dark-logo"
                    sendLabel={t('brand.sendDarkLogo')}
                    replaceLabel={t('brand.replaceDarkLogo')}
                    removeLabel={t('brand.removeDarkLogo')}
                    onChoose={(event) => {
                      void logoChooser(setDarkLogo, setDarkLogoError)(event);
                    }}
                    onRemove={() => {
                      setDarkLogo(savedDarkLogo === null ? { kind: 'saved' } : { kind: 'removed' });
                      setDarkLogoError(undefined);
                    }}
                  />
                </Box>
                <Box component="section" aria-labelledby="visual-colors" sx={sectionSx}>
                  <Box component="h2" id="visual-colors" sx={sectionTitleSx}>
                    {t('visual.colors')}
                  </Box>
                  {fields.map((field) => (
                    <ColorField
                      key={field}
                      name={field}
                      value={colors[field]}
                      shown={previewed[field] ?? defaultOf[field]}
                      defaultColor={defaultOf[field]}
                      notes={notes[field]}
                      error={refused.includes(field) ? t('colors.invalid') : undefined}
                      onPreview={(hex) => {
                        setTrying((current) =>
                          hex === undefined
                            ? Object.fromEntries(
                                Object.entries(current).filter(([name]) => name !== field),
                              )
                            : { ...current, [field]: hex },
                        );
                      }}
                      onChange={(hex) => {
                        setChosen((current) => ({ ...current, [field]: hex }));
                        setRefused((current) => current.filter((name) => name !== field));
                      }}
                    />
                  ))}
                </Box>
              </Form>
              <Box
                sx={{
                  position: 'sticky',
                  top: 0,
                  minWidth: 0,
                  '@media (max-width: 900px)': { position: 'static' },
                }}
              >
                <ColorsPreview
                  colors={previewed}
                  name={typedName.trim() === '' ? (installation?.name ?? '') : typedName.trim()}
                  logo={shown}
                  darkLogo={shownDark}
                />
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
        <Button variant="outlined" disabled={!changed || isSubmitting} onClick={discard}>
          {t('visual.discard')}
        </Button>
        <Button variant="contained" type="submit" form="visual-form" disabled={!changed}>
          {t('visual.save')}
        </Button>
      </Box>
    </Box>
  );
}
