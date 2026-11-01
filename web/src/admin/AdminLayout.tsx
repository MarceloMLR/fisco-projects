import BarChartOutlined from '@mui/icons-material/BarChartOutlined';
import BusinessOutlined from '@mui/icons-material/BusinessOutlined';
import CalendarTodayOutlined from '@mui/icons-material/CalendarTodayOutlined';
import FolderOutlined from '@mui/icons-material/FolderOutlined';
import GroupOutlined from '@mui/icons-material/GroupOutlined';
import HistoryOutlined from '@mui/icons-material/HistoryOutlined';
import HourglassEmptyOutlined from '@mui/icons-material/HourglassEmptyOutlined';
import InboxOutlined from '@mui/icons-material/InboxOutlined';
import LinkOutlined from '@mui/icons-material/LinkOutlined';
import ScheduleOutlined from '@mui/icons-material/ScheduleOutlined';
import SettingsOutlined from '@mui/icons-material/SettingsOutlined';
import TimelineOutlined from '@mui/icons-material/TimelineOutlined';
import Box from '@mui/material/Box';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { NavLink, Outlet } from 'react-router';
import {
  allTemplatesQuery,
  allVendorsQuery,
  clientsQuery,
  defaultFoldersQuery,
  linkCategoriesQuery,
  usersQuery,
} from '../api/queries';
import { theme } from '../theme/theme';
import type { AdminContext } from './adminContext';
import { structureRoutes, usePageMotion } from '../shell/pageMotion';

const sections = [
  {
    labelKey: 'admin.sections.monitoring',
    items: [
      {
        path: '/admin/project-hours',
        labelKey: 'pages.projectHours.title',
        icon: ScheduleOutlined,
        count: null,
      },
      {
        path: '/admin/team-load',
        labelKey: 'pages.teamLoad.title',
        icon: BarChartOutlined,
        count: null,
      },
    ],
  },
  {
    labelKey: 'admin.sections.people',
    items: [
      { path: '/admin/users', labelKey: 'pages.users.title', icon: GroupOutlined, count: 'users' },
      {
        path: '/admin/audit',
        labelKey: 'pages.audit.title',
        icon: HistoryOutlined,
        count: null,
      },
    ],
  },
  {
    labelKey: 'admin.sections.installation',
    items: [
      {
        path: '/admin/settings',
        labelKey: 'pages.settings.title',
        icon: SettingsOutlined,
        count: null,
      },
      {
        path: '/admin/calendar',
        labelKey: 'pages.calendar.title',
        icon: CalendarTodayOutlined,
        count: null,
      },
      {
        path: '/admin/ticket-deadlines',
        labelKey: 'pages.ticketDeadlines.title',
        icon: HourglassEmptyOutlined,
        count: null,
      },
    ],
  },
  {
    labelKey: 'admin.sections.records',
    items: [
      {
        path: '/admin/clients',
        labelKey: 'pages.clients.title',
        icon: BusinessOutlined,
        count: 'clients',
      },
      {
        path: '/admin/vendors',
        labelKey: 'pages.vendors.title',
        icon: InboxOutlined,
        count: 'vendors',
      },
      {
        path: '/admin/templates',
        labelKey: 'pages.templates.title',
        icon: TimelineOutlined,
        count: 'templates',
      },
      {
        path: '/admin/document-folders',
        labelKey: 'pages.defaultFolders.title',
        icon: FolderOutlined,
        count: 'defaultFolders',
      },
      {
        path: '/admin/link-categories',
        labelKey: 'pages.linkCategories.title',
        icon: LinkOutlined,
        count: 'linkCategories',
      },
    ],
  },
] as const;

/**
 * Administration area, as in the canvas: the sections in a column on the left (fp-nav2 in a fp-pane--nav) and the
 * section on the right. Each section joins it with its own change. On the phone the column is left out, and each
 * section is reached from the address; the canvas has no phone version of the Administration.
 */
