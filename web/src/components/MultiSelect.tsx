import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import Autocomplete from '@mui/material/Autocomplete';
import InputBase from '@mui/material/InputBase';
import { useId, useMemo } from 'react';
import { fieldControl } from './fieldControl';
import { FormField } from './FormField';

export interface MultiSelectOption {
  value: string;
  label: string;
  /** The group the option is listed under, such as the phase of a task. */
  group?: string;
}

/**
 * A field to choose several options, such as the tasks a task depends on: looks like the design system select
 * (fp-field), with the chosen options as chips in the control and a search by typing. The options keep the order
 * given, under their groups.
 */
export function MultiSelect({
  label,
  optional,
  value,
  options,
  onChange,
  placeholder,
  noOptionsText,
  error,
  helper,
  disabled = false,
}: {
  label: string;
  /** The text that marks the field as optional, as "(opcional)". */
  optional?: string;
  value: readonly string[];
  options: readonly MultiSelectOption[];
  onChange: (value: string[]) => void;
  placeholder?: string;
  /** What the list says without options. */
  noOptionsText: string;
  error?: string;
  helper?: string;
  disabled?: boolean;
}) {
  const helpId = useId();
  const chosen = useMemo(
    () => options.filter((option) => value.includes(option.value)),
    [options, value],
  );
  const describedBy = error !== undefined || helper !== undefined ? helpId : undefined;
  const grouped = options.some((option) => option.group !== undefined);

  return (
    <FormField label={label} optional={optional} error={error} helper={helper} helpId={helpId}>
      <Autocomplete<MultiSelectOption, true>
        multiple
        disabled={disabled}
        value={chosen}
        onChange={(_event, options) => {
          onChange(options.map((option) => option.value));
        }}
        options={options}
        groupBy={grouped ? (option) => option.group ?? '' : undefined}
        getOptionLabel={(option) => option.label}
        getOptionKey={(option) => option.value}
        isOptionEqualToValue={(option, other) => option.value === other.value}
        disableCloseOnSelect
        fullWidth
        noOptionsText={noOptionsText}
        popupIcon={<ExpandMoreOutlined />}
        slotProps={{
          paper: { sx: { marginTop: 'var(--space-1)' } },
          chip: { size: 'small' },
        }}
        sx={{
          '&.MuiAutocomplete-hasPopupIcon .MuiAutocomplete-inputRoot, &.MuiAutocomplete-hasClearIcon .MuiAutocomplete-inputRoot, &.MuiAutocomplete-hasPopupIcon.MuiAutocomplete-hasClearIcon .MuiAutocomplete-inputRoot':
            { paddingRight: 'var(--space-2)' },
        }}
        renderInput={({ slotProps: { input: inputSlot, htmlInput } }) => (
          <InputBase
            ref={inputSlot.ref}
            className={inputSlot.className}
            onMouseDown={inputSlot.onMouseDown}
            inputProps={{
              ...htmlInput,
              'aria-label': label,
              'aria-invalid': error === undefined ? undefined : true,
              'aria-describedby': describedBy,
            }}
            placeholder={chosen.length === 0 ? placeholder : undefined}
            fullWidth
            startAdornment={inputSlot.startAdornment}
            endAdornment={inputSlot.endAdornment}
            sx={{
              ...fieldControl('md'),
              width: '100%',
              height: 'auto',
              minHeight: 'var(--control-md)',
              flexWrap: 'wrap',
              gap: 'var(--space-1)',
              padding: 'var(--space-1) var(--space-2) var(--space-1) var(--space-3)',
              ...(error === undefined ? {} : { borderColor: 'var(--status-danger-text)' }),
              '& input': { flex: '1 0 96px', minWidth: 96, textOverflow: 'ellipsis' },
              '& input::placeholder': { color: 'var(--text-placeholder)', opacity: 1 },
              '& .MuiChip-root': {
                height: 24,
                margin: 0,
                borderRadius: 'var(--radius-sm)',
                backgroundColor: 'var(--bg-subtle)',
                color: 'var(--text)',
                fontSize: 13,
              },
              '& .MuiAutocomplete-endAdornment': {
                position: 'static',
                display: 'flex',
                alignItems: 'center',
                marginLeft: 'auto',
                transform: 'none',
              },
              '& .MuiAutocomplete-popupIndicator, & .MuiAutocomplete-clearIndicator': {
                padding: '2px',
                color: 'var(--text-secondary)',
              },
              '& .MuiAutocomplete-endAdornment .MuiSvgIcon-root': { fontSize: 'var(--icon-md)' },
            }}
          />
        )}
      />
    </FormField>
  );
}
