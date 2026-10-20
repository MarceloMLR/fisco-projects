import type {
  Acceptance,
  AcceptanceDetails,
  AcceptanceList,
  AcceptanceStatus,
} from '../api/client';
import { demoPeople } from './server';

// The acceptance terms of ERP-FIN for the screen tests, as the canvas ProjetoTermos (ADR-0035): one pending for Carla,
// one signed by her, one attached by Marina and one refused.

const id = (index: number) => `0199f5a2-7c1e-7d40-9a1b-a${String(index).padStart(11, '0')}`;

/** The milestones of the schedule of ERP-FIN that the terms accept. */
export const demoMilestones = [
  { id: id(91), name: 'Homologação concluída', day: '2026-11-03', reachedOn: null },
  { id: id(92), name: 'Plano de contas aprovado', day: '2026-10-13', reachedOn: '2026-10-13' },
  { id: id(93), name: 'Escopo aprovado', day: '2026-10-05', reachedOn: '2026-10-05' },
  { id: id(94), name: 'Especificação aprovada', day: '2026-10-14', reachedOn: null },
];

const milestoneOf = (index: number) => demoMilestones[index] ?? null;

const by = (person: { id: string; name: string }, at: string) => ({ ...person, at });

/** The terms of ERP-FIN, newest first, as the API lists them. */
export function demoAcceptances(): Acceptance[] {
  return [
    {
      id: id(1),
      title: 'Aceite da homologação',
      status: 'Pending',
      milestoneId: id(91),
      milestone: milestoneOf(0),
      recipient: demoPeople.carla,
      requested: by(demoPeople.marina, '2026-10-15T13:04:00Z'),
      answered: null,
      isRequest: true,
    },
    {
      id: id(2),
      title: 'Aceite da especificação de ICMS-ST',
      status: 'Refused',
      milestoneId: id(94),
      milestone: milestoneOf(3),
      recipient: demoPeople.carla,
      requested: by(demoPeople.marina, '2026-10-14T12:00:00Z'),
      answered: by(demoPeople.carla, '2026-10-14T18:00:00Z'),
      isRequest: true,
    },
    {
      id: id(3),
      title: 'Aceite do plano de contas',
      status: 'Signed',
      milestoneId: id(92),
      milestone: milestoneOf(1),
      recipient: demoPeople.carla,
      requested: by(demoPeople.marina, '2026-10-13T12:00:00Z'),
      answered: by(demoPeople.carla, '2026-10-13T17:00:00Z'),
      isRequest: true,
    },
    {
      id: id(4),
      title: 'Aceite do escopo do projeto',
      status: 'Signed',
      milestoneId: id(93),
      milestone: milestoneOf(2),
      recipient: demoPeople.diego,
      requested: by(demoPeople.marina, '2026-10-05T12:00:00Z'),
      answered: by(demoPeople.marina, '2026-10-05T12:00:00Z'),
      isRequest: false,
    },
  ];
}

/** The list of the API: the terms of one status when asked, the counts of all of them and what the person may do. */
export function acceptanceList(
  terms: readonly Acceptance[],
  url: URL,
  manage = { canManage: true, canRequest: true },
): AcceptanceList {
  const status = url.searchParams.get('status') as AcceptanceStatus | null;
  const count = (value: AcceptanceStatus) => terms.filter((term) => term.status === value).length;
  return {
    items: terms.filter((term) => status === null || term.status === status),
    counts: {
      all: terms.length,
      pending: count('Pending'),
      signed: count('Signed'),
      refused: count('Refused'),
    },
    ...manage,
  };
}

/** A term with its history, as the drawer gets it. */
export function acceptanceDetails(
  term: Acceptance,
  actions: Partial<AcceptanceDetails['actions']> = {},
): AcceptanceDetails {
  const requested = {
    kind: term.isRequest ? ('Requested' as const) : ('Attached' as const),
    actor: { id: term.requested.id, name: term.requested.name },
    at: term.requested.at,
  };
  const answered =
    term.answered === null || !term.isRequest
      ? []
      : [
          {
            kind:
              term.status === 'Refused'
                ? ('Refused' as const)
                : term.answered.id === term.recipient.id
                  ? ('Signed' as const)
                  : ('Attached' as const),
            actor: { id: term.answered.id, name: term.answered.name },
            at: term.answered.at,
          },
        ];
  return {
    term,
    message:
      term.isRequest && term.status === 'Pending'
        ? 'Carla, os testes de homologação terminaram sem pendências. Assine o termo e envie por aqui até 30/10.'
        : null,
    toSign: term.isRequest ? { name: 'termo-homologacao-erp-fin.pdf', size: 217_088 } : null,
    signed:
      term.status === 'Signed'
        ? { documentId: id(80), name: 'termo-assinado.pdf', size: 245_760 }
        : null,
    refusalReason: term.status === 'Refused' ? 'Falta o relatório de testes de ICMS-ST.' : null,
    events: [requested, ...answered],
    actions: {
      canAnswer: false,
      canAttach: false,
      canRemind: false,
      canCancel: false,
      ...actions,
    },
  };
}
