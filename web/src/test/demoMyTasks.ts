import type { MyTasks, ScheduleItem } from '../api/client';
import { demoItem, demoPeople, demoToday } from './server';

// Minhas tarefas of Marina on Friday, 16/10/2026 (ADR-0158): the week goes from Monday 12/10 to Sunday 18/10.

export const myTasksProjects = {
  erp: {
    id: '0199f5a2-7c1e-7d40-9a1b-400000000001',
    code: 'ERP-FIN',
    name: 'Implantação do ERP financeiro',
  },
  fisc: {
    id: '0199f5a2-7c1e-7d40-9a1b-400000000002',
    code: 'FISC-SP',
    name: 'Revisão fiscal da filial SP',
  },
} as const;

const marina = { id: demoPeople.marina.id, name: demoPeople.marina.name };

/** A task of Marina, who updates its progress. */
function task(
  index: number,
  item: Pick<ScheduleItem, 'name' | 'start' | 'end'> & Partial<ScheduleItem>,
): ScheduleItem {
  return demoItem(80 + index, '0199f5a2-7c1e-7d40-9a1b-50000000f001', {
    owner: marina,
    canUpdateProgress: true,
    ...item,
  });
}

/** The tasks of Marina, by name, so a test can change one before the page opens. */
export function demoMyTaskItems() {
  return {
    icms: task(1, {
      name: 'Parametrizar regras de ICMS',
      start: '2026-10-08',
      end: '2026-10-14',
      duration: 5,
      progress: 40,
      late: true,
      lateDays: 2,
    }),
    accounts: task(2, {
      name: 'Validar plano de contas',
      start: '2026-10-16',
      end: '2026-10-16',
      note: 'Conferir com a contabilidade antes de fechar.',
    }),
    script: task(3, {
      name: 'Roteiro de testes',
      start: '2026-10-21',
      end: '2026-10-23',
      duration: 3,
      commentCount: 2,
    }),
    install: task(4, {
      name: 'Instalar o ERP',
      start: '2026-10-05',
      end: '2026-10-09',
      duration: 5,
      progress: 100,
      completedOn: '2026-10-09',
    }),
    entries: task(5, {
      name: 'Levantar notas de entrada',
      start: '2026-10-13',
      end: '2026-10-17',
      duration: 4,
      progress: 60,
    }),
    report: task(6, {
      name: 'Relatório final',
      start: '2026-11-16',
      end: '2026-11-20',
      duration: 5,
      deadline: '2026-11-27',
      deadlineDeviation: -5,
    }),
  };
}

export type DemoMyTaskItems = ReturnType<typeof demoMyTaskItems>;

/** Minhas tarefas as the API answers them: the completed tasks only when asked. */
export function demoMyTasks(items: DemoMyTaskItems, done: boolean): MyTasks {
  const keep = (item: ScheduleItem) => done || item.progress < 100;
  const entry = (item: ScheduleItem, phase: string, parent: string | null = null) => ({
    item,
    phase,
    parent,
  });
  const projects = [
    {
      ...myTasksProjects.erp,
      tasks: [
        entry(items.icms, 'Configuração'),
        entry(items.accounts, 'Configuração'),
        entry(items.script, 'Homologação', 'Testes integrados'),
        entry(items.install, 'Levantamento'),
      ].filter((one) => keep(one.item)),
    },
    {
      ...myTasksProjects.fisc,
      tasks: [entry(items.entries, 'Diagnóstico'), entry(items.report, 'Entrega')].filter((one) =>
        keep(one.item),
      ),
    },
  ];
  return { today: demoToday, projects: projects.filter((project) => project.tasks.length > 0) };
}
