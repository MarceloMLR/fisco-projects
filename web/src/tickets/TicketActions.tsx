import CheckOutlined from '@mui/icons-material/CheckOutlined';
import CloseOutlined from '@mui/icons-material/CloseOutlined';
import HelpOutlineOutlined from '@mui/icons-material/HelpOutlineOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import PlayArrowOutlined from '@mui/icons-material/PlayArrowOutlined';
import ReplayOutlined from '@mui/icons-material/ReplayOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import type SvgIcon from '@mui/material/SvgIcon';
import { useQueryClient } from '@tanstack/react-query';
import { lazy, Suspense, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type Ticket, type TicketAction } from '../api/client';
import { queryKeys } from '../api/queries';
import { Dialog } from '../components/Dialog';

const RichTextEditor = lazy(async () => ({
  default: (await import('../components/RichTextEditor')).RichTextEditor,
}));

type OpenAction = Ticket['permissions']['actions'][number];

/** The order of the actions in the header (design of Chamados-3): the first the person may take is the button. */
const actionOrder: readonly TicketAction[] = [
  'Resolve',
  'StartWork',
  'StartTriage',
  'ConfirmAndClose',
  'Reopen',
  'AskForInformation',
  'Cancel',
];

const actionIcons: Partial<Record<TicketAction, typeof SvgIcon>> = {
  Resolve: CheckOutlined,
  ConfirmAndClose: CheckOutlined,
  StartWork: PlayArrowOutlined,
  StartTriage: PlayArrowOutlined,
  Reopen: ReplayOutlined,
  AskForInformation: HelpOutlineOutlined,
  Cancel: CloseOutlined,
};

/** The actions of the flow open to the person now, in the order of the header. */
function orderedActions(actions: readonly OpenAction[]): OpenAction[] {
  const rank = (action: TicketAction) => {
    const index = actionOrder.indexOf(action);
    return index < 0 ? actionOrder.length : index;
  };
  return [...actions].sort((a, b) => rank(a.action) - rank(b.action));
}

/**
 * The message an action of the flow asks for (ADR-0024), as Pedir informações, Reabrir and Cancelar chamado: the text
 * goes to the conversation with the change of status. Mentions stay in the answer box.
 */
