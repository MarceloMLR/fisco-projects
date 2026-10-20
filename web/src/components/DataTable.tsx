import Box from '@mui/material/Box';
import type { SxProps, Theme } from '@mui/material/styles';
import {
  DataGrid,
  type GridCellParams,
  type GridColDef,
  type GridRowClassNameParams,
  type GridRowHeightParams,
  type GridRowId,
  type GridRowParams,
  type GridValidRowModel,
} from '@mui/x-data-grid';
import { ptBR } from '@mui/x-data-grid/locales';
import { type KeyboardEvent, type ReactNode, useCallback, useMemo } from 'react';
import { DelayedLoadingOverlay } from './DelayedLoadingOverlay';

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
  /**
   * Waiting for the rows: the indicator shows only if the wait goes past a moment, as a bar over the rows on screen
   * or, without rows, as the skeleton of the rows.
   */
  loading?: boolean;
  /** Rows shown dimmed, such as an inactive person (fp-gt__row--muted). Keep it the same function between renders. */
  isRowMuted?: (row: Row) => boolean;
  /**
   * Rows that name a group of the rows below, such as a team of a project (fp-group-row): lower, in the subtle
   * background and, unless groupRowsChoosable, never chosen. Their first column may span the whole row (colSpan in its
   * definition). Keep it the same function between renders.
   */
  isGroupRow?: (row: Row) => boolean;
  /**
   * The group rows are chosen like the others, such as a phase of a schedule, which opens to be renamed. They keep the
   * look of a group and their own cells.
   */
  groupRowsChoosable?: boolean;
  /** Search and filters above the table (fp-gt-tools). */
  tools?: ReactNode;
  /**
   * The actions on the rows the person marked, under the tools (fp-gt-tools in the selected background), as
   * "2 selecionados · Atribuir a…"; shown while there are marked rows.
   */
  selectionActions?: { label: string; content: ReactNode };
  rowHeight?: number;
  /** Message of a table without rows. */
  emptyLabel?: string;
  /** The row shown as selected (fp-gt__row--selected), such as the record whose details are open. */
  selectedRowId?: GridRowId;
  /** Choosing a row, by click or by Enter on one of its cells. */
  onRowChoose?: (row: Row) => void;
  /** Text of the footer, such as "1–25 de 312 no período"; by default the Data Grid's own. */
  displayedRowsLabel?: (range: { from: number; to: number; count: number }) => string;
  /** The id of a row whose field id is not unique or empty. Keep it the same function between renders. */
  getRowId?: (row: Row) => GridRowId;
}

const localeText = ptBR.components.MuiDataGrid.defaultProps.localeText;
const slots = { loadingOverlay: DelayedLoadingOverlay };

const sectionSx: SxProps<Theme> = {
  flex: 1,
  minHeight: 0,
  display: 'flex',
  flexDirection: 'column',
  boxSizing: 'border-box',
  backgroundColor: 'var(--bg-surface)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius-lg)',
  overflow: 'hidden',
};

const toolsSx: SxProps<Theme> = {
  flex: 'none',
  display: 'flex',
  alignItems: 'center',
  gap: 'var(--space-2)',
  flexWrap: 'wrap',
  padding: 'var(--space-3)',
  borderBottom: '1px solid var(--border)',
};

/** The actions on the marked rows: the tools in the selected background. */
const selectionActionsSx: SxProps<Theme> = {
  flex: 'none',
  display: 'flex',
  alignItems: 'center',
  gap: 'var(--space-3)',
  flexWrap: 'wrap',
  padding: 'var(--space-2) var(--space-3)',
  borderBottom: '1px solid var(--border)',
  backgroundColor: 'var(--bg-selected)',
};

/** Height of a group row (fp-group-row). */
const groupRowHeight = 36;

const gridAreaSx: SxProps<Theme> = {
  flex: 1,
  minHeight: 0,
  display: 'flex',
  flexDirection: 'column',
};

const gridSx = {
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
  '& .row-muted': { color: 'var(--text-secondary)' },
  // A group of the rows below (fp-group-row), with its count (fp-count) in the secondary color.
  '& .MuiDataGrid-row.row-group, & .MuiDataGrid-row.row-group:hover, & .MuiDataGrid-row.row-group.Mui-selected':
    {
      backgroundColor: 'var(--bg-subtle)',
      cursor: 'default',
    },
  '& .row-group .MuiDataGrid-cell': {
    fontSize: 13,
    lineHeight: '18px',
    fontWeight: 500,
    color: 'var(--text)',
  },
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
} satisfies SxProps<Theme>;

