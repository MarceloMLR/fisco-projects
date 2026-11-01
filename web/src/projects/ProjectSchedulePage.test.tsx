import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { type Project, projectProblems, type Schedule, type ScheduleItem } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { taskHoursOf, taskHoursRow } from '../test/demoProjectHours';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  type DemoProject,
  demoPeople,
  demoProjects,
  demoSchedule,
  demoScheduleWithSubtasks,
  emailOf,
  emptySchedule,
  membersOf,
  problem,
  projectsPage,
  scheduleSeenBy,
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

/** Opens the Lista of the tab; on a phone, it is the view that opens by itself (ADR-0071). */
async function openSchedule(width?: number) {
  const view = await renderApp(
    `/projects/${project.id}/schedule${width === phoneWidth ? '' : '?view=list'}`,
    width,
  );
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

/** The bodies the screen sent to the progress of the tasks; the simulated API saves them on the schedule. */
const progressSent: unknown[] = [];

/** The simulated API of the progress of a task: 100% keeps 16/10/2026 as the day it was completed. */
function answerProgress() {
  progressSent.length = 0;
  server.use(
    http.put('/api/projects/:id/schedule/items/:itemId/progress', async ({ request, params }) => {
      const { progress } = (await request.json()) as { progress: number };
      progressSent.push({ id: params.itemId, progress });
      const item = schedule.phases
        .flatMap((phase) => phase.items)
        .find((one) => one.id === params.itemId);
      if (item !== undefined) {
        const late = progress < 100 && item.end < '2026-10-16';
        Object.assign(item, {
          progress,
          completedOn: progress === 100 ? '2026-10-16' : null,
          late,
          lateDays: late ? item.lateDays : 0,
        });
      }
      schedule = { ...schedule };
      return new HttpResponse(null, { status: 204 });
    }),
  );
}

async function closeDrawer(drawer: HTMLElement) {
  fireEvent.click(within(drawer).getByRole('button', { name: ptBR.details.close }));
  await waitFor(() => {
    expect(screen.queryByRole('dialog', { name: text.details.label })).not.toBeInTheDocument();
  });
}

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
    // The late task, by icon, color and text (ADR-0065), with the business days of delay under its end.
    const oneDayLate = text.lateDays_one.replace('{{count}}', '1');
    expect(cellsOf('Validar cadastro de produtos')).toEqual([
      'Validar cadastro de produtos',
      '13/10/2026',
      `15/10/2026${oneDayLate}`,
      '',
      days(3),
      'RSRafael Souza',
      '80%',
      'Mapear processos fiscais',
    ]);
    expect(within(rowOf('Validar cadastro de produtos')).getByText(oneDayLate)).toHaveStyle({
      color: 'var(--status-danger-text)',
    });
    expect(cellsOf('Integrar emissão de NF-e')[5]).toBe(text.noOwner);
    // The milestone: its flag, its day, its state, and no end or owner of its own.
    const milestone = rowOf('Aceite da homologação');
    expect(within(milestone).getByLabelText(text.kinds.Milestone)).toBeInTheDocument();
    expect(cellsOf('Aceite da homologação')).toEqual([
      'Aceite da homologação',
      '03/11/2026',
      '',
      '',
      text.kinds.Milestone,
      '',
      text.milestone.pending,
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

  it('Estados do marco pela tela', async () => {
    const [survey] = schedule.phases;
    if (survey === undefined) {
      throw new Error('No Levantamento.');
    }
    const milestone = (name: string, day: string, more: Partial<ScheduleItem>): ScheduleItem => ({
      ...itemNamed('Aceite da homologação'),
      id: `0199f5a2-7c1e-7d40-9a1b-6000000000${day.slice(-2)}`,
      phaseId: survey.id,
      name,
      start: day,
      end: day,
      predecessorIds: [],
      ...more,
    });
    survey.items = [
      ...survey.items.slice(0, 2),
      milestone('Processos mapeados', '2026-10-09', { reachedOn: '2026-10-09' }),
      ...survey.items.slice(2),
      milestone('Aceite do levantamento', '2026-10-15', { late: true, lateDays: 1 }),
    ];
    schedule = { ...schedule, lateMilestones: 1 };
    await openSchedule();

    const state = (name: string) => cellsOf(name)[6];
    expect(state('Processos mapeados')).toBe(text.milestone.reached.replace('{{day}}', '09/10'));
    expect(state('Aceite da homologação')).toBe(text.milestone.pending);
    expect(state('Aceite do levantamento')).toBe(text.milestone.late);
    // Late by icon, color and text (ADR-0065), in the column of its state and not as "Atrasada"; the business days of
    // delay in the column of the end, which a milestone does not have.
    const late = within(rowOf('Aceite do levantamento')).getByText(text.milestone.late);
    expect(late).toHaveStyle({ color: 'var(--status-danger-text)' });
    expect(cellsOf('Aceite do levantamento')[2]).toBe(text.lateDays_one.replace('{{count}}', '1'));
    expect(cellsOf('Aceite da homologação')[2]).toBe('');
    expect(
      within(rowOf('Processos mapeados')).getByText(
        text.milestone.reached.replace('{{day}}', '09/10'),
      ),
    ).toHaveStyle({ color: 'var(--status-success-text)' });
    expect(screen.getByText(text.lateMilestones_one.replace('{{count}}', '1'))).toBeVisible();
    expect(screen.getByText(text.lateTasks_one.replace('{{count}}', '1'))).toBeVisible();
  });

  it('Atraso desde um dia não útil pela tela', async () => {
    // On a Saturday, the task of Friday and the milestone of the day before lost no business day yet.
    const [survey] = schedule.phases;
    if (survey === undefined) {
      throw new Error('No Levantamento.');
    }
    survey.items = survey.items.map((item) =>
      item.name === 'Validar cadastro de produtos' ? { ...item, lateDays: 0 } : item,
    );
    survey.items.push({
      ...itemNamed('Aceite da homologação'),
      id: '0199f5a2-7c1e-7d40-9a1b-600000000077',
      phaseId: survey.id,
      name: 'Aceite do levantamento',
      start: '2026-10-15',
      end: '2026-10-15',
      predecessorIds: [],
      late: true,
      lateDays: 0,
    });
    await openSchedule();

    expect(cellsOf('Validar cadastro de produtos')[2]).toBe(`15/10/2026${text.late}`);
    expect(cellsOf('Aceite do levantamento')[2]).toBe('');
    expect(cellsOf('Aceite do levantamento')[6]).toBe(text.milestone.late);
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
            critical: false,
            baselineStart: null,
            baselineEnd: null,
            baselineDeviation: null,
            deadline: null,
            deadlineDeviation: null,
            links: [],
            float: null,
            latestEnd: null,
            note: null,
            duration: 3,
            progress: 0,
            completedOn: null,
            reachedOn: null,
            late: false,
            lateDays: 0,
            canUpdateProgress: true,
            owner: { id: demoPeople.rafael.id, name: demoPeople.rafael.name },
            ownedByClient: false,
            predecessorIds: [itemNamed('Mapear processos fiscais').id],
            parentId: null,
            hasSubtasks: false,
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
        // Right in the phase (ADR-0088).
        parentId: null,
        ownedByClient: false,
        deadline: null,
        // No note: an empty one is sent, which leaves the item without one (ADR-0139).
        note: '',
        // "Finishes, then starts" without lag, as the API takes them (ADR-0137).
        links: [
          {
            predecessorId: itemNamed('Mapear processos fiscais').id,
            type: 'FinishToStart',
            lag: 0,
          },
        ],
      },
    ]);
    await waitFor(() => {
      expect(cellsOf('Configurar plano de contas').slice(1, 5)).toEqual([
        '19/10/2026',
        '21/10/2026',
        '',
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
        // Ending today, it is no longer late.
        Object.assign(itemNamed('Validar cadastro de produtos'), {
          start: '2026-10-14',
          end: '2026-10-16',
          late: false,
          lateDays: 0,
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
        // The progress stays as it is: it changes by its own action.
        progress: null,
        predecessorIds: [itemNamed('Reunião de abertura').id],
        // Right in the phase (ADR-0088).
        parentId: null,
        ownedByClient: false,
        deadline: null,
        // No note: an empty one is sent, which leaves the item without one (ADR-0139).
        note: '',
        // "Finishes, then starts" without lag, as the API takes them (ADR-0137).
        links: [
          { predecessorId: itemNamed('Reunião de abertura').id, type: 'FinishToStart', lag: 0 },
        ],
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
      '16/10/2026',
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
    expect(within(drawer).getByText(text.lateDays_one.replace('{{count}}', '1'))).toBeVisible();
    expect(within(drawer).getByText('Rafael Souza')).toBeVisible();
    expect(within(drawer).getByText('Mapear processos fiscais')).toBeVisible();
    expect(within(drawer).queryByRole('button', { name: text.form.save })).not.toBeInTheDocument();
    expect(
      within(drawer).queryByRole('button', { name: text.form.remove.Task }),
    ).not.toBeInTheDocument();
    expect(within(drawer).queryByRole('textbox')).not.toBeInTheDocument();
  });
});

// Spec: cronograma, requirement "Progresso pela tela".
describe('Progresso pela tela', () => {
  beforeEach(() => {
    answerProgress();
  });

  it('Iniciar pela tela', async () => {
    // Marina is in charge of Testes integrados, at 0%, and does not edit the schedule.
    permissions = { canEditSchedule: false };
    schedule = scheduleSeenBy(schedule, demoPeople.marina.id);
    await openSchedule();

    const drawer = await chooseItem('Testes integrados');
    expect(within(drawer).queryByRole('button', { name: text.progressActions.update })).toBeNull();
    fireEvent.click(within(drawer).getByRole('button', { name: text.progressActions.start }));
    const dialog = await screen.findByRole('dialog', { name: text.progressDialog.startTitle });
    expect(within(dialog).getByLabelText(text.form.progress)).toHaveValue('');
    fireEvent.change(within(dialog).getByLabelText(text.form.progress), {
      target: { value: '30' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.save }));

    expect(
      await screen.findByText(
        text.messages.progressUpdated
          .replace('{{name}}', 'Testes integrados')
          .replace('{{progress}}', '30'),
      ),
    ).toBeInTheDocument();
    expect(progressSent).toEqual([{ id: itemNamed('Testes integrados').id, progress: 30 }]);
    await waitFor(() => {
      expect(
        within(drawer).getByRole('progressbar', {
          name: text.itemProgress.replace('{{name}}', 'Testes integrados'),
        }),
      ).toHaveAttribute('aria-valuenow', '30');
    });
    expect(within(drawer).getByRole('button', { name: text.progressActions.update })).toBeVisible();
    await closeDrawer(drawer);
    expect(cellsOf('Testes integrados')[6]).toBe('30%');
  });

  it('Atualizar progresso pela tela', async () => {
    await openSchedule();

    const drawer = await chooseItem('Integrar emissão de NF-e');
    // The form of the task has no progress: it changes by its own action.
    expect(within(drawer).queryByLabelText(text.form.progress)).not.toBeInTheDocument();
    fireEvent.click(within(drawer).getByRole('button', { name: text.progressActions.update }));
    const dialog = await screen.findByRole('dialog', { name: text.progressDialog.updateTitle });
    const field = within(dialog).getByLabelText(text.form.progress);
    expect(field).toHaveValue('20');
    fireEvent.change(field, { target: { value: '150' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.save }));
    expect(await within(dialog).findByText(text.errors.progressInvalid)).toBeVisible();
    fireEvent.change(field, { target: { value: '45' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.save }));

    expect(
      await screen.findByText(
        text.messages.progressUpdated
          .replace('{{name}}', 'Integrar emissão de NF-e')
          .replace('{{progress}}', '45'),
      ),
    ).toBeInTheDocument();
    expect(progressSent).toEqual([{ id: itemNamed('Integrar emissão de NF-e').id, progress: 45 }]);
  });

  it('Concluir pela tela', async () => {
    // Rafael is in charge of Validar cadastro de produtos, at 80% and late.
    permissions = { canEditSchedule: false };
    schedule = scheduleSeenBy(schedule, demoPeople.rafael.id);
    await openSchedule();

    const drawer = await chooseItem('Validar cadastro de produtos');
    fireEvent.click(within(drawer).getByRole('button', { name: text.progressActions.complete }));

    expect(
      await screen.findByText(
        text.messages.completed.replace('{{name}}', 'Validar cadastro de produtos'),
      ),
    ).toBeInTheDocument();
    expect(progressSent).toEqual([
      { id: itemNamed('Validar cadastro de produtos').id, progress: 100 },
    ]);
    expect(
      await within(drawer).findByText(text.completedOn.replace('{{day}}', '16/10/2026')),
    ).toBeVisible();
    expect(
      within(drawer).queryByRole('button', { name: text.progressActions.complete }),
    ).not.toBeInTheDocument();
    expect(within(drawer).getByRole('button', { name: text.progressActions.update })).toBeVisible();
    expect(
      within(drawer).queryByText(text.lateDays_one.replace('{{count}}', '1')),
    ).not.toBeInTheDocument();
  });

  it('Progresso sem permissão pela tela', async () => {
    // Carla, a contributor, is in charge of none of the tasks.
    permissions = { canEditSchedule: false };
    schedule = scheduleSeenBy(schedule, demoPeople.carla.id);
    await openSchedule();

    const drawer = await chooseItem('Validar cadastro de produtos');

    expect(
      within(drawer).getByRole('progressbar', {
        name: text.itemProgress.replace('{{name}}', 'Validar cadastro de produtos'),
      }),
    ).toHaveAttribute('aria-valuenow', '80');
    for (const action of Object.values(text.progressActions)) {
      expect(within(drawer).queryByRole('button', { name: action })).not.toBeInTheDocument();
    }
    await closeDrawer(drawer);
    const milestone = await chooseItem('Aceite da homologação');
    expect(within(milestone).getByText(text.milestone.pending)).toBeVisible();
    expect(
      within(milestone).queryByRole('button', { name: text.milestoneActions.markReached }),
    ).not.toBeInTheDocument();
  });

  it('Progresso num projeto encerrado pela tela', async () => {
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId/progress', () =>
        problem(409, projectProblems.closed),
      ),
    );
    await openSchedule();

    const drawer = await chooseItem('Integrar emissão de NF-e');
    fireEvent.click(within(drawer).getByRole('button', { name: text.progressActions.complete }));

    expect(await within(drawer).findByText(text.messages.closed)).toBeVisible();
  });
});

// Spec: cronograma, requirement "Marco atingido pela tela".
describe('Marco atingido pela tela', () => {
  it('Marcar marco pela tela', async () => {
    const sent: unknown[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId/reached', async ({ request, params }) => {
        const { reached } = (await request.json()) as { reached: boolean };
        sent.push({ id: params.itemId, reached });
        Object.assign(itemNamed('Aceite da homologação'), {
          reachedOn: reached ? '2026-10-16' : null,
        });
        schedule = { ...schedule };
        return new HttpResponse(null, { status: 204 });
      }),
    );
    await openSchedule();

    const drawer = await chooseItem('Aceite da homologação');
    expect(within(drawer).getByText(text.milestone.pending)).toBeVisible();
    fireEvent.click(
      within(drawer).getByRole('button', { name: text.milestoneActions.markReached }),
    );

    expect(
      await screen.findByText(text.messages.reached.replace('{{name}}', 'Aceite da homologação')),
    ).toBeInTheDocument();
    expect(sent).toEqual([{ id: itemNamed('Aceite da homologação').id, reached: true }]);
    expect(
      await within(drawer).findByRole('button', { name: text.milestoneActions.unmark }),
    ).toBeVisible();
    expect(
      within(drawer).getByText(text.milestone.reached.replace('{{day}}', '16/10/2026')),
    ).toBeVisible();
    await closeDrawer(drawer);
    expect(cellsOf('Aceite da homologação')[6]).toBe(
      text.milestone.reached.replace('{{day}}', '16/10'),
    );
  });
});

