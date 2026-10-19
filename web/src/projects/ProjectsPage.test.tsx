import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  clientChoicesOf,
  demoClients,
  type DemoProject,
  demoProjects,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects;

let projects: DemoProject[];

beforeEach(() => {
  projects = demoProjects();
  const current = projects;
  signedInAs('ProjectManager');
  server.use(
    http.get('/api/projects', ({ request }) =>
      HttpResponse.json(projectsPage(current, new URL(request.url))),
    ),
    http.get('/api/clients/choices', () => HttpResponse.json(clientChoicesOf(demoClients()))),
  );
});

function table() {
  return screen.getByRole('grid', { name: text.table });
}

function bodyRows() {
  return within(table()).getAllByRole('row').slice(1);
}

/** The names of the projects in the table, in the order shown. */
function names() {
  return bodyRows().map((row) => within(row).getAllByRole('link')[0]?.textContent ?? '');
}

function rowOf(name: string) {
  const row = bodyRows().find((candidate) => candidate.textContent.includes(name));
  if (row === undefined) {
    throw new Error(`No row for ${name}.`);
  }
  return row;
}

async function openProjects(width?: number) {
  const view = await renderApp('/projects', width);
  await waitFor(() => {
    expect(names()).toHaveLength(3);
  });
  return view;
}

// Spec: projetos, requirement "Lista de projetos".
describe('Lista de projetos', () => {
  it('Projetos na lista', async () => {
    await openProjects();

    expect(
      screen.getByRole('heading', { level: 1, name: ptBR.pages.projects.title }),
    ).toBeVisible();
    expect(names()).toEqual([
      'Implantação do ERP financeiro',
      'Portal de notas para fornecedores',
      'Revisão tributária da filial RS',
    ]);
    const cells = within(rowOf('Implantação do ERP financeiro'))
      .getAllByRole('gridcell')
      .map((cell) => cell.textContent);
    expect(cells).toEqual([
      'Implantação do ERP financeiroERP-FIN · Alfa Distribuidora',
      ptBR.projects.statuses.InProgress,
      '42% · Levantamento',
      `03/11/2026${text.schedule.lateTasks_one.replace('{{count}}', '1')}`,
      'MCMarina Costa',
    ]);
    expect(rowOf('Portal de notas')).toHaveTextContent(`PORTAL-NF · ${text.internal}`);
    expect(screen.getByText('3 projetos')).toBeInTheDocument();
  });

  it('Projetos encerrados pelo filtro', async () => {
    await openProjects();

    fireEvent.click(
      within(screen.getByRole('radiogroup', { name: text.situation })).getByRole('radio', {
        name: text.situations.Closed,
      }),
    );

    await waitFor(() => {
      expect(names()).toEqual(['Conciliação bancária automática', 'Implantação do WMS']);
    });
    expect(rowOf('Implantação do WMS')).toHaveTextContent(text.statuses.Completed);
    expect(rowOf('Conciliação')).toHaveTextContent(text.statuses.Cancelled);
  });

  it('Buscar e filtrar projetos', async () => {
    await openProjects();

    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: 'erp' },
    });
    await waitFor(() => {
      expect(names()).toEqual(['Implantação do ERP financeiro']);
    });

    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: '' },
    });
    fireEvent.mouseDown(screen.getByRole('combobox', { name: text.clientFilter }));
    fireEvent.click(await screen.findByRole('option', { name: 'Beta Alimentos' }));
    await waitFor(() => {
      expect(names()).toEqual(['Revisão tributária da filial RS']);
    });
  });

  it('Escolher um projeto na lista', async () => {
    const { router } = await openProjects();

    fireEvent.click(within(rowOf('Revisão tributária')).getByText('Começa em 26/10'));

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/projects/0199f5a2-7c1e-7d40-9a1b-400000000002');
    });
  });
});

/** The strip of phases of a row, by its accessible name. */
function stripOf(row: HTMLElement, start: string, end: string) {
  return within(row).getByRole('img', {
    name: text.schedule.strip.replace('{{start}}', start).replace('{{end}}', end),
  });
}

