import Box from '@mui/material/Box';
import {
  DataGrid,
  type GridCellParams,
  type GridColDef,
  type GridRowId,
  type GridRowParams,
  type GridValidRowModel,
} from '@mui/x-data-grid';
import { ptBR } from '@mui/x-data-grid/locales';
import { type ReactNode, useMemo } from 'react';

export type DataTableColumn<Row extends GridValidRowModel> = GridColDef<Row>;

export interface DataTableProps<Row extends GridValidRowModel> {
  /** Accessible name of the table. */
  label: string;
  rows: readonly Row[];
  columns: readonly DataTableColumn<Row>[];
  /** Rows matching the filters, in all pages (the server pages the list). */
  rowCount: number;
  /** Current page, starting at 1. */
  page: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  loading?: boolean;
  /** Rows shown dimmed, such as an inactive person (fp-gt__row--muted). */
  isRowMuted?: (row: Row) => boolean;
  /** Search and filters above the table (fp-gt-tools). */
  tools?: ReactNode;
  rowHeight?: number;
  /** Message of a table without rows. */
  emptyLabel?: string;
  /** The row shown as selected (fp-gt__row--selected), such as the record whose details are open. */
  selectedRowId?: GridRowId;
  /** Choosing a row, by click or by Enter on one of its cells. */
  onRowChoose?: (row: Row) => void;
  /** Text of the footer, such as "1–25 de 312 no período"; by default the Data Grid's own. */
  displayedRowsLabel?: (range: { from: number; to: number; count: number }) => string;
}

const localeText = ptBR.components.MuiDataGrid.defaultProps.localeText;

/**
 * Standard table of the application (ADR-0050): the free MUI X Data Grid in pt-BR, with the look of the design
 * system table (fp-gt). It fills the area it is in and scrolls inside, with the pagination at the bottom
 * (ADR-0066). The server sorts, filters and pages; the table only shows a page.
 */
export function DataTable<Row extends GridValidRowModel>({
  label,
  rows,
  columns,
  rowCount,
  page,
  pageSize,
  onPageChange,
  loading = false,
  isRowMuted,
  tools,
  rowHeight = 56,
  emptyLabel,
  selectedRowId,
  onRowChoose,
  displayedRowsLabel,
}: DataTableProps<Row>) {
  const text = useMemo(
    () => ({
      ...localeText,
      ...(emptyLabel === undefined ? {} : { noRowsLabel: emptyLabel }),
      ...(displayedRowsLabel === undefined
        ? {}
        : {
            paginationDisplayedRows: ({
              from,
              to,
              count,
            }: {
              from: number;
              to: number;
              count: number;
            }) => displayedRowsLabel({ from, to, count }),
          }),
    }),
    [emptyLabel, displayedRowsLabel],
  );
  const selection = useMemo(
    () => ({
      type: 'include' as const,
      ids: new Set<GridRowId>(selectedRowId === undefined ? [] : [selectedRowId]),
    }),
    [selectedRowId],
  );

  return (
    <Box
      component="section"
      aria-label={label}
      sx={{
        flex: 1,
        minHeight: 0,
        display: 'flex',
        flexDirection: 'column',
        boxSizing: 'border-box',
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-lg)',
        overflow: 'hidden',
      }}
    >
      {tools !== undefined && (
        <Box
          sx={{
            flex: 'none',
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-2)',
            flexWrap: 'wrap',
            padding: 'var(--space-3)',
            borderBottom: '1px solid var(--border)',
          }}
        >
          {tools}
        </Box>
      )}
      <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
        <DataGrid<Row>
          label={label}
          rows={rows}
          columns={columns}
          rowCount={rowCount}
          loading={loading}
          localeText={text}
          rowSelectionModel={selection}
          onRowClick={
            onRowChoose === undefined
              ? undefined
              : (params: GridRowParams<Row>) => {
                  onRowChoose(params.row);
                }
          }
          onCellKeyDown={
            onRowChoose === undefined
              ? undefined
              : (params: GridCellParams<Row>, event) => {
                  if (event.key === 'Enter') {
                    onRowChoose(params.row);
                  }
                }
          }
          paginationMode="server"
          paginationModel={{ page: page - 1, pageSize }}
          onPaginationModelChange={(model) => {
            onPageChange(model.page + 1);
          }}
          pageSizeOptions={[pageSize]}
          getRowClassName={({ row }) => (isRowMuted?.(row) === true ? 'row-muted' : '')}
          rowHeight={rowHeight}
          columnHeaderHeight={40}
          disableColumnMenu
          disableColumnSorting
          disableColumnResize
          disableRowSelectionOnClick
          hideFooterSelectedRowCount
          sx={{
            flex: 1,
            minHeight: 0,
            border: 0,
            borderRadius: 0,
            fontSize: 14,
            lineHeight: '20px',
            color: 'var(--text)',
            fontVariantNumeric: 'tabular-nums',
            '--DataGrid-containerBackground': 'var(--bg-subtle)',
            '--DataGrid-rowBorderColor': 'var(--border)',
            // Header row in the subtle background, as in fp-gt__row--head.
            '& .MuiDataGrid-columnHeaders, & .MuiDataGrid-columnHeaders .MuiDataGrid-columnHeader, & .MuiDataGrid-columnHeaders .MuiDataGrid-filler, & .MuiDataGrid-columnHeaders .MuiDataGrid-scrollbarFiller':
              { backgroundColor: 'var(--bg-subtle)' },
            '& .MuiDataGrid-columnHeader': {
              color: 'var(--text-secondary)',
              fontSize: 13,
              lineHeight: '18px',
              fontWeight: 500,
              padding: '0 var(--space-3)',
            },
            '& .MuiDataGrid-columnHeaderTitle': { fontWeight: 500 },
            '& .MuiDataGrid-columnSeparator': { display: 'none' },
            '& .MuiDataGrid-cell': {
              display: 'flex',
              alignItems: 'center',
              padding: '0 var(--space-3)',
              lineHeight: '20px',
              borderTopColor: 'var(--border)',
            },
            // The empty cell after the last column has no width; with padding it would make the rows wider than
            // the table, and focusing the last column would scroll the table sideways.
            '& .MuiDataGrid-cellEmpty': { padding: 0 },
            '& .MuiDataGrid-cell:focus, & .MuiDataGrid-cell:focus-within, & .MuiDataGrid-columnHeader:focus, & .MuiDataGrid-columnHeader:focus-within':
              { outline: '2px solid var(--focus-ring)', outlineOffset: -2 },
            '& .MuiDataGrid-row:hover': { backgroundColor: 'var(--bg-hover)' },
            '& .MuiDataGrid-row.Mui-selected, & .MuiDataGrid-row.Mui-selected:hover': {
              backgroundColor: 'var(--bg-selected)',
            },
            '& .MuiDataGrid-row': onRowChoose === undefined ? {} : { cursor: 'pointer' },
            '& .row-muted': { color: 'var(--text-secondary)' },
            '& .MuiDataGrid-footerContainer': {
              minHeight: 44,
              borderTop: '1px solid var(--border)',
              justifyContent: 'flex-end',
            },
            '& .MuiTablePagination-toolbar': { minHeight: 44 },
            '& .MuiTablePagination-root, & .MuiTablePagination-displayedRows': {
              fontSize: 13,
              lineHeight: '18px',
              color: 'var(--text-secondary)',
            },
            '& .MuiDataGrid-overlay': { backgroundColor: 'var(--bg-surface)' },
          }}
        />
      </Box>
    </Box>
  );
}
