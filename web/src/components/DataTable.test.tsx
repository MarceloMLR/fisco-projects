import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { useState } from 'react';
import { ptBR } from '@mui/x-data-grid/locales';
import { describe, expect, it, vi } from 'vitest';
import { AppThemeProvider } from '../theme/AppThemeProvider';
import { DataTable } from './DataTable';
import { Select } from './Select';
import { StatusBadge } from './StatusBadge';

interface Person {
  id: string;
  name: string;
  active: boolean;
}

const gridText = ptBR.components.MuiDataGrid.defaultProps.localeText;

const people: Person[] = [
  { id: '1', name: 'Beatriz Lima', active: true },
  { id: '2', name: 'Lívia Prado', active: false },
  { id: '3', name: 'Marina Costa', active: true },
];

function Table({ onPageChange }: { onPageChange: (page: number) => void }) {
  return (
    <AppThemeProvider>
      <div style={{ height: 600, display: 'flex' }}>
        <DataTable
          label="People"
          rows={people.slice(0, 2)}
          columns={[{ field: 'name', headerName: 'Person', flex: 1 }]}
          rowCount={people.length}
          page={1}
          pageSize={2}
          onPageChange={onPageChange}
          isRowMuted={(person) => !person.active}
        />
      </div>
    </AppThemeProvider>
  );
}

describe('Tabela padrão', () => {
  it('Linhas, paginação em pt-BR e linha esmaecida', () => {
    const onPageChange = vi.fn();
    render(<Table onPageChange={onPageChange} />);

    const table = screen.getByRole('grid', { name: 'People' });
    expect(within(table).getByRole('columnheader', { name: 'Person' })).toBeInTheDocument();
    const rows = within(table).getAllByRole('row').slice(1);
    expect(rows.map((row) => row.textContent)).toEqual(['Beatriz Lima', 'Lívia Prado']);
    expect(rows[1]).toHaveClass('row-muted');
    expect(rows[0]).not.toHaveClass('row-muted');
    // The pagination speaks pt-BR, from the locale of the Data Grid.
    expect(
      screen.getByText(
        gridText.paginationDisplayedRows?.({ from: 1, to: 2, count: 3, estimated: undefined }) ??
          '',
      ),
    ).toBeInTheDocument();

    fireEvent.click(
      screen.getByRole('button', { name: gridText.paginationItemAriaLabel?.('next') ?? '' }),
    );
    expect(onPageChange).toHaveBeenCalledWith(2);
  });
});

describe('Seleção', () => {
  it('Escolher uma opção', async () => {
    function Choice() {
      const [value, setValue] = useState<'all' | 'active'>('all');
      return (
        <AppThemeProvider>
          <Select
            label="Status"
            value={value}
            options={[
              { value: 'all', label: 'Every status' },
              { value: 'active', label: 'Active' },
            ]}
            onChange={setValue}
          />
        </AppThemeProvider>
      );
    }
    render(<Choice />);

    const select = screen.getByRole('combobox', { name: 'Status' });
    expect(select).toHaveTextContent('Every status');
    fireEvent.mouseDown(select);
    fireEvent.click(await screen.findByRole('option', { name: 'Active' }));

    expect(screen.getByRole('combobox', { name: 'Status' })).toHaveTextContent('Active');
  });
});

describe('Situação', () => {
  it('Ícone, cor e texto', () => {
    render(
      <AppThemeProvider>
        <StatusBadge tone="success" icon={CheckCircleOutlined}>
          Active
        </StatusBadge>
      </AppThemeProvider>,
    );

    const badge = screen.getByText('Active');
    expect(badge.querySelector('svg')).not.toBeNull();
    expect(getComputedStyle(badge).backgroundColor).not.toBe('');
  });

  it('Linha escolhida e texto do rodapé', () => {
    const onRowChoose = vi.fn();
    render(
      <AppThemeProvider>
        <div style={{ height: 600, display: 'flex' }}>
          <DataTable
            label="People"
            rows={people}
            columns={[{ field: 'name', headerName: 'Person', flex: 1 }]}
            rowCount={312}
            page={1}
            pageSize={3}
            onPageChange={vi.fn()}
            selectedRowId="2"
            onRowChoose={onRowChoose}
            displayedRowsLabel={({ from, to, count }) =>
              `${String(from)}-${String(to)} of ${String(count)} in the period`
            }
          />
        </div>
      </AppThemeProvider>,
    );

    const rows = within(screen.getByRole('grid', { name: 'People' }))
      .getAllByRole('row')
      .slice(1);
    expect(rows.map((row) => row.getAttribute('aria-selected'))).toEqual([
      'false',
      'true',
      'false',
    ]);
    expect(screen.getByText('1-3 of 312 in the period')).toBeInTheDocument();

    fireEvent.click(within(rows[2] ?? document.body).getByText('Marina Costa'));
    fireEvent.keyDown(within(rows[0] ?? document.body).getByRole('gridcell'), { key: 'Enter' });

    expect(onRowChoose.mock.calls.map(([person]) => (person as Person).name)).toEqual([
      'Marina Costa',
      'Beatriz Lima',
    ]);
  });
});
