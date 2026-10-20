// @vitest-environment node
import { describe, expect, it } from 'vitest';
import { dayFrom, demoProjects, firstDayFor, mainProject } from './plan.ts';

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
    // A ticket of the manager waits for her answer, and a request of acceptance waits for its answer.
    const tickets = demoProjects.flatMap((project) => project.tickets ?? []);
    expect(
      tickets.some(
        (ticket) =>
          ticket.by === 'marina' && ticket.changes?.at(-1)?.action === 'AskForInformation',
      ),
    ).toBe(true);
    const acceptances = demoProjects.flatMap((project) => project.acceptances ?? []);
    expect(acceptances.some((term) => term.answer === undefined && term.attached !== true)).toBe(
      true,
    );
  });
});
