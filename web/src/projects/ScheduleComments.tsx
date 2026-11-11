import DeleteOutlined from '@mui/icons-material/DeleteOutlined';
import SendOutlined from '@mui/icons-material/SendOutlined';
import Box from '@mui/material/Box';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import type { TFunction } from 'i18next';
import { lazy, Suspense, useId, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  api,
  type Project,
  projectProblems,
  type ScheduleComment as Comment,
  type ScheduleItem,
} from '../api/client';
import { projectMembersQuery, queryKeys, scheduleCommentsQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Button, IconButton } from '../components/Button';
import { CardAvatar } from '../components/PersonCard';
import { Dialog } from '../components/Dialog';
import { RichText } from '../components/RichText';
import type { MentionChoice } from '../components/RichTextEditor';
import { useDates } from '../format/useDates';
import { useNotify } from '../notices/notify';
import { useSession } from '../session/SessionContext';

// The editor loads only when someone may comment.
const RichTextEditor = lazy(async () => ({
  default: (await import('../components/RichTextEditor')).RichTextEditor,
}));

/** Why the API refused a comment, in words of the screen. */
function refusalOf(status: number, problem: unknown, t: TFunction): string {
  const type =
    typeof problem === 'object' && problem !== null && 'type' in problem ? problem.type : undefined;
  if (type === projectProblems.commentMentionRefused) {
    return t('projects.schedule.comments.mentionRefused');
  }
  if (status === 400) {
    return t('projects.schedule.comments.emptyText');
  }
  return status === 409
    ? t('projects.schedule.comments.closed')
    : t('projects.schedule.comments.failed');
}

/**
 * The Comentários of a task or a milestone, at the end of its details (ADR-0157): the oldest first, each with the
 * author, when and the text with its formatting; "Apagar" with a confirmation where the person may remove it; and the
 * editor of the tickets, with the mentions of the people of the project, for who comments while the project is open.
 */
export function CommentSection({
  project,
  item,
}: {
  project: Pick<Project, 'id'>;
  item: ScheduleItem;
}) {
  const { t } = useTranslation();
  const headingId = useId();
  const comments = useQuery(scheduleCommentsQuery(project.id, item.id));
  const [removing, setRemoving] = useState<Comment>();
  const items = comments.data?.items ?? [];
  return (
    <Box
      component="section"
      aria-labelledby={headingId}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-3)',
        paddingTop: 'var(--space-4)',
        borderTop: '1px solid var(--border)',
      }}
    >
      <Box
        component="h3"
        id={headingId}
        sx={{
          margin: 0,
          fontSize: 13,
          lineHeight: '18px',
          fontWeight: 500,
          color: 'var(--text-secondary)',
        }}
      >
        {items.length === 0
          ? t('projects.schedule.comments.title')
          : `${t('projects.schedule.comments.title')} (${String(items.length)})`}
      </Box>
      {comments.isError && (
        <Alert tone="danger">{t('projects.schedule.comments.loadFailed')}</Alert>
      )}
      {comments.isSuccess && items.length === 0 && (
        <Box component="p" sx={{ margin: 0, fontSize: 14, color: 'var(--text-secondary)' }}>
          {t('projects.schedule.comments.empty')}
        </Box>
      )}
      {items.length > 0 && (
        <Box
          component="ul"
          aria-label={t('projects.schedule.comments.list', { name: item.name })}
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-4)',
            margin: 0,
            padding: 0,
            listStyle: 'none',
          }}
        >
          {items.map((comment) => (
            <CommentLine key={comment.id} comment={comment} onRemove={setRemoving} />
          ))}
        </Box>
      )}
      {comments.data?.canComment === true && <CommentComposer project={project} item={item} />}
      <RemoveCommentDialog
        project={project}
        item={item}
        comment={removing}
        onClose={() => {
          setRemoving(undefined);
        }}
      />
    </Box>
  );
}

/** One comment: the avatar that opens the card of the person, the name, when, the text and "Apagar" when allowed. */
function CommentLine({
  comment,
  onRemove,
}: {
  comment: Comment;
  onRemove: (comment: Comment) => void;
}) {
  const { t } = useTranslation();
  const { formatDateTime } = useDates();
  return (
    <Box
      component="li"
      aria-label={comment.author.name}
      sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-3)', minWidth: 0 }}
    >
      <CardAvatar person={comment.author} size="sm" />
      <Box
        sx={{
          flex: 1,
          minWidth: 0,
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-1)',
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'baseline',
            flexWrap: 'wrap',
            gap: '0 var(--space-2)',
            minHeight: 24,
            fontSize: 13,
            lineHeight: '18px',
          }}
        >
          <Box component="span" sx={{ fontWeight: 600, color: 'var(--text)' }}>
            {comment.author.name}
          </Box>
          <Box component="time" dateTime={comment.sentAt} sx={{ color: 'var(--text-secondary)' }}>
            {formatDateTime(comment.sentAt)}
          </Box>
        </Box>
        <Box sx={{ fontSize: 14, lineHeight: '20px', overflowWrap: 'anywhere' }}>
          <RichText value={comment.body} />
        </Box>
      </Box>
      {comment.canRemove && (
        <IconButton
          size="small"
          aria-label={t('projects.schedule.comments.remove', { name: comment.author.name })}
          title={t('projects.schedule.comments.remove', { name: comment.author.name })}
          onClick={() => {
            onRemove(comment);
          }}
          sx={{ flex: 'none', color: 'var(--text-secondary)' }}
        >
          <DeleteOutlined sx={{ fontSize: 'var(--icon-sm)' }} />
        </IconButton>
      )}
    </Box>
  );
}

