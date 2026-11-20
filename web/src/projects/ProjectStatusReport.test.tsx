import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { StatusReportDraft } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import {
  type DemoProject,
  demoProjects,
  problem,
  projectsPage,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects.statusReport;
const ticket = (number: number) => `#${String(number)}`;

let project: DemoProject;
let draft: StatusReportDraft;
let generate: () => Response;
let sent: unknown[];
let downloads: string[];

beforeEach(() => {
  const projects = demoProjects();
  const erp = projects[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  project = erp;
  sent = [];
  downloads = [];
  draft = {
    last: null,
    summary: '',
    focus: 'Integração fiscal',
    nextSteps: '',
    clientNeeds: '',
    tickets: [
      { number: 539, title: 'Rejeição na emissão', status: 'InProgress', threatens: true },
      { number: 545, title: 'Dúvida sobre o relatório fiscal', status: 'Open', threatens: false },
    ],
    risks: [{ number: 539, level: 'Medium', attention: '' }],
  };
  generate = () =>
    HttpResponse.json({
      documentId: '0199f5a2-7c1e-7d40-9a1b-d00000000001',
      folderId: '0199f5a2-7c1e-7d40-9a1b-e00000000007',
      folderName: 'Relatórios de status',
      name: 'ERP-FIN - Relatório de status - 2026-10-19.pptx',
      version: 1,
    });
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
    http.get('/api/projects/:id/status-report', () => HttpResponse.json(draft)),
    http.post('/api/projects/:id/status-report', async ({ request }) => {
      sent.push(await request.json());
      return generate();
    }),
  );
  vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (
    this: HTMLAnchorElement,
  ) {
    downloads.push(this.getAttribute('href') ?? '');
  });
});

afterEach(() => {
  vi.restoreAllMocks();
});

async function openDialog() {
  await renderApp(`/projects/${project.id}`);
  fireEvent.click(await screen.findByRole('button', { name: ptBR.projects.actions.button }));
  const menu = await screen.findByRole('menu', { name: ptBR.projects.actions.button });
  fireEvent.click(within(menu).getByRole('menuitem', { name: text.action }));
  const dialog = await screen.findByRole('dialog', { name: text.title });
  expect(within(dialog).getByLabelText(text.summary)).toBeVisible();
  return dialog;
}

// Spec: relatorio-de-status, requirement "Gerar o relatório pela tela" (ADR-0133, ADR-0165, ADR-0166).
describe('Gerar o relatório pela tela', () => {
  it('Gerar relatório pela tela', async () => {
    const dialog = await openDialog();

    expect(within(dialog).getByLabelText(text.focus)).toHaveValue('Integração fiscal');
    const hours = within(dialog).getByRole('checkbox', { name: text.showHours });
    expect(hours).not.toBeChecked();
    fireEvent.change(within(dialog).getByLabelText(text.summary), {
      target: { value: 'Cadastros validados' },
    });
    fireEvent.click(hours);
    fireEvent.click(within(dialog).getByRole('button', { name: text.generate }));

    expect(
      await screen.findByText(text.saved.replace('{{folder}}', 'Relatórios de status')),
    ).toBeVisible();
    expect(sent).toEqual([
      {
        summary: 'Cadastros validados',
        focus: 'Integração fiscal',
        nextSteps: '',
        clientNeeds: '',
        risks: [{ number: 539, level: 'Medium', attention: '' }],
        showHours: true,
      },
    ]);
    expect(downloads).toEqual([
      `/api/projects/${project.id}/documents/0199f5a2-7c1e-7d40-9a1b-d00000000001/content?download=true`,
    ]);
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: text.title })).not.toBeInTheDocument();
    });
  });

  it('Ação só para quem gera', async () => {
    project = {
      ...project,
      permissions: { ...project.permissions, canGenerateStatusReport: false },
    };
    await renderApp(`/projects/${project.id}`);

    fireEvent.click(await screen.findByRole('button', { name: ptBR.projects.actions.button }));
    const menu = await screen.findByRole('menu', { name: ptBR.projects.actions.button });

    expect(within(menu).queryByRole('menuitem', { name: text.action })).not.toBeInTheDocument();
  });

  it('Riscos sugeridos na tela', async () => {
    draft = {
      ...draft,
      last: {
        generatedAt: '2026-10-13T15:00:00Z',
        generatedBy: { id: '0199f5a2-7c1e-7d40-9a1b-000000000001', name: 'Marina Costa' },
      },
    };
    const dialog = await openDialog();

    expect(
      within(dialog).getByText(
        text.last.replace('{{day}}', '13/10/2026').replace('{{name}}', 'Marina Costa'),
      ),
    ).toBeVisible();
    const risk = within(dialog).getByRole('region', {
      name: `${ticket(539)} · Rejeição na emissão`,
    });
    expect(within(risk).getByRole('radio', { name: text.levels.Medium })).toBeChecked();
    expect(within(risk).getByLabelText(text.attention)).toHaveValue('');

    fireEvent.click(within(risk).getByRole('radio', { name: text.levels.High }));
    fireEvent.change(within(risk).getByLabelText(text.attention), {
      target: { value: 'Depende do fornecedor' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.generate }));

    await waitFor(() => {
      expect(sent).toHaveLength(1);
    });
    expect(sent[0]).toMatchObject({
      risks: [{ number: 539, level: 'High', attention: 'Depende do fornecedor' }],
    });
  });

  it('Erro de um campo na tela', async () => {
    generate = () =>
      problem(400, 'https://tools.ietf.org/html/rfc9110#section-15.5.1', {
        errors: { risks: ['From 0 to 10 open tickets of the project.'] },
      });
    const dialog = await openDialog();

    fireEvent.click(within(dialog).getByRole('button', { name: text.generate }));

    expect(await within(dialog).findByText(text.errors.risks)).toBeVisible();
    expect(screen.getByRole('dialog', { name: text.title })).toBeVisible();
    expect(downloads).toEqual([]);
  });
});