// Spec: projetos, requirement "Cronograma na lista de projetos".
describe('Cronograma na lista de projetos', () => {
  it('Cronograma na lista', async () => {
    const erp = projects[0];
    if (erp === undefined) {
      throw new Error('No ERP-FIN.');
    }
    erp.schedule = {
      start: '2026-10-05',
      end: '2026-11-13',
      progress: 62,
      lateTasks: 2,
      currentPhase: 'Configuração',
      completedOn: null,
      phases: [
        {
          name: 'Levantamento',
          start: '2026-10-05',
          end: '2026-10-15',
          progress: 100,
          late: false,
        },
        { name: 'Configuração', start: '2026-10-13', end: '2026-10-29', progress: 38, late: true },
        {
          name: 'Entrada em produção',
          start: '2026-11-04',
          end: '2026-11-13',
          progress: 0,
          late: false,
        },
      ],
      milestones: ['2026-11-03', '2026-11-13'],
    };
    await openProjects();

    const row = rowOf('Implantação do ERP financeiro');
    const strip = stripOf(row, '05/10/2026', '13/11/2026');
    expect(within(strip).getByTitle('Levantamento')).toHaveAttribute('data-late', 'false');
    expect(within(strip).getByTitle('Configuração')).toHaveAttribute('data-late', 'true');
    expect(within(strip).getByTitle('Entrada em produção')).toBeInTheDocument();
    expect(within(strip).getAllByTestId('strip-milestone')).toHaveLength(2);
    // Today, 16/10, is the 12th of the 40 days from 05/10 to 13/11: in the middle of it, at 28.75%.
    expect(within(strip).getByTestId('strip-today')).toHaveStyle({ left: '28.75%' });
    expect(row).toHaveTextContent('62% · Configuração');
    expect(row).toHaveTextContent('13/11/2026');
    expect(
      within(row).getByText(text.schedule.lateTasks_other.replace('{{count}}', '2')),
    ).toBeVisible();
  });

  it('Projeto que ainda não começou', async () => {
    await openProjects();

    const row = rowOf('Revisão tributária da filial RS');
    expect(stripOf(row, '26/10/2026', '29/01/2027')).toBeInTheDocument();
    expect(row).toHaveTextContent('Começa em 26/10');
    expect(row).toHaveTextContent('29/01/2027');
  });

  it('Projeto concluído na lista', async () => {
    await openProjects();

    fireEvent.click(
      within(screen.getByRole('radiogroup', { name: text.situation })).getByRole('radio', {
        name: text.situations.Closed,
      }),
    );

    await waitFor(() => {
      expect(rowOf('Implantação do WMS')).toHaveTextContent('100% · concluído em 28/08');
    });
  });

  it('Projeto sem cronograma na lista', async () => {
    await openProjects();

    const cells = within(rowOf('Portal de notas para fornecedores'))
      .getAllByRole('gridcell')
      .map((cell) => cell.textContent);
    expect(cells.slice(2, 4)).toEqual([text.noSchedule, text.noDate]);
  });
});

// Spec: projetos, requirement "Tela Novo projeto".
describe('Novo projeto na lista', () => {
  it('Gerente de projeto com Novo projeto', async () => {
    const { router } = await openProjects();

    fireEvent.click(screen.getByRole('button', { name: text.newProject }));

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/projects/new');
    });
  });

  it('Usuário sem Novo projeto', async () => {
    signedInAs('User');
    const { router } = await openProjects();

    expect(screen.queryByRole('button', { name: text.newProject })).not.toBeInTheDocument();
    await router.navigate('/projects/new');
    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/projects');
    });
  });

  it('Novo projeto no celular', async () => {
    await openProjects(phoneWidth);

    expect(screen.getByRole('button', { name: text.newProject })).toBeVisible();
  });
});

// Spec: projetos, requirement "Meus projetos".
describe('Meus projetos', () => {
  it('Meus projetos na barra lateral', async () => {
    await renderApp('/projects/0199f5a2-7c1e-7d40-9a1b-400000000001');
    server.use(
      http.get('/api/projects/:id', ({ params }) => {
        const project = projects.find((candidate) => candidate.id === params.id);
        return project === undefined
          ? new HttpResponse(null, { status: 404 })
          : HttpResponse.json(project);
      }),
    );

    const mine = await screen.findByRole('list', { name: ptBR.shell.myProjects });
    const links = await within(mine).findAllByRole('link');
    expect(links.map((link) => link.textContent)).toEqual([
      'Implantação do ERP financeiroERP-FIN',
      'Portal de notas para fornecedoresPORTAL-NF',
      'Revisão tributária da filial RSTRIB-RS',
    ]);
    expect(within(mine).getByRole('link', { name: /ERP-FIN/ })).toHaveAttribute(
      'aria-current',
      'page',
    );
  });
});
