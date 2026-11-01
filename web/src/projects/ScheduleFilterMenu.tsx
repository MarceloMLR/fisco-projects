import CheckOutlined from '@mui/icons-material/CheckOutlined';
import ExpandMoreOutlined from '@mui/icons-material/ExpandMoreOutlined';
import FilterListOutlined from '@mui/icons-material/FilterListOutlined';
import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import ListSubheader from '@mui/material/ListSubheader';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '../components/Button';
import { filterText, type OwnerOption, plainFilters, type ScheduleFilter } from './scheduleFilter';

/** The key of "Todas as tarefas e marcos", without a filter. */
const noFilterKey = 'all';

const sameFilter = (one: ScheduleFilter | undefined, other: ScheduleFilter | undefined) =>
  one?.kind === other?.kind &&
  (one?.kind !== 'owner' || other?.kind !== 'owner' || one.owner === other.owner);

/**
 * "Filtrar" in the bar of the tab and in Expandir (GanttA-3): a menu of one filter at a time, with "Por responsável"
 * below the others; the button says the filter chosen.
 */
export function ScheduleFilterMenu({
  filter,
  owners,
  onChange,
}: {
  filter: ScheduleFilter | undefined;
  owners: readonly OwnerOption[];
  onChange: (filter: ScheduleFilter | undefined) => void;
}) {
  const { t } = useTranslation();
  const [anchor, setAnchor] = useState<HTMLElement>();
  const choose = (chosen: ScheduleFilter | undefined) => {
    setAnchor(undefined);
    onChange(chosen);
  };
  const item = (key: string, label: string, value: ScheduleFilter | undefined) => {
    const current = sameFilter(filter, value);
    return (
      <MenuItem
        key={key}
        role="menuitemradio"
        aria-checked={current}
        selected={current}
        onClick={() => {
          choose(value);
        }}
      >
        <Box component="span" sx={{ flex: 1 }}>
          {label}
        </Box>
        {current && <CheckOutlined aria-hidden sx={{ color: 'var(--action-text) !important' }} />}
      </MenuItem>
    );
  };
  return (
    <>
      <Button
        variant="outlined"
        size="small"
        aria-haspopup="menu"
        aria-expanded={anchor !== undefined}
        startIcon={<FilterListOutlined />}
        endIcon={<ExpandMoreOutlined />}
        onClick={(event) => {
          setAnchor(event.currentTarget);
        }}
        sx={
          filter === undefined
            ? undefined
            : { color: 'var(--action-text)', borderColor: 'var(--action-text)' }
        }
      >
        {filter === undefined
          ? t('projects.schedule.filter.button')
          : filterText(filter, owners, t)}
      </Button>
      <Menu
        anchorEl={anchor}
        open={anchor !== undefined}
        onClose={() => {
          setAnchor(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
        slotProps={{
          paper: { sx: { minWidth: 240, maxHeight: 420, marginTop: 'var(--space-1)' } },
          list: { 'aria-label': t('projects.schedule.filter.label') },
        }}
      >
        {item(noFilterKey, t('projects.schedule.filter.all'), undefined)}
        {plainFilters.map((kind) =>
          item(kind, t(`projects.schedule.filter.kinds.${kind}`), { kind }),
        )}
        <Divider sx={{ margin: 'var(--space-1) 0' }} />
        <ListSubheader>{t('projects.schedule.filter.byOwner')}</ListSubheader>
        {owners.map((option) =>
          item(`owner-${option.owner}`, option.label, { kind: 'owner', owner: option.owner }),
        )}
      </Menu>
    </>
  );
}
