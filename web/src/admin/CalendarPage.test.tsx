import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { DayOfWeek } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import {
  calendarYearOf,
  type DemoCalendar,
  demoCalendar,
  problem,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.calendar;
const optionalKeys = [
  'CarnivalMonday',
  'CarnivalTuesday',
  'AshWednesday',
  'CorpusChristi',
  'PublicServantsDay',
  'ChristmasEve',
  'NewYearsEve',
];

/** The calendar as the simulated API keeps it, changed by the requests as the real one would. */
let calendar: DemoCalendar;
const requests: string[] = [];

beforeEach(() => {
  // October 19th, 2026: the calendar opens in 2026. Only the date is fake; the timers stay real for waitFor.
  vi.useFakeTimers({ toFake: ['Date'], now: new Date('2026-10-19T12:00:00Z') });
  calendar = demoCalendar();
  requests.length = 0;
  // The handlers keep the calendar of this test, so a late request of another test never changes it.
  const current = calendar;
  signedInAs('Administrator');
  server.use(
    http.get('/api/calendar/:year', ({ params }) =>
      HttpResponse.json(calendarYearOf(current, Number(params.year))),
    ),
    http.put('/api/calendar/working-days', async ({ request }) => {
      const body = (await request.json()) as { workingDays: DayOfWeek[] };
      requests.push(`PUT working-days ${body.workingDays.join(',')}`);
      current.workingDays = body.workingDays;
      return new HttpResponse(null, { status: 204 });
    }),
    http.put('/api/calendar/considered', async ({ request }) => {
      const body = (await request.json()) as {
        isConsidered: boolean;
        calculatedHolidays: string[];
        localHolidays: string[];
      };
      requests.push(
        `PUT considered ${String(body.isConsidered)} ${body.calculatedHolidays.join(',')} ${body.localHolidays.join(',')}`,
      );
      for (const key of body.calculatedHolidays) {
        if (body.isConsidered) {
          current.considered.add(key);
        } else {
          current.considered.delete(key);
        }
      }
      for (const holiday of current.local.filter((local) =>
        body.localHolidays.includes(local.id),
      )) {
        holiday.isConsidered = body.isConsidered;
      }
      return new HttpResponse(null, { status: 204 });
    }),
    http.post('/api/calendar/local-holidays', async ({ request }) => {
      const body = (await request.json()) as {
        name: string;
        date: string;
        repeatsEveryYear: boolean;
      };
      requests.push(`POST ${body.name} ${body.date} ${String(body.repeatsEveryYear)}`);
      const id = '0199f5a2-7c1e-7d40-9a1b-200000000009';
      current.local.push({ id, ...body, isConsidered: true });
      return HttpResponse.json({ id }, { status: 201 });
    }),
    http.put('/api/calendar/local-holidays/:id', async ({ request, params }) => {
      const body = (await request.json()) as {
        name: string;
        date: string;
        repeatsEveryYear: boolean;
        isConsidered: boolean;
      };
      requests.push(
        `PUT ${body.name} ${body.date} ${String(body.repeatsEveryYear)} ${String(body.isConsidered)}`,
      );
      current.local = current.local.map((holiday) =>
        holiday.id === params.id ? { ...holiday, ...body } : holiday,
      );
      return new HttpResponse(null, { status: 204 });
    }),
    http.delete('/api/calendar/local-holidays/:id', ({ params }) => {
      requests.push(`DELETE ${String(params.id)}`);
      current.local = current.local.filter((holiday) => holiday.id !== params.id);
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

afterEach(() => {
  vi.useRealTimers();
});

async function openCalendar(width?: number) {
  const view = await renderApp('/admin/calendar', width);
  await screen.findByText(text.holidays.NewYearsDay);
  return view;
}

function table() {
  return screen.getByRole('grid', { name: /^Feriados de/ });
}

/** The names of the dates in the table, in order. */
function names() {
  return within(table())
    .getAllByRole('row')
    .slice(1)
    .map((row) => within(row).getAllByRole('gridcell')[1]?.textContent ?? '');
}

function considered(name: string) {
  return screen.getByRole('checkbox', { name: `Considerar ${name}` });
}

function weekday(name: string) {
  return screen.getByRole('checkbox', { name });
}

// Spec: calendario, requirement "Tela Calendário e feriados".
describe('Tela Calendário e feriados', () => {
  it('Calendário e feriados na Administração', async () => {
    await openCalendar();

    expect(
      screen.getByRole('heading', { level: 1, name: ptBR.pages.calendar.title }),
    ).toBeVisible();
    const sections = screen.getByRole('navigation', { name: ptBR.admin.sections.label });
    expect(within(sections).getByRole('link', { name: ptBR.pages.calendar.title })).toHaveAttribute(
      'aria-current',
      'page',
    );
    for (const day of ['Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta']) {
      expect(weekday(day)).toBeChecked();
    }
    expect(weekday('Sábado')).not.toBeChecked();
    expect(weekday('Domingo')).not.toBeChecked();
    expect(table()).toHaveAccessibleName('Feriados de 2026');
    expect(names().slice(0, 4)).toEqual([
      text.holidays.NewYearsDay,
      'Aniversário da cidade',
      text.holidays.CarnivalMonday,
      text.holidays.CarnivalTuesday,
    ]);
    const carnival = screen.getByRole('row', { name: new RegExp(text.holidays.CarnivalMonday) });
    expect(within(carnival).getByText('16/02/2026')).toBeVisible();
    expect(within(carnival).getByText('segunda-feira')).toBeVisible();
    expect(within(carnival).getByText(text.kinds.OptionalHoliday)).toBeVisible();
    expect(considered(text.holidays.CarnivalMonday)).not.toBeChecked();
    expect(considered(text.holidays.Tiradentes)).toBeChecked();
    const emenda = screen.getByRole('row', { name: /Emenda de Tiradentes/ });
    expect(within(emenda).getByText('Só em 2026')).toBeVisible();
    expect(screen.getByText('19 datas em 2026 · 12 consideradas')).toBeVisible();
  });

  it('Trocar o ano', async () => {
    await openCalendar();

    fireEvent.click(screen.getByRole('button', { name: text.nextYear }));

    await waitFor(() => {
      expect(table()).toHaveAccessibleName('Feriados de 2027');
    });
    await waitFor(() => {
      expect(screen.getByText('09/02/2027')).toBeVisible();
    });
    expect(names()).toContain('Aniversário da cidade');
    expect(names()).not.toContain('Emenda de Tiradentes');
    expect(screen.getByText('18 datas em 2027 · 11 consideradas')).toBeVisible();
  });

  it('Filtrar por tipo', async () => {
    await openCalendar();

    fireEvent.click(screen.getByRole('radio', { name: text.filters.OptionalHoliday }));

    expect(names()).toEqual(
      optionalKeys.map((key) => text.holidays[key as keyof typeof text.holidays]),
    );
    expect(screen.getByText('7 datas em 2026 · 0 consideradas')).toBeVisible();
  });

  it('Marcar considerado na lista', async () => {
    await openCalendar();

    fireEvent.click(considered(text.holidays.CarnivalMonday));

    await waitFor(() => {
      expect(considered(text.holidays.CarnivalMonday)).toBeChecked();
    });
    expect(await screen.findByText(text.messages.saved)).toBeVisible();
    expect(requests).toEqual(['PUT considered true CarnivalMonday ']);
  });

  it('Erro ao salvar', async () => {
    server.use(http.put('/api/calendar/considered', () => problem(500)));
    await openCalendar();

    fireEvent.click(considered(text.holidays.CarnivalMonday));

    expect(await screen.findByText(text.messages.failed)).toBeVisible();
    await waitFor(() => {
      expect(considered(text.holidays.CarnivalMonday)).not.toBeChecked();
    });
  });

  it('Trocar os dias de trabalho', async () => {
    await openCalendar();

    fireEvent.click(weekday('Sábado'));

    await waitFor(() => {
      expect(weekday('Sábado')).toBeChecked();
    });
    expect(await screen.findByText(text.messages.saved)).toBeVisible();
    expect(requests).toEqual([
      'PUT working-days Monday,Tuesday,Wednesday,Thursday,Friday,Saturday',
    ]);
  });

  it('Semana sem dia de trabalho', async () => {
    calendar.workingDays = ['Monday'];
    await openCalendar();

    expect(weekday('Segunda')).toBeChecked();
    expect(weekday('Segunda')).toBeDisabled();
    expect(weekday('Terça')).toBeEnabled();
    expect(screen.getByText(text.lastWorkingDay)).toBeVisible();
  });

  it('Calendário no celular', async () => {
    await openCalendar(phoneWidth);

    const list = screen.getByRole('list', { name: 'Feriados de 2026' });
    expect(within(list).getAllByRole('listitem')).toHaveLength(19);
    expect(
      within(list).getByText(`16/02, segunda-feira · ${text.kinds.OptionalHoliday}`),
    ).toBeVisible();
    expect(screen.getByRole('combobox', { name: text.kind })).toBeVisible();
    expect(screen.getByRole('button', { name: text.markAll })).toBeVisible();
    expect(screen.getByRole('button', { name: text.newHoliday })).toBeVisible();
    expect(screen.queryByRole('grid')).not.toBeInTheDocument();
  });
});

// Spec: calendario, requirement "Marcar todos".
describe('Marcar todos', () => {
  it('Marcar todos', async () => {
    await openCalendar();

    fireEvent.click(screen.getByRole('button', { name: text.markAll }));

    await waitFor(() => {
      expect(screen.getByText('19 datas em 2026 · 19 consideradas')).toBeVisible();
    });
    const [request = ''] = requests;
    expect(request).toMatch(/^PUT considered true /);
    expect(request).toContain('CarnivalMonday');
    expect(request).toContain('0199f5a2-7c1e-7d40-9a1b-200000000002');
    expect(screen.getByRole('button', { name: text.unmarkAll })).toBeVisible();

    // Then each one the company does not want is unchecked.
    fireEvent.click(considered(text.holidays.PublicServantsDay));

    await waitFor(() => {
      expect(screen.getByText('19 datas em 2026 · 18 consideradas')).toBeVisible();
    });
    expect(screen.getByRole('button', { name: text.markAll })).toBeVisible();
  });

  it('Marcar todos de um tipo', async () => {
    await openCalendar();
    fireEvent.click(screen.getByRole('radio', { name: text.filters.OptionalHoliday }));

    fireEvent.click(screen.getByRole('button', { name: text.markAll }));

    await waitFor(() => {
      expect(requests).toEqual([`PUT considered true ${optionalKeys.join(',')} `]);
    });
  });

  it('Desmarcar todos', async () => {
    for (const key of optionalKeys) {
      calendar.considered.add(key);
    }
    await openCalendar();

    fireEvent.click(screen.getByRole('button', { name: text.unmarkAll }));

    await waitFor(() => {
      expect(screen.getByText('19 datas em 2026 · 0 consideradas')).toBeVisible();
    });
    expect(requests[0]).toMatch(/^PUT considered false /);
  });
});

// Spec: calendario, requirements "Feriados locais" and "Alterar e excluir feriados locais" (screen side).
describe('Feriados locais na tela', () => {
  it('Novo feriado pela tela', async () => {
    await openCalendar();

    fireEvent.click(screen.getByRole('button', { name: text.newHoliday }));
    const dialog = await screen.findByRole('dialog', { name: text.newHoliday });
    fireEvent.change(within(dialog).getByRole('textbox', { name: text.name }), {
      target: { value: ' Recesso da empresa ' },
    });
    fireEvent.change(within(dialog).getByRole('textbox', { name: text.date }), {
      target: { value: '30122026' },
    });
    expect(within(dialog).getByRole('textbox', { name: text.date })).toHaveValue('30/12/2026');
    fireEvent.click(within(dialog).getByRole('button', { name: text.include }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(requests).toEqual(['POST Recesso da empresa 2026-12-30 false']);
    expect(await screen.findByText('Feriado Recesso da empresa incluído.')).toBeVisible();
    await waitFor(() => {
      expect(names()).toContain('Recesso da empresa');
    });
  });

  it('Feriado local inválido', async () => {
    await openCalendar();

    fireEvent.click(screen.getByRole('button', { name: text.newHoliday }));
    const dialog = await screen.findByRole('dialog', { name: text.newHoliday });
    fireEvent.change(within(dialog).getByRole('textbox', { name: text.date }), {
      target: { value: '31/02/2026' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: text.include }));

    expect(await within(dialog).findByText(text.errors.nameRequired)).toBeVisible();
    expect(within(dialog).getByText(text.errors.dateInvalid)).toBeVisible();
    expect(requests).toEqual([]);
  });

  it('Alterar feriado local', async () => {
    await openCalendar();

    fireEvent.click(screen.getByRole('button', { name: 'Editar ou excluir Emenda de Tiradentes' }));
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.edit }));
    const dialog = await screen.findByRole('dialog', { name: text.editHoliday });
    const date = within(dialog).getByRole('textbox', { name: text.date });
    expect(date).toHaveValue('20/04/2026');
    fireEvent.change(date, { target: { value: '22/04/2026' } });
    fireEvent.click(within(dialog).getByRole('button', { name: text.save }));

    await waitFor(() => {
      expect(requests).toEqual(['PUT Emenda de Tiradentes 2026-04-22 false true']);
    });
    expect(await screen.findByText('Feriado Emenda de Tiradentes alterado.')).toBeVisible();
  });

  it('Excluir com confirmação', async () => {
    await openCalendar();

    fireEvent.click(screen.getByRole('button', { name: 'Editar ou excluir Emenda de Tiradentes' }));
    fireEvent.click(await screen.findByRole('menuitem', { name: text.menu.remove }));
    const confirmation = await screen.findByRole('alertdialog', { name: text.remove.title });
    expect(confirmation).toHaveAccessibleDescription(
      'O feriado Emenda de Tiradentes, de 20/04/2026, sai do calendário. Não dá para desfazer.',
    );
    expect(requests).toEqual([]);
    fireEvent.click(within(confirmation).getByRole('button', { name: text.remove.confirm }));

    await waitFor(() => {
      expect(requests).toEqual(['DELETE 0199f5a2-7c1e-7d40-9a1b-200000000002']);
    });
    await waitFor(() => {
      expect(names()).not.toContain('Emenda de Tiradentes');
    });
    expect(screen.getByText('Feriado Emenda de Tiradentes excluído.')).toBeVisible();
  });
});

// Spec: calendario, requirement "Calendário só do administrador" (screen side).
describe('Calendário só do administrador', () => {
  it('Endereço do calendário sem ser administrador', async () => {
    signedInAs('ProjectManager');

    await renderApp('/admin/calendar');

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.home.title }),
    ).toBeVisible();
    expect(screen.queryByText(text.workingDays)).not.toBeInTheDocument();
  });
});

// Spec: calendario, requirement "Recálculo das datas pela tela" (ADR-0126).
describe('Recálculo das datas pela tela', () => {
  const recalculationText = text.recalculation;
  const project = {
    id: '0199f5a2-7c1e-7d40-9a1b-300000000001',
    code: 'ERP-FIN',
    name: 'Implantação do ERP financeiro',
    items: 1,
  };

  /**
   * The schedules of the simulated installation: the task of ERP-FIN changes while the Dia do Servidor Público is
   * considered and the dates were not recalculated.
   */
  function simulateSchedules() {
    const current = calendar;
    let recalculated = false;
    const pending = () => current.considered.has('PublicServantsDay') && !recalculated;
    server.use(
      http.get('/api/admin/schedule-recalculation', () =>
        HttpResponse.json(
          pending() ? { items: 1, projects: [project] } : { items: 0, projects: [] },
        ),
      ),
      http.post('/api/admin/schedule-recalculation', () => {
        requests.push('POST schedule-recalculation');
        const done = pending() ? { items: 1, projects: [project] } : { items: 0, projects: [] };
        recalculated = true;
        return HttpResponse.json(done);
      }),
    );
  }

  it('Aviso depois de mudar o calendário', async () => {
    simulateSchedules();
    await openCalendar();
    expect(screen.queryByText(recalculationText.title)).not.toBeInTheDocument();

    fireEvent.click(considered(text.holidays.PublicServantsDay));

    expect(await screen.findByText(recalculationText.title)).toBeVisible();
    expect(
      screen.getByText(
        '1 tarefa ou marco de 1 projeto em aberto ainda segue o calendário anterior.',
      ),
    ).toBeVisible();
    expect(screen.getByRole('button', { name: recalculationText.action })).toBeVisible();
  });

  it('Recalcular as datas pela tela', async () => {
    calendar.considered.add('PublicServantsDay');
    simulateSchedules();
    await openCalendar();

    fireEvent.click(await screen.findByRole('button', { name: recalculationText.action }));

    const dialog = await screen.findByRole('dialog', { name: recalculationText.dialog.title });
    const projects = within(dialog).getByRole('list', { name: recalculationText.dialog.projects });
    expect(within(projects).getByText('ERP-FIN')).toBeVisible();
    expect(within(projects).getByText('Implantação do ERP financeiro')).toBeVisible();
    expect(within(projects).getByText('1 tarefa ou marco')).toBeVisible();
    expect(requests).toEqual([]);
    fireEvent.click(within(dialog).getByRole('button', { name: recalculationText.dialog.confirm }));

    expect(await screen.findByText('Datas recalculadas em 1 projeto.')).toBeVisible();
    expect(requests).toEqual(['POST schedule-recalculation']);
    await waitFor(() => {
      expect(screen.queryByText(recalculationText.title)).not.toBeInTheDocument();
    });
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('Cronogramas em dia', async () => {
    await openCalendar();

    expect(screen.queryByText(recalculationText.title)).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: recalculationText.action }),
    ).not.toBeInTheDocument();
  });
});
