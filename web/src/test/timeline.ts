import { fireEvent } from '@testing-library/react';
import { minUnitWidth } from '../schedule-gantt/ganttLayout';
import { timelineSizes } from '../schedule-gantt/timelineGeometry';

// jsdom lays nothing out: in the scale Dias, each day keeps the narrowest width, and the chart starts at 0, 0. These
// helpers press, move and release the pointer where a person would, from the days of the bars.

export const dayWidth = minUnitWidth.days;
const { row: rowHeight } = timelineSizes;

/**
 * The table of the timeline on the screen (the one of the tab, first, when Expandir is open), or inside a part of it;
 * also while a drawer or a menu hides the tab from the reader of the screen.
 */
export function timelineTable(scope: HTMLElement = document.body): HTMLElement {
  const table = scope.querySelector<HTMLElement>('[role="treegrid"]');
  if (table === null) {
    throw new Error('No timeline.');
  }
  return table;
}

/** The whole timeline: the table, the chart and what it shows (data-scale, data-editable, data-detailed). */
export function timelineOf(scope: HTMLElement = document.body): HTMLElement {
  const root = timelineTable(scope).closest<HTMLElement>('.fp-gantt');
  if (root === null) {
    throw new Error('No timeline.');
  }
  return root;
}

const linesOf = (scope: HTMLElement) => [
  ...timelineTable(scope).querySelectorAll<HTMLElement>('[role="row"]'),
];

/** The texts of the lines of the table, without its header. */
export function timelineLines(scope: HTMLElement = document.body): string[] {
  return linesOf(scope)
    .slice(1)
    .map((row) => row.textContent);
}

/** The line of the table with this name. */
export function timelineLine(name: string, scope: HTMLElement = document.body): HTMLElement {
  const line = linesOf(scope).find(
    (row) => row.querySelector('.fp-gantt__text')?.textContent === name,
  );
  if (line === undefined) {
    throw new Error(`No line ${name}.`);
  }
  return line;
}

/** The bar, lozenge or summary of a line in the chart, whose name says what it is, its name and its dates. */
export function barOf(name: string, scope: HTMLElement = document.body): SVGGElement {
  const bar = [...scope.querySelectorAll<SVGGElement>('g[role="img"]')].find(
    (group) =>
      (group.getAttribute('aria-label') ?? '').split(', ')[0]?.replace(/^\S+ /, '') === name,
  );
  if (bar === undefined) {
    throw new Error(`No bar of ${name}.`);
  }
  return bar;
}

/** Whether the line has a bar in the chart: none inside a hidden phase. */
export function hasBar(name: string, scope: HTMLElement = document.body): boolean {
  try {
    barOf(name, scope);
    return true;
  } catch {
    return false;
  }
}

/** The first day of a bar, counted from the start of the timeline, and how many days it takes. */
export function spanOf(bar: Element): [number, number] {
  const rect = bar.querySelector('.g-bar');
  if (rect !== null) {
    const x = Number(rect.getAttribute('x'));
    return [
      Math.round(x / dayWidth),
      Math.round((Number(rect.getAttribute('width')) + 1) / dayWidth),
    ];
  }
  const path = bar.querySelector('.g-summary, .g-milestone')?.getAttribute('d') ?? '';
  const [, x = 'NaN', width] = /^M([\d.]+) [\d.]+ (?:h([\d.]+))?/.exec(path) ?? [];
  return width === undefined
    ? [Math.floor(Number(x) / dayWidth), 1]
    : [Math.round(Number(x) / dayWidth), Math.round(Number(width) / dayWidth)];
}

const middleOf = (bar: Element) => Number(bar.getAttribute('data-row')) * rowHeight + rowHeight / 2;

/** Drags a bar so many days by its middle, or by one of its edges, as the pointer does, and releases it. */
export function dragBar(bar: Element, days: number, edge?: 'start' | 'end'): void {
  const [start, length] = spanOf(bar);
  const target = edge === undefined ? bar : bar.querySelector(`.g-handle[data-edge="${edge}"]`);
  if (target === null) {
    throw new Error(`No ${String(edge)} edge.`);
  }
  const x =
    edge === 'start'
      ? start * dayWidth
      : edge === 'end'
        ? (start + length) * dayWidth
        : start * dayWidth + dayWidth / 2;
  const y = middleOf(bar);
  fireEvent.pointerDown(target, { button: 0, clientX: x, clientY: y });
  fireEvent.pointerMove(window, { clientX: x + days * dayWidth, clientY: y });
  fireEvent.pointerUp(window, { clientX: x + days * dayWidth, clientY: y });
}

/** Draws a link from the end of one bar to the line of another, as the pointer does. */
export function linkBars(from: Element, to: Element): void {
  const connector = from.querySelector('.g-connector');
  if (connector === null) {
    throw new Error('No point to link from.');
  }
  const x = Number(connector.getAttribute('cx'));
  const y = Number(connector.getAttribute('cy'));
  fireEvent.pointerDown(connector, { button: 0, clientX: x, clientY: y });
  fireEvent.pointerMove(window, { clientX: x + 40, clientY: middleOf(to) });
  fireEvent.pointerUp(window, { clientX: x + 40, clientY: middleOf(to) });
}

/** The body of the chart: the lines, the bars and the arrows. */
export function chartOf(scope: HTMLElement = document.body): SVGSVGElement {
  const chart = timelineOf(scope).querySelector<SVGSVGElement>('.fp-gantt__body');
  if (chart === null) {
    throw new Error('No chart.');
  }
  return chart;
}
