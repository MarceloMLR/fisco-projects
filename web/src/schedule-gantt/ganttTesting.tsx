import { render } from '@testing-library/react';
import { afterEach, beforeEach, expect, vi } from 'vitest';
import { ScheduleGantt, type ScheduleGanttProps } from './ScheduleGantt';

/**
 * What the tests of the real ScheduleGantt need in jsdom, which does not lay anything out: the area of the timeline
 * says it is 1200 px wide, and the observer of its size answers at once.
 */
export const areaWidth = 1200;

export function simulateLayout() {
  beforeEach(() => {
    vi.spyOn(HTMLElement.prototype, 'clientWidth', 'get').mockReturnValue(areaWidth);
    vi.spyOn(HTMLElement.prototype, 'offsetWidth', 'get').mockReturnValue(areaWidth);
    vi.spyOn(HTMLElement.prototype, 'offsetHeight', 'get').mockReturnValue(600);
    vi.stubGlobal(
      'ResizeObserver',
      class {
        constructor(private readonly callback: () => void) {}
        observe() {
          this.callback();
        }
        unobserve() {
          // Nothing to stop: the width never changes.
        }
        disconnect() {
          // Nothing to stop: the width never changes.
        }
      },
    );
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });
}

/** Draws the timeline with these properties, and waits for its lines with the cells of FiscoProjects. */
export async function drawWith(
  props: Partial<ScheduleGanttProps> & Pick<ScheduleGanttProps, 'schedule'>,
  lines = 12,
) {
  const all: ScheduleGanttProps = {
    scale: 'days',
    editable: false,
    license: null,
    onChoose: vi.fn(),
    onMove: vi.fn(),
    onResize: vi.fn(),
    onLink: vi.fn(),
    ...props,
  };
  const view = render(<ScheduleGantt {...all} />);
  // The cells of FiscoProjects come a moment after the rows of Syncfusion.
  await vi.waitFor(() => {
    const rows = view.container.querySelectorAll('.e-row');
    expect(rows).toHaveLength(lines);
    expect(rows[0]?.textContent).not.toBe('');
  }, 5000);
  return {
    container: view.container,
    redraw: (changed: Partial<ScheduleGanttProps>) => {
      Object.assign(all, changed);
      view.rerender(<ScheduleGantt {...all} />);
    },
  };
}

/** The bar of a task or milestone, which Syncfusion names after it. */
export const barOf = (container: HTMLElement, name: string) =>
  container.querySelector(`.e-taskbar-main-container[aria-label^="Name ${name} "]`);
