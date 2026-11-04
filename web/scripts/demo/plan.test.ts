// @vitest-environment node
import { describe, expect, it } from 'vitest';
import {
  dayFrom,
  demoProjects,
  firstDayFor,
  mainProject,
  manager,
  workDaysBefore,
} from './plan.ts';

// Spec: dados-de-demonstracao, requirement "Datas da demonstração".
describe('Datas da demonstração', () => {
  it('Datas relativas a hoje', () => {
    // Monday 19/10/2026, as the captures of Cronograma-3, and the other days of that week.
    expect(firstDayFor('2026-10-19')).toBe('2026-10-05');
    expect(firstDayFor('2026-10-21')).toBe('2026-10-05');
    expect(firstDayFor('2026-10-25')).toBe('2026-10-05');
    // A week later, everything a week later.
    expect(firstDayFor('2026-10-26')).toBe('2026-10-12');
    expect(dayFrom('2026-10-05', 8)).toBe('2026-10-13');
    expect(dayFrom('2026-10-05', -14)).toBe('2026-09-21');

    const main = demoProjects.find((project) => project.code === mainProject);
    expect(main?.phases[0]?.items[0]).toMatchObject({ name: 'Reunião de abertura', day: 0 });
  });

  it('Dependências do plano', () => {
    // Every item depends only on items that come before it in its own project.
    for (const project of demoProjects) {
      const seen = new Set<string>();
      for (const item of project.phases.flatMap((phase) => phase.items)) {
        expect(item.after?.filter((name) => !seen.has(name)) ?? []).toEqual([]);
        // A subtask goes into a task that comes before it (ADR-0088).
        expect(item.inside === undefined || seen.has(item.inside)).toBe(true);
        seen.add(item.name);
      }
    }
  });
});

// Spec: cronograma, requirement "Data-limite de tarefas e marcos" (GanttA-2), in the demonstration.
describe('Datas-limite na demonstração', () => {
  it('Datas-limite na demonstração', () => {
    const main = demoProjects.find((project) => project.code === mainProject);
    const items = (main?.phases ?? []).flatMap((phase) => phase.items);
    const withDeadline = items.filter((item) => item.deadline !== undefined);
    expect(withDeadline.map((item) => item.name)).toEqual([
      'Validar cadastro de produtos',
      'Aceite da homologação',
      'Migrar saldos',
    ]);
    // Loaded on the day 14, one open task already went past its deadline, before its own end; the others are ahead.
    const missed = withDeadline.filter(
      (item) =>
        (item.progress ?? 0) < 100 &&
        (item.deadline ?? 0) < 14 &&
        (item.deadline ?? 0) < item.day + item.duration,
    );
    expect(missed.map((item) => item.name)).toEqual(['Validar cadastro de produtos']);
    expect(withDeadline.filter((item) => (item.deadline ?? 0) > 14)).toHaveLength(2);
  });
});

// Spec: dados-de-demonstracao, requirement "Meu trabalho na demonstração".
describe('Meu trabalho na demonstração', () => {
  it('Meu trabalho na demonstração', () => {
    // Loaded on a Monday, the day 14 of the main project: a task of the manager already late and one of the week.
    const tasks = demoProjects
      .flatMap((project) => project.phases)
      .flatMap((phase) => phase.items)
      .filter(
        (item) => item.kind === 'Task' && item.owner === 'marina' && (item.progress ?? 0) < 100,
      );
    expect(tasks.some((item) => item.day + item.duration < 14)).toBe(true);
    expect(tasks.some((item) => item.day >= 14 && item.day <= 21)).toBe(true);
    // A ticket of the manager waits for her answer, and a request of signature waits for its signers.
    const tickets = demoProjects.flatMap((project) => project.tickets ?? []);
    expect(
      tickets.some((ticket) => {
        const last = ticket.steps?.at(-1);
        return (
          ticket.by === 'marina' && last?.kind === 'action' && last.action === 'AskForInformation'
        );
      }),
    ).toBe(true);
    const acceptances = demoProjects.flatMap((project) => project.acceptances ?? []);
    expect(
      acceptances.some(
        (term) =>
          term.attached !== true && (term.answers ?? []).length < (term.signers ?? []).length,
      ),
    ).toBe(true);
  });
});

