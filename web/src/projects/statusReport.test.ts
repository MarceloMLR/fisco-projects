import { describe, expect, it } from 'vitest';
import { risksAfter, statusReportSchema, ticketOptions, valuesOf } from './statusReport';

const tickets = [
  {
    number: 545,
    title: 'Dúvida sobre o relatório fiscal',
    status: 'Open' as const,
    threatens: false,
  },
  { number: 539, title: 'Rejeição na emissão', status: 'InProgress' as const, threatens: true },
];

// Spec: relatorio-de-status, requirements "Rascunho do relatório" and "Gerar o relatório pela tela" (ADR-0165).
describe('Rascunho do relatório na tela', () => {
  it('fills the form with the draft and never with the hours', () => {
    const values = valuesOf({
      last: null,
      summary: 'Cadastros validados',
      focus: '',
      nextSteps: '',
      clientNeeds: '',
      tickets,
      risks: [{ number: 539, level: 'High', attention: 'Depende do fornecedor' }],
    });

    expect(values).toEqual({
      summary: 'Cadastros validados',
      focus: '',
      nextSteps: '',
      clientNeeds: '',
      risks: [{ number: 539, level: 'High', attention: 'Depende do fornecedor' }],
      showHours: false,
    });
  });

  it('lists the tickets that threaten milestones first', () => {
    expect(ticketOptions(tickets, { threatens: 'Ameaçam', others: 'Outros' })).toEqual([
      { value: '539', label: '#539 · Rejeição na emissão', group: 'Ameaçam' },
      { value: '545', label: '#545 · Dúvida sobre o relatório fiscal', group: 'Outros' },
    ]);
  });

  it('keeps the risks chosen and adds a new one as Medium', () => {
    const risks = [{ number: 539, level: 'High' as const, attention: 'Depende do fornecedor' }];

    expect(risksAfter(risks, ['539', '545'])).toEqual([
      ...risks,
      { number: 545, level: 'Medium', attention: '' },
    ]);
    expect(risksAfter(risks, ['545'])).toEqual([{ number: 545, level: 'Medium', attention: '' }]);
  });

  it('refuses a text over the limit and more than ten risks', () => {
    const values = {
      summary: '',
      focus: '',
      nextSteps: '',
      clientNeeds: '',
      risks: [],
      showHours: false,
    };

    expect(statusReportSchema.safeParse({ ...values, summary: 'a'.repeat(1001) }).success).toBe(
      false,
    );
    expect(
      statusReportSchema.safeParse({
        ...values,
        risks: Array.from({ length: 11 }, (_, index) => ({
          number: index,
          level: 'Low',
          attention: '',
        })),
      }).success,
    ).toBe(false);
    expect(statusReportSchema.safeParse(values).success).toBe(true);
  });
});
