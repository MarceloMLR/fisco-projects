import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import InputBase from '@mui/material/InputBase';
import MenuItem from '@mui/material/MenuItem';
import MuiSelect from '@mui/material/Select';

export interface SelectOption<Value extends string> {
  value: Value;
  label: string;
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
}: {
  /** Accessible name. */
  label: string;
  value: Value;
  options: readonly SelectOption<Value>[];
  onChange: (value: Value) => void;
  size?: 'sm' | 'md' | 'lg';
  width?: number;
}) {
  return (
    <MuiSelect<Value>
      value={value}
      onChange={(event) => {
        onChange(event.target.value as Value);
      }}
      IconComponent={ExpandMoreOutlined}
      input={<InputBase inputProps={{ 'aria-label': label }} />}
      MenuProps={{ slotProps: { paper: { sx: { marginTop: 'var(--space-1)' } } } }}
      sx={{
        boxSizing: 'border-box',
        width,
        height: `var(--control-${size})`,
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border-control)',
        borderRadius: 'var(--radius-md)',
        color: 'var(--text)',
        fontSize: 14,
        lineHeight: '20px',
        transition: 'border-color 150ms ease-out',
        '&:hover': { borderColor: 'var(--text-secondary)' },
        '&.Mui-focused': {
          borderColor: 'var(--focus-ring)',
          boxShadow: 'inset 0 0 0 1px var(--focus-ring)',
        },
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
        <MenuItem key={option.value} value={option.value}>
          {option.label}
        </MenuItem>
      ))}
    </MuiSelect>
  );
}
