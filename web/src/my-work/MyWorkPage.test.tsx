import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { MyWork } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import {
  demoMyWork,
  demoMyWorkItems,
  demoUpcomingMilestones,
  emptyMyWork,
} from '../test/demoMyWork';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  demoPeople,
  demoProjects,
  projectsPage,
  server,
  signedInAs,
  ticketChoiceOf,
} from '../test/server';

const text = ptBR.myWork;

let work: MyWork;
const progress: { itemId: string; progress: number }[] = [];
const assigned: { numbers: number[]; assigneeId: string }[] = [];

beforeEach(() => {
  work = demoMyWork();
  progress.length = 0;
  assigned.length = 0;
  signedInAs('ProjectManager');
  const projects = demoProjects().filter((project) => project.status !== 'Completed');
  server.use(
    http.get('/api/my-work', () => HttpResponse.json(work)),
    http.get('/api/projects/upcoming-milestones', () =>
      HttpResponse.json({ items: demoUpcomingMilestones(), today: '2026-10-16' }),
    ),
    http.get('/api/projects', ({ request }) =>
      HttpResponse.json(projectsPage(projects, new URL(request.url))),
    ),
    http.put('/api/projects/:id/schedule/items/:itemId/progress', async ({ params, request }) => {
      const body = (await request.json()) as { progress: number };
      progress.push({ itemId: String(params.itemId), progress: body.progress });
      return new HttpResponse(null, { status: 204 });
    }),
    http.get('/api/tickets/assignee-choices', () =>
      HttpResponse.json([demoPeople.diego, demoPeople.marina].map(ticketChoiceOf)),
    ),
    http.put('/api/tickets/assignee', async ({ request }) => {
      assigned.push((await request.json()) as { numbers: number[]; assigneeId: string });
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

function queue() {
  return screen.getByRole('region', { name: text.queue });
}

function group(name: string) {
  return within(queue()).getByRole('region', { name });
}

async function openMyWork(width?: number) {
  const view = await renderApp('/', width);
  await screen.findByRole('region', { name: text.queue });
  return view;
}

// Spec: meu-trabalho, requirement "Meu trabalho na tela inicial".
describe('Meu trabalho na tela inicial', () => {
  it('Fila na tela inicial', async () => {
    await openMyWork();

    expect(screen.getByRole('heading', { level: 1, name: ptBR.pages.home.title })).toBeVisible();
    expect(screen.getByText('Sexta-feira, 16 de outubro de 2026')).toBeInTheDocument();
    const views = screen.getByRole('radiogroup', { name: text.views.label });
    expect(within(views).getByRole('radio', { name: /^Para mim/ })).toBeChecked();
    expect(
      within(views)
        .getAllByRole('radio')
        .map((radio) => radio.textContent),
    ).toEqual(['Para mim9', 'Abertos por mim2', 'Que acompanho1']);
    expect(
      within(queue())
        .getAllByRole('heading', { level: 2 })
        .map((heading) => heading.textContent),
    ).toEqual(['Atrasado2', 'Aguardando você2', 'Hoje e segunda2', 'Próximos 7 dias3']);

    const late = group('Atrasado');
    expect(
      within(late).getByRole('link', { name: 'Validar cadastro de produtos' }),
    ).toHaveAttribute(
      'href',
      `/projects/${demoMyWorkItems.lateTask.project.id}/schedule?item=${demoMyWorkItems.lateTask.id}`,
    );
    expect(within(late).getByText('Tarefa · Levantamento · 80%')).toBeInTheDocument();
    expect(
      within(late).getByText(`#${String(98)} Ajustar layout do DANFE simplificado`),
    ).toBeInTheDocument();
    expect(within(late).getByText('Você é responsável · 6 interações')).toBeInTheDocument();
    expect(within(late).getAllByText('Venceu há 1 dia útil')).toHaveLength(2);
    expect(within(late).getByText(ptBR.tickets.statuses.InProgress)).toBeInTheDocument();
    expect(within(late).getAllByText('FISC-SP')).toHaveLength(1);

    const waiting = group('Aguardando você');
    expect(within(waiting).getByText(/^Diego Torres aguarda sua resposta/)).toBeInTheDocument();
    expect(within(waiting).getByText('Prazo pausado')).toBeInTheDocument();
    expect(
      within(waiting).getByText('Termo de aceite pedido por Beatriz Lima'),
    ).toBeInTheDocument();
    expect(within(waiting).getByText('Pedido em 15/10')).toBeInTheDocument();
    expect(
      within(waiting).getByRole('link', { name: 'Responder: Aceite do plano de testes' }),
    ).toHaveAttribute(
      'href',
      `/projects/${demoMyWorkItems.waitingAcceptance.project.id}/acceptances?term=${demoMyWorkItems.waitingAcceptance.id}`,
    );

    const soon = group('Hoje e segunda');
    expect(within(soon).getByText('Desenvolvimento · ninguém assumiu ainda')).toBeInTheDocument();
    expect(within(soon).getByText('1ª resposta até segunda')).toBeInTheDocument();
    expect(within(soon).getByText('Tarefa · Diagnóstico · não iniciada')).toBeInTheDocument();
    expect(within(soon).getByText('Vence hoje')).toBeInTheDocument();

    const week = group('Próximos 7 dias');
    expect(within(week).getByText('Vence quarta')).toBeInTheDocument();
    expect(within(week).getByText('Você pediu a Carla Mendes em 15/10')).toBeInTheDocument();
    expect(within(week).getByText('Marco em 22/10')).toBeInTheDocument();
    expect(
      within(week).getByRole('link', { name: 'Lembrar: Aceite da homologação' }),
    ).toBeInTheDocument();
    expect(
      within(week).getByRole('link', {
        name: `Abrir: #${String(109)} Dúvida sobre o fechamento do SPED`,
      }),
    ).toHaveAttribute('href', '/tickets/109');
  });

  it('Trocar a visão', async () => {
    await openMyWork();

    fireEvent.click(screen.getByRole('radio', { name: /^Abertos por mim/ }));

    expect(
      within(queue())
        .getAllByRole('heading', { level: 2 })
        .map((heading) => heading.textContent),
    ).toEqual(['Aguardando você1', 'Próximos 7 dias1']);
    expect(
      within(queue()).getByText(`#${String(107)} Liberar ambiente de homologação ao cliente`),
    ).toBeInTheDocument();
    expect(within(queue()).queryByText('Validar cadastro de produtos')).not.toBeInTheDocument();
  });

  it('Sem atalhos na tela inicial', async () => {
    await openMyWork();

    expect(screen.queryByRole('link', { name: ptBR.tickets.open })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: ptBR.tickets.open })).not.toBeInTheDocument();
    expect(screen.queryByText(ptBR.projects.newProject)).not.toBeInTheDocument();
  });

  it('Fila vazia', async () => {
    work = emptyMyWork;
    await openMyWork();

    expect(within(queue()).getByText(text.empty.ForMe)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('radio', { name: /^Que acompanho/ }));
    expect(within(queue()).getByText(text.empty.Following)).toBeInTheDocument();
  });
});

// Spec: meu-trabalho, requirement "Ações pela tela".
describe('Ações pela tela', () => {
  it('Concluir pela fila', async () => {
    await openMyWork();
    const item = demoMyWorkItems.lateTask;
    work = {
      ...work,
      views: work.views.map((view) => ({
        ...view,
        groups: view.groups.map((entry) => ({
          ...entry,
          items: entry.items.filter((candidate) => candidate.id !== item.id),
        })),
      })),
    };

    fireEvent.click(
      within(group('Atrasado')).getByRole('button', {
        name: 'Concluir: Validar cadastro de produtos',
      }),
    );

    expect(
      await screen.findByText('Tarefa Validar cadastro de produtos concluída.'),
    ).toBeInTheDocument();
    expect(progress).toEqual([{ itemId: item.id, progress: 100 }]);
    await waitFor(() => {
      expect(within(queue()).queryByText('Validar cadastro de produtos')).not.toBeInTheDocument();
    });
  });

  it('Atualizar pela fila', async () => {
    await openMyWork();

    fireEvent.click(
      within(group('Próximos 7 dias')).getByRole('button', {
        name: 'Atualizar: Configurar plano de contas',
      }),
    );
    const dialog = await screen.findByRole('dialog', {
      name: ptBR.projects.schedule.progressDialog.updateTitle,
    });
    const field = within(dialog).getByRole('textbox', {
      name: ptBR.projects.schedule.form.progress,
    });
    expect(field).toHaveValue('70');
    fireEvent.change(field, { target: { value: '80' } });
    fireEvent.click(within(dialog).getByRole('button', { name: ptBR.projects.schedule.form.save }));

    expect(
      await screen.findByText('Progresso de Configurar plano de contas atualizado para 80%.'),
    ).toBeInTheDocument();
    expect(progress).toEqual([{ itemId: demoMyWorkItems.weekTask.id, progress: 80 }]);
  });

  it('Iniciar pela fila', async () => {
    await openMyWork();

    fireEvent.click(
      within(group('Hoje e segunda')).getByRole('button', {
        name: 'Iniciar: Revisar cadastro de CFOP',
      }),
    );

    const dialog = await screen.findByRole('dialog', {
      name: ptBR.projects.schedule.progressDialog.startTitle,
    });
    expect(
      within(dialog).getByRole('textbox', { name: ptBR.projects.schedule.form.progress }),
    ).toHaveValue('');
  });

  it('Atribuir pela fila', async () => {
    await openMyWork();

    fireEvent.click(
      within(group('Hoje e segunda')).getByRole('button', {
        name: `Atribuir: #${String(110)} Relatório de ICMS-ST sem as notas de devolução`,
      }),
    );
    const dialog = await screen.findByRole('dialog', { name: ptBR.tickets.people.assignTitle_one });
    fireEvent.mouseDown(
      await within(dialog).findByRole('combobox', { name: ptBR.tickets.people.assignee }),
    );
    fireEvent.click(await screen.findByRole('option', { name: /^Diego Torres/ }));
    fireEvent.click(within(dialog).getByRole('button', { name: ptBR.tickets.people.save }));

    expect(await screen.findByText('Chamado atribuído a Diego Torres.')).toBeInTheDocument();
    expect(assigned).toEqual([{ numbers: [110], assigneeId: demoPeople.diego.id }]);
  });

  it('Título leva ao item', async () => {
    const { router } = await openMyWork();

    fireEvent.click(
      within(group('Atrasado')).getByRole('link', { name: 'Validar cadastro de produtos' }),
    );

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(
        `/projects/${demoMyWorkItems.lateTask.project.id}/schedule`,
      );
    });
    expect(router.state.location.search).toBe(`?item=${demoMyWorkItems.lateTask.id}`);
  });
});

// Spec: meu-trabalho, requirement "Meus projetos pela tela".
describe('Meus projetos pela tela', () => {
  function card() {
    return screen.getByRole('region', { name: /^Meus projetos/ });
  }

  it('Projeto atrasado em Meus projetos', async () => {
    await openMyWork();

    await within(card()).findByText('Implantação do ERP financeiro');
    const item = within(card())
      .getAllByRole('listitem')
      .find((candidate) => candidate.textContent.includes('Implantação do ERP financeiro'));
    if (item === undefined) {
      throw new Error('No ERP-FIN.');
    }
    expect(
      within(item).getByRole('img', { name: 'Fases do cronograma de 05/10/2026 a 03/11/2026' }),
    ).toBeInTheDocument();
    expect(within(item).getByText('42% concluído')).toBeInTheDocument();
    expect(within(item).getByText('2 dias úteis de atraso')).toBeInTheDocument();
    expect(within(card()).getByRole('link', { name: text.projects.all })).toHaveAttribute(
      'href',
      '/projects',
    );
  });

  it('Projeto sem cronograma em Meus projetos', async () => {
    await openMyWork();

    await within(card()).findByText('Portal de notas para fornecedores');
    const item = within(card())
      .getAllByRole('listitem')
      .find((candidate) => candidate.textContent.includes('Portal de notas para fornecedores'));
    if (item === undefined) {
      throw new Error('No PORTAL-NF.');
    }
    expect(within(item).getByText(text.projects.noSchedule)).toBeInTheDocument();
    expect(within(item).queryByRole('img')).not.toBeInTheDocument();
  });

  it('Sem projetos', async () => {
    server.use(
      http.get('/api/projects', () =>
        HttpResponse.json({ items: [], total: 0, today: '2026-10-16' }),
      ),
    );
    await openMyWork();

    expect(await within(card()).findByText(text.projects.empty)).toBeInTheDocument();
  });
});

// Spec: meu-trabalho, requirement "Próximos marcos pela tela".
describe('Próximos marcos pela tela', () => {
  function card() {
    return screen.getByRole('region', { name: text.milestones.title });
  }

  it('Próximos marcos na tela', async () => {
    await openMyWork();

    const item = (await within(card()).findAllByRole('listitem'))[1];
    if (item === undefined) {
      throw new Error('No Homologação.');
    }
    expect(within(item).getByRole('img', { name: '03/11/2026' })).toHaveTextContent('03nov');
    expect(within(item).getByText('Homologação')).toBeInTheDocument();
    expect(within(item).getByText('Em 11 dias úteis · aceite pedido')).toBeInTheDocument();
    expect(within(item).getByText('ERP-FIN')).toBeInTheDocument();
  });

  it('Sem próximos marcos', async () => {
    server.use(
      http.get('/api/projects/upcoming-milestones', () =>
        HttpResponse.json({ items: [], today: '2026-10-16' }),
      ),
    );
    await openMyWork();

    expect(await within(card()).findByText(text.milestones.empty)).toBeInTheDocument();
  });
});

// Spec: meu-trabalho, requirement "Meu trabalho no celular".
describe('Meu trabalho no celular', () => {
  it('Meu trabalho no celular', async () => {
    await openMyWork(phoneWidth);

    expect(screen.getByRole('combobox', { name: text.views.label })).toHaveTextContent(
      'Para mim (9)',
    );
    const late = group('Atrasado');
    expect(
      within(late).getByRole('link', { name: 'Validar cadastro de produtos' }),
    ).toBeInTheDocument();
    expect(within(late).getByText(text.kinds.Task)).toBeInTheDocument();
    expect(within(late).getByText(text.kinds.Ticket)).toBeInTheDocument();
    expect(within(late).queryByRole('button')).not.toBeInTheDocument();
    expect(screen.getByRole('region', { name: /^Meus projetos/ })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: text.milestones.title })).toBeInTheDocument();
  });
});
