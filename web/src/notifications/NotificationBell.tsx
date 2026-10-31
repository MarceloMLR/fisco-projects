import AlternateEmailOutlined from '@mui/icons-material/AlternateEmailOutlined';
import AssignmentIndOutlined from '@mui/icons-material/AssignmentIndOutlined';
import BadgeOutlined from '@mui/icons-material/BadgeOutlined';
import BlockOutlined from '@mui/icons-material/BlockOutlined';
import ChatBubbleOutlineOutlined from '@mui/icons-material/ChatBubbleOutlineOutlined';
import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import ConfirmationNumberOutlined from '@mui/icons-material/ConfirmationNumberOutlined';
import DoneAllOutlined from '@mui/icons-material/DoneAllOutlined';
import EventOutlined from '@mui/icons-material/EventOutlined';
import FolderOutlined from '@mui/icons-material/FolderOutlined';
import GroupAddOutlined from '@mui/icons-material/GroupAddOutlined';
import HelpOutlineOutlined from '@mui/icons-material/HelpOutlineOutlined';
import HourglassEmptyOutlined from '@mui/icons-material/HourglassEmptyOutlined';
import NotificationsOutlined from '@mui/icons-material/NotificationsOutlined';
import PersonOutlined from '@mui/icons-material/PersonOutlined';
import StorefrontOutlined from '@mui/icons-material/StorefrontOutlined';
import SwapHorizOutlined from '@mui/icons-material/SwapHorizOutlined';
import SyncOutlined from '@mui/icons-material/SyncOutlined';
import TaskAltOutlined from '@mui/icons-material/TaskAltOutlined';
import WarningAmberOutlined from '@mui/icons-material/WarningAmberOutlined';
import Box from '@mui/material/Box';
import Popover from '@mui/material/Popover';
import type SvgIcon from '@mui/material/SvgIcon';
import { visuallyHidden } from '@mui/utils';
import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router';
import { api } from '../api/client';
import { notificationsQuery, queryKeys, unreadNotificationsQuery } from '../api/queries';
import { SegmentedControl } from '../components/SegmentedControl';
import { useDates } from '../format/useDates';
import {
  type BellNotification,
  type NoticeKind,
  noticeGroup,
  noticeText,
  noticeWhen,
} from './noticeText';
import { Button, IconButton } from '../components/Button';

const icons: Record<NoticeKind, typeof SvgIcon> = {
  TicketOpened: ConfirmationNumberOutlined,
  TicketMessage: ChatBubbleOutlineOutlined,
  TicketMentioned: AlternateEmailOutlined,
  TicketInformationRequested: HelpOutlineOutlined,
  TicketResolved: CheckCircleOutlined,
  TicketAssigned: AssignmentIndOutlined,
  TicketRequesterChanged: PersonOutlined,
  TicketTransferred: SwapHorizOutlined,
  TicketDueSoon: HourglassEmptyOutlined,
  TicketOverdue: WarningAmberOutlined,
  TicketStatusChanged: SyncOutlined,
  VendorTicketUpdated: StorefrontOutlined,
  ProjectMemberAdded: GroupAddOutlined,
  ProjectRoleChanged: BadgeOutlined,
  ProjectStatusChanged: FolderOutlined,
  ScheduleDatesChanged: EventOutlined,
  AcceptanceRequested: TaskAltOutlined,
  AcceptanceReminded: TaskAltOutlined,
  AcceptanceSigned: TaskAltOutlined,
  AcceptanceRefused: BlockOutlined,
};

type Filter = 'unread' | 'all';

/**
 * The bell of the notifications (NotificationBell, fp-bell, ADR-0038 and ADR-0066): the number not read in red, read
 * again every minute and on every navigation, and the panel (fp-notifications) with the notifications of the person
 * by day, "Não lidas" and "Todas", "Marcar todas como lidas" and "Ver todas". Choosing a notification marks it as
 * read and opens its item.
 */
