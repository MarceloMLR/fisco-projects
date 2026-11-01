import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { AdditionalHours, ProjectHours, ProjectHoursDetail } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import {
  balanceOf,
  closedProjectHours,
  erpHoursDetail,
  erpProjectHours,
  openProjectHours,
  taskHoursOf,
  taskHoursRow,
} from '../test/demoProjectHours';
import { demoTimeEntries, hoursNow, timeEntryListOf } from '../test/demoTimeEntries';
import { expectBusy, expectIdle } from '../test/busy';
import { renderApp } from '../test/renderApp';
import { demoPeople, server, signedInAs } from '../test/server';
import { bytesOf, unzip } from '../test/unzip';

const saved = vi.hoisted(() => ({ files: [] as { content: Blob; name: string }[] }));

// The browser saves the spreadsheet; the tests keep it to open.
vi.mock('../format/saveFile', () => ({
  saveFile: (content: Blob, name: string) => {
    saved.files.push({ content, name });
  },
}));

const text = ptBR.projectHours;
const lists: URL[] = [];
const periods: URL[] = [];
const changes: { method: string; path: string; body: unknown }[] = [];
let detail: ProjectHoursDetail;

/** The rows of a view and of a search, as the API answers them. */
function rowsOf(url: URL): ProjectHours[] {
  const view = url.searchParams.get('view') ?? 'Open';
  const search = (url.searchParams.get('search') ?? '').toLowerCase();
  const rows =
    view === 'Closed'
      ? closedProjectHours
      : view === 'All'
        ? [...openProjectHours, ...closedProjectHours]
        : openProjectHours;
  return rows
    .map((row) => (row.id === detail.project.id ? detail.project : row))
    .filter(
      (row) =>
        search === '' ||
        row.code.toLowerCase().includes(search) ||
        row.name
          .normalize('NFD')
          .replace(/\p{Diacritic}/gu, '')
          .toLowerCase()
          .includes(search),
    );
}

/** The detail after a change: the balance computed again from the contracted and the additional hours. */
function withBalance(contracted: number | null, additional: AdditionalHours[]) {
  const sum = additional.reduce((total, item) => total + item.hours, 0);
  detail = {
    ...detail,
    additional,
    project: {
      ...detail.project,
      balance: balanceOf(contracted, sum, detail.project.balance.logged),
    },
  };
}

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'], now: hoursNow });
  lists.length = 0;
  periods.length = 0;
  changes.length = 0;
  saved.files.length = 0;
  detail = erpHoursDetail();
  signedInAs('Administrator');
  server.use(
    http.get('/api/admin/project-hours', ({ request }) => {
      const url = new URL(request.url);
      lists.push(url);
      return HttpResponse.json(rowsOf(url));
    }),
    http.get('/api/admin/project-hours/:projectId', () => HttpResponse.json(detail)),
    http.put('/api/admin/project-hours/:projectId/contracted', async ({ request, params }) => {
      const body = (await request.json()) as { hours: number | null };
      changes.push({ method: 'PUT', path: `${String(params.projectId)}/contracted`, body });
      withBalance(body.hours, detail.additional);
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/admin/project-hours/:projectId/additional', async ({ request, params }) => {
      const body = (await request.json()) as { day: string; hours: number; reason: string };
      changes.push({ method: 'POST', path: `${String(params.projectId)}/additional`, body });
      withBalance(detail.project.balance.contracted, [
        {
          id: '0199f5a2-7c1e-7d40-9a1b-f00000000199',
          ...body,
          createdBy: demoPeople.paulo,
        },
        ...detail.additional,
      ]);
      return HttpResponse.json({ id: '0199f5a2-7c1e-7d40-9a1b-f00000000199' }, { status: 201 });
    }),
    http.get('/api/time-entries', ({ request }) => {
      const url = new URL(request.url);
      periods.push(url);
      return HttpResponse.json(timeEntryListOf(demoTimeEntries(), url));
    }),
  );
});

