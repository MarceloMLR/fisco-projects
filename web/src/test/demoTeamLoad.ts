import type {
  LoadState,
  PersonLoad,
  PersonWeek,
  TeamLoad,
  TeamLoadWeek,
  WeekTask,
} from '../api/client';
import { addDays } from '../hours/hours';
import { demoPeople, emailOf } from './server';

// The load of the team as the Administrator sees it (ADR-0142): all fictitious. The calendar has the holidays of 02/11
// and 20/11, so those weeks have 4 business days.

const erpId = '0199f5a2-7c1e-7d40-9a1b-f00000000001';
const spedId = '0199f5a2-7c1e-7d40-9a1b-f00000000002';
const holidays = new Set(['2026-11-02', '2026-11-20']);

/** The eight weeks from the Monday, with their business days. */
export function weeksFrom(start: string): TeamLoadWeek[] {
  return Array.from({ length: 8 }, (_, index) => {
    const monday = addDays(start, 7 * index);
    const businessDays = [0, 1, 2, 3, 4].filter(
      (day) => !holidays.has(addDays(monday, day)),
    ).length;
    return { start: monday, end: addDays(monday, 6), businessDays };
  });
}

/** The state of a week, as the TeamLoad of the TimeTracking module computes it. */
function stateOf(load: number, capacity: number): LoadState {
  if (load <= 0) {
    return 'Free';
  }
  if (load > capacity) {
    return 'Over';
  }
  return load * 100 >= capacity * 90 ? 'NearLimit' : 'Within';
}

function week(
  businessDays: number,
  tasks: WeekTask[] = [],
  absences: { start: string; end: string }[] = [],
  absenceDays = 0,
): PersonWeek {
  const load = tasks.reduce((sum, task) => sum + task.hours, 0);
  const capacity = (businessDays - absenceDays) * 8;
  return { load, capacity, absenceDays, state: stateOf(load, capacity), tasks, absences };
}

function task(
  project: 'ERP-FIN' | 'SPED-27',
  n: number,
  name: string,
  hours: number,
  planned: number,
  start: string,
  end: string,
): WeekTask {
  return {
    projectId: project === 'ERP-FIN' ? erpId : spedId,
    projectCode: project,
    projectName:
      project === 'ERP-FIN' ? 'Implantação do ERP financeiro' : 'Adequação do SPED Fiscal 2027',
    itemId: `0199f5a2-7c1e-7d40-9a1b-e000000000${String(n).padStart(2, '0')}`,
    name,
    start,
    end,
    planned,
    hours,
  };
}

function person(who: { id: string; name: string }, weeks: PersonWeek[]): PersonLoad {
  return {
    id: who.id,
    name: who.name,
    email: emailOf(who.name),
    overloaded: weeks.some((one) => one.state === 'Over'),
    weeks,
  };
}

const lucas = { id: '0199f5a2-7c1e-7d40-9a1b-000000000007', name: 'Lucas Prado' };

/**
 * The load from the week of 19/10/2026: Carla near the limit in the first week; Diego over the capacity in the week of
 * 26/10, with two projects and two days away; Lucas, Marina and Rafael free or within.
 */
export function demoTeamLoad(start = '2026-10-19'): TeamLoad {
  const weeks = weeksFrom(start);
  const days = weeks.map((one) => one.businessDays);
  const free = (from = 0) => days.slice(from).map((count) => week(count));
  const accounts = task(
    'ERP-FIN',
    1,
    'Configurar plano de contas',
    36,
    48,
    '2026-10-19',
    '2026-10-27',
  );
  const nfe = task('ERP-FIN', 2, 'Integrar emissão de NF-e', 20, 20, '2026-10-26', '2026-10-30');
  const sped = task('SPED-27', 3, 'Gerar o arquivo do SPED', 30, 30, '2026-10-26', '2026-10-30');
  return {
    today: '2026-10-19',
    hoursPerDay: 8,
    weeks,
    people:
      start === '2026-10-19'
        ? [
            person(demoPeople.carla, [
              week(days[0] ?? 5, [accounts]),
              week(days[1] ?? 5, [{ ...accounts, hours: 12 }]),
              ...free(2),
            ]),
            person(demoPeople.diego, [
              week(days[0] ?? 5),
              week(days[1] ?? 5, [sped, nfe], [{ start: '2026-10-29', end: '2026-10-30' }], 2),
              ...free(2),
            ]),
            person(lucas, free()),
            person(demoPeople.marina, free()),
            person(demoPeople.rafael, [
              week(days[0] ?? 5, [
                task(
                  'ERP-FIN',
                  4,
                  'Parametrizar regras de ICMS',
                  16,
                  64,
                  '2026-10-13',
                  '2026-10-22',
                ),
              ]),
              ...free(1),
            ]),
          ]
        : [
            person(demoPeople.carla, free()),
            person(demoPeople.diego, free()),
            person(lucas, free()),
          ],
  };
}