export function NotificationBell({ size = 40 }: { size?: number }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const location = useLocation();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const panelId = useId();
  const { data } = useQuery(unreadNotificationsQuery());
  const unread = data?.unread ?? 0;
  const open = anchor !== null;

  // The count follows the person around the screens (ADR-0038).
  useEffect(() => {
    void queryClient.invalidateQueries({ queryKey: [...queryKeys.notifications, 'unread-count'] });
  }, [location.pathname, queryClient]);

  const label =
    unread > 0 ? t('notifications.bellUnread', { count: unread }) : t('notifications.bell');

  return (
    <>
      <Box
        component="button"
        type="button"
        aria-label={label}
        title={label}
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-controls={open ? panelId : undefined}
        onClick={(event: React.MouseEvent<HTMLElement>) => {
          setAnchor(event.currentTarget);
        }}
        sx={{
          position: 'relative',
          display: 'inline-grid',
          placeItems: 'center',
          flex: 'none',
          width: size,
          height: size,
          padding: 0,
          border: 0,
          borderRadius: 'var(--radius-md)',
          backgroundColor: 'transparent',
          color: 'var(--text-secondary)',
          cursor: 'pointer',
          '&:hover, &[aria-expanded="true"]': {
            backgroundColor: 'var(--bg-hover)',
            color: 'var(--text)',
          },
        }}
      >
        <NotificationsOutlined aria-hidden sx={{ fontSize: 22 }} />
        {unread > 0 && (
          <Box
            component="span"
            aria-hidden
            data-testid="bell-count"
            sx={{
              position: 'absolute',
              top: 3,
              right: 2,
              boxSizing: 'border-box',
              minWidth: 18,
              height: 18,
              padding: '0 5px',
              borderRadius: 'var(--radius-full)',
              backgroundColor: 'var(--notify)',
              color: 'var(--on-notify)',
              fontSize: 11,
              lineHeight: '18px',
              fontWeight: 500,
              textAlign: 'center',
              fontVariantNumeric: 'tabular-nums',
              boxShadow: '0 0 0 2px var(--topbar-bg)',
            }}
          >
            {unread > 99 ? '99+' : unread}
          </Box>
        )}
      </Box>
      <Popover
        open={open}
        anchorEl={anchor}
        onClose={() => {
          setAnchor(null);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: {
            id: panelId,
            role: 'dialog',
            'aria-label': t('notifications.title'),
            sx: {
              boxSizing: 'border-box',
              width: 400,
              maxWidth: 'calc(100vw - var(--space-4) * 2)',
              marginTop: '4px',
              backgroundColor: 'var(--bg-elevated)',
              color: 'var(--text)',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-lg)',
              boxShadow: 'var(--shadow-menu)',
              overflow: 'hidden',
              display: 'flex',
              flexDirection: 'column',
            },
          },
        }}
      >
        {open && (
          <NotificationPanel
            unread={unread}
            onOpened={() => {
              setAnchor(null);
            }}
          />
        )}
      </Popover>
    </>
  );
}

