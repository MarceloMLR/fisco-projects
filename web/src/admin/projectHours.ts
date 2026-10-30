import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import RadioButtonUncheckedOutlined from '@mui/icons-material/RadioButtonUncheckedOutlined';
import WarningAmberOutlined from '@mui/icons-material/WarningAmberOutlined';
import type SvgIcon from '@mui/material/SvgIcon';
import type { TFunction } from 'i18next';
import type {
  HoursBalance,
  HoursBalanceState,
  ProjectHours,
  ProjectHoursView,
} from '../api/client';
import type { ProgressTone } from '../components/ProgressBar';
import type { StatusTone } from '../components/StatusBadge';
import { formatHours } from '../hours/hours';

/**
 * How each state of the balance shows (ADR-0065, canvas Horas do projeto): the selo by icon, color and text, and the
 * color of the bar of the consumption.
 */
export const balanceLook: Record<
  HoursBalanceState,
  { tone: StatusTone; icon: typeof SvgIcon; progress: ProgressTone }
> = {
  Over: { tone: 'danger', icon: ErrorOutlineOutlined, progress: 'danger' },
  NearLimit: { tone: 'warning', icon: WarningAmberOutlined, progress: 'warning' },
  Within: { tone: 'success', icon: CheckCircleOutlined, progress: 'info' },
  NoContract: { tone: 'neutral', icon: RadioButtonUncheckedOutlined, progress: 'info' },
};

/** The text of the balance: "28 h acima", "Restam 14 h" or "Sem horas contratadas". */
export function balanceText(balance: HoursBalance, t: TFunction): string {
  if (balance.state === 'NoContract' || balance.balance === null) {
    return t('projectHours.balance.noContract');
  }
  return balance.state === 'Over'
    ? t('projectHours.balance.over', { hours: formatHours(-balance.balance) })
    : t('projectHours.balance.left', { hours: formatHours(balance.balance) });
}

/** The consumption beside its bar: "468 de 440 h (106%)"; none without contracted hours. */
export function consumptionDetail(balance: HoursBalance, t: TFunction): string | undefined {
  if (balance.total === null || balance.consumption === null) {
    return undefined;
  }
  return t('projectHours.details.consumptionDetail', {
    logged: new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(balance.logged),
    total: formatHours(balance.total),
    share: balance.consumption,
  });
}

/** The hours of a column, or a dash when there are none ("—" for no additional or contracted hours). */
export function hoursOrDash(hours: number | null, t: TFunction): string {
  return hours === null || hours === 0 ? t('projectHours.none') : formatHours(hours);
}

/** The line under the title: how many projects the view shows and, when any, how many are over (ADR-0128). */
export function projectHoursMeta(
  rows: readonly ProjectHours[],
  view: ProjectHoursView,
  t: TFunction,
): string {
  const projects = t(`projectHours.meta.${view}`, { count: rows.length });
  const over = rows.filter((row) => row.balance.state === 'Over').length;
  return over === 0
    ? projects
    : t('projectHours.meta.joined', {
        projects,
        over: t('projectHours.meta.over', { count: over }),
      });
}
