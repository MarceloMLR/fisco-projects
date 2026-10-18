import { Navigate, type RouteObject } from 'react-router';
import { AdminLayout } from './admin/AdminLayout';
import { AuditPage } from './admin/AuditPage';
import { RequireAdministrator } from './admin/RequireAdministrator';
import { UsersPage } from './admin/UsersPage';
import { HomePage } from './pages/HomePage';
import { ProjectsPage } from './pages/ProjectsPage';
import { SignInPage } from './pages/SignInPage';
import { TicketsPage } from './pages/TicketsPage';
import { RequireSession } from './session/RequireSession';
import { AppShell } from './shell/AppShell';
import type { PageHandle } from './shell/pages';

// Every screen and its address (ADR-0077). Paths are in English (ADR-0074).
export const routes: RouteObject[] = [
  // The sign-in page is outside the shell; every other screen needs a session (ADR-0009).
  { path: 'sign-in', element: <SignInPage /> },
  {
    element: (
      <RequireSession>
        <AppShell />
      </RequireSession>
    ),
    children: [
      {
        index: true,
        element: <HomePage />,
        handle: { navigation: 'home', titleKey: 'pages.home.title' } satisfies PageHandle,
      },
      {
        path: 'projects',
        element: <ProjectsPage />,
        handle: { navigation: 'projects', titleKey: 'pages.projects.title' } satisfies PageHandle,
      },
      {
        path: 'tickets',
        element: <TicketsPage />,
        handle: { navigation: 'tickets', titleKey: 'pages.tickets.title' } satisfies PageHandle,
      },
      {
        path: 'admin',
        element: (
          <RequireAdministrator>
            <AdminLayout />
          </RequireAdministrator>
        ),
        handle: { navigation: 'admin', titleKey: 'pages.admin.title' } satisfies PageHandle,
        children: [
          { index: true, element: <Navigate to="users" replace /> },
          {
            path: 'users',
            element: <UsersPage />,
            handle: {
              navigation: 'admin',
              titleKey: 'pages.users.title',
              trail: [{ titleKey: 'pages.admin.title', path: '/admin' }],
            } satisfies PageHandle,
          },
          {
            path: 'audit',
            element: <AuditPage />,
            handle: {
              navigation: 'admin',
              titleKey: 'pages.audit.title',
              trail: [{ titleKey: 'pages.admin.title', path: '/admin' }],
            } satisfies PageHandle,
          },
        ],
      },
    ],
  },
  // An address that matches no screen goes to the home page.
  { path: '*', element: <Navigate to="/" replace /> },
];
