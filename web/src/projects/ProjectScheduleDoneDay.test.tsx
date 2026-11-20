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

let project: DemoProject;
let schedule: Schedule;
let sent: unknown[];

function itemNamed(name: string): ScheduleItem {
  const item = schedule.phases.flatMap((phase) => phase.items).find((one) => one.name === name);
  if (item === undefined) {
    throw new Error(`No item ${name}.`);
  }
  return item;
}

const itemById = (id: unknown) =>
  schedule.phases.flatMap((phase) => phase.items).find((item) => item.id === id);

beforeEach(() => {
  const projects = demoProjects();
  const erp = projects[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  project = erp;
  schedule = demoSchedule();
  sent = [];
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
    // The day up to Monday, 19/10/2026, as the API takes it (ADR-0167).
    http.put('/api/projects/:id/schedule/items/:itemId/progress', async ({ params, request }) => {
      const body = (await request.json()) as { progress: number; completedOn?: string };
      sent.push(body);
      if (body.completedOn !== undefined && body.completedOn > '2026-10-19') {
        return HttpResponse.json(
          { title: 'Invalid', status: 400, errors: { completedOn: ['Up to today.'] } },
          { status: 400 },
        );
      }
      const item = itemById(params.itemId);
      if (item !== undefined) {
        item.progress = body.progress;
        item.completedOn = body.completedOn ?? item.completedOn;
      }
      return new HttpResponse(null, { status: 204 });
    }),
    http.put('/api/projects/:id/schedule/items/:itemId/reached', async ({ params, request }) => {
      const body = (await request.json()) as { reached: boolean; reachedOn?: string };
      sent.push(body);
      const item = itemById(params.itemId);
      if (item !== undefined) {
        item.reachedOn = body.reachedOn ?? item.reachedOn;
      }
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

/** Opens the details of an item by the address, as the Lista does. */
async function openItem(name: string) {
  await renderApp(`/projects/${project.id}/schedule?view=list&item=${itemNamed(name).id}`);
  return screen.findByRole('dialog', { name: text.details.label });
}

// Spec: cronograma, requirement "Alterar o dia da conclusão pela tela" (ADR-0167).
describe('Alterar o dia da conclusão pela tela', () => {
  it('Concluir num dia pela tela', async () => {
    const drawer = await openItem('Integrar emissão de NF-e');

    fireEvent.click(
      await within(drawer).findByRole('button', { name: text.progressActions.update }),
    );
    const dialog = await screen.findByRole('dialog', { name: text.progressDialog.updateTitle });
    // The day is asked only at 100%.
    expect(within(dialog).queryByLabelText(text.progressDialog.completedOn)).toBeNull();
    fireEvent.change(within(dialog).getByLabelText(text.form.progress), {
      target: { value: '100' },
    });
    const day = within(dialog).getByLabelText(text.progressDialog.completedOn);
    fireEvent.change(day, { target: { value: '16/10/2026' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.save }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.progressDialog.updateTitle })).toBeNull();
    });
    expect(sent).toEqual([{ progress: 100, completedOn: '2026-10-16' }]);
    expect(
      await within(drawer).findByText(text.completedOn.replace('{{day}}', '16/10/2026')),
    ).toBeVisible();
  });

  it('Alterar o dia do marco pela tela', async () => {
    itemNamed('Aceite da homologação').reachedOn = '2026-10-16';

    const drawer = await openItem('Aceite da homologação');

    fireEvent.click(
      await within(drawer).findByRole('button', { name: text.milestoneActions.changeDay }),
    );
    const dialog = await screen.findByRole('dialog', {
      name: text.doneDayDialog.milestoneTitle,
    });
    const day = within(dialog).getByLabelText(text.doneDayDialog.reachedOn);
    expect(day).toHaveValue('16/10/2026');
    fireEvent.change(day, { target: { value: '15/10/2026' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.save }));

    expect(
      await screen.findByText(
        text.messages.reachedOnChanged
          .replace('{{name}}', 'Aceite da homologação')
          .replace('{{day}}', '15/10/2026'),
      ),
    ).toBeInTheDocument();
    expect(sent).toEqual([{ reached: true, reachedOn: '2026-10-15' }]);
  });

  it('Dia no futuro pela tela', async () => {
    const drawer = await openItem('Mapear processos fiscais');

    fireEvent.click(
      await within(drawer).findByRole('button', { name: text.progressActions.changeDay }),
    );
    const dialog = await screen.findByRole('dialog', { name: text.doneDayDialog.taskTitle });
    const day = within(dialog).getByLabelText(text.doneDayDialog.completedOn);
    expect(day).toHaveValue('09/10/2026');
    fireEvent.change(day, { target: { value: '20/10/2026' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.save }));

    expect(await within(dialog).findByText(text.errors.doneDayInvalid)).toBeVisible();
    expect(sent).toEqual([{ progress: 100, completedOn: '2026-10-20' }]);
    expect(itemNamed('Mapear processos fiscais').completedOn).toBe('2026-10-09');
  });
});
