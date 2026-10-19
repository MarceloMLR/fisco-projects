import type { TFunction } from 'i18next';
import type { CalendarDay } from '../api/client';

/** The holidays the system calculates (BrazilianNationalCalendar in the Calendar module), named in the translations. */
export const calculatedHolidayKeys = [
  'NewYearsDay',
  'CarnivalMonday',
  'CarnivalTuesday',
  'AshWednesday',
  'GoodFriday',
  'Tiradentes',
  'LabourDay',
  'CorpusChristi',
  'IndependenceDay',
  'OurLadyOfAparecida',
  'PublicServantsDay',
  'AllSoulsDay',
  'RepublicDay',
  'BlackConsciousnessDay',
  'ChristmasEve',
  'ChristmasDay',
  'NewYearsEve',
] as const;

export type CalculatedHolidayKey = (typeof calculatedHolidayKeys)[number];

const holidayKeys: readonly string[] = calculatedHolidayKeys;

export const isCalculatedHolidayKey = (
  key: string | null | undefined,
): key is CalculatedHolidayKey => key !== null && key !== undefined && holidayKeys.includes(key);

/** The name of a calculated holiday by its key; an unknown key shows as it is. */
export function calculatedHolidayName(key: string, t: TFunction): string {
  return isCalculatedHolidayKey(key) ? t(`calendar.holidays.${key}`) : key;
}

/** The name of a date of the calendar: the translated name of a calculated holiday, or the name of a local one. */
export function holidayName(day: CalendarDay, t: TFunction): string {
  return day.key === null ? (day.name ?? '') : calculatedHolidayName(day.key, t);
}

/** The id of a date in the lists, the same in every year for a calculated holiday. */
export function calendarDayId(day: CalendarDay): string {
  return `${day.date}-${day.key ?? day.id ?? ''}`;
}