// Spec: cronograma, requirement "Subtarefas pela tela" (ADR-0088).
describe('Subtarefas pela tela', () => {
  it('Subtarefas na lista', async () => {
    schedule = demoScheduleWithSubtasks();
    await openSchedule();

    expect(lines().slice(4, 10)).toEqual([
      'fase: Configuração',
      'Parametrizar regras de ICMS',
      'Integrar emissão de NF-e',
      'Testes integrados',
      'Roteiro de testes',
      'Executar testes',
    ]);
    // The summary, with the dates, the duration and the progress of its subtasks.
    expect(cellsOf('Testes integrados').slice(1, 5)).toEqual([
      '23/10/2026',
      '29/10/2026',
      '',
      days(5),
    ]);
    expect(within(rowOf('Testes integrados')).getByText('Testes integrados')).toHaveStyle({
      fontWeight: '500',
    });
    // A subtask one step further in than the tasks of the phase.
    const indent = (name: string) => {
      const cell = within(rowOf(name)).getByText(name).parentElement;
      return cell === null ? '' : window.getComputedStyle(cell).paddingLeft;
    };
    expect(indent('Roteiro de testes')).toBe('calc(2 * var(--space-6))');
    expect(indent('Executar testes')).toBe('calc(2 * var(--space-6))');
    expect(indent('Integrar emissão de NF-e')).toBe('var(--space-6)');
    expect(cellsOf('Executar testes')[7]).toBe('Roteiro de testes');
    // The summary has the arrow of its subtasks, shown at first; the other tasks have none.
    expect(
      within(rowOf('Testes integrados')).getByRole('button', {
        name: text.collapseSubtasks.replace('{{name}}', 'Testes integrados'),
      }),
    ).toHaveAttribute('aria-expanded', 'true');
    expect(within(rowOf('Integrar emissão de NF-e')).queryByRole('button')).not.toBeInTheDocument();
  });

  it('Recolher subtarefas na lista', async () => {
    schedule = demoScheduleWithSubtasks();
    await openSchedule();

    fireEvent.click(
      within(rowOf('Testes integrados')).getByRole('button', {
        name: text.collapseSubtasks.replace('{{name}}', 'Testes integrados'),
      }),
    );

    // The subtasks leave the Lista, and the details of the task do not open.
    await waitFor(() => {
      expect(lines()).not.toContain('Roteiro de testes');
    });
    expect(lines().slice(4, 9)).toEqual([
      'fase: Configuração',
      'Parametrizar regras de ICMS',
      'Integrar emissão de NF-e',
      'Testes integrados',
      'fase: Homologação',
    ]);
    expect(screen.queryByRole('dialog', { name: text.details.label })).not.toBeInTheDocument();
    const show = within(rowOf('Testes integrados')).getByRole('button', {
      name: text.expandSubtasks.replace('{{name}}', 'Testes integrados'),
    });
    expect(show).toHaveAttribute('aria-expanded', 'false');
    // By the key too, and they come back.
    fireEvent.keyDown(show, { key: 'Enter' });
    fireEvent.click(show);
    await waitFor(() => {
      expect(lines()).toContain('Executar testes');
    });
    expect(screen.queryByRole('dialog', { name: text.details.label })).not.toBeInTheDocument();
  });

  it('Nova subtarefa pela tela', async () => {
    const posted: Record<string, unknown>[] = [];
    server.use(
      http.post('/api/projects/:id/schedule/items', async ({ request }) => {
        posted.push((await request.json()) as Record<string, unknown>);
        return HttpResponse.json({ id: '0199f5a2-7c1e-7d40-9a1b-600000000098' }, { status: 201 });
      }),
    );
    await openSchedule();

    const drawer = await chooseItem('Integrar emissão de NF-e');
    // In the Subtarefas of the details, still without any.
    const section = within(drawer).getByRole('region', { name: text.details.subtasks });
    expect(within(section).queryByRole('listitem')).not.toBeInTheDocument();
    fireEvent.click(within(section).getByRole('button', { name: text.form.newSubtask }));

    const dialog = await screen.findByRole('dialog', { name: text.form.newSubtask });
    expect(within(dialog).getByRole('combobox', { name: text.form.parent })).toHaveTextContent(
      'Integrar emissão de NF-e',
    );
    expect(
      within(dialog).getByText(
        text.form.becomesSummary.replace('{{name}}', 'Integrar emissão de NF-e'),
      ),
    ).toBeVisible();
    fireEvent.change(within(dialog).getByLabelText(text.form.name), {
      target: { value: 'Configurar o certificado digital' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.form.start), {
      target: { value: '19102026' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.form.duration), {
      target: { value: '2' },
    });
    // The task it goes into is not offered as a dependency.
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.form.dependsOn }));
    expect(await screen.findByRole('option', { name: 'Testes integrados' })).toBeInTheDocument();
    expect(
      screen.queryByRole('option', { name: 'Integrar emissão de NF-e' }),
    ).not.toBeInTheDocument();
    fireEvent.click(within(dialog).getByRole('button', { name: text.form.create.Task }));

    await waitFor(() => {
      expect(posted).toEqual([
        expect.objectContaining({
          kind: 'Task',
          phaseId: schedule.phases[1]?.id,
          name: 'Configurar o certificado digital',
          start: '2026-10-19',
          duration: 2,
          parentId: itemNamed('Integrar emissão de NF-e').id,
        }),
      ]);
    });
  });

  it('Detalhe da tarefa-resumo', async () => {
    schedule = demoScheduleWithSubtasks();
    const changes: Record<string, unknown>[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', async ({ request }) => {
        changes.push((await request.json()) as Record<string, unknown>);
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    await openSchedule();

    const drawer = await chooseItem('Testes integrados');

    // The name, the phase and the owner change; the dates and the progress come from the subtasks.
    expect(within(drawer).getByLabelText(text.form.name)).toHaveValue('Testes integrados');
    expect(within(drawer).queryByLabelText(text.form.start)).not.toBeInTheDocument();
    expect(within(drawer).queryByLabelText(text.form.duration)).not.toBeInTheDocument();
    expect(
      within(drawer).queryByRole('combobox', { name: text.form.dependsOn }),
    ).not.toBeInTheDocument();
    expect(
      within(drawer).queryByRole('combobox', { name: text.form.parent }),
    ).not.toBeInTheDocument();
    expect(within(drawer).getByText('23/10/2026')).toBeVisible();
    expect(within(drawer).getByText('29/10/2026')).toBeVisible();
    expect(within(drawer).getByText(text.computed_other.replace('{{count}}', '2'))).toBeVisible();
    expect(
      within(drawer).queryByRole('button', { name: text.progressActions.start }),
    ).not.toBeInTheDocument();
    // The Subtarefas, with "Nova subtarefa"; the buttons of the end of the details are of the same size.
    const section = within(drawer).getByRole('region', { name: text.details.subtasks });
    expect(
      within(section)
        .getAllByRole('listitem')
        .map((subtask) => subtask.textContent),
    ).toEqual(['Roteiro de testes', 'Executar testes']);
    expect(within(section).getByRole('button', { name: text.form.newSubtask })).toBeVisible();
    expect(within(drawer).getByRole('button', { name: text.form.remove.Task })).toHaveClass(
      'MuiButton-sizeMedium',
    );
    expect(within(drawer).getByRole('button', { name: text.form.save })).toHaveClass(
      'MuiButton-sizeMedium',
    );
    fireEvent.change(within(drawer).getByLabelText(text.form.name), {
      target: { value: 'Testes integrados do ERP' },
    });
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    await waitFor(() => {
      expect(changes).toEqual([
        expect.objectContaining({
          name: 'Testes integrados do ERP',
          progress: null,
          predecessorIds: [],
          parentId: null,
        }),
      ]);
    });
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.remove.Task }));
    expect(
      await screen.findByText(
        text.removeItem.textSummary_other
          .replace('{{name}}', 'Testes integrados')
          .replace('{{count}}', '2'),
      ),
    ).toBeVisible();
    fireEvent.click(screen.getByRole('button', { name: text.removeItem.back }));

    // A subtask of the section opens its own details.
    const subtasks = await within(drawer).findByRole('region', { name: text.details.subtasks });
    fireEvent.click(within(subtasks).getByRole('button', { name: 'Roteiro de testes' }));
    expect(await within(drawer).findByRole('heading', { name: 'Roteiro de testes' })).toBeVisible();
    expect(within(drawer).getByRole('combobox', { name: text.form.parent })).toHaveTextContent(
      'Testes integrados',
    );
  });
});

