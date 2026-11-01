import type { TFunction } from 'i18next';
import type { Project, Schedule, ScheduleItem } from '../api/client';
import type { Dates } from '../format/dates';
import { buildXlsx, spreadsheetDay, type XlsxCell } from '../format/xlsx';
import { xlsxColors } from '../format/xlsxColors';
import { milestoneStatus, ownerText } from './schedule';

export interface ScheduleReport {
  project: Project;
  schedule: Schedule;
  /** When it was made, in UTC, for the tab of the project. */
  generatedAt: string;
  dates: Pick<Dates, 'timeZone' | 'toSpreadsheetDateTime'>;
  t: TFunction;
}

/** What a line of the spreadsheet is, as the screen says it. */
export type ScheduleSituation =
  'done' | 'late' | 'inProgress' | 'notStarted' | 'reached' | 'lateMilestone' | 'pending';

/** A phase or a task by its progress and delay: done, late, in progress or not started. */
export function workSituation(target: { progress: number; late: boolean }): ScheduleSituation {
  if (target.progress >= 100) {
    return 'done';
  }
  if (target.late) {
    return 'late';
  }
  return target.progress > 0 ? 'inProgress' : 'notStarted';
}

/** An item as the screen says it: a milestone reached, late or still ahead, or a task as its work. */
export function itemSituation(item: ScheduleItem): ScheduleSituation {
  if (item.kind !== 'Milestone') {
    return workSituation(item);
  }
  const status = milestoneStatus(item);
  return status === 'reached' ? 'reached' : status === 'late' ? 'lateMilestone' : 'pending';
}

/** The name of the file, with the code of the project, which has only letters, digits and hyphens. */
export const scheduleReportName = (project: Pick<Project, 'code'>, t: TFunction) =>
  t('projects.export.excel.fileName', { code: project.code });

const day = (value: string | null | undefined): XlsxCell =>
  value === null || value === undefined ? '' : { day: spreadsheetDay(value) };

const percent = (progress: number): XlsxCell => ({ percent: progress / 100 });

/**
 * The schedule in Excel (ADR-0021, ADR-0099): the tab Cronograma with a line per phase and item in the order of the
 * screen, numbered as the MS Project numbers them, the dependencies by those numbers, and the tab Projeto with the
 * project. No e-mail goes in it: they are only in the XML of the managers (ADR-0136).
 */
export function scheduleReport({
  project,
  schedule,
  generatedAt,
  dates,
  t,
}: ScheduleReport): Promise<Uint8Array<ArrayBuffer>> {
  const situation = (value: ScheduleSituation) => t(`projects.export.excel.situations.${value}`);
  const clientName = project.client?.name ?? null;

  // The number of each line, the phases counted, for the column of the dependencies.
  const numbers = new Map<string, number>();
  for (const phase of schedule.phases) {
    numbers.set(phase.id, numbers.size + 1);
    for (const item of phase.items) {
      numbers.set(item.id, numbers.size + 1);
    }
  }

  const header: XlsxCell[] = [
    t('projects.export.excel.columns.number'),
    t('projects.export.excel.columns.phase'),
    t('projects.export.excel.columns.name'),
    t('projects.export.excel.columns.type'),
    t('projects.export.excel.columns.owner'),
    t('projects.export.excel.columns.start'),
    t('projects.export.excel.columns.end'),
    t('projects.export.excel.columns.duration'),
    t('projects.export.excel.columns.progress'),
    t('projects.export.excel.columns.situation'),
    t('projects.export.excel.columns.lateDays'),
    t('projects.export.excel.columns.deadline'),
    t('projects.export.excel.columns.deadlinePast'),
    t('projects.export.excel.columns.dependsOn'),
    t('projects.export.excel.columns.critical'),
    t('projects.export.excel.columns.baselineStart'),
    t('projects.export.excel.columns.baselineEnd'),
    t('projects.export.excel.columns.baselineDeviation'),
  ];
  const rows: XlsxCell[][] = [];
  for (const phase of schedule.phases) {
    rows.push([
      numbers.get(phase.id) ?? '',
      phase.name,
      { label: phase.name },
      t('projects.export.excel.types.phase'),
      '',
      day(phase.start),
      day(phase.end),
      '',
      percent(phase.progress),
      situation(workSituation(phase)),
      '',
      '',
      '',
      '',
      '',
      '',
      '',
      '',
    ]);
    for (const item of phase.items) {
      const milestone = item.kind === 'Milestone';
      const subtask = item.parentId !== null;
      rows.push([
        numbers.get(item.id) ?? '',
        phase.name,
        subtask ? { indented: item.name } : item.name,
        milestone
          ? t('projects.export.excel.types.milestone')
          : subtask
            ? t('projects.export.excel.types.subtask')
            : t('projects.export.excel.types.task'),
        ownerText(item, clientName, t) ?? '',
        day(item.start),
        day(milestone ? item.start : item.end),
        milestone ? '' : item.duration,
        milestone ? '' : percent(item.progress),
        situation(itemSituation(item)),
        item.lateDays > 0 ? item.lateDays : '',
        day(item.deadline),
        // Only how far past it went: also an item done after it, which the history keeps (GanttA-2).
        item.deadlineDeviation !== null && item.deadlineDeviation > 0 ? item.deadlineDeviation : '',
        item.predecessorIds
          .map((id) => numbers.get(id))
          .filter((number) => number !== undefined)
          .join('; '),
        item.critical ? t('projects.export.excel.yes') : '',
        day(item.baselineStart),
        day(item.baselineEnd),
        item.baselineDeviation ?? '',
      ]);
    }
  }

  const managers = project.members
    .filter((member) => member.role === 'Manager')
    .map((member) => member.name)
    .join(', ');
  const baseline = schedule.baseline;
  return buildXlsx(
    [
      {
        name: t('projects.export.excel.sheet'),
        widths: [6, 22, 42, 11, 24, 12, 12, 12, 11, 14, 12, 12, 16, 14, 10, 14, 14, 14],
        header: true,
        rows: [header, ...rows],
      },
      {
        name: t('projects.export.excel.projectSheet'),
        widths: [30, 50],
        header: false,
        rows: [
          [{ label: t('projects.export.excel.project.code') }, project.code],
          [{ label: t('projects.export.excel.project.name') }, project.name],
          [
            { label: t('projects.export.excel.project.client') },
            clientName ?? t('projects.export.excel.project.noClient'),
          ],
          [{ label: t('projects.export.excel.project.manager') }, managers],
          [{ label: t('projects.export.excel.project.start') }, day(schedule.start)],
          [{ label: t('projects.export.excel.project.end') }, day(schedule.end)],
          [{ label: t('projects.export.excel.project.progress') }, percent(schedule.progress)],
          [{ label: t('projects.export.excel.project.lateTasks') }, schedule.lateTasks],
          [{ label: t('projects.export.excel.project.lateMilestones') }, schedule.lateMilestones],
          [{ label: t('projects.export.excel.project.missedDeadlines') }, schedule.missedDeadlines],
          [
            { label: t('projects.export.excel.project.baseline') },
            baseline === null
              ? t('projects.export.excel.project.noBaseline')
              : day(baseline.savedOn),
          ],
          [
            { label: t('projects.export.excel.project.baselineDeviation') },
            baseline?.deviation ?? '',
          ],
          [
            { label: t('projects.export.excel.project.generatedAt') },
            { dateTime: dates.toSpreadsheetDateTime(generatedAt) },
          ],
          [{ label: t('projects.export.excel.project.timeZone') }, dates.timeZone],
        ],
      },
    ],
    xlsxColors,
  );
}