// Spec: dados-de-demonstracao, requirement "Horas na demonstração" (ADR-0128).
describe('Horas na demonstração', () => {
  it('Dias úteis antes de hoje', () => {
    // Thursday 29/10/2026: the day before is Wednesday, and five working days before is the Thursday before.
    expect(workDaysBefore('2026-10-29', 0)).toBe('2026-10-29');
    expect(workDaysBefore('2026-10-29', 1)).toBe('2026-10-28');
    expect(workDaysBefore('2026-10-29', 5)).toBe('2026-10-22');
    // From a Monday, the working day before is the Friday.
    expect(workDaysBefore('2026-10-26', 1)).toBe('2026-10-23');
  });

  it('Horas na demonstração', () => {
    const daily = new Map<string, number>();
    for (const project of demoProjects) {
      const leader = project.manager ?? manager;
      const roles = new Map<string, string>([
        [leader, 'Manager'],
        ...(project.techLead === undefined ? [] : [[project.techLead, 'TechLead'] as const]),
        ...(project.members ?? []),
      ]);
      const tasks = new Set(
        project.phases
          .flatMap((phase) => phase.items)
          .filter((item) => item.kind === 'Task')
          .map((item) => item.name),
      );
      const tickets = new Set((project.tickets ?? []).map((ticket) => ticket.title));
      for (const entry of project.hours ?? []) {
        // Only people of the project log hours, never a viewer, on a task or a ticket of the project.
        expect(roles.get(entry.by), `${project.code} ${entry.description}`).toBeDefined();
        expect(roles.get(entry.by)).not.toBe('Viewer');
        expect(entry.task === undefined || tasks.has(entry.task), entry.task).toBe(true);
        expect(entry.ticket === undefined || tickets.has(entry.ticket), entry.ticket).toBe(true);
        expect(entry.task !== undefined && entry.ticket !== undefined).toBe(false);
        expect(Number.isInteger(entry.hours * 4) && entry.hours > 0).toBe(true);
        const day = `${entry.by} ${String(entry.daysAgo)}`;
        daily.set(day, (daily.get(day) ?? 0) + entry.hours);
      }
    }
    expect(Math.max(...daily.values())).toBeLessThanOrEqual(24);

    // The manager of the main project has hours in it and in another project, on tasks and on tickets.
    const ofManager = demoProjects.flatMap((project) =>
      (project.hours ?? [])
        .filter((entry) => entry.by === manager)
        .map((entry) => ({ project, entry })),
    );
    expect(new Set(ofManager.map(({ project }) => project.code)).size).toBeGreaterThan(1);
    expect(ofManager.some(({ project }) => project.code === mainProject)).toBe(true);
    expect(ofManager.some(({ entry }) => entry.task !== undefined)).toBe(true);
    expect(ofManager.some(({ entry }) => entry.ticket !== undefined)).toBe(true);
  });
});

// Spec: dados-de-demonstracao, requirement "Saldo na demonstração" (ADR-0128).
describe('Saldo na demonstração', () => {
  it('Saldo na demonstração', () => {
    const open = demoProjects.filter(
      (project) => !['Completed', 'Cancelled'].includes(project.status ?? 'Planning'),
    );
    // The state of the balance, as the TimeTracking module computes it.
    const stateOf = (project: (typeof demoProjects)[number]) => {
      if (project.contractedHours === undefined) {
        return 'NoContract';
      }
      const total =
        project.contractedHours +
        (project.additionalHours ?? []).reduce((sum, item) => sum + item.hours, 0);
      const logged = (project.hours ?? []).reduce((sum, entry) => sum + entry.hours, 0);
      return logged > total ? 'Over' : logged * 100 >= total * 90 ? 'NearLimit' : 'Within';
    };
    expect(new Set(open.map(stateOf))).toEqual(
      new Set(['Over', 'NearLimit', 'Within', 'NoContract']),
    );
    const main = demoProjects.find((project) => project.code === mainProject);
    expect(main === undefined ? undefined : stateOf(main)).toBe('Over');
    for (const project of demoProjects) {
      if (project.contractedHours !== undefined) {
        expect(Number.isInteger(project.contractedHours * 4) && project.contractedHours > 0).toBe(
          true,
        );
      }
      for (const additional of project.additionalHours ?? []) {
        expect(additional.reason.length).toBeGreaterThan(0);
        expect(additional.reason.length).toBeLessThanOrEqual(500);
        expect(Number.isInteger(additional.hours * 4) && additional.hours > 0).toBe(true);
      }
    }
    expect(
      demoProjects.find((project) => project.code === mainProject)?.additionalHours?.length,
    ).toBeGreaterThan(0);
  });
});

