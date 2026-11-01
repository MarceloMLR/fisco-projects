import Box from '@mui/material/Box';
import type { ComponentProps } from 'react';
import { FieldFrame } from './FieldFrame';
import { fieldInputSx, useFieldIds } from './fieldControl';

export interface TextAreaProps extends ComponentProps<'textarea'> {
  label: string;
  /** Message of a field with an error; it replaces the helper text. */
  error?: string;
  helper?: string;
  width?: number;
  /** The text that marks the field as optional, as "(opcional)", out of its accessible name. */
  optional?: string;
}

/**
 * Text field of several lines of the design system (TextField multiline, fp-field__control--multiline): three lines
 * at first, and the person may make it taller.
 */
export function TextArea({
  label,
  error,
  helper,
  width,
  optional,
  id,
  ref,
  rows = 3,
  ...textarea
}: TextAreaProps) {
  const { inputId, helpId } = useFieldIds(id);
  const help = error ?? helper;

  return (
    <FieldFrame
      inputId={inputId}
      helpId={helpId}
      label={label}
      error={error}
      helper={helper}
      width={width}
      optional={optional}
      multiline
    >
      <Box
        component="textarea"
        id={inputId}
        ref={ref}
        rows={rows}
        aria-invalid={error === undefined ? undefined : true}
        aria-describedby={help === undefined ? undefined : helpId}
        {...textarea}
        sx={{ ...fieldInputSx, minHeight: 80, resize: 'vertical' }}
      />
    </FieldFrame>
  );
}
