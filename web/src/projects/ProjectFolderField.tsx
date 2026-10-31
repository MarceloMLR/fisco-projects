import FolderOpenOutlined from '@mui/icons-material/FolderOpenOutlined';
import Box from '@mui/material/Box';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { TextField } from '../components/TextField';
import { type ChosenFolder, ProjectFolderDialog } from './ProjectFolderDialog';
import { Button } from '../components/Button';

/**
 * "Pasta dos arquivos" of a project (ADR-0122): the folder the files go to, "Escolher pasta…" and, with a folder
 * chosen, "Usar a pasta criada pelo sistema". The value is the folder chosen, or null for the one the system creates,
 * shown by the text given. Locked, after the project stored files, it only shows the folder and why it stays.
 */
export function ProjectFolderField({
  value,
  onChange,
  systemText,
  systemHelper,
  chosenHelper,
  projectId,
  locked,
  error,
}: {
  value: ChosenFolder | null;
  onChange: (folder: ChosenFolder | null) => void;
  /** The folder the system creates, or the one the project has, as the field shows it. */
  systemText: string;
  systemHelper: string;
  chosenHelper: string;
  /** The project being edited; none for a new one. */
  projectId?: string;
  /** Why the folder no longer changes; the actions go away. */
  locked?: string;
  error?: string;
}) {
  const { t } = useTranslation();
  const [choosing, setChoosing] = useState(false);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: '6px', minWidth: 0 }}>
      <TextField
        label={t('projects.folder.field')}
        icon={FolderOpenOutlined}
        readOnly
        value={value?.path ?? systemText}
        helper={locked ?? (value === null ? systemHelper : chosenHelper)}
        error={error}
      />
      {locked === undefined && (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 'var(--space-2)' }}>
          <Button
            variant="text"
            size="small"
            onClick={() => {
              setChoosing(true);
            }}
          >
            {t('projects.folder.choose')}
          </Button>
          {value !== null && (
            <Button
              variant="text"
              size="small"
              onClick={() => {
                onChange(null);
              }}
            >
              {t('projects.folder.useSystem')}
            </Button>
          )}
        </Box>
      )}
      {choosing && (
        <ProjectFolderDialog
          projectId={projectId}
          onClose={() => {
            setChoosing(false);
          }}
          onChoose={(folder) => {
            setChoosing(false);
            onChange(folder);
          }}
        />
      )}
    </Box>
  );
}
