import { screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { ProjectOverview } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import {
  demoOverview,
  demoOverviewDeadlineId,
  demoOverviewTaskId,
  demoOverviewTermId,
  emptyOverview,
} from '../test/demoOverview';
import { phoneWidth, renderApp } from '../test/renderApp';
import { demoProjects, server, signedInAs } from '../test/server';

const text = ptBR.projects.overview;
const erpFin = '0199f5a2-7c1e-7d40-9a1b-400000000001';
const ticket = (number: number) => `#${String(number)}`;

let overview: ProjectOverview;

beforeEach(() => {
  overview = demoOverview();
  signedInAs('ProjectManager');
  const projects = demoProjects();
  server.use(
    http.get('/api/projects/:id', ({ params }) => {
      const project = projects.find((item) => item.id === params.id);
      return project === undefined
        ? new HttpResponse(null, { status: 404 })
        : HttpResponse.json(project);
    }),
    http.get('/api/projects/:id/overview', ({ params }) =>
      params.id === erpFin ? HttpResponse.json(overview) : HttpResponse.json(emptyOverview),
    ),
  );
});

async function openOverview(width?: number) {
  const view = await renderApp(`/projects/${erpFin}`, width);
  await screen.findByRole('region', { name: text.summary });
  return view;
}

const summary = () => screen.getByRole('region', { name: text.summary });
const timeline = () => screen.getByRole('region', { name: text.timeline.title });
const team = () => screen.getByRole('region', { name: text.team });
const attention = () =>
  screen.getByRole('region', { name: new RegExp(`^${text.attention.title}`) });

// Spec: visao-geral-do-projeto, requirement "Visão geral pela tela" (ADR-0071, ADR-0073, canvas ProjetoVisaoGeral).
describe('Visão geral pela tela', () => {
  it('Visão geral na tela', async () => {
    await openOverview();

    const numbers = summary();
    expect(numbers).toHaveTextContent('Andamento62%previsto 70%');
    expect(
      within(numbers).getByRole('img', { name: '62% concluído; previsto para hoje 70%' }),
    ).toBeVisible();
    expect(numbers).toHaveTextContent('Prazo final13/112026+2 dias úteislinha de base 11/11');
    expect(numbers).toHaveTextContent('Chamados abertos7de 91 vencido2 aguardando solicitante');
    expect(numbers).toHaveTextContent('Próximo marcoAceite da homologação03/11 · em 11 dias úteis');

    const phases = timeline();
    expect(
      within(phases).getByRole('img', {
        name: 'Fases e marcos do projeto, de 05/10/2026 a 13/11/2026',
      }),
    ).toBeVisible();
    expect(
      within(phases)
        .getAllByRole('listitem')
        .map((row) => row.textContent),
    ).toEqual([
      `Levantamento${ptBR.projects.schedule.late}96%`,
      'Configuração38%',
      'Homologação0%',
      `Aceite da homologação${ptBR.projects.timeline.milestone.pending}`,
      'Entrada em produção0%',
      `Virada para produção${ptBR.projects.timeline.milestone.pending}`,
    ]);
    expect(within(phases).getByRole('link', { name: text.timeline.open })).toHaveAttribute(
      'href',
      `/projects/${erpFin}/schedule`,
    );

    expect(within(team()).getByRole('link', { name: text.members })).toHaveAttribute(
      'href',
      `/projects/${erpFin}/members`,
    );

    const list = attention();
    expect(within(list).getByRole('heading', { level: 2 })).toHaveTextContent(
      `${text.attention.title}7`,
    );
    expect(within(list).getAllByRole('listitem')).toHaveLength(7);
    // The item past its deadline, after the late tasks (GanttA-2).
    expect(within(list).getByRole('link', { name: 'Testes integrados' })).toHaveAttribute(
      'href',
      `/projects/${erpFin}/schedule?item=${demoOverviewDeadlineId}`,
    );
    expect(
      within(list).getByRole('link', { name: 'Validar cadastro de produtos' }),
    ).toHaveAttribute('href', `/projects/${erpFin}/schedule?item=${demoOverviewTaskId}`);
    expect(
      within(list).getByRole('link', {
        name: `${ticket(104)} Rejeição 539 ao emitir nota de devolução`,
      }),
    ).toHaveAttribute('href', '/tickets/104');
    expect(within(list).getByRole('link', { name: 'Aceite da homologação' })).toHaveAttribute(
      'href',
      `/projects/${erpFin}/acceptances?term=${demoOverviewTermId}`,
    );
    expect(within(list).getByRole('link', { name: 'Emissor Exemplo · FX-20931' })).toHaveAttribute(
      'href',
      '/tickets/104',
    );
  });

  it('Projeto sem cronograma na visão geral', async () => {
    overview = emptyOverview;
    await openOverview();

    const numbers = summary();
    expect(within(numbers).getAllByText(text.noSchedule)).toHaveLength(3);
    expect(numbers).toHaveTextContent('Chamados abertos0de 0Nenhum vencido');
    expect(timeline()).toHaveTextContent(text.timeline.empty);
    expect(within(timeline()).queryByRole('img')).not.toBeInTheDocument();
  });

  it('Nada precisa de atenção', async () => {
    overview = { ...demoOverview(), attention: emptyOverview.attention };
    await openOverview();

    expect(attention()).toHaveTextContent(text.attention.empty);
    expect(within(attention()).getByRole('heading', { level: 2 })).toHaveTextContent(
      `${text.attention.title}0`,
    );
    expect(within(attention()).queryAllByRole('listitem')).toHaveLength(0);
  });

  it('Visão geral no celular', async () => {
    await openOverview(phoneWidth);

    const order = [summary(), attention(), timeline(), team()];
    for (const [index, region] of order.slice(1).entries()) {
      const before = order[index];
      if (before === undefined) {
        throw new Error('No region before.');
      }
      expect(
        before.compareDocumentPosition(region) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();
    }
    // A narrower list: the phases with the percent, the milestones without their state.
    expect(
      within(timeline())
        .getAllByRole('listitem')
        .map((row) => row.textContent),
    ).toEqual([
      `Levantamento${ptBR.projects.schedule.late}96%`,
      'Configuração38%',
      'Homologação0%',
      'Aceite da homologação',
      'Entrada em produção0%',
      'Virada para produção',
    ]);
  });
});

// Spec: visao-geral-do-projeto, requirement "Prazo final contra a linha de base".
describe('Prazo final contra a linha de base', () => {
  it('Desvio do prazo final', async () => {
    await openOverview();

    const deviation = within(summary()).getByText('+2 dias úteis');
    expect(deviation.closest('[data-state]')).toHaveAttribute('data-state', 'soon');
    expect(summary()).toHaveTextContent('linha de base 11/11');
  });

  it('Prazo sem linha de base', async () => {
    overview = {
      ...demoOverview(),
      progress: { actual: 62, planned: 58, byBaseline: false },
      deadline: { end: '2026-11-13', baselineEnd: null, deviation: null },
    };
    await openOverview();

    expect(summary()).toHaveTextContent(`Prazo final13/112026${text.deadline.noBaseline}`);
    expect(summary()).toHaveTextContent('previsto 58%');
  });
});

// Spec: visao-geral-do-projeto, requirements "Chamados e próximo marco na visão geral" and "Equipe na visão geral".
describe('Chamados, próximo marco e equipe na visão geral', () => {
  it('Chamados na visão geral', async () => {
    await openOverview();

    const overdue = within(summary()).getByText('1 vencido');
    expect(overdue.closest('[data-state]')).toHaveAttribute('data-state', 'overdue');
    expect(summary()).toHaveTextContent('7de 9');
    expect(summary()).toHaveTextContent('2 aguardando solicitante');
  });

  it('Próximo marco', async () => {
    overview = {
      ...demoOverview(),
      nextMilestone: {
        id: '0199f5a2-7c1e-7d40-9a1b-0000000f0004',
        name: 'Homologação',
        day: '2026-11-03',
        businessDaysLeft: 0,
        daysLate: 2,
      },
    };
    await openOverview();

    const late = within(summary()).getByText('03/11 · 2 dias úteis de atraso');
    expect(late.closest('[data-state]')).toHaveAttribute('data-state', 'overdue');
    expect(summary()).toHaveTextContent('Próximo marcoHomologação');
  });

  it('Equipe na visão geral', async () => {
    await openOverview();

    expect(
      within(team())
        .getAllByRole('listitem')
        .map((item) => item.textContent),
    ).toEqual([
      expect.stringContaining(`${text.teams.management}${text.managementMeta}`),
      expect.stringContaining(`${text.teams.functional}2 pessoas · 3 chamados abertos`),
      expect.stringContaining(`${text.teams.development}3 pessoas · 4 chamados abertos`),
    ]);
  });
});

// Spec: estrutura-das-telas, requirement "Primeira abertura mais rápida" (ADR-0106).
describe('Primeira abertura mais rápida', () => {
  it('Aba pedida junto com o projeto', async () => {
    // The project answers only after the overview was asked: a page that waited for the project to ask the tab would
    // never open.
    let overviewAsked: () => void = () => undefined;
    const asked = new Promise<void>((resolve) => {
      overviewAsked = resolve;
    });
    const [erp] = demoProjects();
    server.use(
      http.get('/api/projects/:id', async () => {
        await asked;
        return HttpResponse.json(erp);
      }),
      http.get('/api/projects/:id/overview', () => {
        overviewAsked();
        return HttpResponse.json(overview);
      }),
    );

    await openOverview();

    expect(summary()).toHaveTextContent('Andamento62%previsto 70%');
  });
});