export function AdminLayout() {
  const { t } = useTranslation();
  // The section comes in on a change of section; the column of the sections stays (ADR-0133).
  const section = usePageMotion<HTMLDivElement>(structureRoutes.admin);
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  // Only the counts of the people and of the clients, for their sections and the titles of their pages.
  const usersTotal = useQuery(usersQuery({ pageSize: 1 })).data?.total ?? null;
  const clientsTotal = useQuery(clientsQuery({ pageSize: 1 })).data?.total ?? null;

  const templatesTotal = useQuery(allTemplatesQuery()).data?.length ?? null;
  const vendorsTotal = useQuery(allVendorsQuery()).data?.length ?? null;
  const defaultFoldersTotal = useQuery(defaultFoldersQuery()).data?.length ?? null;
  const linkCategoriesTotal = useQuery(linkCategoriesQuery()).data?.length ?? null;

  const context: AdminContext = { usersTotal, clientsTotal };
  const counts = {
    users: usersTotal,
    clients: clientsTotal,
    templates: templatesTotal,
    vendors: vendorsTotal,
    defaultFolders: defaultFoldersTotal,
    linkCategories: linkCategoriesTotal,
  };

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'grid',
        gridTemplateColumns: desktop ? '240px minmax(0, 1fr)' : 'minmax(0, 1fr)',
        overflow: 'hidden',
      }}
    >
      {desktop && (
        <Box
          component="nav"
          aria-label={t('admin.sections.label')}
          sx={{
            minHeight: 0,
            boxSizing: 'border-box',
            padding: 'var(--space-5) var(--space-3)',
            backgroundColor: 'var(--bg-subtle)',
            borderRight: '1px solid var(--border)',
            overflow: 'auto',
          }}
        >
          <Box
            component="ul"
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: '2px',
              margin: 0,
              padding: 0,
              listStyle: 'none',
            }}
          >
            {sections.map((section) => [
              <Box
                component="li"
                key={section.labelKey}
                aria-hidden="true"
                sx={{
                  padding: 'var(--space-4) var(--space-3) var(--space-1)',
                  fontSize: 12,
                  lineHeight: '16px',
                  fontWeight: 500,
                  color: 'var(--text-secondary)',
                  '&:first-of-type': { paddingTop: 0 },
                }}
              >
                {t(section.labelKey)}
              </Box>,
              ...section.items.map(({ path, labelKey, icon: Icon, count }) => (
                <li key={path}>
                  <Box
                    component={NavLink}
                    to={path}
                    sx={{
                      display: 'flex',
                      boxSizing: 'border-box',
                      alignItems: 'center',
                      gap: 'var(--space-2)',
                      minHeight: 36,
                      padding: '0 var(--space-3)',
                      borderRadius: 'var(--radius-md)',
                      color: 'var(--text)',
                      textDecoration: 'none',
                      '& > svg': { fontSize: 18, color: 'var(--text-secondary)' },
                      '&:hover': { backgroundColor: 'var(--bg-hover)' },
                      '&[aria-current="page"]': {
                        backgroundColor: 'var(--bg-surface)',
                        boxShadow: '0 0 0 1px var(--border)',
                        fontWeight: 500,
                        '& > svg': { color: 'var(--action-text)' },
                      },
                    }}
                  >
                    <Icon />
                    <Box
                      component="span"
                      sx={{
                        flex: 1,
                        minWidth: 0,
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                      }}
                    >
                      {t(labelKey)}
                    </Box>
                    {count !== null && counts[count] !== null && (
                      <Box
                        component="span"
                        sx={{
                          fontSize: 12,
                          lineHeight: '16px',
                          fontWeight: 400,
                          color: 'var(--text-secondary)',
                          fontVariantNumeric: 'tabular-nums',
                        }}
                      >
                        {counts[count]}
                      </Box>
                    )}
                  </Box>
                </li>
              )),
            ])}
          </Box>
        </Box>
      )}
      <Box
        ref={section}
        sx={{ minWidth: 0, minHeight: 0, display: 'flex', flexDirection: 'column' }}
      >
        <Outlet context={context} />
      </Box>
    </Box>
  );
}
