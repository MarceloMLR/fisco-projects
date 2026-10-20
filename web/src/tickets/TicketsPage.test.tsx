import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import {
  type DemoProject,
  demoProjects,
  demoTicket,
  demoTicketChoices,
  demoTickets,
  projectsPage,
  server,
  signedInAs,
  ticketsPage,
} from '../test/server';

const text = ptBR.tickets;
let erp: DemoProject;
const asked: URL[] = [];
const opened: unknown[] = [];

beforeEach(() => {
  const projects = demoProjects();
  const first = projects[0];
  if (first === undefined) {
    throw new Error('No ERP-FIN.');
  }
  erp = first;
  asked.length = 0;
  opened.length = 0;
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects', ({ request }) =>
      HttpResponse.json(projectsPage(projects, new URL(request.url))),
    ),
    http.get('/api/projects/:id', ({ params }) =>
      params.id === erp.id ? HttpResponse.json(erp) : new HttpResponse(null, { status: 404 }),
    ),
    http.get('/api/tickets', ({ request }) => {
      const url = new URL(request.url);
      asked.push(url);
      return HttpResponse.json(ticketsPage(demoTickets(), url));
    }),
  );
});

/** The rows of the table, by the number of each ticket. */
async function numbersInTable() {
  const table = await screen.findByRole('grid', { name: text.table });
  await waitFor(() => {
    expect(within(table).getAllByRole('row').length).toBeGreaterThan(1);
  });
  return within(table)
    .getAllByText(/^#\d+$/)
    .map((cell) => cell.textContent);
}

// Spec: chamados, requirement "Chamados pela tela".
describe('Chamados pela tela', () => {
  it('Lista de chamados pela tela', async () => {
    await renderApp('/tickets');

    const views = await screen.findByRole('navigation', { name: text.views.label });
    const unassigned = within(views).getByRole('button', {
      name: new RegExp(text.views.UnassignedInMyTeams),
    });
    // The view stands out while it has tickets, with its count.
    await waitFor(() => {
      expect(within(unassigned).getByText('4')).toHaveAttribute('data-alert', 'true');
    });
    expect(await numbersInTable()).toEqual(['#3', '#1', '#4', '#2']);
    const table = screen.getByRole('grid', { name: text.table });
    // The code of the project, the team and who opened it, under the title; no assignee yet (ADR-0068).
    expect(within(table).getByText('Rejeição 539 ao emitir nota de devolução')).toBeVisible();
    expect(within(table).getAllByText(text.noAssignee)).toHaveLength(4);
    expect(within(table).getAllByText(text.priorities.Critical)).toHaveLength(1);
    expect(within(table).getAllByText(text.statuses.Open)).toHaveLength(4);

    fireEvent.click(unassigned);

    await waitFor(() => {
      expect(asked.at(-1)?.searchParams.get('view')).toBe('UnassignedInMyTeams');
    });
    expect(unassigned).toHaveAttribute('aria-current', 'page');
    expect(asked.at(-1)?.searchParams.get('situation')).toBe('Open');
    // "Abrir chamado" for who opens tickets, never at the home page (ADR-0068).
    expect(screen.getByRole('button', { name: text.open })).toBeVisible();
  });

  it('Abrir chamado pela aba do projeto', async () => {
    signedInAs('User');
    server.use(
      http.post('/api/tickets', async ({ request }) => {
        opened.push(await request.json());
        return HttpResponse.json(
          { id: '0199f5a2-7c1e-7d40-9a1b-700000000105', number: 5 },
          { status: 201 },
        );
      }),
      http.get('/api/tickets/5', () => {
        const [row] = demoTickets();
        if (row === undefined) {
          throw new Error('No ticket.');
        }
        return HttpResponse.json(
          demoTicket({ ...row, number: 5, title: 'Erro ao importar XML de entrada' }),
        );
      }),
    );
    await renderApp(`/projects/${erp.id}/tickets`);
    // The tab lists the tickets of the project only, with the day each was opened.
    await waitFor(() => {
      expect(asked.at(-1)?.searchParams.get('projectId')).toBe(erp.id);
    });
    expect(await numbersInTable()).toEqual(['#1', '#4', '#2']);

    fireEvent.click(await screen.findByRole('button', { name: text.open }));

    // The project comes filled in, and there is no field of the assignee.
    const project = await screen.findByRole('combobox', { name: text.form.project });
    expect(project).toHaveTextContent('ERP-FIN · Implantação do ERP financeiro');
    expect(screen.queryByText(text.columns.assignee)).not.toBeInTheDocument();
    const teams = screen.getByRole('radiogroup', { name: text.form.team });
    expect(within(teams).getAllByRole('radio')).toHaveLength(2);
    // Without the team, the title and the description, it is not sent.
    fireEvent.click(screen.getByRole('button', { name: text.form.submit }));
    expect(await screen.findByText(text.errors.teamRequired)).toBeVisible();
    expect(screen.getByText(text.errors.titleRequired)).toBeVisible();
    expect(screen.getByText(text.errors.descriptionRequired)).toBeVisible();
    expect(opened).toEqual([]);

    fireEvent.click(within(teams).getByRole('radio', { name: text.teams.Development }));
    fireEvent.click(screen.getByRole('radio', { name: text.priorities.High }));
    fireEvent.change(screen.getByLabelText(text.form.title), {
      target: { value: '  Erro ao importar XML de entrada ' },
    });
    fireEvent.change(screen.getByLabelText(text.form.description), {
      target: { value: 'O XML da nota 3381 não entra.' },
    });
    fireEvent.click(screen.getByRole('button', { name: text.form.submit }));

    expect(await screen.findByText(text.opened.replace('{{number}}', '5'))).toBeInTheDocument();
    expect(
      await screen.findByRole('heading', { name: 'Erro ao importar XML de entrada' }),
    ).toBeVisible();
    expect(opened).toEqual([
      {
        projectId: erp.id,
        team: 'Development',
        priority: 'High',
        title: 'Erro ao importar XML de entrada',
        description: 'O XML da nota 3381 não entra.',
      },
    ]);
  });

  it('Leitor sem abrir chamado pela tela', async () => {
    signedInAs('User');
    server.use(
      http.get('/api/tickets/choices', () =>
        HttpResponse.json(
          demoTicketChoices().map((choice) => ({ ...choice, canOpen: false, teams: [] })),
        ),
      ),
    );
    await renderApp(`/projects/${erp.id}/tickets`);

    expect(await numbersInTable()).toEqual(['#1', '#4', '#2']);
    expect(screen.queryByRole('button', { name: text.open })).not.toBeInTheDocument();
  });

  it('Abrir chamado pela página Chamados', async () => {
    await renderApp('/tickets/new');

    const project = await screen.findByRole('combobox', { name: text.form.project });
    // Only the projects where the person opens tickets; a closed project is left out (ADR-0022).
    fireEvent.mouseDown(project);
    const options = await screen.findAllByRole('option');
    expect(options.map((option) => option.textContent)).toEqual([
      text.form.projectPlaceholder,
      'ERP-FIN · Implantação do ERP financeiro',
      'TRIB-RS · Revisão tributária da filial RS',
    ]);
    const trib = options[2];
    if (trib === undefined) {
      throw new Error('No TRIB-RS.');
    }
    fireEvent.click(trib);

    // TRIB-RS has only Funcional, already chosen.
    const teams = await screen.findByRole('radiogroup', { name: text.form.team });
    await waitFor(() => {
      expect(within(teams).getByRole('radio', { name: text.teams.Functional })).toBeChecked();
    });
    expect(within(teams).getAllByRole('radio')).toHaveLength(1);
    expect(screen.getByRole('radio', { name: text.priorities.Medium })).toBeChecked();
  });
});

// Spec: chamados, requirement "Página do chamado".
describe('Página do chamado', () => {
  it('Página do chamado pela tela', async () => {
    await renderApp('/tickets/1');

    expect(
      await screen.findByRole('heading', { name: 'Rejeição 539 ao emitir nota de devolução' }),
    ).toBeVisible();
    expect(screen.getByText('#1')).toBeVisible();
    expect(screen.getAllByText('ERP-FIN').length).toBeGreaterThan(0);
    expect(screen.getByText(/A SEFAZ retorna a rejeição 539/)).toBeVisible();
    const people = screen.getByRole('complementary', { name: text.details.people });
    expect(within(people).getByText(text.noAssignee)).toBeVisible();
    expect(within(people).getByText('Marina Costa')).toBeVisible();
    expect(within(people).getByText('Rafael Souza')).toBeVisible();
  });

  it('Chamado que não existe', async () => {
    await renderApp('/tickets/999');

    expect(await screen.findByText(text.details.notFound)).toBeVisible();
  });
});
