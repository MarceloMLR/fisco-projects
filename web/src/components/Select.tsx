import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import InputBase from '@mui/material/InputBase';
import MenuItem from '@mui/material/MenuItem';
import MuiSelect from '@mui/material/Select';
import { fieldControl } from './fieldControl';

export interface SelectOption<Value extends string> {
  value: Value;
  label: string;
  /** Shown but not chosen, such as a role the person may not have. */
  disabled?: boolean;
}

/**
 * Selection field of the design system (Select, fp-field with fp-select): same control as the text field, with
 * the options in a menu. Used without a visible label, as in the filters of a table, it takes an accessible name.
 */
export function Select<Value extends string>({
  label,
  value,
  options,
  onChange,
  size = 'md',
  width,
  invalid = false,
  describedBy,
  disabled = false,
}: {
  /** Accessible name. */
  label: string;
  value: Value;
  options: readonly SelectOption<Value>[];
  onChange: (value: Value) => void;
  size?: 'sm' | 'md' | 'lg';
  width?: number;
  /** Shows the control with the error border, as a field with an error. */
  invalid?: boolean;
  /** Id of the helper or the error that describes the field. */
  describedBy?: string;
  /** Shown but not changed, such as a role the person may not change. */
  disabled?: boolean;
}) {
  return (
    <MuiSelect<Value>
      value={value}
      // An empty value shows its option, such as "Escolha o fornecedor" or "Manter como está".
      displayEmpty
      disabled={disabled}
      onChange={(event) => {
        onChange(event.target.value as Value);
      }}
      IconComponent={ExpandMoreOutlined}
      input={
        <InputBase
          inputProps={{
            'aria-label': label,
            'aria-invalid': invalid ? true : undefined,
            'aria-describedby': describedBy,
          }}
        />
      }
      MenuProps={{ slotProps: { paper: { sx: { marginTop: 'var(--space-1)' } } } }}
      sx={{
        ...fieldControl(size, width),
        ...(invalid ? { borderColor: 'var(--status-danger-text)' } : {}),
        '& .MuiSelect-select': {
          display: 'flex',
          alignItems: 'center',
          height: '100%',
          boxSizing: 'border-box',
          padding: '0 var(--space-8) 0 var(--space-3)',
        },
        '& .MuiSelect-select:focus': { backgroundColor: 'transparent' },
        '& .MuiSelect-icon': {
          right: 'var(--space-2)',
          fontSize: 'var(--icon-md)',
          color: 'var(--text-secondary)',
        },
      }}
    >
      {options.map((option) => (
        <MenuItem key={option.value} value={option.value} disabled={option.disabled}>
          {option.label}
        </MenuItem>
      ))}
    </MuiSelect>
  );
}
