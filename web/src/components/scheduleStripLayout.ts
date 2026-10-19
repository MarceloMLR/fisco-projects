import { daysBetween, type DayValue } from '../format/dates';

/** A phase of the strip: its days, how much of it is done and whether a task of it is late. */
export interface StripPhase {
  name: string;
  start: DayValue;
  end: DayValue;
  progress: number;
  late: boolean;
}

/** Where a phase, a milestone and today go in the strip, in % of the period of the schedule. */
export interface StripLayout {
  phases: { left: number; width: number }[];
  milestones: number[];
  today: number;
}

/** A number kept from 0 to 100. */
export const clamp = (value: number) => Math.max(0, Math.min(100, value));

/**
 * The places in the strip, in calendar days of the period, from the first day to the end of the last one: a phase
 * from the start of its first day to the end of its last, and a milestone and today in the middle of their day.
 * Today before the start goes at the start, and after the end at the end.
 */
export function stripLayout(
  start: DayValue,
  end: DayValue,
  phases: readonly Pick<StripPhase, 'start' | 'end'>[],
  milestones: readonly DayValue[],
  today: DayValue,
): StripLayout {
  const days = Math.max(1, daysBetween(start, end) + 1);
  // Multiplied before divided, so a place such as 28.75% comes exact.
  const at = (day: DayValue, offset = 0) => clamp((100 * daysBetween(start, day) + offset) / days);
  const middleOf = (day: DayValue) => at(day, 50);
  return {
    phases: phases.map((phase) => {
      const left = at(phase.start);
      return { left, width: at(phase.end, 100) - left };
    }),
    milestones: milestones.map(middleOf),
    today: middleOf(today),
  };
}