afterEach(() => {
  vi.useRealTimers();
});

/** The cells of each row of the list, below the header. */
function rows(): string[][] {
  const grid = screen.getByRole('grid', { name: text.table });
  return within(grid)
    .queryAllByRole('row')
    .slice(1)
    .map((row) =>
      within(row)
        .queryAllByRole('gridcell')
        .map((cell) => cell.textContent),
    );
}

async function showProjectHours() {
  await renderApp('/admin/project-hours');
  await waitFor(() => {
    expect(rows()).toHaveLength(openProjectHours.length);
  });
}

async function openErp() {
  await showProjectHours();
  fireEvent.click(screen.getByText('Implantação do ERP financeiro'));
  const drawer = await screen.findByRole('dialog', { name: text.details.label });
  await within(drawer).findByRole('heading', { name: 'Implantação do ERP financeiro' });
  return drawer;
}

// Spec: horas-do-projeto, "Horas dos projetos pela tela" and "Detalhe das horas pela tela" (ADR-0128).
describe('Horas dos projetos pela tela', () => {
  it('Horas dos projetos pela tela', async () => {
    await showProjectHours();

    // Acompanhamento is the first section of the Administration, with Horas dos projetos.
    const sections = screen.getByRole('navigation', { name: ptBR.admin.sections.label });
    const links = within(sections).getAllByRole('link');
    expect(links[0]).toHaveTextContent(ptBR.pages.projectHours.title);
    expect(links[0]).toHaveAttribute('aria-current', 'page');
    expect(within(sections).getByText(ptBR.admin.sections.monitoring)).toBeInTheDocument();

    expect(
      screen.getByRole('heading', { level: 1, name: ptBR.pages.projectHours.title }),
    ).toBeVisible();
    expect(screen.getByText('5 projetos em aberto · 1 acima das horas contratadas')).toBeVisible();
    expect(lists[0]?.searchParams.get('view')).toBe('Open');
    const [over, near, , , none] = rows();
    expect(over).toEqual([
      'Implantação do ERP financeiroERP-FIN · Cliente Exemplo Alfa',
      'Em andamento',
      '400 h',
      '40 h',
      '468 h',
      '28 h acima',
      '106%',
    ]);
    expect(near?.[5]).toBe('Restam 14 h');
    expect(near?.[6]).toBe('91%');
    expect(none).toEqual([
      'Portal de notas para fornecedoresPORTAL-NF · Interno',
      'Em andamento',
      '—',
      '—',
      '36 h',
      text.balance.noContract,
      '—',
    ]);
    const grid = screen.getByRole('grid', { name: text.table });
    // The consumption has the color of the state of the balance, besides its text.
    const bars = within(grid).getAllByRole('progressbar', { name: text.consumptionLabel });
    expect(bars[0]?.firstElementChild).toHaveStyle({ backgroundColor: 'var(--danger)' });
    expect(bars[1]?.firstElementChild).toHaveStyle({ backgroundColor: 'var(--warning)' });
    expect(bars[2]?.firstElementChild).toHaveStyle({
      backgroundColor: 'var(--control-selected)',
    });

    fireEvent.click(screen.getByRole('radio', { name: text.views.All }));
    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: 'implantacao' },
    });
    await waitFor(() => {
      expect(rows()).toHaveLength(3);
    });
    expect(lists.at(-1)?.searchParams.get('view')).toBe('All');
    expect(lists.at(-1)?.searchParams.get('search')).toBe('implantacao');
    expect(screen.getByText('3 projetos · 1 acima das horas contratadas')).toBeVisible();
  });

  it('Baixar a planilha das horas', async () => {
    await showProjectHours();

    fireEvent.click(screen.getByRole('button', { name: text.download }));

    await waitFor(() => {
      expect(saved.files).toHaveLength(1);
    });
    const [file] = saved.files;
    expect(file?.name).toBe('horas-dos-projetos-2026-10-29.xlsx');
    const parts = await unzip(await bytesOf(file?.content ?? new Blob()));
    const sheet = parts.get('xl/worksheets/sheet1.xml') ?? '';
    // A line per project of the view, with the hours as numbers.
    expect(sheet.match(/<row /g)).toHaveLength(openProjectHours.length + 1);
    expect(sheet).toContain('ERP-FIN');
    expect(sheet).toContain('28 h acima');
    expect(sheet).toContain('<v>468</v>');
    expect(sheet).toContain('<v>-28</v>');
    const filters = parts.get('xl/worksheets/sheet2.xml') ?? '';
    expect(filters).toContain(text.views.Open);
    expect(filters).toContain(text.report.filters.noSearch);
  });

  it('Detalhe do projeto pela tela', async () => {
    const drawer = await openErp();

    expect(
      within(drawer).getByRole('heading', { name: 'Implantação do ERP financeiro' }),
    ).toBeVisible();
    expect(within(drawer).getByText('Total de 440 h')).toBeVisible();
    expect(within(drawer).getByText('Desde 03/08/2026')).toBeVisible();
    expect(within(drawer).getByText('28 h acima')).toBeVisible();
    expect(within(drawer).getByText('468 de 440 h (106%)')).toBeVisible();
    const additional = within(drawer).getByRole('region', { name: text.details.additionalTitle });
    expect(within(additional).getAllByRole('row')).toHaveLength(3);
    expect(within(additional).getAllByText('Incluídas por Paulo Nogueira')).toHaveLength(2);
    const people = within(drawer).getByRole('region', { name: text.details.people });
    const lines = within(people)
      .getAllByRole('row')
      .slice(1)
      .map((row) => row.textContent);
    expect(lines).toEqual([
      expect.stringMatching(/Carla Mendes.*Colaborador.*182 h.*39%/),
      expect.stringMatching(/Diego Torres.*Desenvolvedor.*150 h.*32%/),
      expect.stringMatching(/Marina Costa.*Gerente de projeto.*96 h.*21%/),
      expect.stringMatching(/Rafael Souza.*Saiu do projeto.*40 h.*9%/),
    ]);
  });

  it('Incluir horas adicionais pela tela', async () => {
    const drawer = await openErp();

    fireEvent.click(within(drawer).getByRole('button', { name: text.details.addAdditional }));
    const dialog = await screen.findByRole('dialog', { name: text.additionalDialog.newTitle });
    expect(within(dialog).getByLabelText(text.additionalDialog.day)).toHaveValue('29/10/2026');
    fireEvent.change(within(dialog).getByLabelText(text.additionalDialog.hours), {
      target: { value: '12' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.additionalDialog.reason), {
      target: { value: 'Novos relatórios de conciliação' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.additionalDialog.add }));

    expect(await screen.findByText(text.messages.added)).toBeVisible();
    expect(changes).toEqual([
      {
        method: 'POST',
        path: `${erpProjectHours.id}/additional`,
        body: { day: '2026-10-29', hours: 12, reason: 'Novos relatórios de conciliação' },
      },
    ]);
    await waitFor(() => {
      expect(within(drawer).getByText('Novos relatórios de conciliação')).toBeVisible();
    });
    expect(within(drawer).getByText('Total de 452 h')).toBeVisible();
    expect(within(drawer).getByText('16 h acima')).toBeVisible();
  });

  it('Horas adicionais sem motivo pela tela', async () => {
    const drawer = await openErp();

    fireEvent.click(within(drawer).getByRole('button', { name: text.details.addAdditional }));
    const dialog = await screen.findByRole('dialog', { name: text.additionalDialog.newTitle });
    fireEvent.change(within(dialog).getByLabelText(text.additionalDialog.hours), {
      target: { value: '10,1' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.additionalDialog.add }));

    expect(await within(dialog).findByText(text.errors.hoursInvalid)).toBeVisible();
    expect(within(dialog).getByText(text.errors.reasonRequired)).toBeVisible();
    expect(changes).toEqual([]);
  });

  it('Alterar as horas contratadas pela tela', async () => {
    const drawer = await openErp();

    fireEvent.click(within(drawer).getByRole('button', { name: text.details.changeLabel }));
    const dialog = await screen.findByRole('dialog', { name: text.contractedDialog.title });
    const field = within(dialog).getByLabelText(text.contractedDialog.hours);
    expect(field).toHaveValue('400');
    fireEvent.change(field, { target: { value: '420' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.contractedDialog.save }));

    expect(await screen.findByText(text.messages.contractedSaved)).toBeVisible();
    expect(changes).toEqual([
      { method: 'PUT', path: `${erpProjectHours.id}/contracted`, body: { hours: 420 } },
    ]);
    await waitFor(() => {
      expect(within(drawer).getByText('420 h')).toBeVisible();
    });
    expect(within(drawer).getByText('8 h acima')).toBeVisible();
    // The list shows the new hours too.
    fireEvent.click(within(drawer).getByRole('button', { name: ptBR.details.close }));
    await waitFor(() => {
      expect(rows()[0]?.[2]).toBe('420 h');
    });
  });

  it('Tirar as horas contratadas pela tela', async () => {
    const drawer = await openErp();

    fireEvent.click(within(drawer).getByRole('button', { name: text.details.changeLabel }));
    const dialog = await screen.findByRole('dialog', { name: text.contractedDialog.title });
    fireEvent.change(within(dialog).getByLabelText(text.contractedDialog.hours), {
      target: { value: '' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.contractedDialog.save }));

    expect(await screen.findByText(text.messages.contractedRemoved)).toBeVisible();
    expect(changes).toEqual([
      { method: 'PUT', path: `${erpProjectHours.id}/contracted`, body: { hours: null } },
    ]);
    await waitFor(() => {
      expect(within(drawer).getByText(text.balance.noContract)).toBeVisible();
    });
    expect(within(drawer).queryByRole('progressbar')).toBeNull();
  });

  it('Ver apontamentos pela tela', async () => {
    const drawer = await openErp();

    fireEvent.click(within(drawer).getByRole('button', { name: text.details.viewEntries }));
    const dialog = await screen.findByRole('dialog', { name: text.entries.title });
    expect(within(dialog).getByText('ERP-FIN · Implantação do ERP financeiro')).toBeVisible();
    expect(within(dialog).getByText('Outubro de 2026')).toBeVisible();
    const grid = await within(dialog).findByRole('grid', { name: text.entries.table });
    await waitFor(() => {
      expect(within(grid).getAllByText('Carla Mendes').length).toBeGreaterThan(0);
    });
    const [period] = periods;
    expect(period?.searchParams.get('from')).toBe('2026-10-01');
    expect(period?.searchParams.get('to')).toBe('2026-10-31');
    expect(period?.searchParams.get('projectId')).toBe(erpProjectHours.id);
    expect(within(dialog).getByText('Total do mês: 27 h')).toBeVisible();
    expect(within(dialog).getByRole('button', { name: text.entries.nextMonth })).toBeDisabled();

    fireEvent.click(within(dialog).getByRole('button', { name: text.entries.previousMonth }));

    expect(await within(dialog).findByText('Setembro de 2026')).toBeVisible();
    await waitFor(() => {
      expect(periods.at(-1)?.searchParams.get('from')).toBe('2026-09-01');
    });
    expect(periods.at(-1)?.searchParams.get('to')).toBe('2026-09-30');
    expect(await within(dialog).findByText(text.entries.empty)).toBeVisible();
  });

  it('Muitas horas adicionais pela tela', async () => {
    const many = Array.from({ length: 23 }, (_, index) => ({
      id: `0199f5a2-7c1e-7d40-9a1b-f00000000${String(300 + index)}`,
      day: `2026-10-${String(23 - index).padStart(2, '0')}`,
      hours: 2,
      reason: `Pedido fora do escopo ${String(index + 1)}`,
      createdBy: demoPeople.paulo,
    }));
    withBalance(400, many);
    const drawer = await openErp();

    // The drawer shows the three newest, and the others are in "Ver todas".
    const additional = within(drawer).getByRole('region', { name: text.details.additionalTitle });
    expect(within(additional).getAllByRole('row')).toHaveLength(4);
    expect(within(additional).getByText('Pedido fora do escopo 3')).toBeVisible();
    expect(within(additional).queryByText('Pedido fora do escopo 4')).toBeNull();
    fireEvent.click(within(additional).getByRole('button', { name: 'Ver todas (23)' }));

    const dialog = await screen.findByRole('dialog', { name: text.allAdditional.title });
    const table = within(dialog).getByRole('table', { name: text.allAdditional.table });
    expect(within(table).getAllByRole('row')).toHaveLength(11);
    expect(within(dialog).getByText('1–10 de 23')).toBeVisible();
    expect(
      within(dialog).getByRole('button', { name: text.allAdditional.previous }),
    ).toBeDisabled();
    fireEvent.click(within(dialog).getByRole('button', { name: text.allAdditional.next }));
    expect(within(dialog).getByText('11–20 de 23')).toBeVisible();
    fireEvent.click(within(dialog).getByRole('button', { name: text.allAdditional.next }));
    expect(within(dialog).getByText('21–23 de 23')).toBeVisible();
    expect(within(table).getAllByRole('row')).toHaveLength(4);
    expect(within(dialog).getByRole('button', { name: text.allAdditional.next })).toBeDisabled();

    // The "⋮" of the dialog corrects the additional hours, as the drawer does.
    fireEvent.click(
      within(table).getByRole('button', {
        name: text.additionalMenu.button.replace('{{day}}', '01/10/2026'),
      }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.additionalMenu.edit }));
    const edit = await screen.findByRole('dialog', { name: text.additionalDialog.editTitle });
    expect(within(edit).getByLabelText(text.additionalDialog.reason)).toHaveValue(
      'Pedido fora do escopo 23',
    );
  });
});

