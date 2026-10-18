import { Navigate, type RouteObject } from 'react-router';
import { HomePage } from './pages/HomePage';
import { ProjectsPage } from './pages/ProjectsPage';
import { TicketsPage } from './pages/TicketsPage';
import { AppShell } from './shell/AppShell';
import type { PageHandle } from './shell/pages';

// Every screen and its address (ADR-0077). Paths are in English (ADR-0074).
export const routes: RouteObject[] = [
  {
    element: <AppShell />,
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
    ],
  },
  // An address that matches no screen goes to the home page.
  { path: '*', element: <Navigate to="/" replace /> },
];
