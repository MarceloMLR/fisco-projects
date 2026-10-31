import Box, { type BoxProps } from '@mui/material/Box';
import type { ReactNode, SubmitEvent } from 'react';
import { FormPending, usePendingAction } from './pending';

export type FormSubmit = (event: SubmitEvent<HTMLFormElement>) => unknown;

/**
 * The form of the screens (ADR-0131): while the promise its sending gives runs, its submit buttons are busy and a new
 * sending, by a button or by Enter in a field, does nothing. The fields are checked by the form of each screen, so the
 * browser does not check them (noValidate).
 */
export function Form({
  onSubmit,
  children,
  ...props
}: Omit<BoxProps<'form'>, 'onSubmit' | 'component'> & { onSubmit: FormSubmit; children: ReactNode }) {
  const [sending, run] = usePendingAction(onSubmit);
  return (
    <FormPending.Provider value={sending}>
      <Box component="form" noValidate {...props} onSubmit={run}>
        {children}
      </Box>
    </FormPending.Provider>
  );
}
