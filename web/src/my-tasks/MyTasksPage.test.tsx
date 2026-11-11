import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import {
  type DemoMyTaskItems,
  demoMyTaskItems,
  demoMyTasks,
  myTasksProjects,
} from '../test/demoMyTasks';
import { phoneWidth, renderApp } from '../test/renderApp';
import { problem, server, signedInAs } from '../test/server';

const text = ptBR.myTasks;
const board = ptBR.projects.board;

let items: DemoMyTaskItems;
/** What the screen asked for: with the completed ones or not. */
const asked: boolean[] = [];
/** The progress the screen saved, by project and task. */
const sent: { project: string; item: string; progress: number }[] = [];

beforeEach(() => {
  items = demoMyTaskItems();
  asked.length = 0;
  sent.length = 0;
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/my-tasks', ({ request }) => {
      const done = new URL(request.url).searchParams.get('done') === 'true';
      asked.push(done);
      return HttpResponse.json(demoMyTasks(items, done));
    }),
    // The API keeps the progress, and Minhas tarefas come back with it.
    http.put('/api/projects/:id/schedule/items/:itemId/progress', async ({ request, params }) => {
      const { progress } = (await request.json()) as { progress: number };
      sent.push({ project: String(params.id), item: String(params.itemId), progress });
      for (const [key, item] of Object.entries(items)) {
        if (item.id === params.itemId) {
          items = {
            ...items,
            [key]: {
              ...item,
              progress,
              completedOn: progress === 100 ? '2026-10-16' : null,
              late: false,
            },
          };
        }
      }
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

/** The rows of the list, below the header, as text. */
function rows(): string[] {
  const grid = screen.getByRole('grid', { name: text.table });
  return within(grid)
    .queryAllByRole('row')
    .slice(1)
    .map((row) => row.textContent);
}

function rowOf(name: string): HTMLElement {
  const grid = screen.getByRole('grid', { name: text.table });
  const row = within(grid)
    .queryAllByRole('row')
    .find((candidate) => candidate.textContent.includes(name));
  if (row === undefined) {
    throw new Error(`No row for ${name}.`);
  }
  return row;
}

async function openMyTasks(query = '', width?: number) {
  const view = await renderApp(`/my-tasks${query}`, width);
  await waitFor(() => {
    expect(rows().some((row) => row.includes('Parametrizar regras de ICMS'))).toBe(true);
  });
  return view;
}

const count = (key: 'tasks', value: number) =>
  (value === 1 ? text[`${key}_one`] : text[`${key}_other`]).replace('{{count}}', String(value));

// Spec: minhas-tarefas, requirements "Página Minhas tarefas", "Agrupar por data" and "Concluídas em Minhas tarefas"
// (ADR-0158). Today is Friday, 16/10/2026.
describe('Página Minhas tarefas', () => {
  it('Tarefas de dois projetos', async () => {
    await openMyTasks();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(ptBR.pages.myTasks.title);
    expect(
      screen.getByText(
        `${text.openTasks_other.replace('{{count}}', '5')} ${text.inProjects_other.replace('{{count}}', '2')}`,
      ),
    ).toBeVisible();
    const shown = rows();
    expect(shown).toHaveLength(7);
    expect(shown[0]).toContain(`ERP-FIN${myTasksProjects.erp.name}${count('tasks', 3)}`);
    expect(shown[1]).toContain('Parametrizar regras de ICMS');
    expect(shown[4]).toContain(`FISC-SP${myTasksProjects.fisc.name}${count('tasks', 2)}`);
    // Each task with its phase, the task it is in, the end and the delay.
    expect(rowOf('Roteiro de testes')).toHaveTextContent('Homologação · Testes integrados');
    expect(rowOf('Parametrizar regras de ICMS')).toHaveTextContent('14/10/2026');
    expect(rowOf('Parametrizar regras de ICMS')).toHaveTextContent(
      ptBR.projects.schedule.lateDays_other.replace('{{count}}', '2'),
    );
    // The project leads to its schedule.
    expect(
      screen.getByRole('link', { name: text.openScheduleOf.replace('{{code}}', 'FISC-SP') }),
    ).toHaveAttribute('href', `/projects/${myTasksProjects.fisc.id}/schedule?view=list`);
    expect(asked).toEqual([false]);
  });

  it('Sem tarefas', async () => {
    server.use(
      http.get('/api/my-tasks', () => HttpResponse.json({ today: '2026-10-16', projects: [] })),
    );
    await renderApp('/my-tasks');

    expect(await screen.findByText(text.empty)).toBeVisible();
    expect(screen.getByText(text.openTasks_zero)).toBeVisible();
  });

  it('Falha ao carregar', async () => {
    server.use(http.get('/api/my-tasks', () => problem(500)));
    await renderApp('/my-tasks');

    expect(await screen.findByText(text.loadFailed)).toBeVisible();
  });

  it('Agrupar por data', async () => {
    const view = await renderApp('/my-tasks');
    await waitFor(() => {
      expect(rows()).toHaveLength(7);
    });

    fireEvent.click(screen.getByRole('radio', { name: text.grouping.date }));

    await waitFor(() => {
      expect(rows()[0]).toContain(text.dateGroups.late);
    });
    const shown = rows();
    expect(shown).toHaveLength(10);
    const groups = [
      text.dateGroups.late,
      text.dateGroups.today,
      text.dateGroups.thisWeek,
      text.dateGroups.nextWeek,
      text.dateGroups.later,
    ];
    expect(shown.filter((_row, index) => index % 2 === 0)).toEqual(
      groups.map((group) => `${group}${count('tasks', 1)}`),
    );
    // Grouped by date, each task says its project by the code.
    expect(shown.filter((_row, index) => index % 2 === 1).map((row) => row.slice(0, 7))).toEqual([
      'ERP-FIN',
      'ERP-FIN',
      'FISC-SP',
      'ERP-FIN',
      'FISC-SP',
    ]);
    expect(shown[5]).toContain('Levantar notas de entrada');
    // The grouping stays in the address.
    expect(view.router.state.location.search).toBe('?group=date');
  });

  it('Mostrar concluídas', async () => {
    await openMyTasks('?group=date');
    expect(rows().some((row) => row.includes('Instalar o ERP'))).toBe(false);

    fireEvent.click(screen.getByRole('checkbox', { name: text.showDone }));

    await waitFor(() => {
      expect(rows().some((row) => row.includes('Instalar o ERP'))).toBe(true);
    });
    expect(rows().at(-2)).toContain(text.dateGroups.done);
    expect(asked).toEqual([false, true]);
    // The completed ones do not count as open.
    expect(
      screen.getByText(
        `${text.openTasks_other.replace('{{count}}', '5')} ${text.inProjects_other.replace('{{count}}', '2')}`,
      ),
    ).toBeVisible();
  });

  it('Minhas tarefas no celular', async () => {
    await openMyTasks('', phoneWidth);

    const grid = screen.getByRole('grid', { name: text.table });
    const headers = within(grid)
      .getAllByRole('columnheader')
      .map((header) => header.textContent);
    expect(headers).toEqual([text.columns.task, text.columns.progress]);
    expect(rowOf('Validar plano de contas')).toHaveTextContent(
      board.endsOn.replace('{{day}}', '16/10/2026'),
    );
  });
});

// Spec: minhas-tarefas, requirement "Quadro em Minhas tarefas" (ADR-0158).
describe('Quadro em Minhas tarefas', () => {
  const columnName = (id: 'todo' | 'doing' | 'done', value: number) =>
    (value === 0 ? board.column_zero : value === 1 ? board.column_one : board.column_other)
      .replace('{{name}}', board.columns[id])
      .replace('{{count}}', String(value));

  function cardOf(name: string): HTMLElement {
    const card = screen
      .getAllByRole('listitem')
      .find((candidate) => candidate.querySelector('button')?.textContent === name);
    if (card === undefined) {
      throw new Error(`No card for ${name}.`);
    }
    return card;
  }

  function drag(name: string, to: HTMLElement) {
    const data = new Map<string, string>();
    const dataTransfer = {
      effectAllowed: 'all',
      dropEffect: 'none',
      setData: (type: string, value: string) => data.set(type, value),
      getData: (type: string) => data.get(type) ?? '',
    };
    fireEvent.dragStart(cardOf(name), { dataTransfer });
    fireEvent.dragOver(to, { dataTransfer });
    fireEvent.drop(to, { dataTransfer });
    fireEvent.dragEnd(cardOf(name), { dataTransfer });
  }

  it('Cartões de vários projetos', async () => {
    await renderApp('/my-tasks?view=board');
    await screen.findByRole('region', { name: board.label });

    const todo = await screen.findByRole('region', { name: columnName('todo', 3) });
    expect(
      within(todo)
        .getAllByRole('listitem')
        .map((card) => card.querySelector('button')?.textContent),
    ).toEqual(['Validar plano de contas', 'Roteiro de testes', 'Relatório final']);
    expect(screen.getByRole('region', { name: columnName('doing', 2) })).toBeVisible();
    // Each card says its project by the code, in the place of the owner.
    expect(within(cardOf('Levantar notas de entrada')).getByText('FISC-SP')).toBeVisible();
    expect(within(cardOf('Roteiro de testes')).getByText('ERP-FIN')).toBeVisible();
    expect(within(cardOf('Roteiro de testes')).queryByText('Marina Costa')).not.toBeInTheDocument();
    // The grouping is only of the list.
    expect(screen.queryByRole('radio', { name: text.grouping.date })).not.toBeInTheDocument();
  });

  it('Mover o cartão de outro projeto', async () => {
    await renderApp('/my-tasks?view=board');
    const done = await screen.findByRole('region', { name: columnName('done', 0) });

    drag('Levantar notas de entrada', done);

    expect(
      await screen.findByText(
        ptBR.projects.schedule.messages.completed.replace('{{name}}', 'Levantar notas de entrada'),
      ),
    ).toBeInTheDocument();
    // The progress goes to the project of the task.
    expect(sent).toEqual([
      { project: myTasksProjects.fisc.id, item: items.entries.id, progress: 100 },
    ]);
  });
});

// Spec: minhas-tarefas, requirement "Detalhe em Minhas tarefas" (ADR-0158).
describe('Detalhe em Minhas tarefas', () => {
  it('Detalhe da tarefa', async () => {
    server.use(
      http.get('/api/projects/:id/schedule/items/:itemId/comments', () =>
        HttpResponse.json({
          items: [
            {
              id: '0199f5a2-7c1e-7d40-9a1b-70000000c001',
              author: { id: '0199f5a2-7c1e-7d40-9a1b-000000000006', name: 'Diego Torres' },
              body: JSON.stringify({
                type: 'doc',
                content: [
                  { type: 'paragraph', content: [{ type: 'text', text: 'Conferi a base.' }] },
                ],
              }),
              sentAt: '2026-10-15T14:00:00Z',
              canRemove: false,
            },
          ],
          canComment: false,
        }),
      ),
    );
    const view = await openMyTasks();

    fireEvent.click(within(rowOf('Validar plano de contas')).getByText('Validar plano de contas'));

    const drawer = await screen.findByRole('dialog', { name: text.details.label });
    expect(within(drawer).getByRole('heading', { name: 'Validar plano de contas' })).toBeVisible();
    expect(drawer).toHaveTextContent(myTasksProjects.erp.name);
    expect(drawer).toHaveTextContent('Configuração');
    expect(drawer).toHaveTextContent('Conferir com a contabilidade antes de fechar.');
    expect(
      within(drawer).getByRole('progressbar', {
        name: ptBR.projects.schedule.itemProgress.replace('{{name}}', 'Validar plano de contas'),
      }),
    ).toBeVisible();
    expect(within(drawer).getByRole('link', { name: text.details.openInSchedule })).toHaveAttribute(
      'href',
      `/projects/${myTasksProjects.erp.id}/schedule?view=list&item=${items.accounts.id}`,
    );
    expect(await within(drawer).findByText('Conferi a base.')).toBeVisible();
    expect(view.router.state.location.search).toBe(`?item=${items.accounts.id}`);
  });

  it('Concluir pelo detalhe', async () => {
    // The details open by the address, as the notices and the links do.
    await renderApp(`/my-tasks?item=${items.icms.id}`);
    const drawer = await screen.findByRole('dialog', { name: text.details.label });

    fireEvent.click(
      within(drawer).getByRole('button', {
        name: ptBR.projects.schedule.progressActions.complete,
      }),
    );

    expect(
      await screen.findByText(
        ptBR.projects.schedule.messages.completed.replace(
          '{{name}}',
          'Parametrizar regras de ICMS',
        ),
      ),
    ).toBeInTheDocument();
    expect(sent).toEqual([{ project: myTasksProjects.erp.id, item: items.icms.id, progress: 100 }]);
    // Completed, the task leaves the open ones.
    await waitFor(() => {
      expect(rows().some((row) => row.includes('Parametrizar regras de ICMS'))).toBe(false);
    });
  });
});
