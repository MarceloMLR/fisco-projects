import { describe, expect, it, vi } from 'vitest';
import { demoScheduleWithBaseline } from '../test/server';
import { areaWidth, barOf, drawWith, simulateLayout } from './ganttTesting';

// In a file of its own, as the choice of lines: a Gantt drawn before in the same file may still be in the page while
// Syncfusion draws this one again, and the change of what it shows goes to the wrong one.

/** The area is 1200 px wide; the table takes 320 px and the splitter 4, so each of the 30 days takes 29.2 px. */
const day = (areaWidth - 320 - 4) / 30;

simulateLayout();

/** The days from the start of the schedule where an element of the timeline starts, and how many it takes. */
const daysOf = (element: Element | null) =>
  Math.round(Number.parseFloat((element as HTMLElement | null)?.style.left ?? 'NaN') / day);
const lengthOf = (element: Element | null) =>
  Math.round(Number.parseFloat((element as HTMLElement | null)?.style.width ?? 'NaN') / day);

// Spec: cronograma, requirement "Caminho crítico pela tela".
// Syncfusion draws more slowly when every test file runs together.
describe('Caminho crítico pela tela', { timeout: 20_000 }, () => {
  const isCritical = (container: HTMLElement, name: string) =>
    barOf(container, name)?.classList.contains('fp-gantt-critical');

  it('Caminho crítico na linha do tempo', async () => {
    const { container, redraw } = await drawWith({
      schedule: demoScheduleWithBaseline(),
      showBaseline: true,
      showCritical: true,
    });

    // The tasks and the milestone of the critical path in red; the ones with float as they are.
    expect(
      [
        'Reunião de abertura',
        'Parametrizar regras de ICMS',
        'Testes integrados',
        'Aceite da homologação',
        'Validar cadastro de produtos',
        'Integrar emissão de NF-e',
      ].map((name) => isCritical(container, name)),
    ).toEqual([true, true, true, true, false, false]);
    // A thin bar under each of the seven tasks, at its dates in the baseline: Parametrizar regras de ICMS from 13/10
    // to 20/10, eight days.
    const baselines = [...container.querySelectorAll('.e-baseline-bar')];
    expect(baselines).toHaveLength(7);
    const icms = baselines.find((bar) => daysOf(bar) === 8 && lengthOf(bar) === 8);
    expect(icms).toBeDefined();

    redraw({ showBaseline: false, showCritical: false });

    // Drawn again by Syncfusion, which takes longer when the tests run together.
    await vi.waitFor(() => {
      expect(container.querySelectorAll('.e-baseline-bar')).toHaveLength(0);
      expect(container.querySelectorAll('.fp-gantt-critical')).toHaveLength(0);
    }, 15_000);
  });
});
