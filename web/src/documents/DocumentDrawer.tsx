import CheckOutlined from '@mui/icons-material/CheckOutlined';
import DeleteOutlineOutlined from '@mui/icons-material/DeleteOutlineOutlined';
import DownloadOutlined from '@mui/icons-material/DownloadOutlined';
import LinkOutlined from '@mui/icons-material/LinkOutlined';
import MoreVertOutlined from '@mui/icons-material/MoreVertOutlined';
import OpenInNewOutlined from '@mui/icons-material/OpenInNewOutlined';
import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import ListSubheader from '@mui/material/ListSubheader';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { api, type DocumentDetails, type DocumentStatus, documentProblems } from '../api/client';
import { documentQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { DetailsDrawer } from '../components/DetailsDrawer';
import { Dialog } from '../components/Dialog';
import { sizeText } from '../format/size';
import { useDates } from '../format/useDates';
import { problemType } from '../projects/projectForm';
import { DocumentStatusBadge } from './DocumentStatusBadge';
import { documentFileUrl, opensInBrowser, statusesFor } from './documents';
import { FileIcon } from './FileIcon';
import { LinkDocumentDialog } from './LinkDocumentDialog';
import { Button, IconButton } from '../components/Button';

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

const quietSx = {
  width: 'var(--control-sm)',
  height: 'var(--control-sm)',
  borderRadius: 'var(--radius-md)',
  color: 'var(--text-secondary)',
  '&:hover': { backgroundColor: 'var(--bg-subtle)', color: 'var(--text)' },
} as const;

/**
 * The details of a document chosen in the list (canvas ProjetoDocumentos, in the drawer of ADR-0081): the name, the
 * size and the version, "Abrir" for what the browser shows and "Baixar"; the "⋮" with the statuses the person may set,
 * "Vincular ao cronograma" and "Excluir" (ADR-0045); the status with who changed it, the link, the folder and the
 * versions, newest first, each to download.
 */
export function DocumentDrawer({
  projectId,
  documentId,
  termsFolderId,
  onClose,
  notify,
}: {
  projectId: string;
  /** The document chosen; none while the drawer is closed. */
  documentId: string | undefined;
  /** The folder of the signed terms, whose documents do not change (ADR-0093). */
  termsFolderId?: string;
  onClose: () => void;
  notify: (message: string) => void;
}) {
  const { t } = useTranslation();
  const { data } = useQuery({
    ...documentQuery(projectId, documentId ?? ''),
    enabled: documentId !== undefined,
  });

  return (
    <DetailsDrawer
      open={documentId !== undefined}
      onClose={onClose}
      label={t('documents.details.label')}
    >
      {data !== undefined && data.document.id === documentId && (
        <DocumentDetailsContent
          projectId={projectId}
          details={data}
          signedTerm={data.document.folderId === termsFolderId}
          onRemoved={onClose}
          notify={notify}
        />
      )}
    </DetailsDrawer>
  );
}

function DocumentDetailsContent({
  projectId,
  details,
  signedTerm,
  onRemoved,
  notify,
}: {
  projectId: string;
  details: DocumentDetails;
  /** A signed term, which neither changes status nor is removed (ADR-0093). */
  signedTerm: boolean;
  onRemoved: () => void;
  notify: (message: string) => void;
}) {
  const { t } = useTranslation();
  const { formatDate, formatDateTime } = useDates();
  const queryClient = useQueryClient();
  const { document, versions, permissions } = details;
  const [menu, setMenu] = useState<HTMLElement>();
  const [linking, setLinking] = useState(false);
  const [removing, setRemoving] = useState(false);
  const [failure, setFailure] = useState<string>();
  const statuses = signedTerm ? [] : statusesFor(permissions);
  const hasActions = statuses.length > 0 || (permissions.canDelete && !signedTerm);

  const changed = () => {
    void queryClient.invalidateQueries({ queryKey: [...queryKeys.documents, projectId] });
    void queryClient.invalidateQueries({ queryKey: queryKeys.audit });
  };

  const failed = (problem: unknown) =>
    problemType(problem) === documentProblems.projectClosed
      ? t('documents.messages.closed')
      : t('documents.messages.failed');

  const setStatus = async (status: DocumentStatus) => {
    setMenu(undefined);
    if (status === document.status) {
      return;
    }
    try {
      const { error, response } = await api.PUT(
        '/api/projects/{projectId}/documents/{documentId}/status',
        { params: { path: { projectId, documentId: document.id } }, body: { status } },
      );
      if (response.ok) {
        changed();
        notify(
          t('documents.messages.statusChanged', {
            name: document.name,
            status: t(`documents.statuses.${status}`).toLowerCase(),
          }),
        );
        return;
      }
      setFailure(failed(error));
    } catch {
      setFailure(t('documents.messages.failed'));
    }
  };

  const remove = async () => {
    try {
      const { error, response } = await api.DELETE(
        '/api/projects/{projectId}/documents/{documentId}',
        { params: { path: { projectId, documentId: document.id } } },
      );
      setRemoving(false);
      if (response.ok) {
        changed();
        notify(t('documents.messages.removed', { name: document.name }));
        onRemoved();
        return;
      }
      setFailure(failed(error));
    } catch {
      setRemoving(false);
      setFailure(t('documents.messages.failed'));
    }
  };

  const approved = document.status === 'Approved';

  return (
    <>
      <Box component="section" sx={section}>
        <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-3)' }}>
          <Box
            component="span"
            aria-hidden
            sx={{
              flex: 'none',
              display: 'grid',
              placeItems: 'center',
              width: 44,
              height: 52,
              boxSizing: 'border-box',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-md)',
              backgroundColor: 'var(--bg-surface)',
              color: 'var(--text-secondary)',
            }}
          >
            <FileIcon name={document.name} size={24} />
          </Box>
          <Box sx={{ minWidth: 0 }}>
            <Box
              component="h2"
              sx={{
                margin: 0,
                fontSize: 16,
                lineHeight: '24px',
                fontWeight: 500,
                color: 'var(--text)',
                overflowWrap: 'anywhere',
              }}
            >
              {document.name}
            </Box>
            <Box
              component="span"
              sx={{ fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' }}
            >
              {t('documents.details.sizeAndVersion', {
                size: sizeText(document.size),
                version: document.version,
              })}
            </Box>
          </Box>
        </Box>
        <Box
          sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', flexWrap: 'wrap' }}
        >
          {opensInBrowser(document.name) && (
            <Button
              variant="outlined"
              size="small"
              startIcon={<OpenInNewOutlined />}
              href={documentFileUrl(projectId, document.id)}
              target="_blank"
              rel="noopener noreferrer"
            >
              {t('documents.details.open')}
            </Button>
          )}
          <Button
            variant="outlined"
            size="small"
            startIcon={<DownloadOutlined />}
            href={documentFileUrl(projectId, document.id, { download: true })}
          >
            {t('documents.details.download')}
          </Button>
          {hasActions && (
            <IconButton
              size="small"
              aria-label={t('documents.details.more')}
              aria-haspopup="menu"
              onClick={(event) => {
                setMenu(event.currentTarget);
              }}
              sx={quietSx}
            >
              <MoreVertOutlined sx={{ fontSize: 'var(--icon-md)' }} />
            </IconButton>
          )}
        </Box>
        {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
      </Box>
      <Box component="section" sx={section}>
        <Box
          component="dl"
          sx={{
            display: 'grid',
            gridTemplateColumns: '112px minmax(0, 1fr)',
            alignItems: 'baseline',
            gap: 'var(--space-3)',
            margin: 0,
            '& dt': { fontSize: 13, lineHeight: '20px', color: 'var(--text-secondary)' },
            '& dd': { margin: 0, minWidth: 0, overflowWrap: 'anywhere' },
          }}
        >
          <dt>{t('documents.details.status')}</dt>
          <dd>
            <DocumentStatusBadge status={document.status} />
          </dd>
          <dt>{t(approved ? 'documents.details.approvedBy' : 'documents.details.statusBy')}</dt>
          <dd>
            {t('documents.details.byOn', {
              name: document.statusChanged.name,
              date: formatDate(document.statusChanged.at),
            })}
          </dd>
          <dt>{t('documents.details.schedule')}</dt>
          <dd>
            {document.link === null ? (
              t('documents.link.none')
            ) : (
              <Box
                component={Link}
                to={`/projects/${projectId}/schedule`}
                sx={{
                  color: 'var(--action-text)',
                  textDecoration: 'none',
                  '&:hover': { textDecoration: 'underline' },
                }}
              >
                {document.link.name}
              </Box>
            )}
          </dd>
          <dt>{t('documents.details.folder')}</dt>
          <dd>{document.folderName}</dd>
        </Box>
      </Box>
      <Box component="section" sx={section} aria-labelledby={`versions-${document.id}`}>
        <Box component="h3" id={`versions-${document.id}`} sx={labelSx}>
          {t('documents.details.versions')}
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
          {versions.map((version) => (
            <Box
              component="li"
              key={version.number}
              sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-3)' }}
            >
              <Box
                component="span"
                sx={{
                  flex: 'none',
                  minWidth: 32,
                  boxSizing: 'border-box',
                  padding: '2px 6px',
                  borderRadius: 'var(--radius-sm)',
                  backgroundColor: 'var(--bg-subtle)',
                  color: 'var(--text-secondary)',
                  fontFamily: 'var(--font-mono)',
                  fontSize: 12,
                  lineHeight: '16px',
                  fontWeight: 500,
                  textAlign: 'center',
                }}
              >
                {t('documents.details.versionChip', { number: version.number })}
              </Box>
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Box>{version.uploaded.name}</Box>
                <Box
                  sx={{
                    fontSize: 12,
                    lineHeight: '16px',
                    color: 'var(--text-secondary)',
                    fontVariantNumeric: 'tabular-nums',
                  }}
                >
                  {t('documents.details.versionWhen', {
                    when: formatDateTime(version.uploaded.at),
                    size: sizeText(version.size),
                  })}
                </Box>
              </Box>
              <IconButton
                size="small"
                aria-label={t('documents.details.downloadVersion', { number: version.number })}
                href={documentFileUrl(projectId, document.id, {
                  version: version.number,
                  download: true,
                })}
                sx={quietSx}
              >
                <DownloadOutlined sx={{ fontSize: 'var(--icon-sm)' }} />
              </IconButton>
            </Box>
          ))}
        </Box>
      </Box>
      <Menu
        anchorEl={menu}
        open={menu !== undefined}
        onClose={() => {
          setMenu(undefined);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { width: 240, marginTop: 'var(--space-1)' } },
          list: { 'aria-label': t('documents.details.more') },
        }}
      >
        {statuses.length > 0 && <ListSubheader>{t('documents.menu.status')}</ListSubheader>}
        {statuses.map((status) => {
          const current = status === document.status;
          return (
            <MenuItem
              key={status}
              role="menuitemradio"
              aria-checked={current}
              selected={current}
              onClick={() => {
                void setStatus(status);
              }}
            >
              <Box component="span" sx={{ flex: 1 }}>
                {t(`documents.statuses.${status}`)}
              </Box>
              {current && (
                <CheckOutlined aria-hidden sx={{ color: 'var(--action-text) !important' }} />
              )}
            </MenuItem>
          );
        })}
        {statuses.length > 0 && <Divider sx={{ margin: 'var(--space-1) 0' }} />}
        {permissions.canUpload && !signedTerm && (
          <MenuItem
            onClick={() => {
              setMenu(undefined);
              setLinking(true);
            }}
          >
            <LinkOutlined aria-hidden />
            {t('documents.menu.link')}
          </MenuItem>
        )}
        {permissions.canDelete && !signedTerm && (
          <MenuItem
            onClick={() => {
              setMenu(undefined);
              setRemoving(true);
            }}
          >
            <DeleteOutlineOutlined aria-hidden />
            {t('documents.menu.remove')}
          </MenuItem>
        )}
      </Menu>
      {linking && (
        <LinkDocumentDialog
          projectId={projectId}
          document={document}
          onClose={() => {
            setLinking(false);
          }}
          onSaved={() => {
            setLinking(false);
            changed();
            notify(t('documents.messages.linked', { name: document.name }));
          }}
        />
      )}
      <Dialog
        open={removing}
        alert
        title={t('documents.remove.title')}
        onClose={() => {
          setRemoving(false);
        }}
        actions={
          <>
            <Button
              variant="outlined"
              onClick={() => {
                setRemoving(false);
              }}
            >
              {t('documents.remove.cancel')}
            </Button>
            <Button variant="contained" color="error" onClick={() => remove()}>
              {t('documents.remove.confirm')}
            </Button>
          </>
        }
      >
        {t('documents.remove.text', { name: document.name })}
      </Dialog>
    </>
  );
}