// Spec: cronograma, requirement "Endereço do detalhe".
describe('Endereço do detalhe', () => {
  it('Detalhe pelo endereço', async () => {
    const task = itemNamed('Parametrizar regras de ICMS');
    const { router } = await renderApp(
      `/projects/${project.id}/schedule?view=list&item=${task.id}`,
    );

    const drawer = await screen.findByRole('dialog', { name: text.details.label });
    expect(within(drawer).getByDisplayValue('Parametrizar regras de ICMS')).toBeInTheDocument();
    await closeDrawer(drawer);
    expect(router.state.location.search).toBe('?view=list');
  });

  it('Item que saiu do cronograma', async () => {
    await renderApp(
      `/projects/${project.id}/schedule?view=list&item=0199f5a2-7c1e-7d40-9a1b-5000000000ff`,
    );
    await waitFor(() => {
      expect(within(table()).getAllByRole('row').length).toBeGreaterThan(1);
    });

    expect(screen.queryByRole('dialog', { name: text.details.label })).not.toBeInTheDocument();
  });
});

// Spec: cronograma, requirement "Cliente como responsável" (ADR-0102).
// Spec: usuarios-e-papeis, requirement "E-mail na escolha de pessoa".
describe('E-mail na escolha de pessoa', () => {
  it('Responsável da tarefa com o e-mail', async () => {
    server.use(http.get('/api/projects/:id/members', () => HttpResponse.json(membersOf(project))));
    await openSchedule();

    fireEvent.click(screen.getByRole('button', { name: text.newItem }));
    const dialog = await screen.findByRole('dialog', { name: text.form.title });
    const field = within(dialog).getByRole('combobox', { name: text.form.owner });
    fireEvent.mouseDown(field);

    // The people who may own a task, each with the e-mail below the name; the field shows only the name.
    const people = project.members
      .filter((member) => member.role !== 'Viewer')
      .map((member) => member.name);
    await waitFor(() => {
      expect(screen.getAllByRole('option').map((option) => option.textContent)).toEqual(
        expect.arrayContaining(people.map((name) => `${name}${emailOf(name)}`)),
      );
    });
    fireEvent.click(
      screen.getByRole('option', { name: (name) => name.startsWith('Rafael Souza') }),
    );
    expect(field).toHaveTextContent(/^Rafael Souza$/);
  });
});

