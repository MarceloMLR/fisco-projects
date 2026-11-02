import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { AuditEntry } from '../api/client';
import ptBR from '../locales/pt-BR.json';
import { expectBusy, heldAnswer } from '../test/busy';
import { renderApp } from '../test/renderApp';
import { auditPage, server, signedInAs } from '../test/server';

const text = ptBR.settings;

/** The rules as the simulated API keeps them, changed by the requests as the real one would. */
let timeZone: string;
let dailySummaryAt: string;
let notificationRetentionDays: number;
let auditRetentionYears: number;
let projectsFolder: string;
let workHoursPerDay: number;
const requests: string[] = [];

beforeEach(() => {
  timeZone = 'America/Sao_Paulo';
  dailySummaryAt = '07:30:00';
  notificationRetentionDays = 90;
  auditRetentionYears = 5;
  projectsFolder = '';
  workHoursPerDay = 8;
  requests.length = 0;
  signedInAs('Administrator');
  server.use(
    http.get('/api/installation/rules', () =>
      HttpResponse.json({
        timeZone,
        dailySummaryAt,
        notificationRetentionDays,
        auditRetentionYears,
        projectsFolder,
        workHoursPerDay,
      }),
    ),
    http.put('/api/installation', async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown> & {
        timeZone: string;
        dailySummaryAt: string;
        notificationRetentionDays: number;
        auditRetentionYears: number;
        workHoursPerDay: number;
      };
      requests.push(
        `PUT ${body.timeZone}${'name' in body || 'projectsFolder' in body ? ' and more' : ''}`,
      );
      timeZone = body.timeZone;
      dailySummaryAt = body.dailySummaryAt;
      notificationRetentionDays = body.notificationRetentionDays;
      auditRetentionYears = body.auditRetentionYears;
      workHoursPerDay = body.workHoursPerDay;
      return new HttpResponse(null, { status: 204 });
    }),
  );
});

async function openSettings() {
  const view = await renderApp('/admin/settings');
  await screen.findByRole('heading', { level: 1, name: ptBR.pages.settings.title });
  await waitFor(() => {
    expect(timeZoneField()).toHaveValue(timeZoneLabel(timeZone));
  });
  return view;
}

function saveButton() {
  return screen.getByRole('button', { name: text.save });
}

function discardButton() {
  return screen.getByRole('button', { name: text.discard });
}

function timeZoneField() {
  return screen.getByRole('combobox', { name: text.timeZone });
}

/** The name the browser gives a time zone, with its id, as the field shows it. */
function timeZoneLabel(id: string) {
  const name = new Intl.DateTimeFormat('pt-BR', { timeZone: id, timeZoneName: 'longGeneric' })
    .formatToParts(new Date())
    .find((part) => part.type === 'timeZoneName')?.value;
  return `${name ?? ''} (${id})`;
}

async function chooseTimeZone(id: string) {
  // As a person does: into the field, then typing; outside it, the field shows the chosen one again.
  timeZoneField().focus();
  fireEvent.change(timeZoneField(), { target: { value: id } });
  fireEvent.click(await screen.findByRole('option', { name: timeZoneLabel(id) }));
}

function summaryField() {
  return within(screen.getByRole('region', { name: text.rules })).getByLabelText(
    text.dailySummaryAt,
  );
}

// Spec: configuracao-da-instalacao, requirement "Tela Configurações gerais".
describe('Tela Configurações gerais', () => {
  it('Configurações gerais na Administração', async () => {
    await renderApp('/admin/users');
    const sections = screen.getByRole('navigation', { name: ptBR.admin.sections.label });

    fireEvent.click(within(sections).getByRole('link', { name: ptBR.pages.settings.title }));

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.settings.title }),
    ).toBeInTheDocument();
    const trail = screen.getByRole('navigation', { name: ptBR.shell.breadcrumb });
    expect(within(trail).getByText(ptBR.pages.admin.title)).toBeInTheDocument();
    expect(screen.getByText(ptBR.admin.sections.installation)).toBeInTheDocument();
    const rules = screen.getByRole('region', { name: text.rules });
    await waitFor(() => {
      expect(within(rules).getByRole('combobox', { name: text.timeZone })).toHaveValue(
        timeZoneLabel('America/Sao_Paulo'),
      );
    });
    expect(screen.getByRole('region', { name: text.m365 })).toBeInTheDocument();
    // The brand is in Visual and the folder of the projects in Default folders (ADR-0150).
    expect(screen.queryByRole('region', { name: ptBR.brand.title })).toBeNull();
    expect(screen.queryByRole('textbox', { name: ptBR.brand.name })).toBeNull();
    expect(
      screen.queryByRole('textbox', { name: ptBR.defaultFolders.projectsFolder.field }),
    ).toBeNull();
    expect(saveButton()).toBeDisabled();
    expect(discardButton()).toBeDisabled();
  });

  it('Prévia antes de salvar', async () => {
    await openSettings();

    expect(screen.queryByRole('complementary')).toBeNull();
    expect(screen.queryByTestId('colors-preview-app')).toBeNull();
    expect(screen.queryByTestId('logo-file')).toBeNull();
  });

  it('Descartar alterações', async () => {
    await openSettings();
    await chooseTimeZone('Asia/Tokyo');
    fireEvent.change(summaryField(), { target: { value: '08:00' } });
    expect(saveButton()).toBeEnabled();

    fireEvent.click(discardButton());

    expect(timeZoneField()).toHaveValue(timeZoneLabel('America/Sao_Paulo'));
    expect(summaryField()).toHaveValue('07:30');
    await waitFor(() => {
      expect(saveButton()).toBeDisabled();
    });
    expect(discardButton()).toBeDisabled();
    expect(requests).toEqual([]);
  });
});

