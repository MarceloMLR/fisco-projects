import CodeOutlined from '@mui/icons-material/CodeOutlined';
import FactCheckOutlined from '@mui/icons-material/FactCheckOutlined';
import type SvgIcon from '@mui/material/SvgIcon';
import type { TicketTeam } from '../api/client';

/** The icon of each team (canvas AbrirChamado). */
export const teamIcons: Record<TicketTeam, typeof SvgIcon> = {
  Functional: FactCheckOutlined,
  Development: CodeOutlined,
};

/** The number of a ticket from the address, as in /tickets/104; null for anything else. */
export function ticketNumberOf(value: string | undefined): number | null {
  if (value === undefined || !/^\d{1,9}$/.test(value)) {
    return null;
  }
  const number = Number(value);
  return number > 0 ? number : null;
}
