/**
 * Starts loading the code of the Linha do tempo as soon as a project opens on a computer, so the tab Cronograma does not
 * wait for it the first time (ADR-0106). The same import as the ScheduleTimeline's, so the browser loads it only once.
 */
export function preloadTimeline(): void {
  import('../schedule-gantt/ScheduleGantt').catch(() => undefined);
}
