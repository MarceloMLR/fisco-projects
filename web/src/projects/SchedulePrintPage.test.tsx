import { screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { renderApp } from '../test/renderApp';
import {
  type DemoProject,
  demoProjects,
  demoScheduleWithBaseline,
  server,
  signedInAs,
} from '../test/server';

const text = ptBR.projects;
let project: DemoProject;

beforeEach(() => {
  const erp = demoProjects()[0];
  if (erp === undefined) {
    throw new Error('No ERP-FIN.');
  }
  project = erp;
  signedInAs('User');
  server.use(
    http.get('/api/projects/:id', () => HttpResponse.json(project)),
    http.get('/api/projects/:id/schedule', () => HttpResponse.json(demoScheduleWithBaseline())),
  );
});

// Spec: cronograma, requirement "Exportar em PDF pela impressão".
describe('Exportar em PDF pela impressão', () => {
  it('Página de impressão', async () => {
    const { container } = await renderApp(
      `/projects/${project.id}/schedule/print?from=2026-10-05&to=2026-10-31&scale=days&baseline=1&critical=0`,
    );

    const table = await screen.findByRole('table', { name: text.print.table });
    // The project, the period and the scale in the header, outside the structure of the screens.
    expect(
      screen.getByRole('heading', {
        name: text.print.title.replace('{{code}}', project.code).replace('{{name}}', project.name),
      }),
    ).toBeVisible();
    expect(screen.getByText(/Período 05\/10 – 31\/10\/2026 · Escala Dias/)).toBeVisible();
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: text.print.print })).toBeVisible();
    // A line for each phase and item, with its dates and progress.
    const rows = within(table).getAllByRole('row');
    expect(rows).toHaveLength(13);
    expect(rows[6]?.textContent).toContain('Parametrizar regras de ICMS13/10/202622/10/202650%');
    // The bars of the tasks in the period, with the baseline under them; the critical path is off.
    await waitFor(() => {
      expect(container.querySelectorAll('rect.g-bar').length).toBe(7);
    });
    expect(container.querySelectorAll('rect.g-baseline').length).toBe(7);
    expect(container.querySelectorAll('.g-bar--critical')).toHaveLength(0);
    // The legend says the baseline, not the critical path.
    const legend = screen.getByRole('list', { name: text.timeline.legend.label });
    expect(within(legend).getByText(text.timeline.legend.baseline)).toBeVisible();
    expect(within(legend).queryByText(text.timeline.legend.critical)).not.toBeInTheDocument();
  });
});
