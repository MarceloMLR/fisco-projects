import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import Autocomplete from '@mui/material/Autocomplete';
import Box from '@mui/material/Box';
import InputBase from '@mui/material/InputBase';
import { useId, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { fieldControl } from '../components/fieldControl';

interface TimeZoneOption {
  id: string;
  label: string;
}

/** The name of a time zone in Portuguese and its id: "Horário de Brasília (America/Sao_Paulo)". */
function labelOf(id: string): string {
  const name = new Intl.DateTimeFormat('pt-BR', { timeZone: id, timeZoneName: 'longGeneric' })
    .formatToParts(new Date())
    .find((part) => part.type === 'timeZoneName')?.value;
  return name === undefined || name === id ? id : `${name} (${id})`;
}

let browserOptions: TimeZoneOption[] | undefined;

/** The time zones the browser knows, in alphabetical order, read once. */
function knownOptions(): TimeZoneOption[] {
  browserOptions ??= Intl.supportedValuesOf('timeZone')
    .map((id) => ({ id, label: labelOf(id) }))
    .sort((a, b) => a.label.localeCompare(b.label, 'pt-BR'));
  return browserOptions;
}

/**
 * Time zone of General settings (Select in the canvas): there are more than 400, so it searches by name or id as the
 * Administrator types, with the look of the design system select. The saved one is always an option.
 */
export function TimeZoneField({
  label,
  value,
  onChange,
  onBlur,
  error,
}: {
  label: string;
  value: string;
  onChange: (timeZone: string) => void;
  onBlur?: () => void;
  error?: string;
}) {
  const { t } = useTranslation();
  const inputId = useId();
  const helpId = `${inputId}-help`;
  const options = useMemo(() => {
    const known = knownOptions();
    return value === '' || known.some((option) => option.id === value)
      ? known
      : [...known, { id: value, label: labelOf(value) }];
  }, [value]);
  const chosen = options.find((option) => option.id === value) ?? null;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)', minWidth: 0 }}>
      <Box
        component="label"
        htmlFor={inputId}
        sx={{ fontSize: 13, lineHeight: '18px', fontWeight: 500, color: 'var(--text)' }}
      >
        {label}
      </Box>
      <Autocomplete<TimeZoneOption>
        id={inputId}
        value={chosen}
        onChange={(_event, option) => {
          onChange(option?.id ?? '');
        }}
        onBlur={onBlur}
        options={options}
        getOptionLabel={(option) => option.label}
        isOptionEqualToValue={(option, current) => option.id === current.id}
        noOptionsText={t('settings.noTimeZones')}
        popupIcon={<ExpandMoreOutlined />}
        slotProps={{ paper: { sx: { marginTop: 'var(--space-1)' } } }}
        sx={{
          // MUI keeps room on the right for icons it positions over the field; here they sit in the row, at the end.
          '&.MuiAutocomplete-hasPopupIcon .MuiAutocomplete-inputRoot, &.MuiAutocomplete-hasClearIcon .MuiAutocomplete-inputRoot':
            { paddingRight: 'var(--space-2)' },
        }}
        renderInput={({ slotProps: { input, htmlInput } }) => (
          <InputBase
            ref={input.ref}
            className={input.className}
            onMouseDown={input.onMouseDown}
            inputProps={{
              ...htmlInput,
              'aria-describedby': error === undefined ? undefined : helpId,
              'aria-invalid': error !== undefined,
            }}
            endAdornment={input.endAdornment}
            sx={{
              ...fieldControl('md'),
              width: '100%',
              padding: '0 var(--space-2) 0 var(--space-3)',
              ...(error === undefined ? {} : { borderColor: 'var(--status-danger-text)' }),
              '& input': { textOverflow: 'ellipsis' },
              '& .MuiAutocomplete-endAdornment': {
                position: 'static',
                display: 'flex',
                alignItems: 'center',
                transform: 'none',
              },
              '& .MuiAutocomplete-popupIndicator, & .MuiAutocomplete-clearIndicator': {
                padding: '2px',
                color: 'var(--text-secondary)',
              },
              '& .MuiSvgIcon-root': { fontSize: 'var(--icon-md)' },
            }}
          />
        )}
      />
      {error !== undefined && (
        <Box
          component="span"
          id={helpId}
          sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--status-danger-text)' }}
        >
          {error}
        </Box>
      )}
    </Box>
  );
}
