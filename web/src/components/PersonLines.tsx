import Box from '@mui/material/Box';
import { detailLineSx } from './detailLineSx';

/**
 * A person in a list to choose: the name and, below, the e-mail that tells apart people of the same name, and a note,
 * as "Nunca entrou". An anonymized person has no e-mail, and the line is left out.
 */
export function PersonLines({
  name,
  email,
  note,
}: {
  name: string;
  email?: string | null;
  note?: string;
}) {
  return (
    <Box component="span" sx={{ display: 'flex', flexDirection: 'column', minWidth: 0 }}>
      <span>{name}</span>
      {email !== undefined && email !== null && email !== '' && (
        <Box component="span" sx={detailLineSx}>
          {email}
        </Box>
      )}
      {note !== undefined && (
        <Box component="span" sx={detailLineSx}>
          {note}
        </Box>
      )}
    </Box>
  );
}
