import type {
  Acceptance,
  AcceptanceDetails,
  AcceptanceList,
  AcceptanceSigner,
  AcceptanceStatus,
} from '../api/client';
import { demoPeople } from './server';

// The acceptance terms of ERP-FIN for the screen tests (ADR-0129): one pending with one of two signers signed, one
// refused by a signer, one signed by both, one attached by Marina and one asked of Carla before the provider.

const id = (index: number) => `0199f5a2-7c1e-7d40-9a1b-a${String(index).padStart(11, '0')}`;

/** The milestones of the schedule of ERP-FIN that the terms accept. */
export const demoMilestones = [
  { id: id(91), name: 'Homologação concluída', day: '2026-11-03', reachedOn: null },
  { id: id(92), name: 'Plano de contas aprovado', day: '2026-10-13', reachedOn: '2026-10-13' },
  { id: id(93), name: 'Escopo aprovado', day: '2026-10-05', reachedOn: '2026-10-05' },
  { id: id(94), name: 'Especificação aprovada', day: '2026-10-14', reachedOn: null },
  { id: id(95), name: 'Cadastro de produtos validado', day: '2026-10-09', reachedOn: null },
];

const milestoneOf = (index: number) => demoMilestones[index] ?? null;

const by = (person: { id: string; name: string }, at: string) => ({ ...person, at });

/** The signers of the client in the requests, outside the company and the system. */
export const demoSigners = {
  ana: { name: 'Ana Souza', email: 'ana.souza@cliente.example' },
  bruno: { name: 'Bruno Lima', email: 'bruno.lima@cliente.example' },
};

interface DemoSigner {
  name: string;
  email: string;
  status: AcceptanceSigner['status'];
  respondedAt?: string;
  refusalReason?: string;
}

/** The signers of each term, as the drawer gets them. */
const signersOf: Record<string, DemoSigner[]> = {
  [id(1)]: [
    { ...demoSigners.ana, status: 'Signed', respondedAt: '2026-10-16T14:20:00Z' },
    { ...demoSigners.bruno, status: 'Pending' },
  ],
  [id(2)]: [
    {
      ...demoSigners.ana,
      status: 'Refused',
      respondedAt: '2026-10-14T18:00:00Z',
      refusalReason: 'Falta o relatório de testes de ICMS-ST.',
    },
  ],
  [id(3)]: [
    { ...demoSigners.ana, status: 'Signed', respondedAt: '2026-10-13T15:00:00Z' },
    { ...demoSigners.bruno, status: 'Signed', respondedAt: '2026-10-13T17:00:00Z' },
  ],
};

/** The terms of ERP-FIN, newest first, as the API lists them. */
export function demoAcceptances(): Acceptance[] {
  const summary = (termId: string) =>
    (signersOf[termId] ?? []).map(({ name, status }) => ({ name, status }));
  return [
    {
      id: id(1),
      title: 'Aceite da homologação',
      status: 'Pending',
      milestoneId: id(91),
      milestone: milestoneOf(0),
      signers: summary(id(1)),
      recipient: null,
      requested: by(demoPeople.marina, '2026-10-15T13:04:00Z'),
      answered: null,
      closedAt: null,
      isRequest: true,
    },
    {
      id: id(2),
      title: 'Aceite da especificação de ICMS-ST',
      status: 'Refused',
      milestoneId: id(94),
      milestone: milestoneOf(3),
      signers: summary(id(2)),
      recipient: null,
      requested: by(demoPeople.marina, '2026-10-14T12:00:00Z'),
      answered: null,
      closedAt: '2026-10-14T18:00:00Z',
      isRequest: true,
    },
    {
      id: id(3),
      title: 'Aceite do plano de contas',
      status: 'Signed',
      milestoneId: id(92),
      milestone: milestoneOf(1),
      signers: summary(id(3)),
      recipient: null,
      requested: by(demoPeople.marina, '2026-10-13T12:00:00Z'),
      answered: null,
      closedAt: '2026-10-13T17:00:00Z',
      isRequest: true,
    },
    {
      id: id(5),
      title: 'Aceite do cadastro de produtos',
      status: 'Pending',
      milestoneId: id(95),
      milestone: milestoneOf(4),
      signers: [],
      recipient: demoPeople.carla,
      requested: by(demoPeople.marina, '2026-10-08T12:00:00Z'),
      answered: null,
      closedAt: null,
      isRequest: true,
    },
    {
      id: id(4),
      title: 'Aceite do escopo do projeto',
      status: 'Signed',
      milestoneId: id(93),
      milestone: milestoneOf(2),
      signers: [],
      recipient: null,
      requested: by(demoPeople.marina, '2026-10-05T12:00:00Z'),
      answered: by(demoPeople.marina, '2026-10-05T12:00:00Z'),
      closedAt: '2026-10-05T12:00:00Z',
      isRequest: false,
    },
  ];
}

/** The list of the API: the terms of one status when asked, the counts of all of them and what may be done now. */
export function acceptanceList(
  terms: readonly Acceptance[],
  url: URL,
  can = { canRequest: true, canAttach: true },
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
    ...can,
  };
}

/**
 * A term with its signers and history, as the drawer gets it; with <code>local</code>, the pending signers of a
 * pending request have the link of the local provider.
 */
export function acceptanceDetails(
  term: Acceptance,
  actions: Partial<AcceptanceDetails['actions']> = {},
  local = true,
): AcceptanceDetails {
  const open = term.status === 'Pending';
  // A term made in a test has the signers of its list, with e-mails made from the order.
  const list: DemoSigner[] =
    signersOf[term.id] ??
    term.signers.map((item, index) => ({
      ...item,
      email: `signatario${String(index + 1)}@cliente.example`,
    }));
  const signers: AcceptanceSigner[] = list.map((item, index) => ({
    id: `${term.id.slice(0, -2)}${String(index + 10)}`,
    name: item.name,
    email: open ? item.email : null,
    status: item.status,
    respondedAt: item.respondedAt ?? null,
    refusalReason: item.refusalReason ?? null,
    signingLink:
      open && local && item.status === 'Pending'
        ? `/sign/token-${term.id.slice(-4)}-${String(index)}`
        : null,
  }));
  const requested = {
    kind: term.isRequest ? ('Requested' as const) : ('Attached' as const),
    actor: { id: term.requested.id, name: term.requested.name },
    signer: null,
    at: term.requested.at,
  };
  const answers = signers
    .filter((item) => item.respondedAt !== null)
    .map((item) => ({
      kind: item.status === 'Refused' ? ('Refused' as const) : ('Signed' as const),
      actor: null,
      signer: item.name,
      at: item.respondedAt ?? term.requested.at,
    }));
  const attached =
    term.isRequest && term.answered !== null
      ? [
          {
            kind: 'Attached' as const,
            actor: { id: term.answered.id, name: term.answered.name },
            signer: null,
            at: term.answered.at,
          },
        ]
      : [];
  return {
    term,
    message:
      term.isRequest && open && term.recipient === null
        ? 'Os testes de homologação terminaram sem pendências. Assinem o termo até 30/10, por favor.'
        : null,
    toSign: term.isRequest ? { name: 'termo-homologacao-erp-fin.pdf', size: 217_088 } : null,
    signers,
    signed:
      term.status === 'Signed'
        ? { documentId: id(80), name: 'termo-assinado.pdf', size: 245_760 }
        : null,
    refusalReason: term.status === 'Refused' ? 'Falta o relatório de testes de ICMS-ST.' : null,
    events: [requested, ...answers, ...attached],
    actions: {
      canAttach: false,
      canRemind: false,
      canCancel: false,
      ...actions,
    },
  };
}
