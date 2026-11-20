import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Schedule, ScheduleItem } from '../api/client';
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
const tickets = text.tickets;

let project: DemoProject;
let schedule: Schedule;

function itemNamed(name: string): ScheduleItem {
  const item = schedule.phases.flatMap((phase) => phase.items).find((one) => one.name === name);
  if (item === undefined) {
    throw new Error(`No item ${name}.`);
  }
  return item;
}

/** An open ticket on the schedule, as the API sends it (ADR-0164). */
const ticket = (number: number, title: string): ScheduleItem['tickets'][number] => ({
  number,
  title,
  deadline: 'Resolution',
  dueState: 'Overdue',
  dueOn: '2026-10-15',
  daysLate: 2,
});

beforeEach(() => {
  const projects = demoProjects();
  const erp = projects[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  project = erp;
  schedule = demoSchedule();
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

// Spec: cronograma, requirement "Chamados do item pela tela" (ADR-0164).
describe('Chamados do item pela tela', () => {
  it('Marca na Lista e no Quadro', async () => {
    const task = itemNamed('Testes integrados');
    task.tickets = [
      ticket(4, 'Rejeição 539 ao emitir nota de devolução'),
      ticket(7, 'Relatório de notas'),
    ];
    const count = tickets.count_other.replace('{{count}}', '2');

    const { router } = await renderApp(`/projects/${project.id}/schedule?view=list`);

    const names = await screen.findAllByText('Testes integrados', { selector: 'span' });
    const row = names
      .map((name) => name.closest<HTMLElement>('[role="row"]'))
      .find((one) => one !== null);
    expect(within(row ?? document.body).getByRole('img', { name: count })).toBeVisible();
    // Only the items with tickets.
    expect(screen.getAllByRole('img', { name: /chamados? em aberto/ })).toHaveLength(1);

    await router.navigate(`/projects/${project.id}/schedule?view=board`);
    const card = await waitFor(() => {
      const found = document.querySelector<HTMLElement>(`[data-item="${task.id}"]`);
      if (found === null) {
        throw new Error('No card.');
      }
      return found;
    });
    expect(within(card).getByRole('img', { name: count })).toBeVisible();
  });

  it('Seção Chamados no detalhe', async () => {
    itemNamed('Testes integrados').tickets = [
      ticket(4, 'Rejeição 539 ao emitir nota de devolução'),
    ];

    const drawer = await openItem('Testes integrados');

    const section = await within(drawer).findByRole('region', { name: tickets.title });
    const list = within(section).getByRole('list', {
      name: tickets.list.replace('{{name}}', 'Testes integrados'),
    });
    const link = within(list).getByRole('link', {
      name: '#4 Rejeição 539 ao emitir nota de devolução',
    });
    expect(link).toHaveAttribute('href', '/tickets/4');
    // The deadline that counts now, by icon, color and text.
    expect(within(list).getByText(/2 dias úteis/)).toBeVisible();
    fireEvent.click(link);
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.details.label })).not.toBeInTheDocument();
    });
  });

  it('Ameaçado por no marco', async () => {
    const acceptance = itemNamed('Aceite da homologação');
    const tests = itemNamed('Testes integrados');
    acceptance.threats = [
      {
        ticket: ticket(4, 'Rejeição 539 ao emitir nota de devolução'),
        through: [{ id: tests.id, name: tests.name }],
      },
    ];

    const drawer = await openItem('Aceite da homologação');

    const section = await within(drawer).findByRole('region', { name: tickets.title });
    const threats = within(section).getByRole('list', { name: tickets.threatenedBy });
    expect(
      within(threats).getByRole('link', { name: '#4 Rejeição 539 ao emitir nota de devolução' }),
    ).toBeVisible();
    expect(
      within(threats).getByText(tickets.through.replace('{{items}}', 'Testes integrados')),
    ).toBeVisible();
  });

  it('Item sem chamados', async () => {
    const drawer = await openItem('Testes integrados');

    await within(drawer).findByRole('region', { name: new RegExp(`^${text.comments.title}`) });
    expect(within(drawer).queryByRole('region', { name: tickets.title })).not.toBeInTheDocument();
    expect(screen.queryByRole('img', { name: /chamados? em aberto/ })).not.toBeInTheDocument();
  });
});
