import ChevronRightOutlined from '@mui/icons-material/ChevronRightOutlined';
import FolderOutlined from '@mui/icons-material/FolderOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useQuery } from '@tanstack/react-query';
import type { TFunction } from 'i18next';
import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { ProjectFolderChoice, ProjectFolderRefusal } from '../api/client';
import { projectFoldersQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { CodeChip } from '../components/Card';
import { Dialog } from '../components/Dialog';

/** A folder chosen in the dialog: its id and the path of names from the root, as "Clientes / Alfa". */
export interface ChosenFolder {
  id: string;
  path: string;
}

interface Step {
  id: string;
  name: string;
}

/** The folders that open: the others hold nothing a project may use (ADR-0122). */
const closed: readonly ProjectFolderRefusal[] = ['InUse', 'InsideProject', 'SystemFolder'];

/** Why a folder may not be the folder of the project, as the dialog says it. */
function refusalText(t: TFunction, choice: Pick<ProjectFolderChoice, 'refusal' | 'projectCode'>) {
  return choice.refusal === null
    ? undefined
    : t(`projects.folder.refusals.${choice.refusal}`, { code: choice.projectCode ?? '' });
}

const rowSx = {
  display: 'flex',
  alignItems: 'center',
  gap: 'var(--space-2)',
  width: '100%',
  boxSizing: 'border-box',
  minHeight: 44,
  padding: 'var(--space-2) var(--space-3)',
  border: 0,
  borderRadius: 'var(--radius-md)',
  backgroundColor: 'transparent',
  color: 'var(--text)',
  fontFamily: 'var(--font-sans)',
  fontSize: 14,
  lineHeight: '20px',
  textAlign: 'left',
  '& > svg': { flex: 'none', fontSize: 20, color: 'var(--text-secondary)' },
} as const;

const openRowSx = {
  ...rowSx,
  cursor: 'pointer',
  '&:hover': { backgroundColor: 'var(--bg-hover)' },
  '&:focus-visible': { outline: '2px solid var(--focus-ring)', outlineOffset: -2 },
} as const;

/**
 * "Escolher pasta" (ADR-0122): the folders of the file storage from its root, each one opening the folders inside it,
 * with the path above to go back and "Usar esta pasta" for the folder open, only when the project may use it. The
 * folder of another project shows its code and does not open; the root is never chosen.
 */
export function ProjectFolderDialog({
  projectId,
  onClose,
  onChoose,
}: {
  /** The project being edited, whose own folder may be chosen again; none for a new project. */
  projectId?: string;
  onClose: () => void;
  onChoose: (folder: ChosenFolder) => void;
}) {
  const { t } = useTranslation();
  const [trail, setTrail] = useState<Step[]>([]);
  const parentId = trail.at(-1)?.id;
  const { data, isError, isPending } = useQuery(projectFoldersQuery(parentId, projectId));
  const current = data?.current ?? null;
  const hintId = useId();
  // At the root, nothing is chosen yet; in a folder the project may not use, the dialog says why.
  const hint =
    data === undefined
      ? undefined
      : current === null
        ? t('projects.folder.openOne')
        : current.refusal === null
          ? undefined
          : t(`projects.folder.why.${current.refusal}`, { code: current.projectCode ?? '' });

  return (
    <Dialog
      open
      title={t('projects.folder.dialogTitle')}
      onClose={onClose}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('projects.form.cancel')}
          </Button>
          <Button
            variant="contained"
            disabled={current?.refusal !== null}
            aria-describedby={hint === undefined ? undefined : hintId}
            onClick={() => {
              if (current !== null) {
                onChoose({ id: current.id, path: trail.map((step) => step.name).join(' / ') });
              }
            }}
          >
            {t('projects.folder.use')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
        <Box
          component="nav"
          aria-label={t('projects.folder.path')}
          sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '2px' }}
        >
          {[{ id: '', name: t('projects.folder.root') }, ...trail].map((step, index, steps) => {
            const last = index === steps.length - 1;
            return (
              <Box
                key={step.id}
                component="span"
                sx={{ display: 'inline-flex', alignItems: 'center', gap: '2px', minWidth: 0 }}
              >
                {index > 0 && (
                  <ChevronRightOutlined
                    aria-hidden
                    sx={{ fontSize: 16, color: 'var(--text-secondary)' }}
                  />
                )}
                <Button
                  variant="text"
                  size="small"
                  aria-current={last ? 'location' : undefined}
                  disabled={last}
                  onClick={() => {
                    setTrail(trail.slice(0, index));
                  }}
                  sx={{ minWidth: 0, '&.Mui-disabled': { color: 'var(--text)' } }}
                >
                  {step.name}
                </Button>
              </Box>
            );
          })}
        </Box>
        {isError && <Alert tone="danger">{t('projects.folder.failed')}</Alert>}
        {isPending && (
          <Box sx={{ fontSize: 14, color: 'var(--text-secondary)' }}>
            {t('projects.folder.loading')}
          </Box>
        )}
        {data?.folders.length === 0 && (
          <Box sx={{ fontSize: 14, color: 'var(--text-secondary)' }}>
            {t('projects.folder.empty')}
          </Box>
        )}
        {data !== undefined && data.folders.length > 0 && (
          <Box
            component="ul"
            aria-label={t('projects.folder.folders')}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: '2px',
              maxHeight: 320,
              margin: 0,
              padding: 0,
              overflow: 'auto',
              listStyle: 'none',
            }}
          >
            {data.folders.map((folder) => {
              const opens = folder.refusal === null || !closed.includes(folder.refusal);
              const note = refusalText(t, folder);
              const content = (
                <>
                  <FolderOutlined aria-hidden />
                  <Box component="span" sx={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>
                    {folder.name}
                    {note !== undefined && (
                      <Box
                        component="span"
                        sx={{
                          display: 'block',
                          fontSize: 12,
                          lineHeight: '16px',
                          color: 'var(--text-secondary)',
                        }}
                      >
                        {note}
                      </Box>
                    )}
                  </Box>
                  {folder.projectCode !== null && <CodeChip code={folder.projectCode} />}
                  {opens && <ChevronRightOutlined aria-hidden />}
                </>
              );
              return (
                <li key={folder.id}>
                  {opens ? (
                    <Box
                      component="button"
                      type="button"
                      onClick={() => {
                        setTrail([...trail, { id: folder.id, name: folder.name }]);
                      }}
                      sx={openRowSx}
                    >
                      {content}
                    </Box>
                  ) : (
                    <Box aria-disabled="true" sx={{ ...rowSx, color: 'var(--text-secondary)' }}>
                      {content}
                    </Box>
                  )}
                </li>
              );
            })}
          </Box>
        )}
        {hint !== undefined && (
          <Box
            id={hintId}
            sx={{ fontSize: 12, lineHeight: '16px', color: 'var(--text-secondary)' }}
          >
            {hint}
          </Box>
        )}
      </Box>
    </Dialog>
  );
}
