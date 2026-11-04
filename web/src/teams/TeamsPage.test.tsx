import { screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { demoTeamsTicket, diego, marina } from '../test/demoTeamsTicket';
import { phoneWidth, renderApp } from '../test/renderApp';
import { defaultInstallation, problem, server, signedOut } from '../test/server';
import type { TeamsHost } from './teamsHost';
import { safeItemPath, schemeOfTeams } from './teamsPage';

// Teams around the page, as each test wants it: null is outside Teams (ADR-0152).
const teams = vi.hoisted(() => ({ host: null as TeamsHost | null }));
vi.mock('./teamsHost', async (importOriginal) => ({
  ...(await importOriginal<typeof import('./teamsHost')>()),
  connectToTeams: () => Promise.resolve(teams.host),
}));

const open = ptBR.teamsPage.open.replace('{{name}}', 'FiscoProjects');

beforeEach(() => {
  // Teams opens the page in its own frame, with no session of the application.
  signedOut();
  teams.host = null;
});

async function openLink(path: string, width?: number) {
  await renderApp(path, width);
  return screen.findByRole('link', { name: open });
}

// Spec: avisos, requirement "Página do FiscoProjects no Teams".
describe('Página do FiscoProjects no Teams', () => {
  it('Abrir o item no navegador', async () => {
    const link = await openLink(`/teams?item=${encodeURIComponent('/tickets/104')}&theme=default`);

    expect(link).toHaveAttribute('href', '/tickets/104');
    expect(link).toHaveAttribute('target', '_blank');
    expect(link).toHaveAttribute('rel', 'noopener noreferrer');
    expect(screen.getByRole('heading', { level: 1, name: 'FiscoProjects' })).toBeInTheDocument();
  });

  it('Página sem item', async () => {
    // Teams leaves the place of the item empty when the person opens the page by its bar.
    const link = await openLink('/teams?item=&theme=dark');

    expect(link).toHaveAttribute('href', '/');
  });

  it('Endereço de fora recusado', async () => {
    const link = await openLink(
      `/teams?item=${encodeURIComponent('https://outro.example/tickets/104')}`,
    );

    expect(link).toHaveAttribute('href', '/');
  });

  it('Marca da instalação e tema do Teams', async () => {
    server.use(
      http.get('/api/installation', () =>
        HttpResponse.json({ ...defaultInstallation, name: 'Projetos da Empresa' }),
      ),
    );
    await renderApp('/teams?item=%2Ftickets%2F104&theme=contrast', phoneWidth);

    const link = await screen.findByRole('link', {
      name: ptBR.teamsPage.open.replace('{{name}}', 'Projetos da Empresa'),
    });
    expect(link.closest('[data-theme]')).toHaveAttribute('data-theme', 'dark');
  });

  it.each([
    ['/tickets/104', '/tickets/104'],
    ['/projects/0193f0a0/schedule?item=0193f0a1', '/projects/0193f0a0/schedule?item=0193f0a1'],
    [null, '/'],
    ['', '/'],
    ['tickets/104', '/'],
    ['//outro.example/tickets', '/'],
    ['/\\outro.example', '/'],
    ['/\t/outro.example', '/'],
    ['javascript:alert(1)', '/'],
    ['/.//outro.example', '/'],
    ['/..//outro.example/tickets', '/'],
    ['/%2e//outro.example', '/'],
    ['https://outro.example', '/'],
  ])('Caminho do item %j', (item, path) => {
    expect(safeItemPath(item)).toBe(path);
  });

  it.each([
    ['default', 'light'],
    ['dark', 'dark'],
    ['contrast', 'dark'],
    [null, undefined],
    ['{app.theme}', undefined],
  ])('Tema do Teams %j', (theme, scheme) => {
    expect(schemeOfTeams(theme)).toBe(scheme);
  });
});

const text = ptBR.teamsPage;
const ticketPath = `/teams?item=${encodeURIComponent('/tickets/6')}&theme=default`;

/** Teams with a token for each time the page asks, and the theme given. */
function insideTeams(tokens: string[] = ['token-1'], scheme: TeamsHost['scheme'] = 'light') {
  const token = vi.fn(() => Promise.resolve(tokens.shift() ?? 'token-sem-fim'));
  teams.host = { token, scheme, onScheme: () => undefined };
  return token;
}

/** The ticket #6 for the token given, and the tokens the reading received. */
function ticketFor(token: string, answers: (() => Response)[] = []) {
  const received: (string | null)[] = [];
  server.use(
    http.get('/api/teams/tickets/:number', ({ request, params }) => {
      received.push(request.headers.get('Authorization'));
      const answer = answers.shift();
      if (answer !== undefined) {
        return answer();
      }
      return request.headers.get('Authorization') === `Bearer ${token}` && params.number === '6'
        ? HttpResponse.json(demoTeamsTicket)
        : problem(401);
    }),
  );
  return received;
}

async function openTicket(width?: number) {
  await renderApp(ticketPath, width);
  return screen.findByRole('heading', { level: 1, name: /Importar XML de entrada/ });
}

const reply = ptBR.teamsPage.ticket.reply.replace('{{name}}', 'FiscoProjects');

// Spec: avisos, requirement "Página do FiscoProjects no Teams" (ADR-0149, ADR-0152).
describe('Página do Teams sem o chamado', () => {
  it('Aviso de outro item no Teams', async () => {
    const token = insideTeams();
    const item = '/projects/0193f0a0/schedule?item=0193f0a1';

    const link = await openLink(`/teams?item=${encodeURIComponent(item)}`);

    expect(link).toHaveAttribute('href', item);
    expect(token).not.toHaveBeenCalled();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('Página do Teams fora do Teams', async () => {
    const link = await openLink(ticketPath);

    expect(link).toHaveAttribute('href', '/tickets/6');
    await waitFor(() => {
      expect(screen.queryByText(text.opening)).not.toBeInTheDocument();
    });
    expect(screen.queryByText(text.failed)).not.toBeInTheDocument();
  });
});

// Spec: avisos, requirement "Chamado na página do Teams" (ADR-0152).
describe('Chamado na página do Teams', () => {
  it('Chamado do aviso no Teams', async () => {
    insideTeams();
    ticketFor('token-1');

    const heading = await openTicket();

    expect(heading).toHaveTextContent('#6');
    expect(screen.getByText('ERP-FIN')).toBeInTheDocument();
    expect(screen.getByText('Implantação do ERP financeiro')).toBeInTheDocument();
    expect(screen.getAllByText(ptBR.tickets.statuses.InProgress).length).toBeGreaterThan(0);
    expect(screen.getAllByText(ptBR.tickets.priorities.High).length).toBeGreaterThan(0);
    const people = screen.getByRole('region', { name: text.ticket.people });
    expect(within(people).getByText(diego.name).closest('span')).toHaveTextContent(
      `${diego.name} ${text.ticket.you}`,
    );
    expect(within(people).getByText(marina.name)).toBeInTheDocument();
    expect(within(people).getByText(text.ticket.manager)).toBeInTheDocument();
    const deadlines = screen.getByRole('region', { name: text.ticket.deadlines });
    expect(within(deadlines).getByText(text.ticket.firstResponse)).toBeInTheDocument();
    expect(within(deadlines).getByText(text.ticket.resolution)).toBeInTheDocument();
    const about = screen.getByRole('region', { name: text.ticket.about });
    expect(within(about).getByText(ptBR.tickets.teams.Development)).toBeInTheDocument();
    expect(
      within(about).getByText(text.ticket.attachmentCount_other.replace('{{count}}', '2')),
    ).toBeInTheDocument();
    expect(screen.getByText('nfe-devolucao-3391.xml').closest('a')).toBeNull();
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
  });

  it('Menção no chamado do Teams', async () => {
    insideTeams();
    ticketFor('token-1');
    await openTicket();

    const message = screen.getByRole('article', {
      name: ptBR.tickets.conversation.message.replace('{{name}}', marina.name),
    });

    expect(within(message).getByText(`@${diego.name}`)).toHaveClass('fp-mention');
    expect(within(message).getByText(ptBR.tickets.roles.Manager)).toBeInTheDocument();
    expect(within(message).getByText('14:59')).toBeInTheDocument();
  });

  it('Interações anteriores no Teams', async () => {
    insideTeams();
    ticketFor('token-1');
    await openTicket();

    const conversation = screen.getByRole('list', { name: text.ticket.conversation });

    expect(within(conversation).getAllByRole('listitem')[0]).toHaveTextContent(
      text.ticket.earlier_other.replace('{{count}}', '10').replace('{{name}}', 'FiscoProjects'),
    );
  });

  it('Responder no FiscoProjects', async () => {
    insideTeams();
    ticketFor('token-1');
    await openTicket();

    const link = screen.getByRole('link', { name: reply });

    expect(link).toHaveAttribute('href', '/tickets/6');
    expect(link).toHaveAttribute('target', '_blank');
    expect(link).toHaveAttribute('rel', 'noopener noreferrer');
  });

  it('Chamado no Teams do celular', async () => {
    insideTeams();
    ticketFor('token-1');

    await openTicket(phoneWidth);

    expect(
      within(screen.getByRole('contentinfo')).getByRole('link', { name: reply }),
    ).toBeInTheDocument();
    expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
    expect(screen.getByText(text.ticket.assignee)).toBeInTheDocument();
    expect(screen.getByRole('list', { name: text.ticket.conversation })).toBeInTheDocument();
  });

  it('Tema escuro do Teams no chamado', async () => {
    insideTeams(['token-1'], 'dark');
    ticketFor('token-1');

    const heading = await openTicket();

    expect(heading.closest('[data-theme]')).toHaveAttribute('data-theme', 'dark');
  });
});

// Spec: avisos, requirement "Chamado que a pessoa não vê no Teams" (ADR-0152).
describe('Chamado que a pessoa não vê no Teams', () => {
  it('Chamado de outro projeto no Teams', async () => {
    insideTeams();
    ticketFor('token-1', [() => problem(404)]);
    await renderApp(ticketPath);

    expect(await screen.findByRole('heading', { name: text.noAccess.title })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: open })).toHaveAttribute('href', '/tickets/6');
    expect(screen.queryByText(demoTeamsTicket.title)).not.toBeInTheDocument();
  });

  it('Falha ao ler o chamado no Teams', async () => {
    insideTeams();
    ticketFor('token-1', [() => problem(500)]);
    await renderApp(ticketPath);

    expect(await screen.findByText(text.failed)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: open })).toHaveAttribute('href', '/tickets/6');
  });
});

