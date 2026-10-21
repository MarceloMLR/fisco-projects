import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useId } from 'react';
import { useTranslation } from 'react-i18next';
import { TextField } from '../components/TextField';
import { defaultMainColor, hasContrast, normalizeHex } from '../theme/brandColors';
import { color } from '../theme/tokens';

/**
 * A color of General settings, as in the canvas: the swatch (fp-swatch), which opens the color picker of the browser
 * when clicked (ADR-0100), the color in hexadecimal and the button that empties it. Empty is the color of the design
 * system (shown in the swatch). With the contrast check, the result shows by icon, color and text (ADR-0065).
 */
export function ColorField({
  label,
  hexLabel,
  emptyColor,
  emptyLabel,
  checkContrast,
  hint,
  value,
  onChange,
  onBlur,
  error,
}: {
  label: string;
  hexLabel: string;
  /** The color while the field is empty, as #RRGGBB. */
  emptyColor: string;
  /** The button that empties the field. */
  emptyLabel: string;
  checkContrast: boolean;
  hint?: string;
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  error?: string;
}) {
  const { t } = useTranslation();
  const labelId = useId();
  const statusId = useId();
  const hintId = useId();
  const typed = value.trim();
  const hex = typed === '' ? emptyColor : normalizeHex(typed);
  const approved = hex !== undefined && hasContrast(hex);
  // The result of the contrast describes the field; an error, when there is one, describes it instead.
  const describedBy = [
    hint === undefined ? undefined : hintId,
    error === undefined && hex !== undefined && checkContrast ? statusId : undefined,
  ]
    .filter((id) => id !== undefined)
    .join(' ');

  return (
    <Box
      role="group"
      aria-labelledby={labelId}
      sx={{ display: 'flex', flexDirection: 'column', gap: '6px', minWidth: 0 }}
    >
      <Box
        component="span"
        id={labelId}
        sx={{ fontSize: 13, lineHeight: '18px', fontWeight: 500, color: 'var(--text)' }}
      >
        {label}
      </Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
        <Box
          component="span"
          sx={{
            position: 'relative',
            flex: 'none',
            width: 32,
            height: 32,
            borderRadius: 'var(--radius-md)',
            '&:focus-within': { outline: '2px solid var(--focus-ring)', outlineOffset: 2 },
          }}
        >
          <Box
            component="span"
            aria-hidden="true"
            data-testid="color-swatch"
            sx={{
              display: 'block',
              width: '100%',
              height: '100%',
              boxSizing: 'border-box',
              border: '1px solid var(--border-strong)',
              borderRadius: 'var(--radius-md)',
              // The swatch shows the color as it is typed; until it is a color, the one of the empty field.
              backgroundColor: hex ?? emptyColor,
            }}
          />
          {/* The picker of the browser, over the swatch: clicking the swatch opens it (ADR-0100). */}
          <Box
            component="input"
            type="color"
            aria-label={t('settings.pickColor', { color: label.toLowerCase() })}
            value={(hex ?? emptyColor).toLowerCase()}
            onChange={(event) => {
              onChange((normalizeHex(event.target.value) ?? event.target.value).toUpperCase());
            }}
            onBlur={onBlur}
            sx={{
              position: 'absolute',
              inset: 0,
              width: '100%',
              height: '100%',
              margin: 0,
              padding: 0,
              border: 0,
              opacity: 0,
              cursor: 'pointer',
            }}
          />
        </Box>
        <TextField
          label={hexLabel}
          hideLabel
          size="sm"
          width={130}
          autoComplete="off"
          spellCheck={false}
          placeholder={emptyColor}
          value={value}
          error={error}
          {...(describedBy === '' ? {} : { 'aria-describedby': describedBy })}
          onChange={(event) => {
            onChange(event.target.value);
          }}
          onBlur={onBlur}
        />
        <Button
          variant="text"
          size="small"
          disabled={typed === ''}
          onClick={() => {
            onChange('');
          }}
        >
          {emptyLabel}
        </Button>
      </Box>
      {hint !== undefined && (
        <Box
          component="span"
          id={hintId}
          sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
        >
          {hint}
        </Box>
      )}
      {checkContrast && hex !== undefined && (
        <Box
          component="span"
          id={statusId}
          role="status"
          sx={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 'var(--space-1)',
            fontSize: 13,
            lineHeight: '18px',
            whiteSpace: 'nowrap',
            color: approved ? 'var(--status-success-text)' : 'var(--status-danger-text)',
            fontWeight: approved ? 400 : 500,
            '& > svg': { fontSize: 'var(--icon-sm)' },
          }}
        >
          {approved ? <CheckCircleOutlined aria-hidden /> : <ErrorOutlineOutlined aria-hidden />}
          {approved ? t('settings.contrastApproved') : t('settings.contrastLow')}
        </Box>
      )}
    </Box>
  );
}

/**
 * Main color of General settings (ADR-0082): empty is the default color, and the color needs its contrast with white.
 */
export function MainColorField(props: {
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  error?: string;
}) {
  const { t } = useTranslation();
  return (
    <ColorField
      {...props}
      label={t('settings.mainColor')}
      hexLabel={t('settings.mainColorHex')}
      emptyColor={defaultMainColor()}
      emptyLabel={t('settings.defaultColor')}
      checkContrast
    />
  );
}

/**
 * Secondary color of General settings (ADR-0100): empty is no secondary color, and the swatch shows the sidebar of the
 * design system; any color, since the texts over it take their contrast.
 */
export function SecondaryColorField(props: {
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  error?: string;
}) {
  const { t } = useTranslation();
  return (
    <ColorField
      {...props}
      label={t('settings.secondaryColor')}
      hexLabel={t('settings.secondaryColorHex')}
      emptyColor={normalizeHex(color('sidebar-bg', 'light')) ?? color('sidebar-bg', 'light')}
      emptyLabel={t('settings.noSecondaryColor')}
      checkContrast={false}
      hint={t('settings.secondaryHint')}
    />
  );
}
