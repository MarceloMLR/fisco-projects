import { useQuery } from '@tanstack/react-query';
import { generalRulesQuery } from '../api/queries';
import { type Dates, datesIn, defaultTimeZone } from './dates';

/**
 * The dates of the screens in the time zone of the installation (ADR-0013). The shell waits for the rules before
 * showing any screen; if they cannot be read, the default time zone.
 */
export function useDates(): Dates {
  const { data } = useQuery(generalRulesQuery());
  return datesIn(data?.timeZone ?? defaultTimeZone);
}
