import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { components } from '../api/schema';
import type {
  CalendarDay,
  CalendarDayKind,
  CalendarYear,
  DayOfWeek,
  Installation,
  Profile,
  SessionUser,
} from '../api/client';

type UserResponse = components['schemas']['UserResponse'];
type AuditEntryResponse = components['schemas']['AuditEntryResponse'];

// The API, simulated for the screen tests (ADR-0056). By default Marina is signed in and the local sign-in is on;
// each test changes only what it needs with server.use(...).

export const marina: SessionUser = {
  id: '0199f5a2-7c1e-7d40-9a1b-2f3c4d5e6f70',
  name: 'Marina Costa',
  email: 'marina.costa@empresa.example',
  role: 'User',
  theme: 'Automatic',
};

/**
 * People of the simulated installation, as the Users screen of the canvas shows them. The tests run in the time
 * zone set in vite.config.ts, where 2026-10-19T15:00:00Z is 12:00.
 */
export const demoUsers: UserResponse[] = [
  person(1, 'Paulo Nogueira', 'Administrator', 'Active', '2026-10-19T15:00:00Z'),
  person(2, 'Marina Costa', 'ProjectManager', 'Active', '2026-10-19T12:12:00Z'),
  person(3, 'Beatriz Lima', 'ProjectManager', 'Active', '2026-10-19T11:47:00Z'),
  person(4, 'Rafael Souza', 'User', 'Active', '2026-10-18T20:30:00Z'),
  person(5, 'Lívia Prado', 'User', 'Inactive', '2026-08-28T18:00:00Z'),
];

