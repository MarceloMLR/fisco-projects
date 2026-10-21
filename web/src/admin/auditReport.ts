import type { TFunction } from 'i18next';
import { type AuditEntry, type AuditItemType } from '../api/client';
import { type Dates, type DayValue, formatDayText } from '../format/dates';
import { buildXlsx, type XlsxCell } from '../format/xlsx';
import { color } from '../theme/tokens';
import { describeAuditEntry } from './auditText';

/** The filters of the spreadsheet, as the screen had them. */
export interface AuditReportFilters {
  from: DayValue;
  to: DayValue;
  itemType?: AuditItemType;
  /** The name of the person, when the screen had one. */
  person?: string;
}

export interface AuditReport {
  entries: readonly AuditEntry[];
  filters: AuditReportFilters;
  /** When it was made and by whom, for the tab of the filters. */
  generatedAt: string;
  generatedBy: string;
  dates: Pick<Dates, 'timeZone' | 'toSpreadsheetDateTime'>;
  t: TFunction;
}

/** A color of the tokens, light theme, as RRGGBB. */
const tokenColor = (name: string) => color(name, 'light').replace(/^#/, '');

/** The name of the file: auditoria-2026-09-21-a-2026-10-20.xlsx. */
export const auditReportName = (filters: AuditReportFilters, t: TFunction) =>
  t('audit.report.fileName', { from: filters.from, to: filters.to });

/**
 * The spreadsheet of the audit (ADR-0099): the tab Auditoria with a line per record, with the texts of the screen
 * (describeAuditEntry), and the tab Filtros with what was exported, when and by whom.
 */
export function auditReport({
  entries,
  filters,
  generatedAt,
  generatedBy,
  dates,
  t,
}: AuditReport): Promise<Uint8Array<ArrayBuffer>> {
  const header: XlsxCell[] = [
    t('audit.report.columns.when'),
    t('audit.report.columns.who'),
    t('audit.report.columns.type'),
    t('audit.report.columns.what'),
    t('audit.report.columns.summary'),
    t('audit.report.columns.details'),
    t('audit.report.columns.changes'),
  ];
  const rows = entries.map((entry): XlsxCell[] => {
    const description = describeAuditEntry(entry, t);
    return [
      { dateTime: dates.toSpreadsheetDateTime(entry.occurredAt) },
      entry.actor?.name ?? t('audit.system'),
      t(`audit.itemTypes.${entry.itemType as AuditItemType}`, { defaultValue: entry.itemType }),
      description.title,
      description.summary,
      description.properties.map((property) => t('audit.report.property', property)).join('\n'),
      description.changes.map((change) => t('audit.report.change', change)).join('\n'),
    ];
  });
  const period = t('audit.export.period', {
    from: formatDayText(filters.from),
    to: formatDayText(filters.to),
  });
  return buildXlsx(
    [
      {
        name: t('audit.report.sheet'),
        widths: [17, 24, 22, 30, 50, 45, 60],
        header: true,
        rows: [header, ...rows],
      },
      {
        name: t('audit.report.filtersSheet'),
        widths: [18, 60],
        header: false,
        rows: [
          [{ label: t('audit.report.filters.period') }, period],
          [
            { label: t('audit.report.filters.type') },
            filters.itemType === undefined
              ? t('audit.filters.allTypes')
              : t(`audit.itemTypes.${filters.itemType}`),
          ],
          [
            { label: t('audit.report.filters.person') },
            filters.person ?? t('audit.filters.allPeople'),
          ],
          [{ label: t('audit.report.filters.rows') }, entries.length],
          [{ label: t('audit.report.filters.timeZone') }, dates.timeZone],
          [
            { label: t('audit.report.filters.generatedAt') },
            { dateTime: dates.toSpreadsheetDateTime(generatedAt) },
          ],
          [{ label: t('audit.report.filters.generatedBy') }, generatedBy],
        ],
      },
    ],
    {
      headerFill: tokenColor('bg-subtle'),
      headerText: tokenColor('text'),
      border: tokenColor('border'),
    },
  );
}