/** The editor of a new comment, with the mentions of the people of the project but the person in the session. */
function CommentComposer({ project, item }: { project: Pick<Project, 'id'>; item: ScheduleItem }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { session } = useSession();
  const me = session.status === 'signed-in' ? session.user.id : '';
  const members = useQuery(projectMembersQuery(project.id));
  // The text of now, read when sending: a click right after typing sends what was typed.
  const text = useRef({ json: '', empty: true });
  const [error, setError] = useState<string>();
  const [round, setRound] = useState(0);
  const mentionable: MentionChoice[] = (members.data ?? [])
    .filter((member) => member.id !== me && member.name !== '')
    .map((member) => ({ id: member.id, name: member.name, email: member.email || undefined }))
    .sort(
      (one, other) =>
        one.name.localeCompare(other.name, 'pt-BR') ||
        (one.email ?? '').localeCompare(other.email ?? '', 'pt-BR'),
    );

  const send = async () => {
    if (text.current.empty) {
      setError(t('projects.schedule.comments.emptyText'));
      return;
    }
    setError(undefined);
    try {
      const { error: problem, response } = await api.POST(
        '/api/projects/{id}/schedule/items/{itemId}/comments',
        {
          params: { path: { id: project.id, itemId: item.id } },
          body: { body: text.current.json },
        },
      );
      if (response.ok) {
        text.current = { json: '', empty: true };
        setRound((value) => value + 1);
        // The comments and the count of the schedule.
        void queryClient.invalidateQueries({
          queryKey: [...queryKeys.projects, project.id, 'schedule'],
        });
        return;
      }
      setError(refusalOf(response.status, problem, t));
    } catch {
      setError(t('projects.schedule.comments.failed'));
    }
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}>
      {error !== undefined && <Alert tone="danger">{error}</Alert>}
      <Suspense fallback={<Box sx={{ height: 112 }} />}>
        <RichTextEditor
          key={round}
          label={t('projects.schedule.comments.label')}
          minHeight={44}
          placeholder={t('projects.schedule.comments.placeholder')}
          mentionable={mentionable}
          invalid={error !== undefined}
          onChange={(json, empty) => {
            text.current = { json, empty };
          }}
          footer={
            <Box
              sx={{
                display: 'flex',
                justifyContent: 'flex-end',
                padding: '0 var(--space-2) var(--space-2)',
              }}
            >
              <Button variant="contained" startIcon={<SendOutlined />} onClick={() => send()}>
                {t('projects.schedule.comments.send')}
              </Button>
            </Box>
          }
        />
      </Suspense>
    </Box>
  );
}

/** "Apagar o comentário?": the comment goes for good, as the conversation of the tickets stays out of the audit. */
function RemoveCommentDialog({
  project,
  item,
  comment,
  onClose,
}: {
  project: Pick<Project, 'id'>;
  item: ScheduleItem;
  comment: Comment | undefined;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const notify = useNotify();
  const [error, setError] = useState<string>();
  const remove = async (chosen: Comment) => {
    setError(undefined);
    try {
      const { error: problem, response } = await api.DELETE(
        '/api/projects/{id}/schedule/items/{itemId}/comments/{commentId}',
        { params: { path: { id: project.id, itemId: item.id, commentId: chosen.id } } },
      );
      if (response.ok) {
        void queryClient.invalidateQueries({
          queryKey: [...queryKeys.projects, project.id, 'schedule'],
        });
        notify(t('projects.schedule.comments.removed'));
        onClose();
        return;
      }
      setError(refusalOf(response.status, problem, t));
    } catch {
      setError(t('projects.schedule.comments.failed'));
    }
  };
  return (
    <Dialog
      open={comment !== undefined}
      alert
      title={t('projects.schedule.comments.removeTitle')}
      onClose={() => {
        setError(undefined);
        onClose();
      }}
      actions={
        <>
          <Button
            variant="outlined"
            onClick={() => {
              setError(undefined);
              onClose();
            }}
          >
            {t('projects.schedule.comments.removeBack')}
          </Button>
          <Button
            variant="contained"
            color="error"
            onClick={() => (comment === undefined ? undefined : remove(comment))}
          >
            {t('projects.schedule.comments.removeConfirm')}
          </Button>
        </>
      }
    >
      {error !== undefined && <Alert tone="danger">{error}</Alert>}
      {comment !== undefined &&
        t('projects.schedule.comments.removeText', { name: comment.author.name })}
    </Dialog>
  );
}