describe('Cliente como responsável', () => {
  it('Cliente como responsável pela tela', async () => {
    const changes: unknown[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', async ({ request }) => {
        const body = (await request.json()) as { ownerId: string | null; ownedByClient: boolean };
        changes.push(body);
        Object.assign(itemNamed('Mapear processos fiscais'), {
          owner: null,
          ownedByClient: body.ownedByClient,
        });
        schedule = { ...schedule };
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    const client = project.client?.name ?? '';
    await openSchedule();

    const drawer = await chooseItem('Mapear processos fiscais');
    fireEvent.mouseDown(within(drawer).getByRole('combobox', { name: text.form.owner }));
    fireEvent.click(
      await screen.findByRole('option', {
        name: text.form.clientOwner.replace('{{name}}', client),
      }),
    );
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    await waitFor(() => {
      expect(changes).toEqual([expect.objectContaining({ ownerId: null, ownedByClient: true })]);
    });
    await closeDrawer(drawer);
    await waitFor(() => {
      expect(
        within(rowOf('Mapear processos fiscais')).getByText(
          text.clientOwner.replace('{{name}}', client),
        ),
      ).toBeInTheDocument();
    });
  });

  it('Cliente como responsável sem cliente pela tela', async () => {
    project = { ...project, client: null };
    await openSchedule();

    const drawer = await chooseItem('Mapear processos fiscais');
    fireEvent.mouseDown(within(drawer).getByRole('combobox', { name: text.form.owner }));

    const options = await screen.findAllByRole('option');
    expect(options.map((option) => option.textContent)).not.toContain(
      text.clientOwnerWithoutClient,
    );
    expect(options.some((option) => option.textContent.endsWith('(cliente)'))).toBe(false);
  });
});

// Spec: cronograma, requirement "Data-limite pela tela" (GanttA-2).
describe('Data-limite pela tela', () => {
  it('Data-limite no detalhe', async () => {
    const changes: unknown[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', async ({ request }) => {
        const body = (await request.json()) as { deadline: string | null; duration: number };
        changes.push(body);
        // The API keeps the deadline and counts it: on 29/10 a business day before it; pushed to 03/11, past it.
        const tests = itemNamed('Testes integrados');
        Object.assign(
          tests,
          body.duration === 5
            ? { deadline: body.deadline, deadlineDeviation: -1 }
            : {
                duration: body.duration,
                end: '2026-11-03',
                deadline: body.deadline,
                deadlineDeviation: 1,
              },
        );
        schedule = { ...schedule, missedDeadlines: body.duration === 5 ? 0 : 1 };
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    await openSchedule();

    const drawer = await chooseItem('Testes integrados');
    const field = within(drawer).getByLabelText(text.form.deadline);
    expect(field).toHaveValue('');
    expect(field).toHaveAccessibleDescription(text.form.deadlineHelp);
    fireEvent.change(field, { target: { value: '30102026' } });
    expect(field).toHaveValue('30/10/2026');
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    const before = text.deadline.before_one.replace('{{count}}', '1');
    expect(await within(drawer).findByText(before)).toBeVisible();
    expect(changes).toEqual([expect.objectContaining({ deadline: '2026-10-30', duration: 5 })]);

    fireEvent.change(within(drawer).getByLabelText(text.form.duration), {
      target: { value: '7' },
    });
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    const past = text.deadline.past_one.replace('{{count}}', '1');
    const warning = await within(drawer).findByText(past);
    expect(warning).toHaveStyle({ color: 'var(--status-danger-text)' });
    expect(warning).toHaveAttribute('data-state', 'overdue');
    expect(warning.querySelector('svg')).not.toBeNull();
    // An empty deadline takes it out.
    fireEvent.change(within(drawer).getByLabelText(text.form.deadline), { target: { value: '' } });
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));
    await waitFor(() => {
      expect(changes).toHaveLength(3);
    });
    expect(changes[2]).toEqual(expect.objectContaining({ deadline: null }));
  });

  it('Data-limite inválida pela tela', async () => {
    let sent = false;
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', () => {
        sent = true;
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    await openSchedule();

    const drawer = await chooseItem('Testes integrados');
    fireEvent.change(within(drawer).getByLabelText(text.form.deadline), {
      target: { value: '31022026' },
    });
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    expect(await within(drawer).findByText(text.errors.deadlineInvalid)).toBeVisible();
    expect(sent).toBe(false);
  });

  it('Data-limite na Lista e no resumo', async () => {
    Object.assign(itemNamed('Testes integrados'), {
      deadline: '2026-10-28',
      deadlineDeviation: 1,
    });
    // Done before its deadline: only the day, without the alarm.
    Object.assign(itemNamed('Reunião de abertura'), {
      deadline: '2026-10-07',
      deadlineDeviation: -2,
    });
    schedule = { ...schedule, missedDeadlines: 1 };
    await openSchedule();

    const pastShort = text.deadline.pastShort_one.replace('{{count}}', '1');
    expect(cellsOf('Testes integrados')[3]).toBe(`28/10/2026${pastShort}`);
    expect(within(rowOf('Testes integrados')).getByText(pastShort)).toHaveStyle({
      color: 'var(--status-danger-text)',
    });
    expect(cellsOf('Reunião de abertura')[3]).toBe('07/10/2026');
    expect(cellsOf('Aceite da homologação')[3]).toBe('');
    const missed = screen.getByText(text.missedDeadlines_one.replace('{{count}}', '1'));
    expect(missed).toHaveAttribute('data-state', 'overdue');
  });

  it('Data-limite no detalhe de quem só vê', async () => {
    permissions = { canEditSchedule: false };
    Object.assign(itemNamed('Testes integrados'), {
      deadline: '2026-10-28',
      deadlineDeviation: 1,
    });
    await openSchedule();

    const drawer = await chooseItem('Testes integrados');

    expect(within(drawer).queryByLabelText(text.form.deadline)).not.toBeInTheDocument();
    expect(within(drawer).getByText(text.details.deadline).nextElementSibling).toHaveTextContent(
      '28/10/2026',
    );
    expect(
      within(drawer).getByText(text.deadline.past_one.replace('{{count}}', '1')),
    ).toHaveAttribute('data-state', 'overdue');
  });
});