// Spec: dados-de-demonstracao, requirement "Horas previstas na demonstração" (ADR-0138).
describe('Horas previstas na demonstração', () => {
  it('Horas previstas na demonstração', () => {
    const main = demoProjects.find((project) => project.code === mainProject);
    if (main === undefined) {
      throw new Error('No main project.');
    }
    const tasks = new Map(
      main.phases.flatMap((phase) => phase.items).map((item) => [item.name, item] as const),
    );
    // The state of the hours of a task, as the TimeTracking module computes it, with the task done at 100%.
    const states = (main.plannedHours ?? []).map(({ task, hours }) => {
      const item = tasks.get(task);
      expect(item?.kind).toBe('Task');
      expect(Number.isInteger(hours * 4) && hours > 0 && hours <= 9999).toBe(true);
      const logged = (main.hours ?? [])
        .filter((entry) => entry.task === task)
        .reduce((sum, entry) => sum + entry.hours, 0);
      return logged > hours
        ? 'Over'
        : item?.progress !== 100 && logged * 100 >= hours * 90
          ? 'NearLimit'
          : 'Within';
    });
    expect(new Set(states)).toEqual(new Set(['Over', 'NearLimit', 'Within']));
  });
});

// Spec: dados-de-demonstracao, requirement "Notas na demonstração" (ADR-0139).
describe('Notas na demonstração', () => {
  it('Notas na demonstração', () => {
    const main = demoProjects.find((project) => project.code === mainProject);
    const items = (main?.phases ?? []).flatMap((phase) => phase.items);
    const noted = items.filter((item) => item.note !== undefined);

    expect(new Set(noted.map((item) => item.kind))).toEqual(new Set(['Task', 'Milestone']));
    for (const item of noted) {
      expect((item.note ?? '').trim().length).toBeGreaterThan(0);
      expect((item.note ?? '').length).toBeLessThanOrEqual(2000);
    }
  });
});

// Spec: dados-de-demonstracao, requirement "Ausências na demonstração" (ADR-0141).
describe('Ausências na demonstração', () => {
  it('Ausências na demonstração', () => {
    const main = demoProjects.find((project) => project.code === mainProject);
    if (main === undefined) {
      throw new Error('No main project.');
    }
    const absences = main.absences ?? [];
    const people = new Set([
      main.manager ?? manager,
      main.techLead,
      ...(main.members ?? []).map(([person]) => person),
    ]);
    expect(absences.length).toBeGreaterThan(0);
    for (const absence of absences) {
      expect(people.has(absence.of)).toBe(true);
      expect(absence.end).toBeGreaterThanOrEqual(absence.start);
      expect(absence.end - absence.start + 1).toBeLessThanOrEqual(366);
      // After the week it is loaded (today is from day 14 to 20): never over the hours logged.
      expect(absence.start).toBeGreaterThan(20);
      // Told in Membros: it starts in the next 30 days.
      expect(absence.start).toBeLessThanOrEqual(14 + 30);
    }
    // One of them over an open task of the person, its calendar days roughly by its working days.
    const tasks = main.phases.flatMap((phase) => phase.items);
    expect(
      absences.some((absence) =>
        tasks.some(
          (item) =>
            item.kind === 'Task' &&
            item.owner === absence.of &&
            item.progress !== 100 &&
            item.day <= absence.end &&
            absence.start <= item.day + Math.ceil((item.duration * 7) / 5),
        ),
      ),
    ).toBe(true);
  });
});

