import { screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Project, Schedule, ScheduleItem } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import {
  demoProjects,
  demoSchedule,
  type DemoProject,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects.schedule;
const projection = text.projection;

let project: DemoProject;
let schedule: Schedule;
let permissions: Pick<Project['permissions'], 'canEditSchedule'>;

function itemNamed(name: string): ScheduleItem {
  const item = schedule.phases.flatMap((phase) => phase.items).find((one) => one.name === name);
  if (item === undefined) {
    throw new Error(`No item ${name}.`);
  }
  return item;
}

/** A projection as the API sends it (ADR-0164). */
const projected = (
  value: Partial<NonNullable<ScheduleItem['projection']>>,
): NonNullable<ScheduleItem['projection']> => ({
  start: '2026-10-13',
  end: '2026-10-20',
  days: 3,
  milestones: [],
  projectEnd: null,
  ...value,
});

beforeEach(() => {
  const projects = demoProjects();
  const erp = projects[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  project = erp;
  schedule = demoSchedule();
  permissions = { canEditSchedule: true };
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects', ({ request }) =>
      HttpResponse.json(projectsPage(projects, new URL(request.url))),
    ),
    http.get('/api/projects/:id', ({ params }) =>
      params.id === project.id
        ? HttpResponse.json({ ...project, permissions: { ...project.permissions, ...permissions } })
        : new HttpResponse(null, { status: 404 }),
    ),
    http.get('/api/projects/:id/schedule', () => HttpResponse.json(schedule)),
    http.get('/api/projects/:id/schedule/items/:itemId/comments', () =>
      HttpResponse.json({ items: [], canComment: false }),
    ),
  );
});

/** Opens the details of an item by the address, as the Lista does. */
async function openItem(name: string) {
  await renderApp(`/projects/${project.id}/schedule?view=list&item=${itemNamed(name).id}`);
  return screen.findByRole('dialog', { name: text.details.label });
}

// Spec: cronograma, requirement "Projeção do atraso pela tela" (ADR-0164).
describe('Projeção do atraso pela tela', () => {
  it.each([
    ['quem edita', true],
    ['quem só vê', false],
  ])('Projeção no detalhe (%s)', async (_who, edits) => {
    permissions = { canEditSchedule: edits };
    const task = itemNamed('Testes integrados');
    Object.assign(task, {
      progress: 40,
      projection: projected({
        milestones: [
          {
            id: itemNamed('Aceite da homologação').id,
            name: 'Go-live',
            day: '2026-11-03',
            projectedDay: '2026-11-06',
            days: 3,
          },
        ],
        projectEnd: { end: '2026-11-13', projectedEnd: '2026-11-18', days: 3 },
      }),
    });

    const drawer = await openItem('Testes integrados');

    const section = await within(drawer).findByRole('region', { name: projection.title });
    expect(
      within(section).getByText(
        projection.ends_other.replace('{{day}}', '20/10').replace('{{count}}', '3'),
      ),
    ).toBeVisible();
    const list = within(section).getByRole('list', {
      name: projection.list.replace('{{name}}', 'Testes integrados'),
    });
    expect(
      within(list)
        .getAllByRole('listitem')
        .map((line) => line.textContent),
    ).toEqual([
      'Empurra o marco Go-live em 3 dias úteis, para 06/11',
      'Empurra o fim do projeto em 3 dias úteis, para 18/11',
    ]);
    expect(within(section).getByText(projection.help)).toBeVisible();
  });

  it('Tarefa que devia ter começado no detalhe', async () => {
    Object.assign(itemNamed('Testes integrados'), {
      progress: 0,
      projection: projected({ start: '2026-10-19', end: '2026-10-23', days: 4 }),
    });

    const drawer = await openItem('Testes integrados');

    const section = await within(drawer).findByRole('region', { name: projection.title });
    expect(
      within(section).getByText(
        projection.startsAndEnds_other
          .replace('{{start}}', '19/10')
          .replace('{{day}}', '23/10')
          .replace('{{count}}', '4'),
      ),
    ).toBeVisible();
  });

  it('Atraso que não empurra nada', async () => {
    Object.assign(itemNamed('Testes integrados'), { progress: 40, projection: projected({}) });

    const drawer = await openItem('Testes integrados');

    const section = await within(drawer).findByRole('region', { name: projection.title });
    const line = within(section).getByRole('listitem');
    expect(line).toHaveTextContent(projection.nothing);
    expect(line).toHaveAttribute('data-state', 'absorbed');
  });

  it('Tarefa sem projeção', async () => {
    const drawer = await openItem('Testes integrados');

    await within(drawer).findByRole('region', { name: new RegExp(`^${text.comments.title}`) });
    expect(
      within(drawer).queryByRole('region', { name: projection.title }),
    ).not.toBeInTheDocument();
  });
});
