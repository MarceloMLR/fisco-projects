import { zodResolver } from '@hookform/resolvers/zod';
import BlockOutlined from '@mui/icons-material/BlockOutlined';
import OpenInNewOutlined from '@mui/icons-material/OpenInNewOutlined';
import PictureAsPdfOutlined from '@mui/icons-material/PictureAsPdfOutlined';
import ScheduleOutlined from '@mui/icons-material/ScheduleOutlined';
import SendOutlined from '@mui/icons-material/SendOutlined';
import TaskAltOutlined from '@mui/icons-material/TaskAltOutlined';
import UploadFileOutlined from '@mui/icons-material/UploadFileOutlined';
import UploadOutlined from '@mui/icons-material/UploadOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import type SvgIcon from '@mui/material/SvgIcon';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery } from '@tanstack/react-query';
import { useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { api, type AcceptanceDetails, type AcceptanceEventKind, type Project } from '../api/client';
import { acceptanceQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { Dialog } from '../components/Dialog';
import { TextArea } from '../components/TextArea';
import { documentFileUrl } from '../documents/documents';
import { formatDayText } from '../format/dates';
import { sizeText } from '../format/size';
import { useDates } from '../format/useDates';
import { theme } from '../theme/theme';
import { AcceptanceDialog } from './AcceptanceDialog';
import {
  acceptanceFileUrl,
  answerOf,
  firstName,
  isAcceptanceError,
  isPdfName,
  refusalSchema,
  sendAcceptance,
} from './acceptances';
import { AcceptanceStatusBadge } from './AcceptanceStatusBadge';

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

const eventIcons: Record<AcceptanceEventKind, typeof SvgIcon> = {
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
 * the title, the milestone, for whom and the request; the message, the term to sign, the signed term and the reason of
 * a refusal; the answer of the person of a pending request, with the signed PDF or the refusal; the history; and, for
 * who manages the terms, "Lembrar", "Anexar termo assinado" and "Cancelar pedido" (ADR-0095).
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
  const input = useRef<HTMLInputElement>(null);
  const { term, message, toSign, signed, refusalReason, events, actions } = details;
  const [file, setFile] = useState<File>();
  const [fileError, setFileError] = useState<string>();
  const [failure, setFailure] = useState<string>();
  const [sending, setSending] = useState(false);
  const [refusing, setRefusing] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [attaching, setAttaching] = useState(false);
  const recipient = term.recipient.name;

  const send = async () => {
    if (file === undefined) {
      setFileError(t('acceptances.errors.fileRequired'));
      return;
    }
    setSending(true);
    const answer = await sendAcceptance(
      project.id,
      'sign',
      { milestoneId: '', recipientId: '', title: '', message: '', file, termId: term.id },
      t,
    );
    setSending(false);
    if (answer.kind === 'done') {
      setFile(undefined);
      onChanged(t('acceptances.messages.signed'));
      return;
    }
    if (answer.kind === 'fields') {
      setFileError(t('acceptances.errors.fileNotPdf'));
      return;
    }
    setFailure(answer.message);
  };

  const remind = async () => {
    try {
      const { error, response } = await api.POST(
        '/api/projects/{projectId}/acceptances/{termId}/reminder',
        { params: { path: { projectId: project.id, termId: term.id } } },
      );
      const answer = answerOf(response, error, t);
      if (answer.kind === 'done') {
        onChanged(t('acceptances.messages.reminded', { name: recipient }));
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

  const eventText = (kind: AcceptanceEventKind, actor: string) =>
    kind === 'Requested'
      ? t('acceptances.history.Requested', { recipient })
      : kind === 'Reminded'
        ? t('acceptances.history.Reminded')
        : kind === 'Attached'
          ? t('acceptances.history.Attached', { actor, recipient })
          : t(`acceptances.history.${kind}`, { actor });
  const eventMeta = (kind: AcceptanceEventKind, actor: string, at: string) =>
    kind === 'Requested'
      ? t('acceptances.history.RequestedMeta', { when: formatDateTime(at) })
      : kind === 'Reminded'
        ? t('acceptances.history.RemindedMeta', { when: formatDateTime(at), actor })
        : t('acceptances.history.Meta', { when: formatDateTime(at) });

  const manages = actions.canRemind || actions.canAttach || actions.canCancel;

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
          <dt>{t('acceptances.details.recipient')}</dt>
          <dd>{recipient}</dd>
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
      {toSign !== null && (
        <Box component="section" sx={section}>
          <Box component="h3" sx={labelSx}>
            {t(actions.canAnswer ? 'acceptances.details.toSign' : 'acceptances.details.sent')}
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
      {refusalReason !== null && (
        <Box component="section" sx={section}>
          <Box component="h3" sx={labelSx}>
            {t('acceptances.details.refusal')}
          </Box>
          <Box sx={{ whiteSpace: 'pre-wrap' }}>{refusalReason}</Box>
        </Box>
      )}
      {actions.canAnswer && (
        <Box component="section" sx={section} aria-label={t('acceptances.details.signed')}>
          <Box component="h3" sx={labelSx}>
            {t('acceptances.details.signed')}
          </Box>
          <Box component="p" sx={{ margin: 0, color: 'var(--text-secondary)' }}>
            {t('acceptances.details.answer')}
          </Box>
          <Button
            variant="outlined"
            size={desktop ? 'small' : 'large'}
            startIcon={<UploadFileOutlined />}
            onClick={() => input.current?.click()}
            sx={desktop ? { alignSelf: 'flex-start' } : { width: '100%' }}
          >
            {t(file === undefined ? 'acceptances.actions.choose' : 'acceptances.actions.change')}
          </Button>
          {file !== undefined && (
            <Box data-testid="acceptance-signed-name" sx={{ overflowWrap: 'anywhere' }}>
              {`${file.name} · ${sizeText(file.size)}`}
            </Box>
          )}
          {fileError !== undefined && <Alert tone="danger">{fileError}</Alert>}
          <input
            ref={input}
            type="file"
            accept=".pdf,application/pdf"
            hidden
            aria-hidden
            tabIndex={-1}
            data-testid="acceptance-signed-file"
            onChange={(event) => {
              const picked = event.target.files?.[0];
              event.target.value = '';
              if (picked === undefined) {
                return;
              }
              setFileError(isPdfName(picked.name) ? undefined : t('acceptances.errors.fileNotPdf'));
              setFile(isPdfName(picked.name) ? picked : undefined);
            }}
          />
          <Box sx={{ display: 'flex', gap: 'var(--space-2)' }}>
            <Button
              variant="outlined"
              size={desktop ? 'small' : 'large'}
              onClick={() => {
                setRefusing(true);
              }}
              sx={desktop ? undefined : { flex: 1 }}
            >
              {t('acceptances.actions.refuse')}
            </Button>
            <Button
              variant="contained"
              size={desktop ? 'small' : 'large'}
              disabled={file === undefined || sending}
              onClick={() => {
                void send();
              }}
              sx={desktop ? undefined : { flex: 1 }}
            >
              {t('acceptances.actions.send')}
            </Button>
          </Box>
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
          {events.map((item) => {
            const Icon = eventIcons[item.kind];
            return (
              <Box
                component="li"
                key={`${item.kind}-${item.at}`}
                sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-3)' }}
              >
                <Icon
                  aria-hidden
                  sx={{ flex: 'none', fontSize: 18, color: 'var(--text-secondary)' }}
                />
                <Box sx={{ minWidth: 0 }}>
                  <Box>{eventText(item.kind, item.actor.name)}</Box>
                  <Box sx={metaSx}>{eventMeta(item.kind, item.actor.name, item.at)}</Box>
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
              {t('acceptances.actions.remind', { name: firstName(recipient) })}
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
      {refusing && (
        <RefuseDialog
          projectId={project.id}
          termId={term.id}
          title={term.title}
          onClose={() => {
            setRefusing(false);
          }}
          onRefused={() => {
            setRefusing(false);
            onChanged(t('acceptances.messages.refused'));
          }}
        />
      )}
      {attaching && (
        <AcceptanceDialog
          projectId={project.id}
          mode="onBehalf"
          members={project.members}
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
        {t('acceptances.cancelRequest.text', { title: term.title, name: recipient })}
      </Dialog>
    </>
  );
}

/** "Recusar termo": the reason, which who asked hears about; the milestone stays open (ADR-0035). */
function RefuseDialog({
  projectId,
  termId,
  title,
  onClose,
  onRefused,
}: {
  projectId: string;
  termId: string;
  title: string;
  onClose: () => void;
  onRefused: () => void;
}) {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<{ reason: string }>({
    resolver: zodResolver(refusalSchema),
    defaultValues: { reason: '' },
  });

  const refuse = handleSubmit(async ({ reason }) => {
    try {
      const { error, response } = await api.POST(
        '/api/projects/{projectId}/acceptances/{termId}/refusal',
        { params: { path: { projectId, termId } }, body: { reason: reason.trim() } },
      );
      const answer = answerOf(response, error, t);
      if (answer.kind === 'done') {
        onRefused();
        return;
      }
      if (answer.kind === 'fields') {
        setError('reason', { message: 'acceptances.errors.reasonRequired' });
        return;
      }
      setError('root', { message: answer.message });
    } catch {
      setError('root', { message: t('acceptances.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t('acceptances.refuse.title')}
      onClose={onClose}
      onSubmit={(event) => {
        void refuse(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('acceptances.refuse.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t('acceptances.refuse.submit')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
        <Box component="p" sx={{ margin: 0, color: 'var(--text-secondary)' }}>
          {t('acceptances.refuse.text', { title })}
        </Box>
        <TextArea
          label={t('acceptances.refuse.reason')}
          rows={4}
          maxLength={1010}
          error={isAcceptanceError(errors.reason?.message) ? t(errors.reason.message) : undefined}
          {...register('reason')}
        />
      </Box>
    </Dialog>
  );
}
