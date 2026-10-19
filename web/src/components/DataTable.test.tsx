import CheckCircleOutlined from '@mui/icons-material/CheckCircleOutlined';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { useState } from 'react';
import { ptBR } from '@mui/x-data-grid/locales';
import { afterEach, describe, expect, it, vi } from 'vitest';
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

/** Waits a real while, as a person would see the screen. */
function wait(ms: number) {
  return act(() => new Promise<void>((resolve) => setTimeout(resolve, ms)));
}

function LoadingTable({ rows }: { rows: Person[] }) {
  return (
    <AppThemeProvider>
      <div style={{ height: 600, display: 'flex' }}>
        <DataTable
          label="People"
          rows={rows}
          columns={[{ field: 'name', headerName: 'Person', flex: 1 }]}
          rowCount={rows.length}
          page={1}
          pageSize={25}
          onPageChange={vi.fn()}
          loading
          emptyLabel="Nobody here"
        />
      </div>
    </AppThemeProvider>
  );
}

describe('Espera nas listas', () => {
  it('Resposta demorada com indicador', async () => {
    render(<LoadingTable rows={people} />);
    const table = screen.getByRole('grid', { name: 'People' });

    // A short wait shows nothing over the rows on screen.
    await wait(150);
    expect(within(table).queryByRole('progressbar')).not.toBeInTheDocument();
    expect(within(table).getByText('Beatriz Lima')).toBeVisible();

    // Past the moment, a bar shows on top, and the rows stay.
    await waitFor(() => {
      expect(within(table).getByRole('progressbar')).toBeInTheDocument();
    });
    expect(within(table).getByText('Beatriz Lima')).toBeInTheDocument();
    expect(table.querySelector('.MuiDataGrid-skeletonLoadingOverlay')).toBeNull();
  });

  it('Primeira abertura da lista', async () => {
    render(<LoadingTable rows={[]} />);
    const table = screen.getByRole('grid', { name: 'People' });

    // At first only the header: no skeleton yet, and never the message of an empty list.
    await wait(150);
    expect(within(table).getByRole('columnheader', { name: 'Person' })).toBeInTheDocument();
    expect(table.querySelector('.MuiDataGrid-skeletonLoadingOverlay')).toBeNull();
    expect(screen.queryByText('Nobody here')).not.toBeInTheDocument();

    await waitFor(() => {
      expect(table.querySelector('.MuiDataGrid-skeletonLoadingOverlay')).not.toBeNull();
    });
    expect(screen.queryByText('Nobody here')).not.toBeInTheDocument();
  });
});

describe('Tabela sem redesenho', () => {
  it('Tela desenhada de novo sem linhas novas', () => {
    const drawn = vi.fn();
    const columns = [
      {
        field: 'name',
        headerName: 'Person',
        flex: 1,
        renderCell: ({ row }: { row: Person }) => {
          drawn(row.id);
          return row.name;
        },
      },
    ];
    const noPageChange = vi.fn();
    function Screen() {
      const [filter, setFilter] = useState('all');
      return (
        <AppThemeProvider>
          <div style={{ height: 600, display: 'flex' }}>
            <DataTable
              label="People"
              rows={people}
              columns={columns}
              rowCount={people.length}
              page={1}
              pageSize={25}
              onPageChange={noPageChange}
              tools={
                <button
                  type="button"
                  onClick={() => {
                    setFilter('active');
                  }}
                >
                  {filter}
                </button>
              }
            />
          </div>
        </AppThemeProvider>
      );
    }
    render(<Screen />);
    expect(screen.getByText('Marina Costa')).toBeInTheDocument();
    drawn.mockClear();

    fireEvent.click(screen.getByRole('button', { name: 'all' }));

    expect(screen.getByRole('button', { name: 'active' })).toBeInTheDocument();
    expect(drawn).not.toHaveBeenCalled();
  });
});

describe('Menus fecham na hora', () => {
  // jsdom lays nothing out; with a height, the menu of MUI would take its usual time to leave.
  const height = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientHeight');
  afterEach(() => {
    if (height !== undefined) {
      Object.defineProperty(HTMLElement.prototype, 'clientHeight', height);
    }
  });

  it('Escolha num campo de seleção', async () => {
    Object.defineProperty(HTMLElement.prototype, 'clientHeight', {
      configurable: true,
      get: () => 200,
    });
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
    fireEvent.mouseDown(screen.getByRole('combobox', { name: 'Status' }));
    const option = await screen.findByRole('option', { name: 'Active' });
    const menu = option.closest('.MuiPaper-root');

    fireEvent.click(option);

    // The menu leaves without an animation, and is gone right after.
    expect(menu).toHaveStyle({ opacity: '0' });
    expect((menu as HTMLElement | null)?.style.transition).toMatch(/^opacity 0ms/);
    await wait(20);
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
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

  it('Linhas de grupo que se escolhem', () => {
    const onRowChoose = vi.fn();
    const isGroup = (person: Person) => person.id === '1';
    const table = (choosable: boolean) => (
      <AppThemeProvider>
        <div style={{ height: 600, display: 'flex' }}>
          <DataTable
            label="People"
            rows={people}
            columns={[{ field: 'name', headerName: 'Person', flex: 1 }]}
            rowCount={3}
            page={1}
            pageSize={3}
            onPageChange={vi.fn()}
            onRowChoose={onRowChoose}
            isGroupRow={isGroup}
            groupRowsChoosable={choosable}
          />
        </div>
      </AppThemeProvider>
    );
    const { rerender } = render(table(false));
    const groupRow = () =>
      within(screen.getByRole('grid', { name: 'People' })).getAllByRole('row')[1] ?? document.body;

    fireEvent.click(within(groupRow()).getByText('Beatriz Lima'));
    expect(onRowChoose).not.toHaveBeenCalled();

    rerender(table(true));
    expect(groupRow()).toHaveClass('row-group');
    fireEvent.click(within(groupRow()).getByText('Beatriz Lima'));
    fireEvent.keyDown(within(groupRow()).getByRole('gridcell'), { key: 'Enter' });

    expect(onRowChoose.mock.calls.map(([person]) => (person as Person).name)).toEqual([
      'Beatriz Lima',
      'Beatriz Lima',
    ]);
  });
});