// Spec: dados-de-demonstracao, requirement "Carga na demonstração" (ADR-0142).
describe('Carga na demonstração', () => {
  it('Carga na demonstração', () => {
    const main = demoProjects.find((project) => project.code === mainProject);
    // The weeks of the plan, from the Monday of the first day, and the calendar days of a task by its working days.
    const weekOf = (day: number) => Math.floor(day / 7);
    const spanOf = (item: { day: number; duration: number }) => [
      item.day,
      item.day + Math.ceil((item.duration * 7) / 5),
    ];
    const loaded = (main?.absences ?? []).filter((absence) => {
      const week = weekOf(absence.start);
      const projects = demoProjects.filter((project) =>
        (project.plannedHours ?? []).some(({ task }) => {
          const item = project.phases
            .flatMap((phase) => phase.items)
            .find((one) => one.name === task);
          if (item?.owner !== absence.of || item.progress === 100) {
            return false;
          }
          const [first = 0, last = 0] = spanOf(item);
          return weekOf(first) <= week && week <= weekOf(last);
        }),
      );
      return projects.length >= 2;
    });

    expect(loaded.map((absence) => absence.of)).toContain('diego');
  });
});

type PlanProject = (typeof demoProjects)[number];
type PlanItem = PlanProject['phases'][number]['items'][number];

/** The people of a project and their roles, the manager and the tech lead included. */
const rolesOf = (project: PlanProject) =>
  new Map<string, string>([
    [project.manager ?? manager, 'Manager'],
    ...(project.techLead === undefined ? [] : [[project.techLead, 'TechLead'] as const]),
    ...(project.members ?? []),
  ]);

/**
 * The state of the hours of each task with planned hours of a project, as the TimeTracking module computes it, with
 * the task done at 100% (ADR-0138).
 */
const taskHoursStates = (project: PlanProject) => {
  const tasks = new Map(
    project.phases.flatMap((phase) => phase.items).map((item) => [item.name, item] as const),
  );
  return (project.plannedHours ?? []).map(({ task, hours }) => {
    const item = tasks.get(task);
    expect(item?.kind, `${project.code} ${task}`).toBe('Task');
    expect(Number.isInteger(hours * 4) && hours > 0 && hours <= 9999).toBe(true);
    const logged = (project.hours ?? [])
      .filter((entry) => entry.task === task)
      .reduce((sum, entry) => sum + entry.hours, 0);
    return logged > hours
      ? 'Over'
      : item?.progress !== 100 && logged * 100 >= hours * 90
        ? 'NearLimit'
        : 'Within';
  });
};

/** Monday to Friday, the day 0 being a Monday; the holidays of the day it is loaded are not known here. */
const isWorkday = (day: number) => ((day % 7) + 7) % 7 < 5;
const workdayFrom = (day: number): number => (isWorkday(day) ? day : workdayFrom(day + 1));
/** The working day `count` working days after (or before, when negative) a working day. */
const workdaysAfter = (day: number, count: number): number => {
  let current = day;
  for (let left = Math.abs(count); left > 0;) {
    current += Math.sign(count);
    if (isWorkday(current)) {
      left -= 1;
    }
  }
  return current;
};

/**
 * The working days of the items of a project, roughly as the server schedules them (ADR-0051, ADR-0088, ADR-0137): the
 * start of the plan (or where the manager moved it), pushed by the dependencies, and the first subtask with the
 * dependencies of its task.
 */
const scheduleOf = (project: PlanProject) => {
  const items = project.phases.flatMap((phase) => phase.items);
  const byName = new Map(items.map((item) => [item.name, item] as const));
  const dates = new Map<string, { start: number; end: number }>();
  const parents = new Set(
    items.flatMap((item) => (item.inside === undefined ? [] : [item.inside])),
  );
  for (const item of items) {
    const parent = item.inside === undefined ? undefined : byName.get(item.inside);
    const first = parent !== undefined && items.find((one) => one.inside === parent.name) === item;
    const source: PlanItem = first && (item.after ?? []).length === 0 ? parent : item;
    const span = Math.max(item.duration - 1, 0);
    let start = workdayFrom(item.movedTo ?? item.day);
    for (const name of source.after ?? []) {
      const before = dates.get(name);
      if (before === undefined) {
        continue;
      }
      const link = source.links?.[name];
      const lag = link?.lag ?? 0;
      const earliest =
        link?.type === 'StartToStart'
          ? workdaysAfter(before.start, lag)
          : link?.type === 'FinishToFinish'
            ? workdaysAfter(workdaysAfter(before.end, lag), -span)
            : workdaysAfter(before.end, item.duration === 0 ? lag : 1 + lag);
      start = Math.max(start, earliest);
    }
    if (!parents.has(item.name)) {
      dates.set(item.name, { start, end: workdaysAfter(start, span) });
    }
  }
  return dates;
};

