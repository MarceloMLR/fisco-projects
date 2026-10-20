import Box from '@mui/material/Box';
import type SvgIcon from '@mui/material/SvgIcon';
import type { ComponentProps } from 'react';
import { FieldFrame } from './FieldFrame';
import { fieldInputSx, useFieldIds } from './fieldControl';

export interface TextFieldProps extends Omit<ComponentProps<'input'>, 'size'> {
  label: string;
  /** Message of a field with an error; it replaces the helper text. */
  error?: string;
  helper?: string;
  size?: 'sm' | 'md' | 'lg';
  /** Icon at the start of the control, such as the search icon. */
  icon?: typeof SvgIcon;
  /** Keeps the label only for screen readers, as in the search above a table. */
  hideLabel?: boolean;
  width?: number;
}

/** Text field of the design system (TextField, fp-field): label above, control, helper or error below. */
export function TextField({
  label,
  error,
  helper,
  size = 'md',
  icon,
  hideLabel = false,
  width,
  id,
  ref,
  readOnly,
  ...input
}: TextFieldProps) {
  const { inputId, helpId } = useFieldIds(id);
  const help = error ?? helper;

  return (
    <FieldFrame
      inputId={inputId}
      helpId={helpId}
      label={label}
      hideLabel={hideLabel}
      error={error}
      helper={helper}
      width={width}
      size={size}
      readOnly={readOnly}
      icon={icon}
    >
      <Box
        component="input"
        id={inputId}
        ref={ref}
        aria-invalid={error === undefined ? undefined : true}
        aria-describedby={help === undefined ? undefined : helpId}
        readOnly={readOnly}
        {...input}
        sx={fieldInputSx}
      />
    </FieldFrame>
  );
}
