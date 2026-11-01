import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { demoTeamLoad } from '../test/demoTeamLoad';
import { renderApp } from '../test/renderApp';
import { demoPeople, server, signedInAs } from '../test/server';

const text = ptBR.teamLoad;
const asked: (string | null)[] = [];

beforeEach(() => {
  asked.length = 0;
  signedInAs('Administrator');
  server.use(
    http.get('/api/admin/team-load', ({ request }) => {
      const week = new URL(request.url).searchParams.get('week');
      asked.push(week);
      return HttpResponse.json(demoTeamLoad(week ?? '2026-10-19'));
    }),
  );
});

function grid() {
  return screen.getByRole('grid', { name: text.table });
}

/** The cells of each row of the list, below the header. */
function rows(): string[][] {
  return within(grid())
    .queryAllByRole('row')
    .slice(1)
    .map((row) =>
      within(row)
        .queryAllByRole('gridcell')
        .map((cell) => cell.textContent),
    );
}

function headers(): string[] {
  return within(grid())
    .getAllByRole('columnheader')
    .map((header) => header.textContent);
}

async function showTeamLoad() {
  await renderApp('/admin/team-load');
  await waitFor(() => {
    expect(rows()).toHaveLength(5);
  });
}

// Spec: horas-do-projeto, requirements "Carga da equipe pela tela" and "Detalhe da carga pela tela" (ADR-0142).
describe('Carga da equipe pela tela', () => {
  it('Carga da equipe na tela', async () => {
    await showTeamLoad();

    // In Acompanhamento of the Administration, after Horas dos projetos.
    const sections = screen.getByRole('navigation', { name: ptBR.admin.sections.label });
    const links = within(sections).getAllByRole('link');
    expect(links[1]).toHaveTextContent(ptBR.pages.teamLoad.title);
    expect(links[1]).toHaveAttribute('aria-current', 'page');
    expect(
      screen.getByRole('heading', { level: 1, name: ptBR.pages.teamLoad.title }),
    ).toBeVisible();
    expect(screen.getByText('5 pessoas · 1 acima da capacidade')).toBeVisible();
    expect(screen.getByText(text.rule.replace('{{hours}}', '8 h'))).toBeVisible();
    expect(asked).toEqual([null]);
    expect(headers().slice(0, 4)).toEqual([
      text.columns.person,
      '19/10 – 25/10',
      '26/10 – 01/11',
      '02/11 – 08/11',
    ]);
    const [carla, diego, lucas] = rows();
    expect(carla?.slice(0, 4)).toEqual([
      'Carla Mendescarla.mendes@empresa.example',
      '36 de 40 hPerto do limite',
      '12 de 40 hDentro',
      '0 de 32 hLivre',
    ]);
    expect(diego?.[2]).toBe('50 de 24 hAcima');
    expect(lucas?.[1]).toBe('0 de 40 hLivre');
    // The state by icon, color and text, and the days away by an icon with its text.
    const diegoRow = within(grid()).getAllByRole('row')[2];
    if (diegoRow === undefined) {
      throw new Error('No row of Diego.');
    }
    const over = within(diegoRow).getByText(text.states.Over);
    expect(over).toHaveAttribute('data-state', 'Over');
    expect(over).toHaveStyle({ color: 'var(--status-danger-text)' });
    expect(
      within(diegoRow).getByRole('img', { name: text.absenceDays_other.replace('{{count}}', '2') }),
    ).toBeInTheDocument();
  });

  it('Com sobrecarga pela tela', async () => {
    await showTeamLoad();

    fireEvent.click(screen.getByRole('radio', { name: text.views.Overloaded }));

    await waitFor(() => {
      expect(rows()).toHaveLength(1);
    });
    expect(rows()[0]?.[0]).toContain('Diego Torres');
    fireEvent.click(screen.getByRole('radio', { name: text.views.All }));
    fireEvent.change(screen.getByRole('searchbox', { name: text.search }), {
      target: { value: 'marina' },
    });
    await waitFor(() => {
      expect(rows()).toHaveLength(1);
    });
    expect(rows()[0]?.[0]).toContain('Marina Costa');
  });

  it('Próximas semanas pela tela', async () => {
    await showTeamLoad();
    const thisWeek = screen.getByRole('button', { name: text.thisWeek });
    expect(thisWeek).toBeDisabled();

    fireEvent.click(screen.getByRole('button', { name: text.next }));

    await waitFor(() => {
      expect(headers()[1]).toBe('26/10 – 01/11');
    });
    expect(asked.at(-1)).toBe('2026-10-26');
    expect(thisWeek).toBeEnabled();
    fireEvent.click(screen.getByRole('button', { name: text.previous }));
    await waitFor(() => {
      expect(asked.at(-1)).toBe('2026-10-19');
    });
    fireEvent.click(screen.getByRole('button', { name: text.next }));
    await waitFor(() => {
      expect(asked.at(-1)).toBe('2026-10-26');
    });
    fireEvent.click(screen.getByRole('button', { name: text.thisWeek }));
    await waitFor(() => {
      expect(headers()[1]).toBe('19/10 – 25/10');
    });
  });

  it('Detalhe da carga', async () => {
    const { router } = await renderApp('/admin/team-load');
    await waitFor(() => {
      expect(rows()).toHaveLength(5);
    });

    fireEvent.click(screen.getByText('Diego Torres'));

    const drawer = await screen.findByRole('dialog', { name: text.details.label });
    expect(router.state.location.search).toBe(`?person=${demoPeople.diego.id}`);
    expect(within(drawer).getByRole('heading', { level: 2, name: 'Diego Torres' })).toBeVisible();
    const second = within(drawer).getByRole('region', {
      name: text.details.week.replace('{{period}}', '26/10 – 01/11'),
    });
    expect(second).toHaveTextContent('50 de 24 h');
    expect(within(second).getByText(text.states.Over)).toBeVisible();
    expect(second).toHaveTextContent(
      `${text.details.away.replace('{{periods}}', '29/10 a 30/10')} · 2 dias de ausência`,
    );
    const tasks = within(second).getAllByRole('listitem');
    expect(tasks.map((task) => task.textContent)).toEqual([
      'SPED-27Gerar o arquivo do SPED30 h previstas, de 26/10/2026 a 30/10/202630 h na semana',
      'ERP-FINIntegrar emissão de NF-e20 h previstas, de 26/10/2026 a 30/10/202620 h na semana',
    ]);
    const link = within(second).getByRole('link', {
      name: text.details.openTask
        .replace('{{name}}', 'Gerar o arquivo do SPED')
        .replace('{{code}}', 'SPED-27'),
    });
    expect(link).toHaveAttribute(
      'href',
      '/projects/0199f5a2-7c1e-7d40-9a1b-f00000000002/schedule?view=list&item=0199f5a2-7c1e-7d40-9a1b-e00000000003',
    );
    const first = within(drawer).getByRole('region', {
      name: text.details.week.replace('{{period}}', '19/10 – 25/10'),
    });
    expect(first).toHaveTextContent(text.details.noTasks);
  });
});
