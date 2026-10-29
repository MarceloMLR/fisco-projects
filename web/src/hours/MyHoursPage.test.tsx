import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { TimeEntry } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import {
  demoTimeEntries,
  hoursNow,
  hoursProjects,
  hoursWork,
  timeEntryListOf,
} from '../test/demoTimeEntries';
import { phoneWidth, renderApp } from '../test/renderApp';
import { server, signedInAs } from '../test/server';

const text = ptBR.hours;
const requests: { method: string; path: string; body: unknown }[] = [];
const periods: string[] = [];
let entries: TimeEntry[];

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'], now: hoursNow });
  requests.length = 0;
  periods.length = 0;
  entries = demoTimeEntries();
  signedInAs('User');
  server.use(
    http.get('/api/time-entries', ({ request }) => {
      const url = new URL(request.url);
      periods.push(`${url.searchParams.get('from') ?? ''}/${url.searchParams.get('to') ?? ''}`);
      return HttpResponse.json(timeEntryListOf(entries, url));
    }),
    http.get('/api/time-entries/projects', () => HttpResponse.json(hoursProjects)),
    http.get('/api/time-entries/projects/:projectId/work', () => HttpResponse.json(hoursWork)),
    http.post('/api/time-entries', async ({ request }) => {
      const body = (await request.json()) as Omit<
        TimeEntry,
        'id' | 'project' | 'task' | 'ticket' | 'canChange'
      > & {
        projectId: string;
        scheduleItemId: string | null;
      };
      requests.push({ method: 'POST', path: '', body });
      const project = hoursProjects.find((item) => item.id === body.projectId) ?? hoursProjects[0];
      const task = hoursWork.tasks.find((item) => item.id === body.scheduleItemId) ?? null;
      entries = [
        {
          id: '0199f5a2-7c1e-7d40-9a1b-e00000000199',
          day: body.day,
          hours: body.hours,
          description: body.description,
          project: { ...project },
          task,
          ticket: null,
          canChange: true,
        },
        ...entries,
      ];
      return HttpResponse.json({ id: '0199f5a2-7c1e-7d40-9a1b-e00000000199' }, { status: 201 });
    }),
    http.put('/api/time-entries/:entryId', async ({ params, request }) => {
      const body = (await request.json()) as { hours: number };
      requests.push({ method: 'PUT', path: String(params.entryId), body });
      entries = entries.map((item) =>
        item.id === params.entryId ? { ...item, hours: body.hours } : item,
      );
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/time-entries/:entryId', ({ params }) => {
      requests.push({ method: 'DELETE', path: String(params.entryId), body: null });
      entries = entries.filter((item) => item.id !== params.entryId);
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

afterEach(() => {
  vi.useRealTimers();
});

/** The text of the first cell of each line of the grid: the days and the projects of the entries. */
function lines(): string[] {
  const grid = screen.getByRole('grid', { name: text.table });
  return within(grid)
    .queryAllByRole('row')
    .slice(1)
    .map((row) => within(row).queryAllByRole('gridcell')[0]?.textContent ?? '');
}

async function openHours(width?: number) {
  await renderApp('/hours', width);
  await waitFor(() => {
    expect(lines().length).toBeGreaterThan(0);
  });
}

// Spec: horas-do-projeto: "Minhas horas pela tela" and "Apontar pela tela" (ADR-0128).
describe('Minhas horas pela tela', () => {
  it('Semana pela tela', async () => {
    await openHours();

    expect(periods[0]).toBe('2026-10-26/2026-11-01');
    expect(lines()).toEqual([
      'Quinta-feira, 29/10Hoje',
      'ERP-FINImplantação do ERP financeiro',
      'Quarta-feira, 28/10',
      'SPED-27Adequação do SPED Fiscal 2027',
      'ERP-FINImplantação do ERP financeiro',
      'Terça-feira, 27/10',
      'ERP-FINImplantação do ERP financeiro',
      'ERP-FINImplantação do ERP financeiro',
      'Segunda-feira, 26/10',
      'ERP-FINImplantação do ERP financeiro',
    ]);
    const grid = screen.getByRole('grid', { name: text.table });
    expect(grid).toHaveTextContent('7,5 h');
    expect(grid).toHaveTextContent(text.onlyProject);
    expect(grid).toHaveTextContent(text.deletedTask);
    expect(grid).toHaveTextContent('#101 Erro ao importar XML de entrada');
    expect(screen.getByText(text.total.replace('{{hours}}', '27 h'))).toBeVisible();
    expect(screen.getByText(/27 h na semana/)).toBeVisible();
    expect(screen.getByRole('button', { name: text.nextWeek })).toBeDisabled();
  });

  it('Apontar pela tela', async () => {
    await openHours();

    fireEvent.click(screen.getByRole('button', { name: text.log }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    expect(within(dialog).getByLabelText(text.dialog.day)).toHaveValue('29/10/2026');
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.dialog.project }));
    fireEvent.click(
      within(await screen.findByRole('listbox')).getByRole('option', { name: /^ERP-FIN/ }),
    );
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.dialog.work }));
    fireEvent.click(await screen.findByRole('option', { name: /Roteiro de testes/ }));
    fireEvent.change(within(dialog).getByLabelText(text.dialog.hours), {
      target: { value: '1:30' },
    });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.description), {
      target: { value: 'Revisão do roteiro' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));

    expect(await screen.findByText(text.messages.added)).toBeVisible();
    expect(requests).toEqual([
      {
        method: 'POST',
        path: '',
        body: {
          projectId: hoursProjects[0].id,
          day: '2026-10-29',
          hours: 1.5,
          description: 'Revisão do roteiro',
          scheduleItemId: hoursWork.tasks[1]?.id,
          ticketId: null,
        },
      },
    ]);
    await waitFor(() => {
      expect(screen.getByText(/28,5 h na semana/)).toBeVisible();
    });
  });

  it('Erro no campo', async () => {
    server.use(
      http.post('/api/time-entries', () =>
        HttpResponse.json(
          {
            type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
            status: 400,
            errors: { hours: ['Up to 24.'] },
          },
          { status: 400 },
        ),
      ),
    );
    await openHours();

    fireEvent.click(screen.getByRole('button', { name: text.log }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.newTitle });
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: text.dialog.project }));
    fireEvent.click(
      within(await screen.findByRole('listbox')).getByRole('option', { name: /^ERP-FIN/ }),
    );
    fireEvent.change(within(dialog).getByLabelText(text.dialog.hours), { target: { value: '22' } });
    fireEvent.change(within(dialog).getByLabelText(text.dialog.description), {
      target: { value: 'Virada' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.add }));

    expect(await within(dialog).findByText(text.errors.hoursInvalid)).toBeVisible();
    expect(screen.getByRole('dialog', { name: text.dialog.newTitle })).toBeVisible();
  });

  it('Corrigir e apagar pela tela', async () => {
    await openHours();

    const grid = screen.getByRole('grid', { name: text.table });
    fireEvent.click(
      within(grid).getByRole('button', { name: text.menu.button.replace('{{day}}', '29/10/2026') }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.edit }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.editTitle });
    expect(within(dialog).getByLabelText(text.dialog.hours)).toHaveValue('3,5');
    fireEvent.change(within(dialog).getByLabelText(text.dialog.hours), { target: { value: '2' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.dialog.save }));
    expect(await screen.findByText(text.messages.saved)).toBeVisible();
    await waitFor(() => {
      expect(screen.getByText(/25,5 h na semana/)).toBeVisible();
    });

    fireEvent.click(
      within(grid).getByRole('button', { name: text.menu.button.replace('{{day}}', '26/10/2026') }),
    );
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.remove }));
    const confirm = await screen.findByRole('alertdialog', { name: text.remove.title });
    fireEvent.click(within(confirm).getByRole('button', { name: text.remove.confirm }));

    expect(await screen.findByText(text.messages.removed)).toBeVisible();
    await waitFor(() => {
      expect(screen.getByText(/17,5 h na semana/)).toBeVisible();
    });
    expect(requests.map((request) => [request.method, request.path])).toEqual([
      ['PUT', '0199f5a2-7c1e-7d40-9a1b-e00000000101'],
      ['DELETE', '0199f5a2-7c1e-7d40-9a1b-e00000000106'],
    ]);
  });

  it('Trocar a semana', async () => {
    await openHours();

    fireEvent.click(screen.getByRole('button', { name: text.previousWeek }));
    await waitFor(() => {
      expect(periods).toContain('2026-10-19/2026-10-25');
    });
    expect(await screen.findByText(text.empty)).toBeVisible();
    fireEvent.click(screen.getByRole('button', { name: text.thisWeek }));

    await waitFor(() => {
      expect(lines()[0]).toBe('Quinta-feira, 29/10Hoje');
    });
  });

  it('Semana sem horas', async () => {
    entries = [];
    await renderApp('/hours');

    expect(await screen.findByText(text.empty)).toBeVisible();
    expect(screen.getByRole('button', { name: text.log })).toBeVisible();
  });

  it('Minhas horas no celular', async () => {
    await openHours(phoneWidth);

    const grid = screen.getByRole('grid', { name: text.table });
    const header = within(grid)
      .getAllByRole('columnheader')
      .map((cell) => cell.textContent);
    expect(header).toEqual([text.columns.description, text.columns.hours, '']);
    const [, , first] = within(grid).getAllByRole('row');
    if (first === undefined) {
      throw new Error('No entry in the grid.');
    }
    expect(first).toHaveTextContent('ERP-FIN');
    expect(first).toHaveTextContent('Roteiro dos testes de emissão de NF-e');
    expect(first).toHaveTextContent('3,5 h');
    expect(screen.getByRole('button', { name: text.log })).toBeVisible();
  });
});
