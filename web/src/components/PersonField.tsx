import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import Autocomplete from '@mui/material/Autocomplete';
import InputBase from '@mui/material/InputBase';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useId, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { peopleQuery } from '../api/queries';
import { useDebounced } from '../hooks/useDebounced';
import { fieldControl } from './fieldControl';
import { FormField } from './FormField';

export interface PersonChoice {
  id: string;
  name: string;
}

const searchDelay = 300;
const optionsShown = 20;

/**
 * A person to choose in a form, such as the manager of a project: looks like the design system select and searches
 * the active people as the person types, so it works with any number of people. The person in the session is marked
 * "(você)".
 */
export function PersonField({
  label,
  optional,
  value,
  onChange,
  managersOnly = false,
  placeholder,
  youId,
  error,
  helper,
}: {
  label: string;
  /** The text that marks the field as optional; an optional field can be emptied. */
  optional?: string;
  value: PersonChoice | null;
  onChange: (person: PersonChoice | null) => void;
  /** Only the Administrators and the Project managers. */
  managersOnly?: boolean;
  /** What the empty field says, as "Sem líder técnico". */
  placeholder?: string;
  /** The person in the session, marked "(você)". */
  youId?: string;
  error?: string;
  helper?: string;
}) {
  const { t } = useTranslation();
  const helpId = useId();
  const [open, setOpen] = useState(false);
  const [input, setInput] = useState<string>();
  const search = useDebounced(input ?? '', searchDelay);
  const nameOf = (person: PersonChoice) =>
    person.id === youId ? t('projects.form.you', { name: person.name }) : person.name;
  // The typed text is the search, unless it is just the name of the person already chosen.
  const term = value !== null && search === nameOf(value) ? '' : search.trim();
  // Asked only with the list open; while the person types, the last options stay until the new ones arrive.
  const { data } = useQuery({
    ...peopleQuery({
      search: term === '' ? undefined : term,
      managers: managersOnly,
      limit: optionsShown,
    }),
    enabled: open,
    placeholderData: keepPreviousData,
  });
  const options = useMemo<PersonChoice[]>(
    () => data?.map(({ id, name }) => ({ id, name })) ?? [],
    [data],
  );
  const describedBy = error !== undefined || helper !== undefined ? helpId : undefined;

  return (
    <FormField label={label} optional={optional} error={error} helper={helper} helpId={helpId}>
      <Autocomplete<PersonChoice, false, boolean>
        open={open}
        onOpen={() => {
          setOpen(true);
        }}
        onClose={() => {
          setOpen(false);
        }}
        value={value}
        onChange={(_event, person) => {
          onChange(person);
        }}
        inputValue={input ?? (value === null ? '' : nameOf(value))}
        onInputChange={(_event, text, reason) => {
          setInput(text);
          if (reason === 'input' && text === '' && optional !== undefined) {
            onChange(null);
          }
        }}
        disableClearable={optional === undefined}
        fullWidth
        options={options}
        // The API already searched; the options are kept as they came, in alphabetical order.
        filterOptions={(people) => people}
        getOptionLabel={nameOf}
        isOptionEqualToValue={(option, chosen) => option.id === chosen.id}
        noOptionsText={t('projects.form.noPeople')}
        popupIcon={<ExpandMoreOutlined />}
        slotProps={{ paper: { sx: { marginTop: 'var(--space-1)' } } }}
        // The buttons sit in the flow of the field (position static), so the room MUI keeps for them on the right
        // is taken back, also with both the arrow and the clear button (an optional field with a person chosen).
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
            placeholder={placeholder}
            fullWidth
            endAdornment={inputSlot.endAdornment}
            sx={{
              ...fieldControl('md'),
              width: '100%',
              ...(error === undefined ? {} : { borderColor: 'var(--status-danger-text)' }),
              padding: '0 var(--space-2) 0 var(--space-3)',
              '& input::placeholder': { color: 'var(--text-placeholder)', opacity: 1 },
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
    </FormField>
  );
}