function NotificationPanel({ unread, onOpened }: { unread: number; onOpened: () => void }) {
  const { t } = useTranslation();
  const dates = useDates();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<Filter>('unread');
  const list = useInfiniteQuery(notificationsQuery(filter === 'unread'));
  const items = useMemo(() => {
    // A notification that arrived between two pages may come twice.
    const byId = new Map<string, BellNotification>();
    for (const item of (list.data?.pages ?? []).flatMap((page) => page.items)) {
      if (!byId.has(item.id)) {
        byId.set(item.id, item);
      }
    }
    return [...byId.values()];
  }, [list.data]);
  const now = new Date();

  const refresh = () => queryClient.invalidateQueries({ queryKey: queryKeys.notifications });
  const read = useMutation({
    mutationFn: async (id: string) => {
      await api.PUT('/api/notifications/{id}/read', { params: { path: { id } } });
    },
    onSettled: refresh,
  });
  const readAll = useMutation({
    mutationFn: async () => {
      await api.PUT('/api/notifications/read-all');
    },
    onSettled: refresh,
  });

  const choose = (item: BellNotification) => {
    if (!item.read) {
      read.mutate(item.id);
    }
    onOpened();
    void navigate(item.path);
  };

  const groups: { name: string; items: BellNotification[] }[] = [];
  for (const item of items) {
    const name = noticeGroup(item.createdAt, dates, t, now);
    const last = groups.at(-1);
    if (last?.name === name) {
      last.items.push(item);
    } else {
      groups.push({ name, items: [item] });
    }
  }

  return (
    <>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-2)',
          height: 52,
          flex: 'none',
          padding: '0 var(--space-2) 0 var(--space-4)',
          borderBottom: '1px solid var(--border)',
        }}
      >
        <Box
          component="h2"
          sx={{ flex: 1, margin: 0, fontSize: 16, lineHeight: '24px', fontWeight: 500 }}
        >
          {t('notifications.title')}
        </Box>
        <SegmentedControl
          label={t('notifications.show')}
          value={filter}
          options={[
            {
              value: 'unread',
              label: t('notifications.unread'),
              count: unread > 0 ? unread : undefined,
            },
            { value: 'all', label: t('notifications.all') },
          ]}
          onChange={setFilter}
        />
        <IconButton
          size="small"
          aria-label={t('notifications.readAll')}
          title={t('notifications.readAll')}
          disabled={unread === 0}
          loading={readAll.isPending}
          onClick={() => {
            readAll.mutate();
          }}
        >
          <DoneAllOutlined fontSize="small" />
        </IconButton>
      </Box>
      <Box sx={{ maxHeight: 520, overflow: 'auto' }}>
        {list.isError && items.length === 0 ? (
          <Empty>{t('notifications.loadFailed')}</Empty>
        ) : !list.isPending && items.length === 0 ? (
          <Empty>
            {filter === 'unread' ? t('notifications.emptyUnread') : t('notifications.empty')}
          </Empty>
        ) : (
          groups.map((group) => (
            <Box key={group.name}>
              <Box
                sx={{
                  padding: 'var(--space-3) var(--space-4) var(--space-1)',
                  fontSize: 12,
                  lineHeight: '16px',
                  fontWeight: 500,
                  color: 'var(--text-secondary)',
                }}
              >
                {group.name}
              </Box>
              <Box component="ul" sx={{ margin: 0, padding: 0, listStyle: 'none' }}>
                {group.items.map((item) => {
                  const Icon = icons[item.kind];
                  const project = item.values.project;
                  return (
                    <Box component="li" key={item.id}>
                      <Box
                        component="button"
                        type="button"
                        onClick={() => {
                          choose(item);
                        }}
                        sx={{
                          position: 'relative',
                          display: 'flex',
                          gap: 'var(--space-3)',
                          width: '100%',
                          padding: 'var(--space-3) var(--space-4) var(--space-3) 28px',
                          border: 0,
                          backgroundColor: 'transparent',
                          color: 'inherit',
                          font: 'inherit',
                          textAlign: 'left',
                          cursor: 'pointer',
                          '&:hover': { backgroundColor: 'var(--bg-hover)' },
                          '&:focus-visible': {
                            outline: '2px solid var(--focus-ring)',
                            outlineOffset: -2,
                          },
                          ...(item.read
                            ? {}
                            : {
                                '&::before': {
                                  content: '""',
                                  position: 'absolute',
                                  left: 12,
                                  top: 19,
                                  width: 8,
                                  height: 8,
                                  borderRadius: 'var(--radius-full)',
                                  backgroundColor: 'var(--notify)',
                                },
                              }),
                        }}
                      >
                        <Icon aria-hidden sx={{ color: 'var(--text-secondary)' }} />
                        <Box sx={{ minWidth: 0 }}>
                          {!item.read && (
                            <Box component="span" sx={visuallyHidden}>
                              {t('notifications.unreadMark')}{' '}
                            </Box>
                          )}
                          <Box
                            sx={{
                              fontSize: 14,
                              lineHeight: '20px',
                              fontWeight: item.read ? 400 : 500,
                              color: 'var(--text)',
                              overflowWrap: 'anywhere',
                            }}
                          >
                            {noticeText(item, t)}
                          </Box>
                          <Box
                            sx={{
                              marginTop: '2px',
                              fontSize: 12,
                              lineHeight: '16px',
                              color: 'var(--text-secondary)',
                            }}
                          >
                            {[noticeWhen(item.createdAt, dates, t, now), project]
                              .filter(Boolean)
                              .join(' · ')}
                          </Box>
                        </Box>
                      </Box>
                    </Box>
                  );
                })}
              </Box>
            </Box>
          ))
        )}
      </Box>
      {(filter === 'unread' || list.hasNextPage) && (
        <Box
          sx={{
            flex: 'none',
            padding: 'var(--space-2) var(--space-4)',
            borderTop: '1px solid var(--border)',
            textAlign: 'center',
          }}
        >
          <Button
            variant="text"
            size="small"
            loading={list.isFetchingNextPage}
            onClick={() => {
              if (filter === 'unread') {
                setFilter('all');
              } else {
                void list.fetchNextPage();
              }
            }}
          >
            {filter === 'unread' ? t('notifications.seeAll') : t('notifications.more')}
          </Button>
        </Box>
      )}
    </>
  );
}

function Empty({ children }: { children: React.ReactNode }) {
  return (
    <Box
      sx={{
        padding: 'var(--space-6) var(--space-4)',
        fontSize: 14,
        lineHeight: '20px',
        color: 'var(--text-secondary)',
        textAlign: 'center',
      }}
    >
      {children}
    </Box>
  );
}
