// The button of MUI used straight, which the lint refuses (ADR-0131): every button comes from src/components/Button.
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';

export function Straight() {
  return (
    <>
      <Button>Salvar</Button>
      <IconButton aria-label="Fechar" />
    </>
  );
}
