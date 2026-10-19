import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useId } from 'react';
import { useTranslation } from 'react-i18next';
import { TextField } from '../components/TextField';
import { defaultMainColor, hasContrast, normalizeHex } from '../theme/brandColors';

/**
 * Main color of General settings, as in the canvas: the swatch (fp-swatch), the color in hexadecimal, "Back to the
 * default" and the result of the contrast (DueDate), by icon, color and text (ADR-0065). Empty is the default color.
 */
export function MainColorField({
  value,
  onChange,
  onBlur,
  error,
}: {
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  error?: string;
}) {
  const { t } = useTranslation();
  const labelId = useId();
  const statusId = useId();
  const typed = value.trim();
  const hex = typed === '' ? defaultMainColor() : normalizeHex(typed);
  const approved = hex !== undefined && hasContrast(hex);

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
        {t('settings.mainColor')}
      </Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
        <Box
          component="span"
          aria-hidden="true"
          data-testid="color-swatch"
          sx={{
            flex: 'none',
            width: 32,
            height: 32,
            boxSizing: 'border-box',
            border: '1px solid var(--border-strong)',
            borderRadius: 'var(--radius-md)',
            // The swatch shows the color as it is typed; until it is a color, the default one.
            backgroundColor: hex ?? defaultMainColor(),
          }}
        />
        <TextField
          label={t('settings.mainColorHex')}
          hideLabel
          size="sm"
          width={130}
          autoComplete="off"
          spellCheck={false}
          placeholder={defaultMainColor()}
          value={value}
          error={error}
          // The result of the contrast describes the field; an error, when there is one, describes it instead.
          {...(error === undefined && hex !== undefined ? { 'aria-describedby': statusId } : {})}
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
          {t('settings.defaultColor')}
        </Button>
      </Box>
      {hex !== undefined && (
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