// Spec: cronograma, requirement "Filtros do cronograma" (GanttA-3).
describe('Filtros pela tela', () => {
  const filters = text.filter;

  async function chooseFilter(name: string) {
    fireEvent.click(screen.getByRole('button', { name: filters.button }));
    fireEvent.click(await screen.findByRole('menuitemradio', { name }));
  }

  const shownText = (shown: number, count: number) =>
    filters.shown_other.replace('{{shown}}', String(shown)).replace('{{count}}', String(count));

  it('Filtrar as atrasadas pela tela', async () => {
    const { router } = await openSchedule();

    await chooseFilter(filters.kinds.late);

    await waitFor(() => {
      expect(lines()).toEqual(['fase: Levantamento', 'Validar cadastro de produtos']);
    });
    // The footer of the Lista and the summary say how many show, and the button says the filter.
    expect(screen.getAllByText(shownText(1, 8)).length).toBeGreaterThan(0);
    expect(
      screen.getByRole('button', { name: filters.active.replace('{{name}}', filters.kinds.late) }),
    ).toHaveAttribute('aria-pressed', 'true');
    expect(router.state.location.search).toBe('?view=list&filter=late');
  });

  it('Minhas tarefas pela tela', async () => {
    await openSchedule();

    // Marina is in the session.
    await chooseFilter(filters.kinds.mine);

    await waitFor(() => {
      expect(lines()).toEqual([
        'fase: Levantamento',
        'Reunião de abertura',
        'fase: Configuração',
        'Testes integrados',
      ]);
    });
  });

  it('Por responsável pela tela', async () => {
    schedule = demoScheduleWithSubtasks();
    await openSchedule();

    fireEvent.click(screen.getByRole('button', { name: filters.button }));
    const menu = await screen.findByRole('menu', { name: filters.label });
    expect(within(menu).getByText(filters.byOwner)).toBeVisible();
    fireEvent.click(within(menu).getByRole('menuitemradio', { name: demoPeople.carla.name }));

    // The subtask under its task, which keeps its place.
    await waitFor(() => {
      expect(lines()).toEqual(['fase: Configuração', 'Testes integrados', 'Roteiro de testes']);
    });
    expect(
      screen.getByRole('button', {
        name: filters.active.replace('{{name}}', demoPeople.carla.name),
      }),
    ).toBeVisible();
  });

  it('Filtro sem resultado pela tela', async () => {
    await openSchedule();

    await chooseFilter(filters.kinds.missedDeadlines);

    expect(await within(table()).findByText(filters.empty)).toBeVisible();
    fireEvent.click(screen.getByRole('button', { name: filters.clear }));
    await waitFor(() => {
      expect(lines()).toHaveLength(12);
    });
    expect(screen.getByRole('button', { name: filters.button })).toBeVisible();
  });

  it('Filtro no endereço', async () => {
    await renderApp(`/projects/${project.id}/schedule?view=list&filter=critical`);

    await waitFor(() => {
      expect(lines()).toEqual([
        'fase: Levantamento',
        'Reunião de abertura',
        'Mapear processos fiscais',
        'fase: Configuração',
        'Parametrizar regras de ICMS',
        'Testes integrados',
        'fase: Homologação',
        'Testes com a equipe do cliente',
        'Aceite da homologação',
      ]);
    });
  });
});

