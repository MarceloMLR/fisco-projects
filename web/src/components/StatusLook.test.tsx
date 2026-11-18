import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Alert } from './Alert';
import { StatusBadge } from './StatusBadge';

// Spec: estrutura-das-telas, requirement "Cores e fontes do design system" (ADR-0159).
describe('Cores e fontes do design system', () => {
  it('Alerta informativo em grafite', () => {
    render(<Alert tone="info">Nenhum projeto em aberto.</Alert>);

    const alert = screen.getByRole('status');
    expect(alert).toHaveStyle({ backgroundColor: 'var(--status-neutral-bg)' });
    expect(alert.querySelector('svg')).toHaveStyle({ color: 'var(--status-neutral-text)' });
  });

  it('Etiqueta de status com canto pequeno', () => {
    render(
      <StatusBadge tone="info" icon={CheckCircleOutlined}>
        Em andamento
      </StatusBadge>,
    );

    const badge = screen.getByText('Em andamento');
    expect(badge).toHaveStyle({
      borderRadius: 'var(--radius-sm)',
      backgroundColor: 'var(--status-info-bg)',
      color: 'var(--status-info-text)',
    });
  });
});