// Spec: configuracao-da-instalacao, requirement "Mudança das configurações na auditoria".
describe('Configurações na auditoria', () => {
  beforeEach(() => {
    // 12:00:30 of October 19th in the time zone of the tests; only the date is simulated, timers stay real.
    vi.useFakeTimers({ toFake: ['Date'], now: new Date('2026-10-19T15:00:30Z') });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('Configurações no filtro da Auditoria', async () => {
    const entries: AuditEntry[] = [
      {
        id: '0199f5a2-7c1e-7d40-9a1b-200000000001',
        occurredAt: '2026-10-19T13:20:00Z',
        actor: { id: '0199f5a2-7c1e-7d40-9a1b-000000000001', name: 'Paulo Nogueira' },
        action: 'Updated',
        itemType: 'InstallationSettings',
        itemId: '0199f2a0-6b5c-7d1e-9a42-3c8e5f7a1b20',
        itemLabel: null,
        detail: null,
        changes: [
          {
            field: 'Name',
            before: 'FiscoProjects',
            after: 'Consultoria Fictícia',
            secret: false,
            reference: null,
          },
        ],
      },
    ];
    const asked: (string | null)[] = [];
    server.use(
      http.get('/api/audit', ({ request }) => {
        const url = new URL(request.url);
        asked.push(url.searchParams.get('itemType'));
        return HttpResponse.json(auditPage(entries, url));
      }),
    );
    await renderApp('/admin/audit');

    fireEvent.mouseDown(screen.getByRole('combobox', { name: ptBR.audit.filters.type }));
    fireEvent.click(
      await screen.findByRole('option', { name: ptBR.audit.itemTypes.InstallationSettings }),
    );

    await waitFor(() => {
      expect(asked.at(-1)).toBe('InstallationSettings');
    });
    const grid = screen.getByRole('grid', { name: ptBR.audit.table });
    expect(
      await within(grid).findByText(ptBR.audit.actions.InstallationSettings.Updated),
    ).toBeInTheDocument();
    expect(
      within(grid).getByText(
        `${ptBR.audit.fields.InstallationSettings.Name.toLowerCase()} FiscoProjects → Consultoria Fictícia`,
      ),
    ).toBeInTheDocument();
  });
});

// Spec: configuracao-da-instalacao, requirement "Fuso da instalação" (General settings).
describe('Fuso nas Configurações gerais', () => {
  it('Fuso padrão', async () => {
    await openSettings();

    const rules = screen.getByRole('region', { name: text.rules });
    expect(within(rules).getByRole('combobox', { name: text.timeZone })).toHaveValue(
      timeZoneLabel('America/Sao_Paulo'),
    );
    expect(timeZoneLabel('America/Sao_Paulo')).toMatch(/Brasília \(America\/Sao_Paulo\)$/);
  });

  it('Trocar o fuso', async () => {
    await openSettings();

    await chooseTimeZone('Asia/Tokyo');
    expect(saveButton()).toBeEnabled();
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT Asia/Tokyo']);
    await waitFor(() => {
      expect(timeZoneField()).toHaveValue(timeZoneLabel('Asia/Tokyo'));
    });
    await waitFor(() => {
      expect(saveButton()).toBeDisabled();
    });
  });

  it('Descartar o fuso', async () => {
    await openSettings();
    await chooseTimeZone('Asia/Tokyo');

    fireEvent.click(discardButton());

    expect(timeZoneField()).toHaveValue(timeZoneLabel('America/Sao_Paulo'));
    await waitFor(() => {
      expect(saveButton()).toBeDisabled();
    });
  });

  it('Fuso recusado pela API', async () => {
    server.use(
      http.put('/api/installation', () =>
        HttpResponse.json(
          { status: 400, title: 'Problem', errors: { timeZone: ['Choose an IANA time zone.'] } },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    await openSettings();
    await chooseTimeZone('Asia/Tokyo');

    fireEvent.click(saveButton());

    expect(await screen.findByText(text.errors.timeZoneInvalid)).toBeInTheDocument();
    expect(timeZoneField()).toHaveAttribute('aria-invalid', 'true');
    expect(screen.queryByText(text.saved)).not.toBeInTheDocument();
  });

  it('Fuso obrigatório', async () => {
    await openSettings();

    timeZoneField().focus();
    fireEvent.change(timeZoneField(), { target: { value: '' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.errors.timeZoneRequired)).toBeInTheDocument();
    expect(requests).toEqual([]);
  });
});

// Colors as the Administrator types them, without "#": the colors in the code come only from tokens.json.
// Spec: configuracao-da-instalacao, requirement "Horário do resumo diário".
describe('Horário do resumo diário', () => {
  it('Horário padrão', async () => {
    await openSettings();

    await waitFor(() => {
      expect(summaryField()).toHaveValue('07:30');
    });
  });

  it('Trocar o horário', async () => {
    await openSettings();
    await waitFor(() => {
      expect(summaryField()).toHaveValue('07:30');
    });

    fireEvent.change(summaryField(), { target: { value: '08:00' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(dailySummaryAt).toBe('08:00:00');
  });

  it('Horário inválido', async () => {
    await openSettings();

    fireEvent.change(summaryField(), { target: { value: '' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.errors.dailySummaryRequired)).toBeInTheDocument();
    expect(requests).toEqual([]);
  });
});

// Spec: configuracao-da-instalacao, requirement "Guarda das notificações na instalação".
describe('Guarda das notificações na instalação', () => {
  function retentionField() {
    return within(screen.getByRole('region', { name: text.rules })).getByRole('spinbutton', {
      name: text.notificationRetention,
    });
  }

  it('Guarda padrão', async () => {
    await openSettings();

    await waitFor(() => {
      expect(retentionField()).toHaveValue(90);
    });
  });

  it('Guarda inválida', async () => {
    await openSettings();
    await waitFor(() => {
      expect(retentionField()).toHaveValue(90);
    });

    fireEvent.change(retentionField(), { target: { value: '0' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.errors.retentionRange)).toBeInTheDocument();
    expect(requests).toEqual([]);
  });

  it('Trocar a guarda', async () => {
    await openSettings();
    await waitFor(() => {
      expect(retentionField()).toHaveValue(90);
    });

    fireEvent.change(retentionField(), { target: { value: '30' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(notificationRetentionDays).toBe(30);
  });
});

// Spec: dados-pessoais, requirement "Prazo da auditoria" (ADR-0058, ADR-0110).
describe('Prazo da auditoria', () => {
  function auditRetentionField() {
    return within(screen.getByRole('region', { name: text.rules })).getByRole('spinbutton', {
      name: text.auditRetention,
    });
  }

  it('Prazo da auditoria', async () => {
    await openSettings();
    await waitFor(() => {
      expect(auditRetentionField()).toHaveValue(5);
    });

    fireEvent.change(auditRetentionField(), { target: { value: '11' } });
    fireEvent.click(saveButton());
    expect(await screen.findByText(text.errors.auditRetentionRange)).toBeInTheDocument();
    expect(requests).toEqual([]);

    fireEvent.change(auditRetentionField(), { target: { value: '7' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(auditRetentionYears).toBe(7);
  });

  it('Prazo recusado pela API', async () => {
    server.use(
      http.put('/api/installation', () =>
        HttpResponse.json(
          {
            type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
            title: 'One or more validation errors occurred.',
            status: 400,
            errors: { auditRetentionYears: ['Keep the audit from 1 to 10 years.'] },
          },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    await openSettings();
    await waitFor(() => {
      expect(auditRetentionField()).toHaveValue(5);
    });

    fireEvent.change(auditRetentionField(), { target: { value: '3' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.errors.auditRetentionRange)).toBeInTheDocument();
  });

  // Spec: configuracao-da-instalacao, requirement "Horas por dia de trabalho" (the screen, ADR-0142).
  function workHoursField() {
    return within(screen.getByRole('region', { name: text.rules })).getByRole('textbox', {
      name: text.workHoursPerDay,
    });
  }

  it('Horas por dia pela tela', async () => {
    await openSettings();
    await waitFor(() => {
      expect(workHoursField()).toHaveValue('8');
    });

    fireEvent.change(workHoursField(), { target: { value: '7,3' } });
    fireEvent.click(saveButton());
    expect(await screen.findByText(text.errors.workHoursRange)).toBeInTheDocument();
    expect(requests).toEqual([]);

    fireEvent.change(workHoursField(), { target: { value: '7,5' } });
    fireEvent.click(saveButton());

    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(workHoursPerDay).toBe(7.5);
    await waitFor(() => {
      expect(workHoursField()).toHaveValue('7,5');
    });
  });
});

// Spec: estrutura-das-telas, requirement "Botões em andamento" (ADR-0131).
describe('Botões em andamento nas Configurações gerais', () => {
  it('Formulário em andamento ao salvar', async () => {
    const held = heldAnswer();
    // Held first, then the saving of the simulated API.
    server.use(
      http.put('/api/installation', async () => {
        await held.until;
      }),
    );
    await openSettings();
    fireEvent.change(summaryField(), { target: { value: '08:00' } });
    // Salvar sits in the bar under the page, outside the form, and is busy as well.
    const save = saveButton();

    fireEvent.click(save);

    await waitFor(() => {
      expectBusy(save, text.save);
    });
    fireEvent.click(save);
    held.release();
    expect(await screen.findByText(text.saved)).toBeInTheDocument();
    expect(requests).toEqual(['PUT America/Sao_Paulo']);
  });
});
