import { describe, expect, it } from 'vitest';
import i18n from '../i18n';
import { taskHoursOf, taskHoursRow } from '../test/demoProjectHours';
import { needsAttention, taskBalanceText, taskHoursById, taskHoursText } from './taskHours';

const t = i18n.t.bind(i18n);

// Spec: horas-do-projeto, requirements "Estado das horas da tarefa" and "Horas na Lista do cronograma pela tela"
// (ADR-0138).
describe('Horas das tarefas', () => {
  const over = taskHoursRow('a', 'Parametrizar regras de ICMS', 40, 52);
  const near = taskHoursRow('b', 'Integrar emissão de NF-e', 20, 18);
  const doneNear = taskHoursRow('c', 'Mapear processos fiscais', 20, 19, true);
  const unplanned = taskHoursRow('d', 'Testes integrados', null, 6.5);

  it('Tarefa acima das previstas', () => {
    expect(taskHoursText(over, t)).toBe('52 de 40 h');
    expect(taskBalanceText(over, t)).toBe('12 h acima');
    expect(needsAttention(over)).toBe(true);
  });

  it('Tarefa perto das previstas', () => {
    expect(taskBalanceText(near, t)).toBe('Restam 2 h');
    expect(needsAttention(near)).toBe(true);
  });

  it('Tarefa concluída perto das previstas', () => {
    expect(taskBalanceText(doneNear, t)).toBe('Restam 1 h');
    expect(needsAttention(doneNear)).toBe(false);
  });

  it('Tarefa sem horas previstas', () => {
    expect(taskHoursText(unplanned, t)).toBe('6,5 h');
    expect(taskBalanceText(unplanned, t)).toBe('Sem horas previstas');
    expect(needsAttention(unplanned)).toBe(false);
  });

  it('Horas de cada tarefa', () => {
    const byId = taskHoursById(taskHoursOf([over, unplanned], 3));

    expect([...byId.keys()]).toEqual(['a', 'd']);
    expect(taskHoursById(undefined).size).toBe(0);
  });
});
