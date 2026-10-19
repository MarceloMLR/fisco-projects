import { Navigate, type RouteObject } from 'react-router';
import { AdminLayout } from './admin/AdminLayout';
import { AuditPage } from './admin/AuditPage';
import { CalendarPage } from './admin/CalendarPage';
import { ClientsPage } from './admin/ClientsPage';
import { RequireAdministrator } from './admin/RequireAdministrator';
import { SettingsPage } from './admin/SettingsPage';
import { UsersPage } from './admin/UsersPage';
import { InstallationRoot } from './installation/InstallationRoot';
import { HomePage } from './pages/HomePage';
import { ProjectsPage } from './pages/ProjectsPage';
import { ProfilePage } from './profile/ProfilePage';
import { SignInPage } from './pages/SignInPage';
import { TicketsPage } from './pages/TicketsPage';
import { RequireSession } from './session/RequireSession';
import { AppShell } from './shell/AppShell';
import type { PageHandle } from './shell/pages';

// Every screen and its address (ADR-0077). Paths are in English (ADR-0074).
export const routes: RouteObject[] = [
  // Around every screen: the name of the installation in the tab of the browser.
  {
    element: <InstallationRoot />,
    children: [
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
            handle: {
              navigation: 'projects',
              titleKey: 'pages.projects.title',
            } satisfies PageHandle,
          },
          {
            path: 'tickets',
            element: <TicketsPage />,
            handle: { navigation: 'tickets', titleKey: 'pages.tickets.title' } satisfies PageHandle,
          },
          // My profile is reached from the account menu (and the phone menu); it is no item of the sidebar.
          {
            path: 'profile',
            element: <ProfilePage />,
            handle: { titleKey: 'pages.profile.title' } satisfies PageHandle,
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
              {
                path: 'settings',
                element: <SettingsPage />,
                handle: {
                  navigation: 'admin',
                  titleKey: 'pages.settings.title',
                  trail: [{ titleKey: 'pages.admin.title', path: '/admin' }],
                } satisfies PageHandle,
              },
              {
                path: 'calendar',
                element: <CalendarPage />,
                handle: {
                  navigation: 'admin',
                  titleKey: 'pages.calendar.title',
                  trail: [{ titleKey: 'pages.admin.title', path: '/admin' }],
                } satisfies PageHandle,
              },
              {
                path: 'clients',
                element: <ClientsPage />,
                handle: {
                  navigation: 'admin',
                  titleKey: 'pages.clients.title',
                  trail: [{ titleKey: 'pages.admin.title', path: '/admin' }],
                } satisfies PageHandle,
              },
            ],
          },
        ],
      },
      // An address that matches no screen goes to the home page.
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
];
