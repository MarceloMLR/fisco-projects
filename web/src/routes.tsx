import { Navigate, type RouteObject } from 'react-router';
import { AdminLayout } from './admin/AdminLayout';
import { AuditPage } from './admin/AuditPage';
import { CalendarPage } from './admin/CalendarPage';
import { DefaultFoldersPage } from './admin/DefaultFoldersPage';
import { ClientsPage } from './admin/ClientsPage';
import { TemplatesPage } from './admin/TemplatesPage';
import { VendorsPage } from './admin/VendorsPage';
import { RequireAdministrator } from './admin/RequireAdministrator';
import { SettingsPage } from './admin/SettingsPage';
import { UsersPage } from './admin/UsersPage';
import { ProjectDocumentsPage } from './documents/ProjectDocumentsPage';
import { ProjectLinksPage } from './links/ProjectLinksPage';
import { LinkCategoriesPage } from './admin/LinkCategoriesPage';
import { InstallationRoot } from './installation/InstallationRoot';
import { HomePage } from './pages/HomePage';
import { NewProjectPage } from './projects/NewProjectPage';
import { ProjectLayout } from './projects/ProjectLayout';
import { ProjectMembersPage } from './projects/ProjectMembersPage';
import { ProjectSchedulePage } from './projects/ProjectSchedulePage';
import { SchedulePrintPage } from './projects/SchedulePrintPage';
import { ProjectOverview } from './projects/ProjectOverview';
import { ProjectsPage } from './projects/ProjectsPage';
import { RequireProjectCreator } from './projects/RequireProjectCreator';
import { ProfilePage } from './profile/ProfilePage';
import { SignInPage } from './pages/SignInPage';
import { RequireSession } from './session/RequireSession';
import { AppShell } from './shell/AppShell';
import { OpenTicketPage } from './tickets/OpenTicketPage';
import { ProjectTicketsPage } from './tickets/ProjectTicketsPage';
import { TicketFlowPage } from './tickets/TicketFlowPage';
import { TicketDeadlinesPage } from './admin/TicketDeadlinesPage';
import { TicketPage } from './tickets/TicketPage';
import { TicketsPage } from './tickets/TicketsPage';
import type { PageHandle } from './shell/pages';