function ActionMessageDialog({
  action,
  busy,
  onClose,
  onConfirm,
}: {
  action: TicketAction;
  busy: boolean;
  onClose: () => void;
  onConfirm: (body: string) => void;
}) {
  const { t } = useTranslation();
  const [text, setText] = useState({ json: '', empty: true });
  // The text as typed now, for the submit, which the dialog may keep from an earlier render.
  const latest = useRef(text);
  const [tried, setTried] = useState(false);
  const missing = tried && text.empty;
  const intro =
    action === 'AskForInformation' || action === 'Reopen' || action === 'Cancel'
      ? t(`tickets.actionDialog.${action}`)
      : t('tickets.actionDialog.other');
  return (
    <Dialog
      open
      title={t(`tickets.actions.${action}`)}
      onClose={onClose}
      onSubmit={(event) => {
        event.preventDefault();
        setTried(true);
        if (!latest.current.empty) {
          onConfirm(latest.current.json);
        }
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('tickets.actionDialog.back')}
          </Button>
          <Button
            type="submit"
            variant="contained"
            color={action === 'Cancel' ? 'error' : 'primary'}
            disabled={busy}
          >
            {t(`tickets.actions.${action}`)}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
        <Box component="p" sx={{ margin: 0, fontSize: 14, lineHeight: '20px' }}>
          {intro}
        </Box>
        <Suspense fallback={<Box sx={{ minHeight: 120 }} />}>
          <RichTextEditor
            label={t('tickets.actionDialog.label')}
            placeholder={t('tickets.actionDialog.placeholder')}
            minHeight={96}
            invalid={missing}
            onChange={(json, empty) => {
              latest.current = { json, empty };
              setText({ json, empty });
            }}
          />
        </Suspense>
        {missing && (
          <Box
            component="p"
            role="alert"
            sx={{ margin: 0, fontSize: 12, color: 'var(--status-danger-text)' }}
          >
            {t('tickets.actionDialog.needed')}
          </Box>
        )}
      </Box>
    </Dialog>
  );
}

/**
 * The actions of the flow in the header of the ticket (canvas Chamado, ADR-0024): the first open to the person is the
 * button, the others go in the "⋮"; the one that asks for a message opens a dialog for the text. Automatic actions, as
 * the first answer and the closing without answer, are never offered.
 */
export function TicketActions({
  ticket,
  onMessage,
}: {
  ticket: Ticket;
  onMessage: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [anchor, setAnchor] = useState<HTMLElement>();
  const [asking, setAsking] = useState<TicketAction>();
  const [busy, setBusy] = useState(false);
  const [primary, ...others] = orderedActions(ticket.permissions.actions);

  const take = async (action: TicketAction, body?: string) => {
    setBusy(true);
    try {
      const { response } = await api.POST('/api/tickets/{number}/transitions', {
        params: { path: { number: ticket.number } },
        body: { action, body: body ?? null },
      });
      if (response.ok) {
        setAsking(undefined);
        onMessage(t(`tickets.actionDone.${action}`));
      } else {
        // Someone changed the status meanwhile: the page shows the ticket as it is now.
        onMessage(t(response.status === 409 ? 'tickets.actionStale' : 'tickets.actionFailed'));
        if (response.status === 409) {
          setAsking(undefined);
        }
      }
      void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
    } catch {
      onMessage(t('tickets.actionFailed'));
    } finally {
      setBusy(false);
    }
  };

  const choose = (action: OpenAction) => {
    if (action.requiresMessage) {
      setAsking(action.action);
    } else {
      void take(action.action);
    }
  };

  if (primary === undefined) {
    return null;
  }

  const PrimaryIcon = actionIcons[primary.action];
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-1)', flex: 'none' }}>
      <Button
        variant="contained"
        disabled={busy}
        startIcon={PrimaryIcon === undefined ? undefined : <PrimaryIcon aria-hidden />}
        onClick={() => {
          choose(primary);
        }}
      >
        {t(`tickets.actions.${primary.action}`)}
      </Button>
      {others.length > 0 && (
        <>
          <IconButton
            aria-label={t('tickets.actionMenu')}
            title={t('tickets.actionMenu')}
            aria-haspopup="menu"
            aria-expanded={anchor !== undefined}
            onClick={(event) => {
              setAnchor(event.currentTarget);
            }}
            sx={{ color: 'var(--text-secondary)' }}
          >
            <MoreVertOutlined />
          </IconButton>
          <Menu
            anchorEl={anchor}
            open={anchor !== undefined}
            onClose={() => {
              setAnchor(undefined);
            }}
            anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
            transformOrigin={{ vertical: 'top', horizontal: 'right' }}
            slotProps={{
              paper: { sx: { minWidth: 220, marginTop: 'var(--space-1)' } },
              list: { 'aria-label': t('tickets.actionMenu') },
            }}
          >
            {others.map((action) => {
              const Icon = actionIcons[action.action];
              return (
                <MenuItem
                  key={action.action}
                  disabled={busy}
                  onClick={() => {
                    setAnchor(undefined);
                    choose(action);
                  }}
                >
                  {Icon !== undefined && <Icon aria-hidden />}
                  {t(`tickets.actions.${action.action}`)}
                </MenuItem>
              );
            })}
          </Menu>
        </>
      )}
      {asking !== undefined && (
        <ActionMessageDialog
          action={asking}
          busy={busy}
          onClose={() => {
            setAsking(undefined);
          }}
          onConfirm={(body) => {
            void take(asking, body);
          }}
        />
      )}
    </Box>
  );
}
