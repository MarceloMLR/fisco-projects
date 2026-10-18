import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import Autocomplete from '@mui/material/Autocomplete';
import InputBase from '@mui/material/InputBase';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { usersQuery } from '../api/queries';
import { fieldControl } from '../components/fieldControl';
import { useDebounced } from '../hooks/useDebounced';

export interface PersonOption {
  id: string;
  name: string;
}

const searchDelay = 300;
const optionsShown = 20;

/**
 * Filter of the Audit by person: looks like the design system select and searches the people as the administrator
 * types, so it works with any number of people. Empty, it shows every person.
 */
export function PersonFilter({
  value,
  onChange,
  width,
}: {
  value: PersonOption | null;
  onChange: (person: PersonOption | null) => void;
  width: number;
}) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [input, setInput] = useState('');
  const search = useDebounced(input, searchDelay);
  // The typed text is the search, unless it is just the name of the person already chosen.
  const term = value !== null && search === value.name ? '' : search.trim();
  // Asked only with the list open; while the administrator types, the last options stay until the new ones arrive.
  const { data } = useQuery({
    ...usersQuery({ search: term === '' ? undefined : term, pageSize: optionsShown }),
    enabled: open,
    placeholderData: keepPreviousData,
  });
  const options = useMemo<PersonOption[]>(
    () => data?.items.map(({ id, name }) => ({ id, name })) ?? [],
    [data],
  );

  return (
    <Autocomplete<PersonOption>
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
      inputValue={input}
      onInputChange={(_event, text, reason) => {
        setInput(text);
        if (reason === 'input' && text === '') {
          onChange(null);
        }
      }}
      options={options}
      // The API already searched; the options are kept as they came, in alphabetical order.
      filterOptions={(people) => people}
      getOptionLabel={(person) => person.name}
      isOptionEqualToValue={(option, chosen) => option.id === chosen.id}
      noOptionsText={t('audit.filters.noPeople')}
      popupIcon={<ExpandMoreOutlined />}
      slotProps={{ paper: { sx: { marginTop: 'var(--space-1)' } } }}
      sx={{ width }}
      renderInput={({ slotProps: { input, htmlInput } }) => (
        <InputBase
          ref={input.ref}
          className={input.className}
          onMouseDown={input.onMouseDown}
          inputProps={{ ...htmlInput, 'aria-label': t('audit.filters.person') }}
          placeholder={t('audit.filters.allPeople')}
          endAdornment={input.endAdornment}
          sx={{
            ...fieldControl('sm', width),
            padding: '0 var(--space-2) 0 var(--space-3)',
            '& input::placeholder': { color: 'var(--text)', opacity: 1 },
            // A name that does not fit ends in an ellipsis instead of being cut.
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
  );
}