/** A table whose rows open something shows the pointer over them. */
const choosableGridSx = {
  ...gridSx,
  '& .MuiDataGrid-row:not(.row-group)': { cursor: 'pointer' },
} satisfies SxProps<Theme>;

/** A table whose group rows open something too, such as the phases of a schedule. */
const choosableGroupsGridSx = {
  ...gridSx,
  '& .MuiDataGrid-row, & .MuiDataGrid-row.row-group, & .MuiDataGrid-row.row-group:hover': {
    cursor: 'pointer',
  },
} satisfies SxProps<Theme>;

/**
 * Standard table of the application (ADR-0050): the free MUI X Data Grid in pt-BR, with the look of the design
 * system table (fp-gt). It fills the area it is in and scrolls inside, with the pagination at the bottom
 * (ADR-0066). The server sorts, filters and pages; the table only shows a page.
 *
 * The Data Grid draws again only when one of its properties changes, so everything given to it here keeps the same
 * value between renders: a screen that renders again without new rows, such as when a filter changes, does not
 * draw the cells again.
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
  isGroupRow,
  groupRowsChoosable = false,
  tools,
  selectionActions,
  rowHeight = 56,
  emptyLabel,
  selectedRowId,
  onRowChoose,
  displayedRowsLabel,
  getRowId,
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
  const paginationModel = useMemo(() => ({ page: page - 1, pageSize }), [page, pageSize]);
  const pageSizeOptions = useMemo(() => [pageSize], [pageSize]);
  const onPaginationModelChange = useCallback(
    (model: { page: number }) => {
      onPageChange(model.page + 1);
    },
    [onPageChange],
  );
  const getRowClassName = useCallback(
    ({ row }: GridRowClassNameParams<Row>) =>
      isGroupRow?.(row) === true ? 'row-group' : isRowMuted?.(row) === true ? 'row-muted' : '',
    [isRowMuted, isGroupRow],
  );
  const getRowHeight = useMemo(
    () =>
      isGroupRow === undefined
        ? undefined
        : ({ model }: GridRowHeightParams) => (isGroupRow(model as Row) ? groupRowHeight : null),
    [isGroupRow],
  );
  const isChoosable = useCallback(
    (row: Row) => groupRowsChoosable || isGroupRow?.(row) !== true,
    [groupRowsChoosable, isGroupRow],
  );
  const onRowClick = useMemo(
    () =>
      onRowChoose === undefined
        ? undefined
        : (params: GridRowParams<Row>) => {
            if (isChoosable(params.row)) {
              onRowChoose(params.row);
            }
          },
    [onRowChoose, isChoosable],
  );
  const onCellKeyDown = useMemo(
    () =>
      onRowChoose === undefined
        ? undefined
        : (params: GridCellParams<Row>, event: KeyboardEvent) => {
            if (event.key === 'Enter' && isChoosable(params.row)) {
              onRowChoose(params.row);
            }
          },
    [onRowChoose, isChoosable],
  );

  return (
    <Box component="section" aria-label={label} sx={sectionSx}>
      {tools !== undefined && <Box sx={toolsSx}>{tools}</Box>}
      {selectionActions !== undefined && (
        <Box role="region" aria-label={selectionActions.label} sx={selectionActionsSx}>
          {selectionActions.content}
        </Box>
      )}
      <Box sx={gridAreaSx}>
        <DataGrid<Row>
          label={label}
          rows={rows}
          getRowId={getRowId}
          columns={columns}
          rowCount={rowCount}
          loading={loading}
          slots={slots}
          localeText={text}
          rowSelectionModel={selection}
          onRowClick={onRowClick}
          onCellKeyDown={onCellKeyDown}
          paginationMode="server"
          paginationModel={paginationModel}
          onPaginationModelChange={onPaginationModelChange}
          pageSizeOptions={pageSizeOptions}
          getRowClassName={getRowClassName}
          getRowHeight={getRowHeight}
          rowHeight={rowHeight}
          columnHeaderHeight={40}
          disableColumnMenu
          disableColumnSorting
          disableColumnResize
          disableRowSelectionOnClick
          hideFooterSelectedRowCount
          sx={
            onRowChoose === undefined
              ? gridSx
              : groupRowsChoosable
                ? choosableGroupsGridSx
                : choosableGridSx
          }
        />
      </Box>
    </Box>
  );
}