function person(
  index: number,
  name: string,
  role: UserResponse['role'],
  status: UserResponse['status'],
  lastSignInAt: string,
): UserResponse {
  const email = `${name
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(' ', '.')}@empresa.example`;
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-00000000000${String(index)}`,
    name,
    email,
    role,
    status,
    lastSignInAt,
  };
}

const withoutAccents = (text: string) =>
  text
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();

/** The list of users as the API answers it: search without accents, filters and pages, in alphabetical order. */
export function usersPage(users: readonly UserResponse[], url: URL) {
  const search = withoutAccents(url.searchParams.get('search') ?? '');
  const role = url.searchParams.get('role');
  const status = url.searchParams.get('status');
  const page = Number(url.searchParams.get('page') ?? '1');
  const pageSize = Number(url.searchParams.get('pageSize') ?? '25');
  const matching = users
    .filter(
      (user) =>
        withoutAccents(user.name).includes(search) || withoutAccents(user.email).includes(search),
    )
    .filter((user) => role === null || user.role === role)
    .filter((user) => status === null || user.status === status)
    .toSorted((a, b) => a.name.localeCompare(b.name, 'pt-BR'));
  return {
    items: matching.slice((page - 1) * pageSize, page * pageSize),
    total: matching.length,
  };
}

const paulo = { id: '0199f5a2-7c1e-7d40-9a1b-000000000001', name: 'Paulo Nogueira' };
const marinaActor = { id: '0199f5a2-7c1e-7d40-9a1b-000000000002', name: 'Marina Costa' };

function auditEntry(
  index: number,
  occurredAt: string,
  actor: AuditEntryResponse['actor'],
  record: Pick<AuditEntryResponse, 'action' | 'itemType'> & Partial<AuditEntryResponse>,
): AuditEntryResponse {
  return {
    id: `0199f5a2-7c1e-7d40-9a1b-1000000000${String(index).padStart(2, '0')}`,
    occurredAt,
    actor,
    itemId: null,
    itemLabel: null,
    detail: null,
    changes: [],
    ...record,
  };
}

/**
 * Audit records of the simulated installation, newest first, as in the Audit screen of the canvas. In the time
 * zone of the tests (vite.config.ts), 2026-10-19T13:14:00Z is 10:14 of October 19th.
 */
export const demoAuditEntries: AuditEntryResponse[] = [
  auditEntry(1, '2026-10-19T13:14:00Z', paulo, {
    action: 'Updated',
    itemType: 'User',
    itemId: '0199f5a2-7c1e-7d40-9a1b-000000000003',
    itemLabel: 'Beatriz Lima',
    changes: [{ field: 'Role', before: 'User', after: 'ProjectManager', secret: false }],
  }),
  auditEntry(2, '2026-10-19T12:58:00Z', marinaActor, {
    action: 'SignedIn',
    itemType: 'SignIn',
    itemId: marinaActor.id,
    detail: 'local',
  }),
  auditEntry(3, '2026-10-19T11:05:00Z', null, {
    action: 'SignInRefused',
    itemType: 'SignIn',
    itemLabel: 'joao.silva@parceiro.example',
    detail: 'NotInDirectory',
  }),
  auditEntry(4, '2026-10-18T14:47:00Z', paulo, {
    action: 'Updated',
    itemType: 'User',
    itemId: '0199f5a2-7c1e-7d40-9a1b-000000000005',
    itemLabel: 'Lívia Prado',
    changes: [{ field: 'IsActive', before: 'true', after: 'false', secret: false }],
  }),
  auditEntry(5, '2026-10-17T12:02:00Z', null, {
    action: 'Created',
    itemType: 'User',
    itemId: '0199f5a2-7c1e-7d40-9a1b-000000000004',
    itemLabel: 'Rafael Souza',
    changes: [
      { field: 'Email', before: null, after: 'rafael.souza@empresa.example', secret: false },
      { field: 'IsActive', before: null, after: 'true', secret: false },
      { field: 'Name', before: null, after: 'Rafael Souza', secret: false },
      { field: 'Role', before: null, after: 'User', secret: false },
    ],
  }),
  auditEntry(6, '2026-09-10T12:00:00Z', paulo, {
    action: 'SignedIn',
    itemType: 'SignIn',
    itemId: paulo.id,
    detail: 'local',
  }),
];

/** The audit list as the API answers it: by author, item type and period (end excluded), newest first, in pages. */
export function auditPage(entries: readonly AuditEntryResponse[], url: URL) {
  const actorId = url.searchParams.get('actorId');
  const itemType = url.searchParams.get('itemType');
  const from = url.searchParams.get('from');
  const to = url.searchParams.get('to');
  const page = Number(url.searchParams.get('page') ?? '1');
  const pageSize = Number(url.searchParams.get('pageSize') ?? '25');
  const matching = entries
    .filter((entry) => actorId === null || entry.actor?.id === actorId)
    .filter((entry) => itemType === null || entry.itemType === itemType)
    .filter((entry) => from === null || Date.parse(entry.occurredAt) >= Date.parse(from))
    .filter((entry) => to === null || Date.parse(entry.occurredAt) < Date.parse(to))
    .toSorted((a, b) => Date.parse(b.occurredAt) - Date.parse(a.occurredAt));
  return {
    items: matching.slice((page - 1) * pageSize, page * pageSize),
    total: matching.length,
  };
}

/** The brand of the simulated installation: the name nobody changed yet, and no logo. */
export const defaultInstallation: Installation = {
  name: 'FiscoProjects',
  logoUrl: null,
  mainColor: null,
};

/** My profile of a person in the session, who signs in locally and last did so at 09:12 of October 19th. */
export function profileOf(person: SessionUser): Profile {
  return { ...person, lastSignInAt: '2026-10-19T12:12:00Z', signInMethods: ['local'] };
}

/** A local holiday of the simulated calendar, as the Calendar module keeps it. */
export interface DemoLocalHoliday {
  id: string;
  name: string;
  date: string;
  repeatsEveryYear: boolean;
  isConsidered: boolean;
}

/** The calendar of the simulated installation, changed by the requests of a test as the real one would be. */
export interface DemoCalendar {
  workingDays: DayOfWeek[];
  /** The calculated holidays considered, by key. */
  considered: Set<string>;
  local: DemoLocalHoliday[];
}

const calculatedRules: [
  string,
  CalendarDayKind,
  (year: number, easter: number) => number,
  number?,
][] = [
  ['NewYearsDay', 'NationalHoliday', (year) => Date.UTC(year, 0, 1)],
  ['CarnivalMonday', 'OptionalHoliday', (_year, easter) => easter - 48 * 86_400_000],
  ['CarnivalTuesday', 'OptionalHoliday', (_year, easter) => easter - 47 * 86_400_000],
  ['AshWednesday', 'OptionalHoliday', (_year, easter) => easter - 46 * 86_400_000],
  ['GoodFriday', 'NationalHoliday', (_year, easter) => easter - 2 * 86_400_000],
  ['Tiradentes', 'NationalHoliday', (year) => Date.UTC(year, 3, 21)],
  ['LabourDay', 'NationalHoliday', (year) => Date.UTC(year, 4, 1)],
  ['CorpusChristi', 'OptionalHoliday', (_year, easter) => easter + 60 * 86_400_000],
  ['IndependenceDay', 'NationalHoliday', (year) => Date.UTC(year, 8, 7)],
  ['OurLadyOfAparecida', 'NationalHoliday', (year) => Date.UTC(year, 9, 12)],
  ['PublicServantsDay', 'OptionalHoliday', (year) => Date.UTC(year, 9, 28)],
  ['AllSoulsDay', 'NationalHoliday', (year) => Date.UTC(year, 10, 2)],
  ['RepublicDay', 'NationalHoliday', (year) => Date.UTC(year, 10, 15)],
  ['BlackConsciousnessDay', 'NationalHoliday', (year) => Date.UTC(year, 10, 20), 2024],
  ['ChristmasEve', 'OptionalHoliday', (year) => Date.UTC(year, 11, 24)],
  ['ChristmasDay', 'NationalHoliday', (year) => Date.UTC(year, 11, 25)],
  ['NewYearsEve', 'OptionalHoliday', (year) => Date.UTC(year, 11, 31)],
];

/** Easter Sunday of the Gregorian calendar, at midnight UTC (the same rule as the Calendar module). */
function easterOf(year: number): number {
  const a = year % 19;
  const b = Math.floor(year / 100);
  const c = year % 100;
  const h =
    (19 * a + b - Math.floor(b / 4) - Math.floor((b - Math.floor((b + 8) / 25) + 1) / 3) + 15) % 30;
  const l = (32 + 2 * (b % 4) + 2 * Math.floor(c / 4) - h - (c % 4)) % 7;
  const m = Math.floor((a + 11 * h + 22 * l) / 451);
  const month = Math.floor((h + l - 7 * m + 114) / 31);
  const day = ((h + l - 7 * m + 114) % 31) + 1;
  return Date.UTC(year, month - 1, day);
}

/** A new simulated calendar: Monday to Friday, the national holidays considered and two local holidays of 2026. */
export function demoCalendar(): DemoCalendar {
  return {
    workingDays: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'],
    considered: new Set(
      calculatedRules.filter(([, kind]) => kind === 'NationalHoliday').map(([key]) => key),
    ),
    local: [
      {
        id: '0199f5a2-7c1e-7d40-9a1b-200000000001',
        name: 'Aniversário da cidade',
        date: '2026-01-25',
        repeatsEveryYear: true,
        isConsidered: true,
      },
      {
        id: '0199f5a2-7c1e-7d40-9a1b-200000000002',
        name: 'Emenda de Tiradentes',
        date: '2026-04-20',
        repeatsEveryYear: false,
        isConsidered: true,
      },
    ],
  };
}

/** The calendar of a year as the API answers it: the calculated holidays of the year and the local ones, in order. */
export function calendarYearOf(calendar: DemoCalendar, year: number): CalendarYear {
  const easter = easterOf(year);
  const calculated: CalendarDay[] = calculatedRules
    .filter(([, , , since]) => since === undefined || year >= since)
    .map(([key, kind, dateOf]) => ({
      date: new Date(dateOf(year, easter)).toISOString().slice(0, 10),
      kind,
      key,
      id: null,
      name: null,
      repeatsEveryYear: true,
      isConsidered: calendar.considered.has(key),
    }));
  const local: CalendarDay[] = calendar.local.flatMap((holiday) => {
    const holidayYear = Number(holiday.date.slice(0, 4));
    const date =
      holidayYear === year
        ? holiday.date
        : holiday.repeatsEveryYear && year > holidayYear
          ? `${String(year)}${holiday.date.slice(4)}`
          : undefined;
    return date === undefined
      ? []
      : [
          {
            date,
            kind: 'LocalHoliday' as const,
            key: null,
            id: holiday.id,
            name: holiday.name,
            repeatsEveryYear: holiday.repeatsEveryYear,
            isConsidered: holiday.isConsidered,
          },
        ];
  });
  return {
    year,
    workingDays: calendar.workingDays,
    days: [...calculated, ...local].toSorted((first, second) =>
      first.date.localeCompare(second.date),
    ),
  };
}

export const handlers = [
  http.get('/api/session', () => HttpResponse.json(marina)),
  http.get('/api/users', ({ request }) =>
    HttpResponse.json(usersPage(demoUsers, new URL(request.url))),
  ),
  http.get('/api/audit', ({ request }) =>
    HttpResponse.json(auditPage(demoAuditEntries, new URL(request.url))),
  ),
  http.get('/api/session/sign-in-options', () => HttpResponse.json({ localSignIn: true })),
  http.post('/api/session/sign-out', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/profile', () => HttpResponse.json(profileOf(marina))),
  http.put('/api/profile/theme', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/installation', () => HttpResponse.json(defaultInstallation)),
  http.get('/api/installation/rules', () => HttpResponse.json({ timeZone: 'America/Sao_Paulo' })),
  http.get('/api/calendar/:year', ({ params }) =>
    HttpResponse.json(calendarYearOf(demoCalendar(), Number(params.year))),
  ),
];

export const server = setupServer(...handlers);

export function problem(status: number, type?: string) {
  return HttpResponse.json(
    { status, title: 'Problem', ...(type === undefined ? {} : { type }) },
    { status, headers: { 'Content-Type': 'application/problem+json' } },
  );
}

/** Marina is signed in with the given role in the system. */
export function signedInAs(role: SessionUser['role']): void {
  server.use(http.get('/api/session', () => HttpResponse.json({ ...marina, role })));
}

/** Nobody is signed in: the API answers 401 to "who is in the session". */
export function signedOut(): void {
  server.use(http.get('/api/session', () => problem(401)));
}
