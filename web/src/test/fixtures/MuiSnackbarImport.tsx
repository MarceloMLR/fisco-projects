// The notice of MUI used straight, which the lint refuses (ADR-0133): every notice is the one of the structure of the
// screens, by useNotify.
import Snackbar from '@mui/material/Snackbar';
import SnackbarContent from '@mui/material/SnackbarContent';

export function Straight() {
  return (
    <>
      <Snackbar open message="Salvo." />
      <SnackbarContent message="Salvo." />
    </>
  );
}
