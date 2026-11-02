import Box from '@mui/material/Box';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { type SubmitEvent, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../api/client';
import { installationQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { Button } from '../components/Button';
import { Form } from '../components/Form';
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
import { ColorField, type ColorFieldName } from './ColorField';
import { ColorsPreview } from './ColorsPreview';

const fields: readonly ColorFieldName[] = ['main', 'secondary', 'sidebar'];

const apiField: Record<ColorFieldName, 'mainColor' | 'secondaryColor' | 'sidebarColor'> = {
  main: 'mainColor',
  secondary: 'secondaryColor',
  sidebar: 'sidebarColor',
};

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
 * Colors, in the Administration (ADR-0146): the main color, the secondary one and the one of the sidebar, each
 * optional, with the preview of the screens in the light and the dark theme, and the bar of actions with Descartar and
 * Salvar alterações, enabled only when a color changed. Saved on their own, by the Administrator.
 */
export function ColorsPage() {
  const { t } = useTranslation();
  const notify = useNotify();
  const queryClient = useQueryClient();
  const { data: installation, isError } = useQuery(installationQuery());
  const saved = useMemo(() => kept(colorsOf(installation)), [installation]);
  // The colors changed on this screen, until they are saved or discarded.
  const [chosen, setChosen] = useState<Partial<BrandColorChoice>>({});
  // The color being typed or picked in a field, shown only in the preview.
  const [trying, setTrying] = useState<Partial<Record<ColorFieldName, string>>>({});
  const [refused, setRefused] = useState<ColorFieldName[]>([]);
  const colors = useMemo(() => ({ ...saved, ...chosen }), [saved, chosen]);
  const previewed = useMemo(() => ({ ...colors, ...trying }), [colors, trying]);
  const notes = useMemo(() => colorNotes(previewed), [previewed]);
  const changed = fields.some((field) => colors[field] !== saved[field]);
  const defaults = useMemo(() => brandTokens(null).light, []);

  // What each field shows when it has no color of its own: the color the design system (or the main color) gives.
  const defaultOf: Record<ColorFieldName, string> = {
    main: defaultMainColor(),
    secondary: previewed.main ?? defaultMainColor(),
    sidebar: normalizeHex(defaults['sidebar-bg']) ?? defaults['sidebar-bg'],
  };

  const discard = () => {
    setChosen({});
    setTrying({});
    setRefused([]);
  };

  const save = async (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
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
      notify(t('colors.saveFailed'), { tone: 'error' });
      return;
    }
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.installation }),
      queryClient.invalidateQueries({ queryKey: queryKeys.audit }),
    ]);
    setChosen({});
    setRefused([]);
    notify(t('colors.saved'));
  };

  return (
    <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
      <Page subtitle={t('colors.subtitle')}>
        <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          {isError && installation === undefined ? (
            <Alert tone="danger">{t('colors.loadFailed')}</Alert>
          ) : (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: 'minmax(0, 380px) minmax(0, 1fr)',
                gap: 'var(--space-5)',
                alignItems: 'start',
                '@media (max-width: 900px)': { gridTemplateColumns: 'minmax(0, 1fr)' },
              }}
            >
              <Form
                id="colors-form"
                aria-label={t('colors.fieldsLabel')}
                onSubmit={(event) => save(event)}
                sx={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 'var(--space-5)',
                  minWidth: 0,
                  padding: 'var(--space-5)',
                  backgroundColor: 'var(--bg-surface)',
                  border: '1px solid var(--border)',
                  borderRadius: 'var(--radius-lg)',
                }}
              >
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
                  name={installation?.name ?? ''}
                  logo={installation?.logoUrl ?? null}
                  darkLogo={installation?.darkLogoUrl ?? null}
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
        <Button variant="outlined" disabled={!changed} onClick={discard}>
          {t('settings.discard')}
        </Button>
        <Button variant="contained" type="submit" form="colors-form" disabled={!changed}>
          {t('settings.save')}
        </Button>
      </Box>
    </Box>
  );
}
