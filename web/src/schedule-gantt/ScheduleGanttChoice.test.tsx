import type { GanttComponent } from '@syncfusion/ej2-react-gantt';
import { fireEvent } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { demoSchedule } from '../test/server';
import { barOf, drawWith, simulateLayout } from './ganttTesting';

/**
 * Choosing in the real timeline, in a file of its own: a timeline drawn before by another test of the same file leaves
 * Syncfusion without choosing lines on the click.
 */
simulateLayout();

// Spec: cronograma, requirement "Detalhe pela linha do tempo".
describe('Detalhe pela linha do tempo', { timeout: 30_000 }, () => {
  it('Reabrir o detalhe pela linha do tempo', async () => {
    const onChoose = vi.fn();
    const schedule = demoSchedule();
    const mapping = schedule.phases[0]?.items[1];
    const { container, redraw } = await drawWith({ schedule, onChoose });
    // The lines Syncfusion keeps chosen, from the component it hangs on its element.
    const chosenLines = () => {
      const element = container.querySelector<Element & { ej2_instances?: GanttComponent[] }>(
        '.e-gantt',
      );
      return element?.ej2_instances?.[0]?.selectionModule.getSelectedRowIndexes() ?? [];
    };
    const choose = () => {
      const bar = barOf(container, 'Mapear processos fiscais');
      if (bar === null) {
        throw new Error('No bar of Mapear processos fiscais.');
      }
      // Syncfusion chooses a line on the press and the release of the mouse, before the click.
      fireEvent.mouseDown(bar);
      fireEvent.mouseUp(bar);
      fireEvent.click(bar);
    };

    // Syncfusion may answer a moment later, or miss the first click, when the machine is busy: each step clicks again
    // until it answers.
    const chooses = async (times: number) => {
      await vi.waitFor(
        () => {
          if (onChoose.mock.calls.length < times) {
            choose();
          }
          expect(onChoose.mock.calls.length).toBeGreaterThanOrEqual(times);
        },
        { timeout: 10_000, interval: 500 },
      );
      expect(onChoose).toHaveBeenLastCalledWith(
        expect.objectContaining({ kind: 'item', item: mapping }),
      );
    };

    await chooses(1);
    const chosen = onChoose.mock.calls.length;
    // The details open with the line chosen, and it stays chosen while they are open.
    redraw({ selectedId: mapping?.id });
    await vi.waitFor(() => {
      expect(chosenLines()).toEqual([2]);
    }, 5000);
    // Closed, the timeline forgets the line, and the same bar opens the details again.
    redraw({ selectedId: undefined });
    await vi.waitFor(() => {
      expect(chosenLines()).toEqual([]);
    }, 5000);
    await chooses(chosen + 1);
  });
});