// Spec: estrutura-das-telas, requirement "Botões em andamento" (ADR-0131, ADR-0132).
describe('Botões em andamento nas horas dos projetos', () => {
  it('Planilha das horas em andamento', async () => {
    await showProjectHours();
    const download = screen.getByRole('button', { name: text.download });

    fireEvent.click(download);

    // Busy, not grey, while the spreadsheet is put together, and at least half a second.
    expectBusy(download, text.download);
    expect(download).not.toBeDisabled();
    await waitFor(() => {
      expect(saved.files).toHaveLength(1);
    });
    await waitFor(() => {
      expectIdle(download);
    });
  });
});

// Spec: horas-do-projeto, requirement "Horas por tarefa no detalhe das horas pela tela" (ADR-0138).
describe('Horas por tarefa pela tela', () => {
  const icms = '0199f5a2-7c1e-7d40-9a1b-300000000011';
  const nfe = '0199f5a2-7c1e-7d40-9a1b-300000000012';
  let planned: Record<string, number | null>;

  beforeEach(() => {
    planned = { [icms]: 40, [nfe]: 20 };
    server.use(
      http.get('/api/admin/project-hours/:projectId/tasks', () =>
        HttpResponse.json(
          taskHoursOf(
            [
              taskHoursRow(icms, 'Parametrizar regras de ICMS', planned[icms] ?? null, 52),
              taskHoursRow(nfe, 'Integrar emissão de NF-e', planned[nfe] ?? null, 18),
            ],
            12,
          ),
        ),
      ),
      http.put('/api/admin/project-hours/:projectId/tasks/:itemId', async ({ request, params }) => {
        const body = (await request.json()) as { hours: number | null };
        changes.push({
          method: 'PUT',
          path: `${String(params.projectId)}/tasks/${String(params.itemId)}`,
          body,
        });
        planned[String(params.itemId)] = body.hours;
        return new HttpResponse(null, { status: 204 });
      }),
    );
  });

  it('Por tarefa no detalhe das horas', async () => {
    const drawer = await openErp();

    const tasks = await within(drawer).findByRole('region', { name: text.tasks.title });
    await within(tasks).findByText('Parametrizar regras de ICMS');
    expect(within(tasks).getByText(text.tasks.inTasks).nextElementSibling).toHaveTextContent(
      '60 h',
    );
    expect(within(tasks).getByText(text.tasks.loggedInTasks).nextElementSibling).toHaveTextContent(
      '70 h',
    );
    expect(within(tasks).getByText(text.tasks.outside).nextElementSibling).toHaveTextContent(
      '12 h',
    );
    const lines = within(tasks)
      .getAllByRole('row')
      .slice(1)
      .map((row) => row.textContent);
    expect(lines).toEqual([
      expect.stringMatching(/Parametrizar regras de ICMS.*12 h acima.*40 h.*52 h/),
      expect.stringMatching(/Integrar emissão de NF-e.*Restam 2 h.*20 h.*18 h/),
    ]);
    expect(within(tasks).getByText('12 h acima')).toHaveAttribute('data-state', 'Over');
    expect(within(tasks).getByText('Restam 2 h')).toHaveAttribute('data-state', 'NearLimit');
    expect(within(tasks).getByRole('link', { name: text.tasks.openSchedule })).toHaveAttribute(
      'href',
      `/projects/${erpProjectHours.id}/schedule?view=list`,
    );
  });

  it('Alterar as previstas pelo detalhe das horas', async () => {
    const drawer = await openErp();
    const tasks = await within(drawer).findByRole('region', { name: text.tasks.title });
    await within(tasks).findByText('Integrar emissão de NF-e');

    fireEvent.click(
      within(tasks).getByRole('button', {
        name: text.tasks.changeLabel.replace('{{name}}', 'Integrar emissão de NF-e'),
      }),
    );
    const dialog = await screen.findByRole('dialog', { name: text.plannedDialog.title });
    const field = within(dialog).getByLabelText(text.plannedDialog.hours);
    expect(field).toHaveValue('20');
    fireEvent.change(field, { target: { value: '24' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.plannedDialog.save }));

    expect(
      await screen.findByText(
        text.messages.plannedSaved.replace('{{name}}', 'Integrar emissão de NF-e'),
      ),
    ).toBeVisible();
    expect(changes).toEqual([
      { method: 'PUT', path: `${erpProjectHours.id}/tasks/${nfe}`, body: { hours: 24 } },
    ]);
    await waitFor(() => {
      expect(within(tasks).getByText('Restam 6 h')).toBeVisible();
    });
    expect(within(tasks).getByText(text.tasks.inTasks).nextElementSibling).toHaveTextContent(
      '64 h',
    );
  });

  it('Horas previstas inválidas pela tela', async () => {
    const drawer = await openErp();
    const tasks = await within(drawer).findByRole('region', { name: text.tasks.title });
    await within(tasks).findByText('Integrar emissão de NF-e');

    fireEvent.click(
      within(tasks).getByRole('button', {
        name: text.tasks.changeLabel.replace('{{name}}', 'Integrar emissão de NF-e'),
      }),
    );
    const dialog = await screen.findByRole('dialog', { name: text.plannedDialog.title });
    fireEvent.change(within(dialog).getByLabelText(text.plannedDialog.hours), {
      target: { value: '10000' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.plannedDialog.save }));

    expect(await within(dialog).findByText(text.errors.plannedInvalid)).toBeVisible();
    expect(changes).toEqual([]);
  });
});