// Spec: avisos, requirement "Token do Teams só na memória da página" (ADR-0054, ADR-0152).
describe('Token do Teams só na memória da página', () => {
  it('Token só na memória da página', async () => {
    insideTeams(['token-secreto-do-teams']);
    const received = ticketFor('token-secreto-do-teams');
    const view = await renderApp(ticketPath);
    await screen.findByRole('heading', { level: 1, name: /Importar XML de entrada/ });

    expect(received).toEqual(['Bearer token-secreto-do-teams']);
    const kept = [
      JSON.stringify(Object.entries(localStorage)),
      JSON.stringify(Object.entries(sessionStorage)),
      document.cookie,
      window.location.href,
      view.router.state.location.pathname + view.router.state.location.search,
      JSON.stringify(
        view.queryClient
          .getQueryCache()
          .getAll()
          .map((query) => [query.queryKey, query.state.data]),
      ),
    ];
    for (const place of kept) {
      expect(place).not.toContain('token-secreto-do-teams');
    }
  });

  it('Token recusado uma vez', async () => {
    const token = insideTeams(['token-velho', 'token-novo']);
    const received = ticketFor('token-novo');

    await openTicket();

    expect(token).toHaveBeenCalledTimes(2);
    expect(received).toEqual(['Bearer token-velho', 'Bearer token-novo']);
  });

  it('Token recusado de novo', async () => {
    insideTeams(['token-velho', 'outro-token-velho']);
    const received = ticketFor('token-novo');
    await renderApp(ticketPath);

    await waitFor(() => {
      expect(received).toHaveLength(2);
    });
    expect(await screen.findByRole('link', { name: open })).toHaveAttribute('href', '/tickets/6');
    await waitFor(() => {
      expect(screen.queryByText(text.opening)).not.toBeInTheDocument();
    });
    expect(screen.queryByText(text.failed)).not.toBeInTheDocument();
    expect(screen.queryByText(demoTeamsTicket.title)).not.toBeInTheDocument();
  });
});
