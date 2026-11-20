import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Schedule, ScheduleItem } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import {
  demoProjects,
  demoSchedule,
  demoScheduleWithBaseline,
  type DemoProject,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects.schedule;

let project: DemoProject;
let schedule: Schedule;

function itemNamed(name: string): ScheduleItem {
  const item = schedule.phases.flatMap((phase) => phase.items).find((one) => one.name === name);
  if (item === undefined) {
    throw new Error(`No item ${name}.`);
  }
  return item;
}

/** Validar cadastro de produtos, planned for 15/10 in the baseline and completed on 20/10. */
function completedLate() {
  Object.assign(itemNamed('Validar cadastro de produtos'), {
    baselineEnd: '2026-10-15',
    progress: 100,
    completedOn: '2026-10-20',
  });
}

beforeEach(() => {
  const projects = demoProjects();
  const erp = projects[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  project = erp;
  schedule = demoScheduleWithBaseline();
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects', ({ request }) =>
      HttpResponse.json(projectsPage(projects, new URL(request.url))),
    ),
    http.get('/api/projects/:id', ({ params }) =>
      params.id === project.id
        ? HttpResponse.json(project)
        : new HttpResponse(null, { status: 404 }),
    ),
    http.get('/api/projects/:id/schedule', () => HttpResponse.json(schedule)),
  );
});

function table() {
  return screen.getByRole('grid', { name: text.table });
}

function cellOf(name: string, field: string): HTMLElement {
  const cell = table().querySelector<HTMLElement>(
    `[data-id="${itemNamed(name).id}"] [data-field="${field}"]`,
  );
  if (cell === null) {
    throw new Error(`No cell ${field} for ${name}.`);
  }
  return cell;
}

async function openList() {
  await renderApp(`/projects/${project.id}/schedule?view=list`);
  await waitFor(() => {
    expect(within(table()).getAllByRole('row').length).toBeGreaterThan(1);
  });
}

const headers = () =>
  within(table())
    .getAllByRole('columnheader')
    .map((header) => header.textContent);

// Spec: cronograma, requirement "Fim previsto e fim real na Lista" (ADR-0021, ADR-0143).
describe('Fim previsto e fim real na Lista', () => {
  it('Fim previsto e real na Lista', async () => {
    completedLate();
    await openList();

    const all = headers();
    const end = all.indexOf(text.columns.end);
    expect(all.slice(end, end + 3)).toEqual([
      text.columns.end,
      text.columns.plannedEnd,
      text.columns.actualEnd,
    ]);
    expect(cellOf('Validar cadastro de produtos', 'plannedEnd')).toHaveTextContent('15/10/2026');
    expect(cellOf('Validar cadastro de produtos', 'actualEnd')).toHaveTextContent('20/10/2026');
    // A task still open has no actual end.
    expect(cellOf('Integrar emissão de NF-e', 'actualEnd')).toHaveTextContent('');
  });

  it('Lista sem linha de base', async () => {
    schedule = demoSchedule();
    await openList();

    expect(headers()).toContain(text.columns.actualEnd);
    expect(headers()).not.toContain(text.columns.plannedEnd);
    expect(cellOf('Mapear processos fiscais', 'actualEnd')).toHaveTextContent('09/10/2026');
  });

  it('Fim previsto e real na edição na tabela', async () => {
    completedLate();
    await openList();
    fireEvent.click(screen.getByRole('button', { name: ptBR.projects.tableEdit.button }));
    await screen.findByText(ptBR.projects.tableEdit.help);

    for (const field of ['plannedEnd', 'actualEnd']) {
      fireEvent.click(cellOf('Integrar emissão de NF-e', field));
      fireEvent.keyDown(cellOf('Integrar emissão de NF-e', field), { key: 'Enter' });
    }

    expect(within(table()).queryByRole('textbox')).not.toBeInTheDocument();
  });
});
