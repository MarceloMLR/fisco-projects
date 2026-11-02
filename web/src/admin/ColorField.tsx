import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import ErrorOutlined from '@mui/icons-material/ErrorOutlined';
import InfoOutlined from '@mui/icons-material/InfoOutlined';
import Box from '@mui/material/Box';
import type SvgIcon from '@mui/material/SvgIcon';
import { type ChangeEvent, type KeyboardEvent, type MouseEvent, useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '../components/Button';
import { ColorPicker } from '../components/ColorPicker';
import type { ColorNote } from '../theme/brandColors';
import { hueGradient, parseColor } from '../theme/colorFormats';

/** Which of the three colors of the installation a field is (ADR-0146). */
export type ColorFieldName = 'main' | 'secondary' | 'sidebar';

/** A line under the field, by icon, color and text (ADR-0065). */
function Note({ tone, icon: Icon, text }: { tone: string; icon: typeof SvgIcon; text: string }) {
  return (
    <Box
      component="span"
      sx={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: '6px',
        fontSize: 13,
        lineHeight: '18px',
        color: tone,
      }}
    >
      <Icon sx={{ flex: 'none', fontSize: 16, marginTop: '1px' }} />
      {text}
    </Box>
  );
}

/**
 * A color of the installation (ADR-0146): the swatch that opens the color picker, the value, which takes the color
 * typed or pasted in HEX, RGB or HSL and keeps it as #RRGGBB on leaving the field or with Enter, "Voltar ao padrão",
 * where the color shows and what the screens adjusted over it. While a color is typed or chosen in the picker, the
 * preview shows it (onPreview); it changes the field only when used (onChange).
 */
