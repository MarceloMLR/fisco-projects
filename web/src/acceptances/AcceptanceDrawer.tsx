import BlockOutlined from '@mui/icons-material/BlockOutlined';
import OpenInNewOutlined from '@mui/icons-material/OpenInNewOutlined';
import PictureAsPdfOutlined from '@mui/icons-material/PictureAsPdfOutlined';
import ScheduleOutlined from '@mui/icons-material/ScheduleOutlined';
import SendOutlined from '@mui/icons-material/SendOutlined';
import TaskAltOutlined from '@mui/icons-material/TaskAltOutlined';
import UploadOutlined from '@mui/icons-material/UploadOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import type SvgIcon from '@mui/material/SvgIcon';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, type AcceptanceDetails, type AcceptanceSigner, type Project } from '../api/client';
import { acceptanceQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { Dialog } from '../components/Dialog';
import { documentFileUrl } from '../documents/documents';
import { formatDayText } from '../format/dates';
import { sizeText } from '../format/size';
import { useDates } from '../format/useDates';
import { theme } from '../theme/theme';
import { AcceptanceDialog } from './AcceptanceDialog';
import { acceptanceFileUrl, answerOf, namesText, signedCount } from './acceptances';
import { AcceptanceStatusBadge, SignerStatusBadge } from './AcceptanceStatusBadge';

const section = {
  display: 'flex',
  flexDirection: 'column',
  gap: 'var(--space-3)',
  paddingBottom: 'var(--space-5)',
  borderBottom: '1px solid var(--border)',
  '&:last-of-type': { borderBottom: 0, paddingBottom: 0 },
} as const;

const labelSx = {
  margin: 0,
  fontSize: 12,
  lineHeight: '16px',
  fontWeight: 500,
  color: 'var(--text-secondary)',
} as const;

const metaSx = {
  fontSize: 12,
  lineHeight: '16px',
  color: 'var(--text-secondary)',
  fontVariantNumeric: 'tabular-nums',
} as const;

type AcceptanceEvent = AcceptanceDetails['events'][number];

const eventIcons: Record<AcceptanceEvent['kind'], typeof SvgIcon> = {
  Requested: SendOutlined,
  Reminded: ScheduleOutlined,
  Signed: TaskAltOutlined,
  Attached: UploadOutlined,
  Refused: BlockOutlined,
};

/** A PDF of a term (fp-pdf): the icon, the name, the size and "Abrir" in a new tab. */
function PdfTile({ name, size, href }: { name: string; size: number; href: string }) {
  const { t } = useTranslation();
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--space-3)',
        padding: 'var(--space-3)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-md)',
        backgroundColor: 'var(--bg-surface)',
      }}
    >
      <PictureAsPdfOutlined aria-hidden sx={{ flex: 'none', color: 'var(--text-secondary)' }} />
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Box sx={{ overflowWrap: 'anywhere' }}>{name}</Box>
        <Box sx={metaSx}>{sizeText(size)}</Box>
      </Box>
      <Button
        variant="outlined"
        size="small"
        startIcon={<OpenInNewOutlined />}
        href={href}
        target="_blank"
        rel="noopener noreferrer"
        aria-label={t('acceptances.details.open', { name })}
        sx={{ flex: 'none' }}
      >
        {t('documents.details.open')}
      </Button>
    </Box>
  );
}

/**
 * The details of a term (canvas ProjetoTermos in the drawer of ADR-0081, and CelularTermo on the phone): the situation,
 * the title, the milestone and the request; the message, the term to sign, each signer with the situation, the e-mail
 * while the request is open and, from the local provider, the link to sign; the signed term, the reason of a refusal
 * and the history; and "Lembrar signatários", "Anexar termo assinado" and "Cancelar pedido" in a pending request
 * (ADR-0129).
 */
export function AcceptanceDrawer({
  project,
  termId,
  onClose,
  onChanged,
}: {
  project: Project;
  /** The term chosen; none while the drawer is closed. */
  termId: string | undefined;
  onClose: () => void;
  onChanged: (message: string) => void;
}) {
  const { t } = useTranslation();
  const { data, isError } = useQuery({
    ...acceptanceQuery(project.id, termId ?? ''),
    enabled: termId !== undefined,
  });

  return (
    <DetailsDrawer
      open={termId !== undefined}
      onClose={onClose}
      label={t('acceptances.details.label')}
    >
      {isError && <Alert tone="danger">{t('acceptances.details.notLoaded')}</Alert>}
      {data !== undefined && data.term.id === termId && (
        <AcceptanceContent
          key={data.term.id}
          project={project}
          details={data}
          onCancelled={onClose}
          onChanged={onChanged}
        />
      )}
    </DetailsDrawer>
  );
}

