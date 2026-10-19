import { fireEvent, render, screen, within } from '@testing-library/react';
import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import { AppThemeProvider } from '../theme/AppThemeProvider';
import { DueDate } from './DueDate';
import { MultiSelect } from './MultiSelect';
import { ProgressBar } from './ProgressBar';
import { ScheduleStrip } from './ScheduleStrip';
import { stripLayout } from './scheduleStripLayout';

// The design system components of the schedule: ScheduleStrip (fp-strip), ProgressBar (fp-progress) and DueDate
// (fp-due), with the color never as the only information (ADR-0065, ADR-0071).
describe('Faixa de fases', () => {
  it('Fases, marcos e hoje pelos dias do período', () => {
    // Ten days, from 01/10 to 10/10: each day is 10% of the strip.
    const layout = stripLayout(
      '2026-10-01',
      '2026-10-10',
      [
        { start: '2026-10-01', end: '2026-10-04' },
        { start: '2026-10-05', end: '2026-10-10' },
      ],
      ['2026-10-04', '2026-10-10'],
      '2026-10-03',
    );

    expect(layout.phases).toEqual([
      { left: 0, width: 40 },
      { left: 40, width: 60 },
    ]);
    expect(layout.milestones).toEqual([35, 95]);
    expect(layout.today).toBe(25);
  });

  it('Hoje antes do início e depois do fim', () => {
    expect(stripLayout('2026-10-01', '2026-10-10', [], [], '2026-09-20').today).toBe(0);
    expect(stripLayout('2026-10-01', '2026-10-10', [], [], '2026-11-20').today).toBe(100);
    expect(stripLayout('2026-10-05', '2026-10-05', [], ['2026-10-05'], '2026-10-05')).toEqual({
      phases: [],
      milestones: [50],
      today: 50,
    });
  });

  it('Fase atrasada, parte concluída e nome de cada fase', () => {
    render(
      <AppThemeProvider>
        <ScheduleStrip
          start="2026-10-01"
          end="2026-10-10"
          phases={[
            {
              name: 'Levantamento',
              start: '2026-10-01',
              end: '2026-10-04',
              progress: 100,
              late: false,
            },
            {
              name: 'Configuração',
              start: '2026-10-05',
              end: '2026-10-10',
              progress: 38,
              late: true,
            },
          ]}
          milestones={['2026-10-10']}
          today="2026-10-06"
          label="Fases de 01/10 a 10/10"
        />
      </AppThemeProvider>,
    );

    const strip = screen.getByRole('img', { name: 'Fases de 01/10 a 10/10' });
    const late = within(strip).getByTitle('Configuração');
    expect(late).toHaveAttribute('data-late', 'true');
    expect(late).toHaveStyle({ left: '40%', width: '60%' });
    expect(late.firstElementChild).toHaveStyle({ width: '38%' });
    expect(within(strip).getByTitle('Levantamento')).toHaveAttribute('data-late', 'false');
    expect(within(strip).getByTestId('strip-milestone')).toHaveStyle({ left: '95%' });
    expect(within(strip).getByTestId('strip-today')).toHaveStyle({ left: '55%' });
  });
});

describe('Barra de progresso', () => {
  it('Percentual ao lado da barra', () => {
    render(
      <AppThemeProvider>
        <ProgressBar value={62.4} label="Progresso do projeto" />
        <ProgressBar value={140} label="Além do fim" />
      </AppThemeProvider>,
    );

    expect(screen.getByRole('progressbar', { name: 'Progresso do projeto' })).toHaveAttribute(
      'aria-valuenow',
      '62',
    );
    expect(screen.getByText('62%')).toBeVisible();
    expect(screen.getByRole('progressbar', { name: 'Além do fim' })).toHaveAttribute(
      'aria-valuenow',
      '100',
    );
  });
});

describe('Prazo', () => {
  it('Ícone, cor e texto do atraso', () => {
    render(
      <AppThemeProvider>
        <DueDate state="overdue">Atrasada</DueDate>
      </AppThemeProvider>,
    );

    const late = screen.getByText('Atrasada');
    expect(late).toHaveAttribute('data-state', 'overdue');
    expect(late.querySelector('svg')).not.toBeNull();
    expect(late).toHaveStyle({ color: 'var(--status-danger-text)' });
  });
});

function Choices() {
  const [value, setValue] = useState<string[]>(['b']);
  return (
    <AppThemeProvider>
      <MultiSelect
        label="Depends on"
        value={value}
        options={[
          { value: 'a', label: 'Map the processes', group: 'Survey' },
          { value: 'b', label: 'Opening meeting', group: 'Survey' },
          { value: 'c', label: 'Set up the taxes', group: 'Setup' },
        ]}
        onChange={setValue}
        noOptionsText="Nothing to choose"
        helper="Starts after these end."
      />
      <output data-testid="chosen">{value.join(',')}</output>
    </AppThemeProvider>
  );
}

describe('Escolha de várias opções', () => {
  it('Escolher e tirar opções, por grupo', async () => {
    render(<Choices />);

    const field = screen.getByRole('combobox', { name: 'Depends on' });
    expect(field).toHaveAccessibleDescription('Starts after these end.');
    expect(screen.getByRole('button', { name: 'Opening meeting' })).toBeInTheDocument();
    fireEvent.mouseDown(field);
    expect(screen.getAllByRole('option').map((option) => option.textContent)).toEqual([
      'Map the processes',
      'Opening meeting',
      'Set up the taxes',
    ]);
    expect(screen.getByText('Setup')).toBeVisible();
    fireEvent.click(await screen.findByRole('option', { name: 'Set up the taxes' }));

    expect(screen.getByTestId('chosen')).toHaveTextContent('b,c');
    fireEvent.click(
      within(screen.getByRole('button', { name: 'Opening meeting' })).getByTestId('CancelIcon'),
    );
    expect(screen.getByTestId('chosen')).toHaveTextContent('c');
  });
});
