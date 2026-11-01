import type { TFunction } from 'i18next';
import type { ProjectHours, ProjectHoursView } from '../api/client';
import type { DayValue } from '../format/dates';
import type { Dates } from '../format/dates';
import { xlsxColors } from '../format/xlsxColors';
import { buildXlsx, type XlsxCell } from '../format/xlsx';
import { balanceText } from './projectHours';

export interface ProjectHoursReport {
  rows: readonly ProjectHours[];
  view: ProjectHoursView;
  search: string;
  /** When it was made and by whom, for the tab of the filters. */
  generatedAt: string;
  generatedBy: string;
  dates: Pick<Dates, 'timeZone' | 'toSpreadsheetDateTime'>;
  t: TFunction;
}

/** The name of the file: horas-dos-projetos-2026-10-29.xlsx. */
export const projectHoursReportName = (today: DayValue, t: TFunction) =>
  t('projectHours.report.fileName', { day: today });

/** A number of hours as a number of the spreadsheet, or empty when there is none. */
const hoursCell = (hours: number | null): XlsxCell => hours ?? '';

/**
 * The spreadsheet of the hours of the projects (ADR-0128), built on the screen like the one of the audit (ADR-0099):
 * the tab Horas dos projetos with a line per project of the view and of the search, the hours as numbers, and the tab
 * Filtros with what was exported, when and by whom.
 */
export function projectHoursReport({
  rows,
  view,
  search,
  generatedAt,
  generatedBy,
  dates,
  t,
}: ProjectHoursReport): Promise<Uint8Array<ArrayBuffer>> {
  const header: XlsxCell[] = [
    t('projectHours.report.columns.code'),
    t('projectHours.report.columns.project'),
    t('projectHours.report.columns.client'),
    t('projectHours.report.columns.status'),
    t('projectHours.report.columns.contracted'),
    t('projectHours.report.columns.additional'),
    t('projectHours.report.columns.logged'),
    t('projectHours.report.columns.balance'),
    t('projectHours.report.columns.consumption'),
    t('projectHours.report.columns.state'),
  ];
  const lines = rows.map((row): XlsxCell[] => [
    row.code,
    row.name,
    row.clientName ?? t('projectHours.internal'),
    t(`projects.statuses.${row.status}`),
    hoursCell(row.balance.contracted),
    row.balance.additional,
    row.balance.logged,
    hoursCell(row.balance.balance),
    row.balance.consumption ?? '',
    balanceText(row.balance, t),
  ]);
  return buildXlsx(
    [
      {
        name: t('projectHours.report.sheet'),
        widths: [14, 40, 28, 16, 14, 14, 14, 12, 12, 24],
        header: true,
        rows: [header, ...lines],
      },
      {
        name: t('projectHours.report.filtersSheet'),
        widths: [18, 60],
        header: false,
        rows: [
          [{ label: t('projectHours.report.filters.view') }, t(`projectHours.views.${view}`)],
          [
            { label: t('projectHours.report.filters.search') },
            search === '' ? t('projectHours.report.filters.noSearch') : search,
          ],
          [{ label: t('projectHours.report.filters.rows') }, rows.length],
          [{ label: t('projectHours.report.filters.timeZone') }, dates.timeZone],
          [
            { label: t('projectHours.report.filters.generatedAt') },
            { dateTime: dates.toSpreadsheetDateTime(generatedAt) },
          ],
          [{ label: t('projectHours.report.filters.generatedBy') }, generatedBy],
        ],
      },
    ],
    xlsxColors,
  );
}