function AcceptanceContent({
  project,
  details,
  onCancelled,
  onChanged,
}: {
  project: Project;
  details: AcceptanceDetails;
  onCancelled: () => void;
  onChanged: (message: string) => void;
}) {
  const { t } = useTranslation();
  const { formatDate, formatDateTime } = useDates();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const { term, message, toSign, signers, signed, refusalReason, events, actions } = details;
  const [failure, setFailure] = useState<string>();
  const [cancelling, setCancelling] = useState(false);
  const [attaching, setAttaching] = useState(false);
  // A request asked of a person of the project before the provider of signatures keeps the person (ADR-0129).
  const recipient = term.recipient?.name ?? null;
  const names = namesText(signers.map((item) => item.name));

  const remind = async () => {
    try {
      const { error, response } = await api.POST(
        '/api/projects/{projectId}/acceptances/{termId}/reminder',
        { params: { path: { projectId: project.id, termId: term.id } } },
      );
      const answer = answerOf(response, error, t);
      if (answer.kind === 'done') {
        onChanged(t('acceptances.messages.reminded'));
        return;
      }
      setFailure(answer.kind === 'refused' ? answer.message : t('acceptances.messages.failed'));
    } catch {
      setFailure(t('acceptances.messages.failed'));
    }
  };

  const cancel = async () => {
    setCancelling(false);
    try {
      const { error, response } = await api.DELETE(
        '/api/projects/{projectId}/acceptances/{termId}',
        { params: { path: { projectId: project.id, termId: term.id } } },
      );
      const answer = answerOf(response, error, t);
      if (answer.kind === 'done') {
        onChanged(t('acceptances.messages.cancelled'));
        onCancelled();
        return;
      }
      setFailure(answer.kind === 'refused' ? answer.message : t('acceptances.messages.failed'));
    } catch {
      setFailure(t('acceptances.messages.failed'));
    }
  };

  const eventText = (item: AcceptanceEvent) => {
    const actor = item.actor?.name ?? '';
    const signer = item.signer ?? '';
    switch (item.kind) {
      case 'Requested':
        return recipient === null
          ? t('acceptances.history.RequestedSigners', { names })
          : t('acceptances.history.Requested', { recipient });
      case 'Reminded':
        return t(
          recipient === null
            ? 'acceptances.history.RemindedSigners'
            : 'acceptances.history.Reminded',
        );
      case 'Attached':
        return recipient === null
          ? t('acceptances.history.AttachedByActor', { actor })
          : t('acceptances.history.Attached', { actor, recipient });
      case 'Signed':
        return item.signer === null
          ? t('acceptances.history.Signed', { actor })
          : t('acceptances.history.SignedBySigner', { signer });
      case 'Refused':
        return item.signer === null
          ? t('acceptances.history.Refused', { actor })
          : t('acceptances.history.RefusedBySigner', { signer });
    }
  };
  const eventMeta = (item: AcceptanceEvent) => {
    const when = formatDateTime(item.at);
    const actor = item.actor?.name ?? '';
    if (item.kind === 'Requested') {
      return recipient === null
        ? t('acceptances.history.RequestedSignersMeta', { when, actor })
        : t('acceptances.history.RequestedMeta', { when });
    }
    return item.kind === 'Reminded'
      ? t('acceptances.history.RemindedMeta', { when, actor })
      : t('acceptances.history.Meta', { when });
  };

  const manages = actions.canRemind || actions.canAttach || actions.canCancel;
  const hasLinks = signers.some((item) => item.signingLink !== null);

  return (
    <>
      <Box component="section" sx={section}>
        <Box>
          <AcceptanceStatusBadge status={term.status} />
        </Box>
        <Box
          component="h2"
          sx={{
            margin: 0,
            fontSize: desktop ? 18 : 20,
            lineHeight: '28px',
            fontWeight: 500,
            color: 'var(--text)',
            overflowWrap: 'anywhere',
          }}
        >
          {term.title}
        </Box>
        <Box
          component="dl"
          sx={{
            display: 'grid',
            gridTemplateColumns: '96px minmax(0, 1fr)',
            alignItems: 'baseline',
            gap: 'var(--space-3)',
            margin: 0,
            '& dt': { fontSize: 13, lineHeight: '20px', color: 'var(--text-secondary)' },
            '& dd': { margin: 0, minWidth: 0, overflowWrap: 'anywhere' },
          }}
        >
          <dt>{t('acceptances.details.milestone')}</dt>
          <dd>
            {term.milestone === null
              ? t('acceptances.milestoneRemoved')
              : term.milestone.day === null
                ? term.milestone.name
                : t('acceptances.milestone', {
                    name: term.milestone.name,
                    day: formatDayText(term.milestone.day),
                  })}
          </dd>
          {recipient !== null && (
            <>
              <dt>{t('acceptances.details.recipient')}</dt>
              <dd>{recipient}</dd>
            </>
          )}
          <dt>
            {t(term.isRequest ? 'acceptances.details.requested' : 'acceptances.details.attached')}
          </dt>
          <dd>
            {t('acceptances.details.byOn', {
              date: formatDate(term.requested.at),
              name: term.requested.name,
            })}
          </dd>
        </Box>
        {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
      </Box>
      {message !== null && (
        <Box component="section" sx={section} aria-label={t('acceptances.details.message')}>
          <Box component="h3" sx={labelSx}>
            {t('acceptances.details.message')}
          </Box>
          <Box
            sx={{
              padding: 'var(--space-3)',
              border: '1px solid var(--msg-manager-border)',
              borderRadius: 'var(--radius-md)',
              backgroundColor: 'var(--msg-manager-bg)',
            }}
          >
            <Box
              sx={{
                display: 'flex',
                alignItems: 'baseline',
                gap: 'var(--space-2)',
                flexWrap: 'wrap',
              }}
            >
              <Box component="span" sx={{ fontWeight: 500 }}>
                {term.requested.name}
              </Box>
              <Box component="time" sx={metaSx}>
                {formatDate(term.requested.at)}
              </Box>
            </Box>
            <Box sx={{ marginTop: 'var(--space-1)', whiteSpace: 'pre-wrap' }}>{message}</Box>
          </Box>
        </Box>
      )}
      {signers.length > 0 && (
        <Box component="section" sx={section} aria-labelledby={`signers-${term.id}`}>
          <Box
            sx={{
              display: 'flex',
              alignItems: 'baseline',
              justifyContent: 'space-between',
              gap: 'var(--space-2)',
            }}
          >
            <Box component="h3" id={`signers-${term.id}`} sx={labelSx}>
              {t('acceptances.details.signers')}
            </Box>
            <Box component="span" sx={metaSx}>
              {t('acceptances.signers.count', {
                count: signedCount(signers),
                total: signers.length,
              })}
            </Box>
          </Box>
          <Box
            component="ul"
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: 'var(--space-2)',
              margin: 0,
              padding: 0,
              listStyle: 'none',
            }}
          >
            {signers.map((item) => (
              <SignerRow key={item.id} signer={item} />
            ))}
          </Box>
          {hasLinks && (
            <Box component="p" sx={{ ...metaSx, margin: 0 }}>
              {t('acceptances.details.localLink')}
            </Box>
          )}
        </Box>
      )}
      {toSign !== null && (
        <Box component="section" sx={section}>
          <Box component="h3" sx={labelSx}>
            {t('acceptances.details.toSign')}
          </Box>
          <PdfTile
            name={toSign.name}
            size={toSign.size}
            href={acceptanceFileUrl(project.id, term.id)}
          />
        </Box>
      )}
      {signed !== null && (
        <Box component="section" sx={section}>
          <Box component="h3" sx={labelSx}>
            {t('acceptances.details.signed')}
          </Box>
          <PdfTile
            name={signed.name}
            size={signed.size}
            href={documentFileUrl(project.id, signed.documentId)}
          />
        </Box>
      )}
      {refusalReason !== null && signers.length === 0 && (
        <Box component="section" sx={section}>
          <Box component="h3" sx={labelSx}>
            {t('acceptances.details.refusal')}
          </Box>
          <Box sx={{ whiteSpace: 'pre-wrap' }}>{refusalReason}</Box>
        </Box>
      )}
      <Box component="section" sx={section} aria-labelledby={`history-${term.id}`}>
        <Box component="h3" id={`history-${term.id}`} sx={labelSx}>
          {t('acceptances.details.history')}
        </Box>
        <Box
          component="ol"
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-3)',
            margin: 0,
            padding: 0,
            listStyle: 'none',
          }}
        >
          {events.map((item, index) => {
            const Icon = eventIcons[item.kind];
            return (
              <Box
                component="li"
                key={`${item.kind}-${item.at}-${String(index)}`}
                sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-3)' }}
              >
                <Icon
                  aria-hidden
                  sx={{ flex: 'none', fontSize: 18, color: 'var(--text-secondary)' }}
                />
                <Box sx={{ minWidth: 0 }}>
                  <Box>{eventText(item)}</Box>
                  <Box sx={metaSx}>{eventMeta(item)}</Box>
                </Box>
              </Box>
            );
          })}
        </Box>
      </Box>
      {manages && (
        <Box
          component="section"
          sx={{ ...section, flexDirection: 'row', flexWrap: 'wrap', gap: 'var(--space-2)' }}
        >
          {actions.canRemind && (
            <Button
              variant="outlined"
              size="small"
              startIcon={<SendOutlined />}
              onClick={() => {
                void remind();
              }}
            >
              {t('acceptances.actions.remind')}
            </Button>
          )}
          {actions.canAttach && (
            <Button
              variant="outlined"
              size="small"
              startIcon={<UploadOutlined />}
              onClick={() => {
                setAttaching(true);
              }}
            >
              {t('acceptances.actions.attach')}
            </Button>
          )}
          {actions.canCancel && (
            <Button
              variant="text"
              size="small"
              onClick={() => {
                setCancelling(true);
              }}
            >
              {t('acceptances.actions.cancel')}
            </Button>
          )}
        </Box>
      )}
      {attaching && (
        <AcceptanceDialog
          projectId={project.id}
          mode="onBehalf"
          term={{ id: term.id, title: term.title, recipient }}
          onClose={() => {
            setAttaching(false);
          }}
          onSaved={(saved) => {
            setAttaching(false);
            onChanged(saved);
          }}
        />
      )}
      <Dialog
        open={cancelling}
        alert
        title={t('acceptances.cancelRequest.title')}
        onClose={() => {
          setCancelling(false);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setCancelling(false);
              }}
            >
              {t('acceptances.cancelRequest.back')}
            </Button>
            <Button
              variant="contained"
              color="error"
              onClick={() => {
                void cancel();
              }}
            >
              {t('acceptances.cancelRequest.confirm')}
            </Button>
          </>
        }
      >
        {recipient === null
          ? t('acceptances.cancelRequest.textSigners', { title: term.title })
          : t('acceptances.cancelRequest.text', { title: term.title, name: recipient })}
      </Dialog>
    </>
  );
}