// Spec: cronograma, requirement "Tipo e intervalo das dependências pela tela" (ADR-0137).
describe('Tipo e intervalo pela tela', () => {
  const links = text.links;

  it('Tipo e intervalo no detalhe', async () => {
    const sent: { links?: unknown }[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', async ({ request }) => {
        const body = (await request.json()) as { links: ScheduleItem['links'] };
        sent.push(body);
        Object.assign(itemNamed('Testes integrados'), { links: body.links });
        schedule = { ...schedule };
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    await openSchedule();
    const icms = itemNamed('Parametrizar regras de ICMS');

    const drawer = await chooseItem('Testes integrados');
    const group = within(drawer).getByRole('group', { name: links.label });
    fireEvent.mouseDown(
      within(group).getByRole('combobox', {
        name: links.type.replace('{{name}}', 'Parametrizar regras de ICMS'),
      }),
    );
    fireEvent.click(await screen.findByRole('option', { name: links.types.StartToStart }));
    fireEvent.change(
      within(group).getByLabelText(links.lag.replace('{{name}}', 'Parametrizar regras de ICMS')),
      { target: { value: '1' } },
    );
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    await waitFor(() => {
      expect(sent).toHaveLength(1);
    });
    expect(sent[0]?.links).toEqual([{ predecessorId: icms.id, type: 'StartToStart', lag: 1 }]);
    await closeDrawer(drawer);
    await waitFor(() => {
      expect(cellsOf('Testes integrados')[7]).toBe(
        `Parametrizar regras de ICMS (${links.short.StartToStart}, +1)`,
      );
    });
  });

  it('Intervalo inválido pela tela', async () => {
    let sent = false;
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', () => {
        sent = true;
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    await openSchedule();

    const drawer = await chooseItem('Testes integrados');
    fireEvent.change(
      within(drawer).getByLabelText(links.lag.replace('{{name}}', 'Parametrizar regras de ICMS')),
      { target: { value: '1000' } },
    );
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    expect(await within(drawer).findByText(text.errors.linksInvalid)).toBeVisible();
    expect(sent).toBe(false);
  });
});

// Spec: cronograma, requirement "Folga pela tela" (GanttB-1).
describe('Folga pela tela', () => {
  it('Folga no detalhe', async () => {
    Object.assign(itemNamed('Integrar emissão de NF-e'), { float: 4, latestEnd: '2026-11-03' });
    Object.assign(itemNamed('Parametrizar regras de ICMS'), { float: 0, latestEnd: '2026-10-22' });
    await openSchedule();

    const drawer = await chooseItem('Integrar emissão de NF-e');
    expect(within(drawer).getByText(text.details.slack).nextElementSibling).toHaveTextContent(
      text.slack.days_other.replace('{{count}}', '4'),
    );
    await closeDrawer(drawer);

    const critical = await chooseItem('Parametrizar regras de ICMS');
    expect(within(critical).getByText(text.details.slack).nextElementSibling).toHaveTextContent(
      text.slack.none,
    );
  });

  it('Folga no detalhe de quem só vê', async () => {
    permissions = { canEditSchedule: false };
    Object.assign(itemNamed('Integrar emissão de NF-e'), { float: 1, latestEnd: '2026-10-29' });
    await openSchedule();

    const drawer = await chooseItem('Integrar emissão de NF-e');

    expect(within(drawer).getByText(text.details.slack).nextElementSibling).toHaveTextContent(
      text.slack.days_one.replace('{{count}}', '1'),
    );
  });
});

// Spec: horas-do-projeto, requirements "Horas na Lista do cronograma pela tela" and "Horas no detalhe da tarefa pela
// tela" (ADR-0138).
describe('Horas das tarefas pela tela', () => {
  const hoursText = ptBR.projectHours.tasks;
  /** The planned hours of each task as the simulated API keeps them, and the hours logged in each one. */
  let planned: Record<string, number | null>;
  const logged: Record<string, number> = {
    'Parametrizar regras de ICMS': 52,
    'Integrar emissão de NF-e': 6,
  };
  const asked: string[] = [];
  const sent: unknown[] = [];

  function answerTaskHours() {
    server.use(
      http.get('/api/admin/project-hours/:projectId/tasks', ({ params }) => {
        asked.push(String(params.projectId));
        return HttpResponse.json(
          taskHoursOf(
            Object.entries(logged).map(([name, hours]) =>
              taskHoursRow(itemNamed(name).id, name, planned[name] ?? null, hours),
            ),
          ),
        );
      }),
      http.put('/api/admin/project-hours/:projectId/tasks/:itemId', async ({ request, params }) => {
        const body = (await request.json()) as { hours: number | null };
        sent.push({ itemId: params.itemId, ...body });
        const name = Object.keys(logged).find((one) => itemNamed(one).id === params.itemId);
        if (name !== undefined) {
          planned[name] = body.hours;
        }
        return new HttpResponse(null, { status: 204 });
      }),
    );
  }

  beforeEach(() => {
    planned = { 'Parametrizar regras de ICMS': 40, 'Integrar emissão de NF-e': null };
    asked.length = 0;
    sent.length = 0;
    answerTaskHours();
  });

  it('Horas na Lista do administrador', async () => {
    signedInAs('Administrator');
    await openSchedule();

    expect(
      within(table()).getByRole('columnheader', { name: text.columns.hours }),
    ).toBeInTheDocument();
    const over = await within(rowOf('Parametrizar regras de ICMS')).findByText('52 de 40 h');
    expect(over).toHaveAttribute('data-state', 'Over');
    expect(over).toHaveStyle({ color: 'var(--status-danger-text)' });
    expect(over).toHaveTextContent('52 de 40 h, 12 h acima');
    // Without planned hours, only the logged ones, without a state.
    const only = within(rowOf('Integrar emissão de NF-e')).getByText('6 h');
    expect(only).not.toHaveAttribute('data-state');
    expect(only).toHaveAttribute('title', hoursText.noPlanned);
    // A task without hours has a dash, and a milestone nothing.
    expect(cellsOf('Testes integrados')).toContain(ptBR.projectHours.none);
    expect(cellsOf('Aceite da homologação')).not.toContain(ptBR.projectHours.none);
  });

  it('Alterar as previstas pelo detalhe da tarefa', async () => {
    signedInAs('Administrator');
    await openSchedule();
    await within(rowOf('Parametrizar regras de ICMS')).findByText('52 de 40 h');

    const drawer = await chooseItem('Parametrizar regras de ICMS');
    const section = within(drawer).getByRole('region', { name: hoursText.section });
    expect(within(section).getByText('40 h')).toBeVisible();
    expect(within(section).getByText('52 h')).toBeVisible();
    expect(within(section).getByText('12 h acima')).toBeVisible();
    fireEvent.click(
      within(section).getByRole('button', {
        name: hoursText.changeLabel.replace('{{name}}', 'Parametrizar regras de ICMS'),
      }),
    );
    const dialog = await screen.findByRole('dialog', {
      name: ptBR.projectHours.plannedDialog.title,
    });
    const field = within(dialog).getByLabelText(ptBR.projectHours.plannedDialog.hours);
    expect(field).toHaveValue('40');
    fireEvent.change(field, { target: { value: '48' } });
    fireEvent.click(
      within(dialog).getByRole('button', { name: ptBR.projectHours.plannedDialog.save }),
    );

    expect(
      await screen.findByText(
        ptBR.projectHours.messages.plannedSaved.replace('{{name}}', 'Parametrizar regras de ICMS'),
      ),
    ).toBeVisible();
    expect(sent).toEqual([{ itemId: itemNamed('Parametrizar regras de ICMS').id, hours: 48 }]);
    await waitFor(() => {
      expect(within(section).getByText('48 h')).toBeVisible();
    });
    expect(within(section).getByText('4 h acima')).toBeVisible();
    await closeDrawer(drawer);
    expect(within(rowOf('Parametrizar regras de ICMS')).getByText('52 de 48 h')).toBeVisible();
  });

  it('Cronograma sem horas para a gestão', async () => {
    await openSchedule();

    expect(
      within(table()).queryByRole('columnheader', { name: text.columns.hours }),
    ).not.toBeInTheDocument();
    const drawer = await chooseItem('Parametrizar regras de ICMS');
    expect(within(drawer).queryByRole('region', { name: hoursText.section })).toBeNull();
    expect(asked).toEqual([]);
  });
});

// Spec: cronograma, requirement "Notas pela tela" (ADR-0139).
describe('Notas pela tela', () => {
  const waiting = 'Aguardando o cliente mandar os arquivos do plano de contas';
  const markOf = (note: string) => text.noteMark.replace('{{note}}', note);

  it('Nota na Lista', async () => {
    Object.assign(itemNamed('Testes integrados'), { note: waiting });
    Object.assign(itemNamed('Aceite da homologação'), { note: 'Depende da homologação na Sefaz' });
    await openSchedule();

    const mark = within(rowOf('Testes integrados')).getByRole('img', { name: markOf(waiting) });
    expect(mark.parentElement).toHaveAttribute('title', waiting);
    expect(
      within(rowOf('Aceite da homologação')).getByRole('img', {
        name: markOf('Depende da homologação na Sefaz'),
      }),
    ).toBeInTheDocument();
    expect(
      within(rowOf('Integrar emissão de NF-e')).queryByRole('img', { name: /^Notas/ }),
    ).toBeNull();
  });

  it('Escrever a nota pela tela', async () => {
    const changes: { note?: string }[] = [];
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', async ({ request }) => {
        const body = (await request.json()) as { note?: string };
        changes.push(body);
        Object.assign(itemNamed('Testes integrados'), {
          note: body.note === '' ? null : body.note,
        });
        schedule = { ...schedule };
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    await openSchedule();

    const drawer = await chooseItem('Testes integrados');
    const field = within(drawer).getByLabelText(text.form.note);
    expect(field).toHaveValue('');
    const typed = `  ${waiting}\nCobrar na segunda.`;
    fireEvent.change(field, { target: { value: typed } });
    expect(field).toHaveAccessibleDescription(
      text.form.noteHelp_other.replace('{{count}}', String(2000 - typed.length)),
    );
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    expect(
      await screen.findByText(
        text.messages.itemSaved.Task.replace('{{name}}', 'Testes integrados'),
      ),
    ).toBeVisible();
    expect(changes).toEqual([expect.objectContaining({ note: `${waiting}\nCobrar na segunda.` })]);
    await closeDrawer(drawer);
    await waitFor(() => {
      expect(
        within(rowOf('Testes integrados')).getByRole('img', {
          name: markOf(`${waiting}\nCobrar na segunda.`),
        }),
      ).toBeInTheDocument();
    });
  });

  it('Nota longa demais pela tela', async () => {
    let sent = false;
    server.use(
      http.put('/api/projects/:id/schedule/items/:itemId', () => {
        sent = true;
        return HttpResponse.json({ pushed: 0 });
      }),
    );
    await openSchedule();

    const drawer = await chooseItem('Testes integrados');
    const field = within(drawer).getByLabelText(text.form.note);
    // The field stops at 2,000 characters; a pasted text past it is refused by the form.
    expect(field).toHaveAttribute('maxLength', '2000');
    fireEvent.change(field, { target: { value: 'n'.repeat(2001) } });
    fireEvent.click(within(drawer).getByRole('button', { name: text.form.save }));

    expect(await within(drawer).findByText(text.errors.noteTooLong)).toBeVisible();
    expect(sent).toBe(false);
  });

  it('Nota no detalhe de quem só vê', async () => {
    permissions = { canEditSchedule: false };
    Object.assign(itemNamed('Testes integrados'), { note: `${waiting}\nCobrar na segunda.` });
    await openSchedule();

    const drawer = await chooseItem('Testes integrados');

    const note = within(drawer).getByText(text.details.note).nextElementSibling;
    expect(note).toHaveTextContent(`${waiting} Cobrar na segunda.`);
    expect(note).toHaveStyle({ whiteSpace: 'pre-wrap' });
    expect(within(drawer).queryByLabelText(text.form.note)).toBeNull();
  });
});
