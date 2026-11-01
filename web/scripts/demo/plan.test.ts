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
