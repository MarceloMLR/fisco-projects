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
describe('Detalhe pela linha do tempo', () => {
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

    choose();
    expect(onChoose).toHaveBeenLastCalledWith(
      expect.objectContaining({ kind: 'item', item: mapping }),
    );
    const chosen = onChoose.mock.calls.length;
    // The details open with the line chosen, and it stays chosen while they are open.
    redraw({ selectedId: mapping?.id });
    expect(chosenLines()).toEqual([2]);
    // Closed, the timeline forgets the line, and the same bar opens the details again.
    redraw({ selectedId: undefined });
    expect(chosenLines()).toEqual([]);
    choose();

    expect(onChoose.mock.calls.length).toBeGreaterThan(chosen);
    expect(onChoose).toHaveBeenLastCalledWith(
      expect.objectContaining({ kind: 'item', item: mapping }),
    );
  });
});
