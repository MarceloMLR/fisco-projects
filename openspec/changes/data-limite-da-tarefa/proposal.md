## Why

Nos projetos fiscais, muita entrega tem um prazo que não depende do cronograma: o SPED, a EFD-Reinf e a DCTFWeb têm data legal, e o cliente costuma fixar uma data para a virada. No MS Project isso é a "data-limite" (Deadline): a tarefa continua com as próprias datas, empurradas pelas dependências, e a data-limite só avisa quando o fim passa dela. O FiscoProjects não tem onde guardar esse prazo, e a gestão só percebe o estouro olhando data por data. GanttA-2, do grupo 14 (Aprimoramento do Gantt 1), a pedido de Marcelo.

## What Changes

- Cada tarefa (inclusive a tarefa-resumo e a subtarefa) e cada marco pode ter uma data-limite, opcional, posta e tirada por quem edita o cronograma (o administrador e o gerente, ADR-0043), no detalhe do item ao incluir e ao editar.
- A data-limite não muda as datas: nem as do item, nem as das dependências (ADR-0019). Ela só compara: a tarefa pelo fim; o marco pelo dia em que foi atingido ou, antes disso, pelo dia dele.
- O cronograma diz, em dias úteis do calendário da instalação, quanto cada item passou da data-limite, e quantos itens passaram (ADR-0018):
  - na Linha do tempo, um marcador no dia da data-limite, na linha do item, em vermelho quando passou, com a legenda "Data-limite";
  - no detalhe, a data-limite e "Passou 2 dias úteis da data-limite" ou "3 dias úteis antes da data-limite", com ícone, cor e texto (ADR-0065);
  - na Lista e no Expandir, a coluna "Data-limite", com o estouro destacado;
  - no resumo da aba, ao lado das tarefas e marcos atrasados, "1 data-limite estourada";
  - na Visão geral, em "Precisa de atenção", cada item que passou da data-limite (ADR-0073).
- A página de impressão (PDF) desenha o mesmo marcador. O Excel ganha as colunas "Data-limite" e "Passou da data-limite (dias úteis)".
- O XML do MS Project leva a data-limite (`Deadline`) e a importação a traz, movida em dias úteis junto com o cronograma quando o início muda (ADR-0135).
- Pôr, mudar e tirar a data-limite entra no desfazer (ADR-0091) e na auditoria (ADR-0039).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: a data-limite de tarefas e marcos, pela API e pela tela, no PDF, no Excel e no XML do MS Project.
- `visao-geral-do-projeto`: os itens que passaram da data-limite em "Precisa de atenção".

## Non-goals

- Uma restrição que muda as datas, como o "Deve terminar em" do MS Project: as datas continuam saindo das dependências (ADR-0019).
- Aviso por e-mail ou no sino quando a data-limite estoura; pode entrar depois, na tabela do ADR-0037.
- Data-limite na fase ou no projeto; data-limite nos modelos de projeto, que não têm datas (ADR-0017).
- Filtrar os itens que passaram da data-limite; entra nos filtros do cronograma (GanttA-3).
- Data-limite no Meu trabalho.

## Impact

- `src/Modules/Projects`:
  - o domínio guarda a data-limite no `ScheduleItem` (migração com a coluna nova) e a leva nas fotos do desfazer;
  - o `ScheduleFigures` conta os dias úteis depois da data-limite, num lugar só;
  - os pedidos de incluir e editar item, o `GET /schedule` e a visão geral levam a data-limite e o estouro;
  - o `MsProjectExport` e o `MsProjectImport` levam e trazem o `Deadline`, e o `SchedulePlanOutline` ganha a distância da data-limite em dias úteis;
  - os testes.
- `web/src`: o campo no detalhe do item, o marcador do `ScheduleGantt` e do `SchedulePrintPage`, a coluna na Lista e no Expandir, o resumo, a Visão geral, o Excel (`scheduleReport.ts`), os textos e os testes; o contrato e os tipos gerados de novo; a demonstração com datas-limite.
- Documentação: `CLAUDE.md`, `docs/glossario.md` e o roadmap (GanttA-2, ADR-0018, ADR-0019, ADR-0020, ADR-0043, ADR-0065).
