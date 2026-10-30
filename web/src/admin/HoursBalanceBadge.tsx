import { useTranslation } from 'react-i18next';
import type { HoursBalance } from '../api/client';
import { StatusBadge } from '../components/StatusBadge';
import { balanceLook, balanceText } from './projectHours';

/** The balance of the hours of a project by icon, color and text (ADR-0065, ADR-0128): "28 h acima", "Restam 14 h". */
export function HoursBalanceBadge({ balance }: { balance: HoursBalance }) {
  const { t } = useTranslation();
  const { tone, icon } = balanceLook[balance.state];
  return (
    <StatusBadge tone={tone} icon={icon}>
      {balanceText(balance, t)}
    </StatusBadge>
  );
}