// Every screen and its address (ADR-0077). Paths are in English (ADR-0074).
export const routes: RouteObject[] = [
  // Around every screen: the name of the installation in the tab of the browser.
  {
    element: <InstallationRoot />,
    children: [
      // The sign-in page is outside the shell; every other screen needs a session (ADR-0009).
      { path: 'sign-in', element: <SignInPage /> },
      // The print page of a schedule, for paper, outside the shell (ADR-0021).
      {
        path: 'projects/:projectId/schedule/print',
        element: (
          <RequireSession>
            <SchedulePrintPage />
          </RequireSession>
        ),
      },
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
            path: 'projects/new',
            element: (
              <RequireProjectCreator>
                <NewProjectPage />
              </RequireProjectCreator>
            ),
            handle: {
              navigation: 'projects',
              titleKey: 'pages.newProject.title',
              trail: [{ titleKey: 'pages.projects.title', path: '/projects' }],
            } satisfies PageHandle,
          },
          // The page of a project, with a tab at each address (ProjectLayout).
          {
            path: 'projects/:projectId',
            element: <ProjectLayout />,
            children: [
              {
                index: true,
                element: <ProjectOverview />,
                handle: {
                  navigation: 'projects',
                  titleKey: 'pages.project.title',
                  trail: [
                    { titleKey: 'pages.projects.title', path: '/projects' },
                    { project: true },
                  ],
                } satisfies PageHandle,
              },
              {
                path: 'schedule',
                element: <ProjectSchedulePage />,
                handle: {
                  navigation: 'projects',
                  titleKey: 'pages.projectSchedule.title',
                  trail: [
                    { titleKey: 'pages.projects.title', path: '/projects' },
                    { project: true },
                  ],
                } satisfies PageHandle,
              },
              {
                path: 'tickets',
                element: <ProjectTicketsPage />,
                handle: {
                  navigation: 'projects',
                  titleKey: 'pages.projectTickets.title',
                  trail: [
                    { titleKey: 'pages.projects.title', path: '/projects' },
                    { project: true },
                  ],
                } satisfies PageHandle,
              },
              {
                path: 'documents',
                element: <ProjectDocumentsPage />,
                handle: {
                  navigation: 'projects',
                  titleKey: 'pages.projectDocuments.title',
                  trail: [
                    { titleKey: 'pages.projects.title', path: '/projects' },
                    { project: true },
                  ],
                } satisfies PageHandle,
              },
              {
                path: 'links',
                element: <ProjectLinksPage />,
                handle: {
                  navigation: 'projects',
                  titleKey: 'pages.projectLinks.title',
                  trail: [
                    { titleKey: 'pages.projects.title', path: '/projects' },
                    { project: true },
                  ],
                } satisfies PageHandle,
              },
              {
                path: 'members',
                element: <ProjectMembersPage />,
                handle: {
                  navigation: 'projects',
                  titleKey: 'pages.projectMembers.title',
                  trail: [
                    { titleKey: 'pages.projects.title', path: '/projects' },
                    { project: true },
                  ],
                } satisfies PageHandle,
              },
            ],
          },
          {
            path: 'projects/:projectId/tickets/new',
            element: <OpenTicketPage />,
            handle: {
              navigation: 'projects',
              titleKey: 'pages.newTicket.title',
              trail: [{ titleKey: 'pages.projects.title', path: '/projects' }, { project: true }],
            } satisfies PageHandle,
          },
          {
            path: 'tickets',
            element: <TicketsPage />,
            handle: { navigation: 'tickets', titleKey: 'pages.tickets.title' } satisfies PageHandle,
          },
          {
            path: 'tickets/new',
            element: <OpenTicketPage />,
            handle: {
              navigation: 'tickets',
              titleKey: 'pages.newTicket.title',
              trail: [{ titleKey: 'pages.tickets.title', path: '/tickets' }],
            } satisfies PageHandle,
          },
          {
            path: 'tickets/flow',
            element: <TicketFlowPage />,
            handle: {
              navigation: 'tickets',
              titleKey: 'pages.ticketFlow.title',
              trail: [{ titleKey: 'pages.tickets.title', path: '/tickets' }],
            } satisfies PageHandle,
          },
          {
            path: 'tickets/:number',
            element: <TicketPage />,
            handle: {
              navigation: 'tickets',
              titleKey: 'pages.ticket.title',
              trail: [{ titleKey: 'pages.tickets.title', path: '/tickets' }],
            } satisfies PageHandle,
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
              {
                path: 'ticket-deadlines',
                element: <TicketDeadlinesPage />,
                handle: {
                  navigation: 'admin',
                  titleKey: 'pages.ticketDeadlines.title',
                  trail: [{ titleKey: 'pages.admin.title', path: '/admin' }],
                } satisfies PageHandle,
              },
              {
                path: 'templates',
                element: <TemplatesPage />,
                handle: {
                  navigation: 'admin',
                  titleKey: 'pages.templates.title',
                  trail: [{ titleKey: 'pages.admin.title', path: '/admin' }],
                } satisfies PageHandle,
              },
              {
                path: 'document-folders',
                element: <DefaultFoldersPage />,
                handle: {
                  navigation: 'admin',
                  titleKey: 'pages.defaultFolders.title',
                  trail: [{ titleKey: 'pages.admin.title', path: '/admin' }],
                } satisfies PageHandle,
              },
              {
                path: 'link-categories',
                element: <LinkCategoriesPage />,
                handle: {
                  navigation: 'admin',
                  titleKey: 'pages.linkCategories.title',
                  trail: [{ titleKey: 'pages.admin.title', path: '/admin' }],
                } satisfies PageHandle,
              },
              {
                path: 'vendors',
                element: <VendorsPage />,
                handle: {
                  navigation: 'admin',
                  titleKey: 'pages.vendors.title',
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
