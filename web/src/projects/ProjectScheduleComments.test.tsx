import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import {
  type Project,
  projectProblems,
  type Schedule,
  type ScheduleComment,
  type ScheduleItem,
} from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { typeInEditor } from '../test/editor';
import { renderApp } from '../test/renderApp';
import {
  demoPeople,
  demoProjects,
  demoSchedule,
  type DemoProject,
  emailOf,
  problem,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects.schedule;
const comments = text.comments;

let project: DemoProject;
let schedule: Schedule;
let permissions: Pick<Project['permissions'], 'canEditSchedule'>;
/** The comments of the task, as the API keeps them, and whether the person in the session comments. */
let kept: ScheduleComment[];
let canComment: boolean;
/** The bodies the screen sent, and the answer to the next one: saved, or refused with a problem. */
const sent: string[] = [];
const removed: string[] = [];
let refusal: Response | undefined;

function itemNamed(name: string): ScheduleItem {
  const item = schedule.phases.flatMap((phase) => phase.items).find((one) => one.name === name);
  if (item === undefined) {
    throw new Error(`No item ${name}.`);
  }
  return item;
}

const comment = (
  id: string,
  author: { id: string; name: string },
  words: string,
  more: Partial<ScheduleComment> = {},
): ScheduleComment => ({
  id: `0199f5a2-7c1e-7d40-9a1b-a000000000${id}`,
  author,
  body: JSON.stringify({
    type: 'doc',
    content: [{ type: 'paragraph', content: [{ type: 'text', text: words }] }],
  }),
  sentAt: '2026-10-16T12:58:00Z',
  canRemove: false,
  ...more,
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
  kept = [];
  canComment = true;
  sent.length = 0;
  removed.length = 0;
  refusal = undefined;
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
    // The people of the project, who may be mentioned.
    http.get('/api/projects/:id/members', () =>
      HttpResponse.json(
        [demoPeople.marina, demoPeople.carla, demoPeople.diego].map((person) => ({
          ...person,
          email: emailOf(person.name),
          role: 'Contributor',
        })),
      ),
    ),
    http.get('/api/projects/:id/schedule/items/:itemId/comments', () =>
      HttpResponse.json({ items: kept, canComment }),
    ),
    http.post('/api/projects/:id/schedule/items/:itemId/comments', async ({ request }) => {
      const { body } = (await request.json()) as { body: string };
      sent.push(body);
      if (refusal !== undefined) {
        return refusal;
      }
      kept = [
        ...kept,
        {
          ...comment('99', demoPeople.marina, ''),
          body,
          sentAt: '2026-10-16T13:10:00Z',
          canRemove: true,
        },
      ];
      return HttpResponse.json({ id: '0199f5a2-7c1e-7d40-9a1b-a00000000099' }, { status: 201 });
    }),
    http.delete('/api/projects/:id/schedule/items/:itemId/comments/:commentId', ({ params }) => {
      removed.push(String(params.commentId));
      kept = kept.filter((one) => one.id !== params.commentId);
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

/** Opens the details of the task by the address, as the Lista does. */
async function openTask(name = 'Parametrizar regras de ICMS') {
  await renderApp(`/projects/${project.id}/schedule?view=list&item=${itemNamed(name).id}`);
  const drawer = await screen.findByRole('dialog', { name: text.details.label });
  return within(drawer).findByRole('region', { name: new RegExp(`^${comments.title}`) });
}

// Spec: cronograma, requirement "Comentários do item" (ADR-0157).
describe('Comentários do item', () => {
  it('Ver os comentários', async () => {
    permissions = { canEditSchedule: false };
    canComment = false;
    kept = [
      comment('01', demoPeople.carla, 'Mandei o plano de contas.'),
      comment('02', demoPeople.diego, 'Recebi, vou importar hoje.', {
        sentAt: '2026-10-16T13:20:00Z',
      }),
    ];

    const section = await openTask();

    const list = await within(section).findByRole('list', {
      name: comments.list.replace('{{name}}', 'Parametrizar regras de ICMS'),
    });
    const lines = within(list).getAllByRole('listitem');
    expect(lines.map((line) => line.getAttribute('aria-label'))).toEqual([
      'Carla Mendes',
      'Diego Torres',
    ]);
    const first = lines.at(0) ?? list;
    expect(within(first).getByText('Mandei o plano de contas.')).toBeVisible();
    // On the clock of the installation (São Paulo).
    expect(within(first).getByText('16/10/2026 09:58')).toBeVisible();
    expect(within(section).getByRole('heading', { name: `${comments.title} (2)` })).toBeVisible();
    // Who only reads: no editor and no "Apagar".
    expect(within(section).queryByRole('textbox')).not.toBeInTheDocument();
    expect(within(section).queryByRole('button', { name: /Apagar/ })).not.toBeInTheDocument();
  });

  it('Sem comentários', async () => {
    const section = await openTask();

    expect(within(section).getByText(comments.empty)).toBeVisible();
    expect(await within(section).findByRole('textbox', { name: comments.label })).toBeVisible();
  });
});

// Spec: cronograma, requirement "Comentar um item" (ADR-0157).
describe('Comentar um item', () => {
  it('Comentar com menção', async () => {
    const section = await openTask();

    await typeInEditor(comments.label, 'pode importar o plano? ');
    fireEvent.click(within(section).getByRole('button', { name: ptBR.editor.mention }));
    const people = await screen.findByRole('menu', { name: ptBR.editor.mention });
    // The people of the project, without the person who writes.
    expect(
      within(people)
        .getAllByRole('menuitem')
        .map((item) => item.textContent),
    ).toEqual([
      'CMCarla Mendescarla.mendes@empresa.example',
      'DTDiego Torresdiego.torres@empresa.example',
    ]);
    fireEvent.click(within(people).getByRole('menuitem', { name: /Diego Torres/ }));
    fireEvent.click(within(section).getByRole('button', { name: comments.send }));

    await waitFor(() => {
      expect(sent).toHaveLength(1);
    });
    expect(sent[0]).toContain(demoPeople.diego.id);
    expect(sent[0]).toContain('pode importar o plano?');
    expect(await within(section).findByText('@Diego Torres')).toBeVisible();
    expect(within(section).getByRole('heading', { name: `${comments.title} (1)` })).toBeVisible();
  });

  it('Comentário vazio', async () => {
    const section = await openTask();
    await within(section).findByRole('textbox', { name: comments.label });

    fireEvent.click(within(section).getByRole('button', { name: comments.send }));

    expect(await within(section).findByText(comments.emptyText)).toBeVisible();
    expect(sent).toHaveLength(0);
  });

  it('Menção fora do projeto', async () => {
    refusal = problem(400, projectProblems.commentMentionRefused);
    const section = await openTask();

    await typeInEditor(comments.label, 'veja');
    fireEvent.click(within(section).getByRole('button', { name: comments.send }));

    await waitFor(() => {
      expect(sent).toHaveLength(1);
    });
    expect(await within(section).findByText(comments.mentionRefused)).toBeVisible();
    expect(within(section).getByText(comments.empty)).toBeVisible();
  });

  it('Leitor não comenta', async () => {
    permissions = { canEditSchedule: false };
    canComment = false;

    const section = await openTask();

    expect(within(section).getByText(comments.empty)).toBeVisible();
    expect(within(section).queryByRole('textbox')).not.toBeInTheDocument();
    expect(within(section).queryByRole('button', { name: comments.send })).not.toBeInTheDocument();
  });

  it('Projeto encerrado sem comentários novos', async () => {
    canComment = false;
    kept = [comment('01', demoPeople.marina, 'Encerrado com o cliente.')];

    const section = await openTask();

    expect(await within(section).findByText('Encerrado com o cliente.')).toBeVisible();
    expect(within(section).queryByRole('textbox')).not.toBeInTheDocument();
    expect(within(section).queryByRole('button', { name: /Apagar/ })).not.toBeInTheDocument();
  });
});

// Spec: cronograma, requirement "Apagar um comentário" (ADR-0157).
describe('Apagar um comentário', () => {
  it('Apagar o próprio comentário', async () => {
    kept = [comment('01', demoPeople.marina, 'Confirmar com o fiscal.', { canRemove: true })];
    const section = await openTask();

    fireEvent.click(
      await within(section).findByRole('button', {
        name: comments.remove.replace('{{name}}', 'Marina Costa'),
      }),
    );
    const confirm = await screen.findByRole('alertdialog', { name: comments.removeTitle });
    expect(confirm).toHaveTextContent(comments.removeText.replace('{{name}}', 'Marina Costa'));
    fireEvent.click(within(confirm).getByRole('button', { name: comments.removeConfirm }));

    expect(await screen.findByText(comments.removed)).toBeVisible();
    expect(removed).toEqual(['0199f5a2-7c1e-7d40-9a1b-a00000000001']);
    await waitFor(() => {
      expect(within(section).queryByText('Confirmar com o fiscal.')).not.toBeInTheDocument();
    });
  });

  it('Apagar o comentário de outra pessoa', async () => {
    kept = [
      comment('01', demoPeople.diego, 'Recebi, vou importar hoje.'),
      comment('02', demoPeople.marina, 'Obrigada.', { canRemove: true }),
    ];

    const section = await openTask();

    await within(section).findByText('Recebi, vou importar hoje.');
    expect(
      within(section).queryByRole('button', {
        name: comments.remove.replace('{{name}}', 'Diego Torres'),
      }),
    ).not.toBeInTheDocument();
    expect(
      within(section).getByRole('button', {
        name: comments.remove.replace('{{name}}', 'Marina Costa'),
      }),
    ).toBeVisible();
  });
});

// Spec: cronograma, requirement "Quantidade de comentários" (ADR-0157).
describe('Quantidade de comentários', () => {
  it('Contagem na Lista e no Quadro', async () => {
    const task = itemNamed('Parametrizar regras de ICMS');
    task.commentCount = 3;
    const count = comments.count_other.replace('{{count}}', '3');

    const { router } = await renderApp(`/projects/${project.id}/schedule?view=list`);

    const names = await screen.findAllByText('Parametrizar regras de ICMS', { selector: 'span' });
    const row = names
      .map((name) => name.closest<HTMLElement>('[role="row"]'))
      .find((one) => one !== null);
    expect(within(row ?? document.body).getByRole('img', { name: count })).toBeVisible();
    // Only the items with comments.
    expect(screen.getAllByRole('img', { name: /comentário/ })).toHaveLength(1);

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
});