// Spec: dados-de-demonstracao, requirement "Novidades fora do projeto principal" (ADR-0128, ADR-0137 to ADR-0142).
describe('Novidades fora do projeto principal', () => {
  const others = demoProjects.filter(
    (project) => project.code !== mainProject && project.status === 'InProgress',
  );

  it('Horas de todas as pessoas', () => {
    const workers = new Set(
      demoProjects.flatMap((project) =>
        [...rolesOf(project)].filter(([, role]) => role !== 'Viewer').map(([person]) => person),
      ),
    );
    for (const person of workers) {
      const entries = demoProjects.flatMap((project) =>
        (project.hours ?? [])
          .filter((entry) => entry.by === person)
          .map((entry) => ({ code: project.code, entry })),
      );
      // Loaded on a Monday: today, and days of the week before.
      expect(
        entries.some(({ entry }) => entry.daysAgo === 0),
        person,
      ).toBe(true);
      expect(
        entries.some(({ entry }) => entry.daysAgo >= 1 && entry.daysAgo <= 5),
        person,
      ).toBe(true);
      // Never more than a working day in a day.
      const daily = new Map<number, number>();
      for (const { entry } of entries) {
        daily.set(entry.daysAgo, (daily.get(entry.daysAgo) ?? 0) + entry.hours);
      }
      expect(Math.max(...daily.values()), person).toBeLessThanOrEqual(9);
    }
    // The second manager and the tech lead, in more than one project.
    for (const person of ['beatriz', 'rafael']) {
      const projects = demoProjects.filter((project) =>
        (project.hours ?? []).some((entry) => entry.by === person),
      );
      expect(projects.length, person).toBeGreaterThan(1);
    }
  });

  it('Horas das tarefas da segunda gerente', () => {
    const ofBeatriz = others.filter(
      (project) => project.manager === 'beatriz' && (project.plannedHours ?? []).length > 0,
    );
    expect(ofBeatriz.map((project) => project.code)).toEqual(['TMS-DELTA']);
    expect(new Set(ofBeatriz.flatMap(taskHoursStates))).toEqual(
      new Set(['Over', 'NearLimit', 'Within']),
    );
    for (const project of demoProjects) {
      taskHoursStates(project);
    }
  });

  it('Carga da equipe com todos os estados', () => {
    // Loaded on a Monday, the day 14: the eight weeks from it, with 8 hours a working day (ADR-0142).
    const weeks = Array.from({ length: 8 }, (_, index) => 14 + index * 7);
    const open = demoProjects.filter(
      (project) => !['Completed', 'Cancelled'].includes(project.status ?? 'Planning'),
    );
    const people = new Set(open.flatMap((project) => [...rolesOf(project).keys()]));
    const absences = demoProjects.flatMap((project) => project.absences ?? []);
    const load = new Map<string, number>();
    for (const project of open) {
      const dates = scheduleOf(project);
      const items = new Map(
        project.phases.flatMap((phase) => phase.items).map((item) => [item.name, item] as const),
      );
      for (const { task, hours } of project.plannedHours ?? []) {
        const item = items.get(task);
        const span = dates.get(task);
        if (item?.owner === undefined || item.progress === 100 || span === undefined) {
          continue;
        }
        for (let day = span.start; day <= span.end; day += 1) {
          const week = weeks.find((first) => first <= day && day < first + 7);
          if (isWorkday(day) && week !== undefined) {
            const key = `${item.owner} ${String(week)}`;
            load.set(key, (load.get(key) ?? 0) + hours / item.duration);
          }
        }
      }
    }
    const states = new Map<string, Set<string>>();
    for (const person of people) {
      for (const week of weeks) {
        const away = Array.from({ length: 5 }, (_, index) => week + index).filter((day) =>
          absences.some(
            (absence) => absence.of === person && absence.start <= day && day <= absence.end,
          ),
        ).length;
        const capacity = 8 * (5 - away);
        const hours = load.get(`${person} ${String(week)}`) ?? 0;
        const state =
          capacity === 0
            ? 'Away'
            : hours > capacity
              ? 'Over'
              : hours * 100 >= capacity * 90
                ? 'NearLimit'
                : hours > 0
                  ? 'Within'
                  : 'Free';
        states.set(state, new Set([...(states.get(state) ?? []), person]));
      }
    }
    expect(states.get('Over')).toContain('diego');
    expect(states.get('NearLimit')?.size).toBeGreaterThan(0);
    expect(states.get('Within')?.size).toBeGreaterThan(1);
    expect(states.get('Free')?.size).toBeGreaterThan(0);
    // The absences of at least four people in the eight weeks.
    const away = new Set(
      absences
        .filter((absence) => absence.end >= 14 && absence.start < 14 + 8 * 7)
        .map((absence) => absence.of),
    );
    expect(away.size).toBeGreaterThanOrEqual(4);
  });

  it('Ausências em outros projetos', () => {
    const all = demoProjects.flatMap((project) =>
      (project.absences ?? []).map((absence) => ({ project, absence })),
    );
    for (const { project, absence } of all) {
      // For a person of the project, after the week it is loaded and told in Membros (ADR-0141).
      expect(rolesOf(project).has(absence.of), `${project.code} ${absence.of}`).toBe(true);
      expect(absence.end).toBeGreaterThanOrEqual(absence.start);
      expect(absence.start).toBeGreaterThan(20);
      expect(absence.start).toBeLessThanOrEqual(14 + 30);
    }
    // Never over another absence of the same person, in any project (409 absence-overlap).
    const people = new Set(all.map(({ absence }) => absence.of));
    for (const person of people) {
      const own = all
        .map(({ absence }) => absence)
        .filter((absence) => absence.of === person)
        .sort((one, other) => one.start - other.start);
      own.slice(1).forEach((absence, index) => {
        expect(absence.start, person).toBeGreaterThan(own[index]?.end ?? 0);
      });
    }
    // One outside the main project over an open task of the person in that project.
    expect(
      all.some(({ project, absence }) => {
        if (project.code === mainProject) {
          return false;
        }
        const dates = scheduleOf(project);
        return project.phases
          .flatMap((phase) => phase.items)
          .some((item) => {
            const span = dates.get(item.name);
            return (
              item.kind === 'Task' &&
              item.owner === absence.of &&
              item.progress !== 100 &&
              span !== undefined &&
              span.start <= absence.end &&
              absence.start <= span.end
            );
          });
      }),
    ).toBe(true);
  });

  it('Datas-limite, notas e dependências em outros projetos', () => {
    const itemsOf = (project: PlanProject) => project.phases.flatMap((phase) => phase.items);
    const withDeadlines = others.filter((project) =>
      itemsOf(project).some((item) => item.deadline !== undefined),
    );
    expect(withDeadlines.length).toBeGreaterThan(1);
    // Loaded on the day 14, an open task already past its deadline, before its own end.
    expect(
      others.some((project) => {
        const dates = scheduleOf(project);
        return itemsOf(project).some((item) => {
          const end = dates.get(item.name)?.end ?? 0;
          return (
            (item.progress ?? 0) < 100 &&
            item.deadline !== undefined &&
            item.deadline < 14 &&
            item.deadline < end
          );
        });
      }),
    ).toBe(true);
    const noted = others.filter((project) =>
      itemsOf(project).some((item) => (item.note ?? '').trim().length > 0),
    );
    expect(noted.length).toBeGreaterThan(1);
    expect(
      others.some((project) =>
        itemsOf(project).some((item) =>
          Object.values(item.links ?? {}).some((link) => link?.type === 'StartToStart'),
        ),
      ),
    ).toBe(true);
  });
});