export function ColorField({
  name,
  value,
  shown,
  defaultColor,
  notes,
  error,
  onPreview,
  onChange,
}: {
  name: ColorFieldName;
  /** The color of the field, #RRGGBB, or null for the default one. */
  value: string | null;
  /** The color the swatch shows: the one being chosen, or the one the default gives. */
  shown: string;
  /** The color the default gives, in the placeholder of the value. */
  defaultColor: string;
  notes: readonly ColorNote[];
  /** A color the server refused. */
  error?: string;
  /** The color being typed or chosen, for the preview; undefined when there is none. */
  onPreview: (hex: string | undefined) => void;
  onChange: (hex: string | null) => void;
}) {
  const { t } = useTranslation();
  const labelId = useId();
  const whereId = useId();
  const notesId = useId();
  // The swatch and the color the picker opened with, while it is open.
  const [picking, setPicking] = useState<{ anchor: HTMLElement; from: string } | null>(null);
  // The value as typed, until it is used; null shows the color of the field.
  const [typed, setTyped] = useState<string | null>(null);
  const [invalid, setInvalid] = useState(false);
  const label = t(`colors.${name}.label`);

  const commit = () => {
    if (typed === null) {
      return;
    }
    const text = typed.trim();
    if (text === '') {
      setTyped(null);
      setInvalid(false);
      onPreview(undefined);
      onChange(null);
      return;
    }
    const hex = parseColor(text);
    if (hex === undefined) {
      setInvalid(true);
      return;
    }
    setTyped(null);
    setInvalid(false);
    onPreview(undefined);
    onChange(hex);
  };

  const confirmation =
    value === null || notes.length > 0
      ? undefined
      : name === 'secondary'
        ? 'secondaryAsIs'
        : 'goodContrast';
  const message = invalid ? t('colors.invalid') : error;

  return (
    <Box
      role="group"
      aria-labelledby={labelId}
      sx={{ display: 'flex', flexDirection: 'column', gap: '6px', minWidth: 0 }}
    >
      <Box id={labelId} component="span" sx={{ fontSize: 13, lineHeight: '18px', fontWeight: 500 }}>
        {label}{' '}
        <Box component="span" sx={{ fontWeight: 400, color: 'var(--text-secondary)' }}>
          {t('colors.optional')}
        </Box>
      </Box>
      <Box
        id={whereId}
        component="span"
        sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
      >
        {t(`colors.${name}.where`)}
      </Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', flexWrap: 'wrap' }}>
        <Box
          component="button"
          type="button"
          data-testid={`${name}-swatch`}
          aria-haspopup="dialog"
          aria-expanded={picking !== null}
          aria-label={t('colors.choose', { color: label.toLowerCase() })}
          onClick={(event: MouseEvent<HTMLButtonElement>) => {
            setPicking({ anchor: event.currentTarget, from: shown });
          }}
          sx={{
            position: 'relative',
            flex: 'none',
            width: 36,
            height: 36,
            padding: 0,
            border: '1px solid var(--border-strong)',
            borderRadius: 'var(--radius-md)',
            backgroundColor: shown,
            cursor: 'pointer',
            '&:focus-visible': { outline: '2px solid var(--focus-ring)', outlineOffset: 2 },
            '&::after': {
              content: '""',
              position: 'absolute',
              right: 2,
              bottom: 2,
              width: 10,
              height: 10,
              borderRadius: '2px',
              border: '1px solid var(--bg-surface)',
              backgroundImage: hueGradient('135deg'),
            },
          }}
        />
        <Box
          component="input"
          aria-label={t(`colors.${name}.value`)}
          aria-describedby={`${whereId} ${notesId}`}
          aria-invalid={message === undefined ? undefined : true}
          autoComplete="off"
          spellCheck={false}
          placeholder={t(`colors.${name}.placeholder`, { color: defaultColor })}
          value={typed ?? value ?? ''}
          onChange={(event: ChangeEvent<HTMLInputElement>) => {
            const text = event.target.value;
            setTyped(text);
            setInvalid(false);
            onPreview(parseColor(text));
          }}
          onBlur={commit}
          onKeyDown={(event: KeyboardEvent<HTMLInputElement>) => {
            if (event.key === 'Enter') {
              event.preventDefault();
              commit();
            }
          }}
          sx={{
            height: 36,
            flex: '1 1 140px',
            minWidth: 0,
            maxWidth: 200,
            boxSizing: 'border-box',
            padding: '0 10px',
            border: '1px solid var(--border-control)',
            borderRadius: 'var(--radius-md)',
            backgroundColor: 'var(--bg-surface)',
            color: 'var(--text)',
            font: 'inherit',
            fontFamily: 'var(--font-mono)',
            fontSize: 13,
            '&::placeholder': { color: 'var(--text-secondary)', opacity: 1 },
            '&[aria-invalid="true"]': { borderColor: 'var(--danger)' },
            '&:focus-visible': { outline: '2px solid var(--focus-ring)', outlineOffset: 1 },
          }}
        />
        <Button
          variant="text"
          size="small"
          disabled={value === null && typed === null}
          onClick={() => {
            setTyped(null);
            setInvalid(false);
            onPreview(undefined);
            onChange(null);
          }}
        >
          {t('colors.defaultColor')}
        </Button>
      </Box>
      <Box id={notesId} role="status" sx={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
        {message !== undefined ? (
          <Note tone="var(--status-danger-text)" icon={ErrorOutlined} text={message} />
        ) : (
          <>
            {confirmation !== undefined && (
              <Note
                tone="var(--status-success-text)"
                icon={CheckCircleOutlined}
                text={t(`colors.notes.${confirmation}`)}
              />
            )}
            {notes.map((note) => (
              <Note
                key={note}
                tone="var(--status-info-text)"
                icon={InfoOutlined}
                text={t(`colors.notes.${note}`)}
              />
            ))}
          </>
        )}
      </Box>
      <ColorPicker
        anchor={picking?.anchor ?? null}
        label={label}
        initial={picking?.from ?? shown}
        onChange={(hex) => {
          onPreview(hex);
        }}
        onUse={(hex) => {
          setPicking(null);
          setTyped(null);
          setInvalid(false);
          onPreview(undefined);
          onChange(hex);
        }}
        onCancel={() => {
          setPicking(null);
          onPreview(undefined);
        }}
      />
    </Box>
  );
}
