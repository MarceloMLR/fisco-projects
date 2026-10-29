import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import ptBR from '../locales/pt-BR.json';
import { phoneWidth, renderApp } from '../test/renderApp';
import { defaultInstallation, signedInAs } from '../test/server';

const navigationName = ptBR.shell.mainNavigation;

function sidebarItems() {
  const sidebar = screen.getByRole('navigation', { name: navigationName });
  return within(sidebar)
    .getAllByRole('link')
    .map((link) => link.textContent);
}

// Spec: estrutura-das-telas.
describe('Barra lateral no computador', () => {
  it('Administração na barra lateral', async () => {
    signedInAs('Administrator');
    const admin = await renderApp('/');
    await waitFor(() => {
      expect(sidebarItems()).toEqual([
        defaultInstallation.name,
        ptBR.nav.home,
        ptBR.nav.projects,
        ptBR.nav.tickets,
        ptBR.nav.hours,
        ptBR.nav.admin,
      ]);
    });
    admin.unmount();

    for (const role of ['ProjectManager', 'User'] as const) {
      signedInAs(role);
      const view = await renderApp('/');
      expect(sidebarItems(), role).not.toContain(ptBR.nav.admin);
      view.unmount();
    }
  });

  it('Administração no menu do celular', async () => {
    signedInAs('Administrator');
    await renderApp('/', phoneWidth);

    fireEvent.click(screen.getByRole('button', { name: ptBR.shell.openMenu }));
    const menu = await screen.findByRole('dialog', { name: ptBR.shell.menu });

    expect(within(menu).getByRole('link', { name: ptBR.nav.admin })).toBeInTheDocument();
  });
});

describe('Cabeçalho com a trilha', () => {
  it('Trilha de uma página dentro de uma área', async () => {
    signedInAs('Administrator');
    const { router } = await renderApp('/admin/users');

    const trail = screen.getByRole('navigation', { name: ptBR.shell.breadcrumb });
    const levels = within(trail).getAllByRole('listitem');
    expect(levels.map((level) => level.textContent)).toEqual([
      ptBR.pages.admin.title,
      ptBR.pages.users.title,
    ]);
    expect(within(levels[1] ?? trail).getByText(ptBR.pages.users.title)).toHaveAttribute(
      'aria-current',
      'page',
    );
    const area = within(trail).getByRole('link', { name: ptBR.pages.admin.title });
    expect(area).toHaveAttribute('href', '/admin');

    fireEvent.click(area);
    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/admin/users');
    });
    const sidebar = screen.getByRole('navigation', { name: navigationName });
    expect(within(sidebar).getByRole('link', { name: ptBR.nav.admin })).toHaveAttribute(
      'aria-current',
      'page',
    );
  });
});

// Spec: usuarios-e-papeis, requirement "Administração só do administrador".
describe('Administração só do administrador', () => {
  it('Endereço da Administração sem ser administrador', async () => {
    for (const role of ['ProjectManager', 'User'] as const) {
      signedInAs(role);
      const { router, unmount } = await renderApp('/admin/users');

      expect(
        await screen.findByRole('heading', { level: 1, name: ptBR.pages.home.title }),
        role,
      ).toBeInTheDocument();
      expect(router.state.location.pathname, role).toBe('/');
      expect(sidebarItems(), role).not.toContain(ptBR.nav.admin);
      unmount();
    }
  });
});

describe('Área de Administração', () => {
  it('Navegação das seções', async () => {
    signedInAs('Administrator');
    await renderApp('/admin/users');

    const sections = screen.getByRole('navigation', { name: ptBR.admin.sections.label });
    expect(within(sections).getByText(ptBR.admin.sections.people)).toBeInTheDocument();
    const users = within(sections).getByRole('link', { name: new RegExp(ptBR.pages.users.title) });
    expect(users).toHaveAttribute('aria-current', 'page');
    await waitFor(() => {
      expect(users).toHaveTextContent(`${ptBR.pages.users.title}5`);
    });
  });

  it('Seção Auditoria', async () => {
    signedInAs('Administrator');
    await renderApp('/admin/audit');

    const sections = screen.getByRole('navigation', { name: ptBR.admin.sections.label });
    const audit = within(sections).getByRole('link', { name: ptBR.pages.audit.title });
    expect(audit).toHaveAttribute('href', '/admin/audit');
    expect(audit).toHaveAttribute('aria-current', 'page');
    expect(audit).toHaveTextContent(new RegExp(`^${ptBR.pages.audit.title}$`));
    const trail = screen.getByRole('navigation', { name: ptBR.shell.breadcrumb });
    expect(
      within(trail)
        .getAllByRole('listitem')
        .map((level) => level.textContent),
    ).toEqual([ptBR.pages.admin.title, ptBR.pages.audit.title]);
  });

  it('Endereço da Auditoria sem ser administrador', async () => {
    signedInAs('ProjectManager');
    const { router } = await renderApp('/admin/audit');

    expect(
      await screen.findByRole('heading', { level: 1, name: ptBR.pages.home.title }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/');
  });

  it('Área no celular sem a coluna das seções', async () => {
    signedInAs('Administrator');
    await renderApp('/admin/users', phoneWidth);

    expect(
      screen.queryByRole('navigation', { name: ptBR.admin.sections.label }),
    ).not.toBeInTheDocument();
    expect(screen.getByRole('banner')).toHaveTextContent(ptBR.pages.users.title);
  });
});