/**
 * A signer of a request: the name, the situation by icon, color and text, when the signer signed or refused and the
 * reason, the e-mail while the request is open and, from the local provider, "Abrir assinatura".
 */
function SignerRow({ signer }: { signer: AcceptanceSigner }) {
  const { t } = useTranslation();
  const { formatDateTime } = useDates();
  return (
    <Box
      component="li"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-1)',
        padding: 'var(--space-3)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-md)',
        backgroundColor: 'var(--bg-surface)',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 'var(--space-2)',
          flexWrap: 'wrap',
        }}
      >
        <Box sx={{ minWidth: 0, fontWeight: 500, overflowWrap: 'anywhere' }}>{signer.name}</Box>
        <SignerStatusBadge status={signer.status} />
      </Box>
      {signer.email !== null && (
        <Box sx={{ ...metaSx, overflowWrap: 'anywhere' }}>{signer.email}</Box>
      )}
      {signer.respondedAt !== null && (
        <Box sx={metaSx}>
          {t(
            signer.status === 'Refused'
              ? 'acceptances.details.refusedAt'
              : 'acceptances.details.signedAt',
            { when: formatDateTime(signer.respondedAt) },
          )}
        </Box>
      )}
      {signer.refusalReason !== null && (
        <Box sx={{ whiteSpace: 'pre-wrap' }}>
          <Box component="span" sx={labelSx}>
            {t('acceptances.details.refusalOf', { name: signer.name })}
          </Box>
          <Box>{signer.refusalReason}</Box>
        </Box>
      )}
      {signer.signingLink !== null && (
        <Button
          variant="text"
          size="small"
          startIcon={<OpenInNewOutlined />}
          href={signer.signingLink}
          target="_blank"
          rel="noopener noreferrer"
          aria-label={t('acceptances.details.openSigningOf', { name: signer.name })}
          sx={{ alignSelf: 'flex-start' }}
        >
          {t('acceptances.details.openSigning')}
        </Button>
      )}
    </Box>
  );
}
