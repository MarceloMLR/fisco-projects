import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import ErrorOutlineOutlined from '@mui/icons-material/ErrorOutlineOutlined';
import RadioButtonUncheckedOutlined from '@mui/icons-material/RadioButtonUncheckedOutlined';
import WarningAmberOutlined from '@mui/icons-material/WarningAmberOutlined';
import type SvgIcon from '@mui/material/SvgIcon';
import type { TFunction } from 'i18next';
import type { LoadState, PersonLoad, PersonWeek, TeamLoadWeek } from '../api/client';
import type { StatusTone } from '../components/StatusBadge';
import { type DayValue, formatDayMonthText, formatDayText } from '../format/dates';
import { addDays, formatHours } from '../hours/hours';

/** The views of the load: everyone, or only who is over the capacity in some week (ADR-0142). */
export type TeamLoadView = 'All' | 'Overloaded';

/** How each state of a week shows (ADR-0065, ADR-0142): by icon, color and text, as the balance of the hours. */
export const loadLook: Record<LoadState, { tone: StatusTone; icon: typeof SvgIcon }> = {
  Over: { tone: 'danger', icon: ErrorOutlineOutlined },
  NearLimit: { tone: 'warning', icon: WarningAmberOutlined },
  Within: { tone: 'success', icon: CheckCircleOutlined },
  Free: { tone: 'neutral', icon: RadioButtonUncheckedOutlined },
};

const plain = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 });

/** The load against the capacity of a week: "32 de 40 h", one decimal at most. */
export function loadText(week: Pick<PersonWeek, 'load' | 'capacity'>, t: TFunction): string {
  return t('teamLoad.loadOf', {
    load: plain.format(week.load),
    capacity: formatHours(week.capacity),
  });
}

/** The hours of a task in a week, one decimal at most: "12,5 h". */
export function weekHoursText(hours: number): string {
  return `${plain.format(hours)} h`;
}

/** A week as the header of its column says it: "26/10 – 01/11". */
export function weekText(week: Pick<TeamLoadWeek, 'start' | 'end'>): string {
  return `${formatDayMonthText(week.start)} – ${formatDayMonthText(week.end)}`;
}

/** A period of an absence: "29/10 a 30/10", or the day alone. */
export function absencePeriodText(
  period: { start: DayValue; end: DayValue },
  t: TFunction,
): string {
  return period.start === period.end
    ? formatDayMonthText(period.start)
    : t('teamLoad.period', {
        start: formatDayMonthText(period.start),
        end: formatDayMonthText(period.end),
      });
}

/** The dates of a task in full: "22/10/2026 a 28/10/2026". */
export function taskPeriodText(task: { start: DayValue; end: DayValue }, t: TFunction): string {
  return task.start === task.end
    ? formatDayText(task.start)
    : t('teamLoad.period', { start: formatDayText(task.start), end: formatDayText(task.end) });
}

const simplify = (text: string) => text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();

/** The people of a view whose name or e-mail has the text, without telling case or accents apart. */
export function shownPeople(
  people: readonly PersonLoad[],
  view: TeamLoadView,
  search: string,
): PersonLoad[] {
  const text = simplify(search.trim());
  return people.filter(
    (person) =>
      (view === 'All' || person.overloaded) &&
      (text === '' ||
        simplify(person.name).includes(text) ||
        simplify(person.email).includes(text)),
  );
}

/** The line under the title: how many people and, when any, how many are over the capacity in the period. */
export function teamLoadMeta(people: readonly PersonLoad[], t: TFunction): string {
  const count = t('teamLoad.meta.people', { count: people.length });
  const over = people.filter((person) => person.overloaded).length;
  return over === 0
    ? count
    : t('teamLoad.meta.joined', { people: count, over: t('teamLoad.meta.over', { count: over }) });
}

/** The Monday a week after or before the one shown, as the arrows of the page ask it. */
export function weekAfter(start: DayValue, weeks: number): DayValue {
  return addDays(start, 7 * weeks);
}
