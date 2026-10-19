import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { type Project, projectProblems, type Schedule, type ScheduleItem } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  type DemoProject,
  demoPeople,
  demoProjects,
  demoSchedule,
  emptySchedule,
  problem,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects.schedule;

let project: DemoProject;
let schedule: Schedule;
/** What the person in the session may do with the schedule, as the API answers it. */
let permissions: Pick<Project['permissions'], 'canEditSchedule'>;
const requests: string[] = [];

/** The task or milestone of the simulated schedule with the name. */
function itemNamed(name: string): ScheduleItem {
  const item = schedule.phases.flatMap((phase) => phase.items).find((one) => one.name === name);
  if (item === undefined) {
    throw new Error(`No item ${name}.`);
  }
  return item;
}

beforeEach(() => {
  const projects = demoProjects();
  const erp = projects[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  project = erp;
  schedule = demoSchedule();
  permissions = { canEditSchedule: true };
  requests.length = 0;
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
    http.post('/api/projects/:id/schedule/phases', async ({ request }) => {
      const { name } = (await request.json()) as { name: string };
      requests.push(`POST phase ${name}`);
      const id = '0199f5a2-7c1e-7d40-9a1b-500000000099';
      schedule = {
        ...schedule,
        phases: [
          ...schedule.phases,
          { id, name, start: null, end: null, progress: 0, late: false, items: [] },
        ],
      };
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.delete('/api/projects/:id/schedule/phases/:phaseId', ({ params }) => {
      requests.push(`DELETE phase ${String(params.phaseId)}`);
      schedule = {
        ...schedule,
        phases: schedule.phases.filter((phase) => phase.id !== params.phaseId),
      };
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

function table() {
  return screen.getByRole('grid', { name: text.table });
}

/** The lines of the list, a phase as "fase: name" and an item by its name, as shown. */
function lines() {
  return within(table())
    .getAllByRole('row')
    .slice(1)
    .map((row) => {
      const name = within(row).getAllByRole('gridcell')[0]?.textContent ?? '';
      return row.classList.contains('row-group') ? `fase: ${name}` : name;
    });
}

function rowOf(name: string) {
  const row = within(table())
    .getAllByRole('row')
    .find((candidate) => within(candidate).queryAllByRole('gridcell')[0]?.textContent === name);
  if (row === undefined) {
    throw new Error(`No row for ${name}.`);
  }
  return row;
}

function cellsOf(name: string) {
  return within(rowOf(name))
    .getAllByRole('gridcell')
    .map((cell) => cell.textContent);
}

async function openSchedule(width?: number) {
  const view = await renderApp(`/projects/${project.id}/schedule`, width);
  await waitFor(() => {
    expect(within(table()).getAllByRole('row').length).toBeGreaterThan(1);
  });
  return view;
}

async function chooseItem(name: string) {
  fireEvent.click(within(rowOf(name)).getByText(name));
  return screen.findByRole('dialog', { name: text.details.label });
}

const days = (count: number) => text.duration_other.replace('{{count}}', String(count));

// Spec: cronograma, requirement "Aba Cronograma".
describe('Aba Cronograma', () => {
  it('Cronograma pela tela', async () => {
    await openSchedule();

    expect(lines()).toEqual([
      'fase: Levantamento',
      'Reunião de abertura',
      'Mapear processos fiscais',
      'Validar cadastro de produtos',
      'fase: Configuração',
      'Parametrizar regras de ICMS',
      'Integrar emissão de NF-e',
      'Testes integrados',
      'fase: Homologação',
      'Testes com a equipe do cliente',
      'Aceite da homologação',
      'fase: Entrada em produção',
    ]);
    // The phase: its dates, its delay and its progress.
    expect(cellsOf('Levantamento').slice(0, 3)).toEqual([
      'Levantamento',
      '05/10/2026',
      `15/10/2026${text.late}`,
    ]);
    expect(within(rowOf('Levantamento')).getByRole('progressbar')).toHaveAttribute(
      'aria-valuenow',
      '93',
    );
    // The late task, by icon, color and text (ADR-0065).
    expect(cellsOf('Validar cadastro de produtos')).toEqual([
      'Validar cadastro de produtos',
      '13/10/2026',
      `15/10/2026${text.late}`,
      days(3),
      'RSRafael Souza',
      '80%',
      'Mapear processos fiscais',
    ]);
    expect(within(rowOf('Validar cadastro de produtos')).getByText(text.late)).toHaveStyle({
      color: 'var(--status-danger-text)',
    });
    expect(cellsOf('Integrar emissão de NF-e')[4]).toBe(text.noOwner);
    // The milestone: its flag, its day, and no end, owner or progress of its own.
    const milestone = rowOf('Aceite da homologação');
    expect(within(milestone).getByLabelText(text.kinds.Milestone)).toBeInTheDocument();
    expect(cellsOf('Aceite da homologação')).toEqual([
      'Aceite da homologação',
      '03/11/2026',
      '',
      text.kinds.Milestone,
      '',
      text.none,
      'Testes com a equipe do cliente',
    ]);
    // The summary above the list and the trail.
    expect(screen.getByRole('progressbar', { name: text.progress })).toHaveAttribute(
      'aria-valuenow',
      '42',
    );
    expect(screen.getByText(text.lateTasks_one.replace('{{count}}', '1'))).toBeVisible();
    const trail = screen.getByRole('navigation', { name: ptBR.shell.breadcrumb });
    expect(
      within(trail)
        .getAllByRole('listitem')
        .map((level) => level.textContent),
    ).toEqual([ptBR.pages.projects.title, 'ERP-FIN', ptBR.pages.projectSchedule.title]);
    expect(screen.getByRole('tab', { name: ptBR.pages.projectSchedule.title })).toHaveAttribute(
      'aria-selected',
      'true',
    );
    expect(screen.getByText('4 fases · 8 tarefas e marcos')).toBeInTheDocument();
  });

  it('Cronograma vazio', async () => {
    schedule = emptySchedule;
    await renderApp(`/projects/${project.id}/schedule`);

    expect(await screen.findByText(text.empty)).toBeVisible();
    expect(screen.getByRole('button', { name: text.newPhase })).toBeEnabled();
    expect(screen.getByRole('button', { name: text.newItem })).toBeDisabled();
    expect(screen.queryByRole('progressbar', { name: text.progress })).not.toBeInTheDocument();
  });

  it('Cronograma no celular', async () => {
    await openSchedule(phoneWidth);

    expect(screen.getByRole('button', { name: text.newPhase })).toBeVisible();
    expect(screen.getByRole('button', { name: text.newItem })).toBeVisible();
    expect(lines()).toContain('Validar cadastro de produtos');
  });
});

// Spec: cronograma, requirement "Editar o cronograma pela tela".
describe('Editar o cronograma pela tela', () => {
  it('Nova fase pela tela', async () => {
    await openSchedule();

    fireEvent.click(screen.getByRole('button', { name: text.newPhase }));
    const dialog = await screen.findByRole('dialog', { name: text.phaseDialog.newTitle });
    fireEvent.change(within(dialog).getByLabelText(text.phaseDialog.name), {
      target: { value: '  Treinamento ' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.phaseDialog.create }));

    expect(
      await screen.findByText(text.messages.phaseAdded.replace('{{name}}', 'Treinamento')),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(lines()).toContain('fase: Treinamento');
    });
    expect(requests).toEqual(['POST phase Treinamento']);
  });

  it('Nova tarefa pela tela', async () => {
    const posted: unknown[] = [];
    server.use(
      http.post('/api/projects/:id/schedule/items', async ({ request }) => {
        const body = (await request.json()) as Record<string, unknown>;
        posted.push(body);
        const phase = schedule.phases[1];
        if (phase === undefined) {
          throw new Error('No Configuração.');
        }
        // The API computes the end in business days: 3 from Monday 19/10 end on Wednesday 21/10.
        phase.items = [
          ...phase.items,
          {
            id: '0199f5a2-7c1e-7d40-9a1b-600000000099',
            kind: 'Task',
            phaseId: phase.id,
            name: 'Configurar plano de contas',
            start: '2026-10-19',
            end: '2026-10-21',
            duration: 3,
            progress: 0,
            completedOn: null,
            late: false,
            owner: { id: demoPeople.rafael.id, name: demoPeople.rafael.name },
            predecessorIds: [itemNamed('Mapear processos fiscais').id],
          },
        ];
        schedule = { ...schedule };
        return HttpResponse.json({ id: '0199f5a2-7c1e-7d40-9a1b-600000000099' }, { status: 201 });
      }),
    );
    await openSchedule();

    fireEvent.click(screen.getByRole('button', { name: text.newItem }));
    const dialog = await screen.findByRole('dialog', { name: text.form.title });
    fireEvent.change(within(dialog).getByLabelText(text.form.name), {
      target: { value: 'Configurar plano de contas' },
    });
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.form.phase }));
    fireEvent.click(await screen.findByRole('option', { name: 'Configuração' }));
    fireEvent.change(within(dialog).getByLabelText(text.form.start), {
      target: { value: '19102026' },
    });
    expect(within(dialog).getByLabelText(text.form.start)).toHaveValue('19/10/2026');
    fireEvent.change(within(dialog).getByLabelText(text.form.duration), {
      target: { value: '3' },
    });
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.form.owner }));
    fireEvent.click(await screen.findByRole('option', { name: 'Rafael Souza' }));
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.form.dependsOn }));
    fireEvent.click(await screen.findByRole('option', { name: 'Mapear processos fiscais' }));
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.create.Task }));

    expect(
      await screen.findByText(
        text.messages.itemAdded.Task.replace('{{name}}', 'Configurar plano de contas'),
      ),
    ).toBeInTheDocument();
    expect(posted).toEqual([
      {
        kind: 'Task',
        phaseId: schedule.phases[1]?.id,
        name: 'Configurar plano de contas',
        start: '2026-10-19',
        duration: 3,
        ownerId: demoPeople.rafael.id,
        progress: 0,
        predecessorIds: [itemNamed('Mapear processos fiscais').id],
      },
    ]);
    await waitFor(() => {
      expect(cellsOf('Configurar plano de contas').slice(1, 4)).toEqual([
        '19/10/2026',
        '21/10/2026',
        days(3),
      ]);
    });
  });

  it('Novo marco pela tela', async () => {
    const posted: unknown[] = [];
    server.use(
      http.post('/api/projects/:id/schedule/items', async ({ request }) => {
        posted.push(await request.json());
        return HttpResponse.json({ id: '0199f5a2-7c1e-7d40-9a1b-600000000098' }, { status: 201 });
      }),
    );
    await openSchedule();

    fireEvent.click(screen.getByRole('button', { name: text.newItem }));
    const dialog = await screen.findByRole('dialog', { name: text.form.title });
    fireEvent.click(within(dialog).getByRole('radio', { name: text.kinds.Milestone }));
    expect(within(dialog).queryByLabelText(text.form.duration)).not.toBeInTheDocument();
    expect(
      within(dialog).queryByRole('combobox', { name: text.form.owner }),
    ).not.toBeInTheDocument();
    fireEvent.change(within(dialog).getByLabelText(text.form.name), {
      target: { value: 'Virada para produção' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.form.day), {
      target: { value: '13/11/2026' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.create.Milestone }));

    expect(
      await screen.findByText(
        text.messages.itemAdded.Milestone.replace('{{name}}', 'Virada para produção'),
      ),
    ).toBeInTheDocument();
    expect(posted).toEqual([
      expect.objectContaining({
        kind: 'Milestone',
        start: '2026-11-13',
        duration: 0,
        ownerId: null,
        progress: 0,
      }),
    ]);
  });

  it('Tarefa inválida pela tela', async () => {
    await openSchedule();

    fireEvent.click(screen.getByRole('button', { name: text.newItem }));
    const dialog = await screen.findByRole('dialog', { name: text.form.title });
    fireEvent.change(within(dialog).getByLabelText(text.form.start), {
      target: { value: '31022026' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.form.duration), {
      target: { value: '0' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.create.Task }));

    expect(await within(dialog).findByText(text.errors.nameRequired)).toBeVisible();
    expect(within(dialog).getByLabelText(text.form.start)).toHaveAccessibleDescription(
      text.errors.startInvalid,
    );
    expect(within(dialog).getByLabelText(text.form.duration)).toHaveAccessibleDescription(
      text.errors.durationInvalid,
    );
  });

  it('Empurrar pela tela', async () => {
    const changes: unknown[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', async ({ request, params }) => {
        changes.push({ id: params.itemId, ...((await request.json()) as object) });
        // Five days from 06/10 end on 13/10, and the API pushes the two items after it.
        const mapping = itemNamed('Mapear processos fiscais');
        Object.assign(mapping, { duration: 5, end: '2026-10-13' });
        Object.assign(itemNamed('Validar cadastro de produtos'), {
          start: '2026-10-14',
          end: '2026-10-16',
        });
        Object.assign(itemNamed('Parametrizar regras de ICMS'), {
          start: '2026-10-14',
          end: '2026-10-23',
        });
        schedule = { ...schedule };
        return HttpResponse.json({ pushed: 2 });
      }),
    );
    await openSchedule();

    const drawer = await chooseItem('Mapear processos fiscais');
    expect(within(drawer).getByLabelText(text.form.duration)).toHaveAccessibleDescription(
      text.form.endsOn.replace('{{day}}', '09/10/2026'),
    );
    fireEvent.change(within(drawer).getByLabelText(text.form.duration), {
      target: { value: '5' },
    });
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    const saved = text.messages.itemSaved.Task.replace('{{name}}', 'Mapear processos fiscais');
    expect(
      await screen.findByText(
        text.messages.pushed_other.replace('{{saved}}', saved).replace('{{count}}', '2'),
      ),
    ).toBeInTheDocument();
    expect(changes).toEqual([
      {
        id: itemNamed('Mapear processos fiscais').id,
        phaseId: schedule.phases[0]?.id,
        name: 'Mapear processos fiscais',
        start: '2026-10-06',
        duration: 5,
        ownerId: demoPeople.rafael.id,
        progress: 100,
        predecessorIds: [itemNamed('Reunião de abertura').id],
      },
    ]);
    // The details start again from what the API computed.
    await waitFor(() => {
      expect(within(drawer).getByLabelText(text.form.duration)).toHaveAccessibleDescription(
        text.form.endsOn.replace('{{day}}', '13/10/2026'),
      );
    });
    fireEvent.click(within(drawer).getByRole('button', { name: ptBR.details.close }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.details.label })).not.toBeInTheDocument();
    });
    expect(cellsOf('Mapear processos fiscais')[2]).toBe('13/10/2026');
    expect(cellsOf('Validar cadastro de produtos').slice(1, 3)).toEqual([
      '14/10/2026',
      `16/10/2026${text.late}`,
    ]);
    expect(cellsOf('Parametrizar regras de ICMS').slice(1, 3)).toEqual([
      '14/10/2026',
      '23/10/2026',
    ]);
  });

  it('Dependência circular pela tela', async () => {
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', () =>
        problem(400, projectProblems.scheduleCycle),
      ),
    );
    await openSchedule();

    const drawer = await chooseItem('Integrar emissão de NF-e');
    fireEvent.mouseDown(within(drawer).getByRole('combobox', { name: text.form.dependsOn }));
    // The items after it are not offered: they would make a cycle.
    expect(await screen.findByRole('option', { name: 'Testes integrados' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('option', { name: 'Testes integrados' }));
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    expect(await within(drawer).findByText(text.errors.cycle)).toBeVisible();
  });

  it('Sem as seguintes em Depende de', async () => {
    await openSchedule();

    const drawer = await chooseItem('Mapear processos fiscais');
    fireEvent.mouseDown(within(drawer).getByRole('combobox', { name: text.form.dependsOn }));

    // Reunião de abertura is already chosen; every other item comes after it, directly or through others.
    const options = (await screen.findAllByRole('option')).map((option) => option.textContent);
    expect(options).toEqual(['Reunião de abertura', 'Integrar emissão de NF-e']);
  });

  it('Excluir fase pela tela', async () => {
    await openSchedule();

    fireEvent.click(within(rowOf('Configuração')).getByText('Configuração'));
    const dialog = await screen.findByRole('dialog', { name: text.phaseDialog.editTitle });
    expect(within(dialog).getByLabelText(text.phaseDialog.name)).toHaveValue('Configuração');
    fireEvent.click(within(dialog).getByRole('button', { name: text.phaseDialog.remove }));
    const confirm = await screen.findByRole('alertdialog', { name: text.removePhase.title });
    expect(confirm).toHaveAccessibleDescription(
      text.removePhase.withItems_other
        .replace('{{name}}', 'Configuração')
        .replace('{{count}}', '3'),
    );
    fireEvent.click(within(confirm).getByRole('button', { name: text.removePhase.confirm }));

    expect(
      await screen.findByText(text.messages.phaseRemoved.replace('{{name}}', 'Configuração')),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(lines()).not.toContain('fase: Configuração');
    });
    expect(requests).toEqual([`DELETE phase ${demoSchedule().phases[1]?.id ?? ''}`]);
  });

  it('Projeto encerrado pela tela', async () => {
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', () =>
        problem(409, projectProblems.closed),
      ),
    );
    await openSchedule();

    const drawer = await chooseItem('Testes integrados');
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    expect(await within(drawer).findByText(text.messages.closed)).toBeVisible();
  });

  it('Cronograma sem editar pela tela', async () => {
    permissions = { canEditSchedule: false };
    await openSchedule();

    expect(screen.queryByRole('button', { name: text.newPhase })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: text.newItem })).not.toBeInTheDocument();
    fireEvent.click(within(rowOf('Levantamento')).getByText('Levantamento'));
    expect(
      screen.queryByRole('dialog', { name: text.phaseDialog.editTitle }),
    ).not.toBeInTheDocument();

    const drawer = await chooseItem('Validar cadastro de produtos');
    expect(
      within(drawer).getByRole('heading', { name: 'Validar cadastro de produtos' }),
    ).toBeVisible();
    expect(within(drawer).getByText(text.late)).toBeVisible();
    expect(within(drawer).getByText('Rafael Souza')).toBeVisible();
    expect(within(drawer).getByText('Mapear processos fiscais')).toBeVisible();
    expect(within(drawer).queryByRole('button', { name: text.form.save })).not.toBeInTheDocument();
    expect(
      within(drawer).queryByRole('button', { name: text.form.remove.Task }),
    ).not.toBeInTheDocument();
    expect(within(drawer).queryByRole('textbox')).not.toBeInTheDocument();
  });
});
