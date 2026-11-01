## Context

O item do cronograma (`ScheduleItem`) tem início, duração, fim, progresso, responsável e as datas de conclusão (`CompletedOn`) e de marco atingido (`ReachedOn`). As datas saem das dependências (ADR-0019) e dos dias úteis do calendário da instalação (ADR-0018), e o atraso é contado pelo `ScheduleFigures.LateDaysOf` (ADR-0020). Os pedidos de incluir e editar item (`NewScheduleItemRequest`, `ScheduleItemRequest`) levam todos os campos do detalhe. O desfazer guarda as fotos dos itens (`ItemState`, ADR-0091). A Linha do tempo é desenhada em SVG pelo próprio FiscoProjects, com as classes `g-*` e só os tokens do design system (ADR-0134). O `SchedulePlanOutline` é a forma comum do modelo e do arquivo do MS Project (ADR-0135).

## Goals / Non-Goals

**Goals:**

- Guardar a data-limite de cada tarefa e marco sem mexer no cálculo das datas.
- Dizer, num lugar só, quanto cada item passou (ou falta), em dias úteis, e quantos itens abertos estouraram.
- Mostrar o estouro onde a gestão já olha: Linha do tempo, detalhe, Lista, resumo da aba, Visão geral, PDF e Excel.

**Non-Goals:**

- Restrições que movem datas, avisos por e-mail ou sino, data-limite em fase, projeto ou modelo, e o filtro dos estourados (GanttA-3).

## Decisions

- **Sem ADR novo.** Marcelo escolheu, ao aprovar o grupo 14, a data-limite que não muda as datas e avisa. A alternativa, a restrição que move a tarefa ("Deve terminar em"), quebraria a regra de as datas saírem das dependências (ADR-0019).
- **Domínio.** `ScheduleItem.Deadline` (`DateOnly?`), na tarefa (também a resumo e a subtarefa) e no marco, posta pelos mesmos casos de uso de incluir e editar item, com a mesma faixa de datas dos outros campos (2000 a 2099). Pode ficar antes do início: o item só aparece estourado. Entra no `ItemState` (opcional, para as mudanças já guardadas continuarem valendo), no desfazer e na auditoria. A coluna nova vem por migração do módulo.
- **Quanto passou, num lugar só.** `ScheduleFigures.DeadlineDeviationOf`, em dias úteis com sinal, como a diferença da linha de base: positivo depois, negativo antes, zero no dia. A data comparada é:
  - a tarefa concluída: o dia em que terminou (`CompletedOn`, ou o fim);
  - o marco atingido: o dia em que foi atingido;
  - o item aberto: o fim (o dia do marco) ou hoje, o que vier depois, porque o item atrasado não termina antes de hoje (ADR-0020).
  - Estourado é o item aberto com a diferença positiva. O concluído e o atingido mostram a diferença, mas não contam nem entram na Visão geral.
- **API.** Cada item do `GET /schedule` ganha `deadline` e `deadlineDeviation` (nulos sem data-limite); o cronograma, `missedDeadlines`; os pedidos de incluir e editar item, `deadline` (nulo tira). A Visão geral ganha, em "Precisa de atenção", os itens estourados (`deadlines`, com o nome, a data-limite e os dias úteis).
- **Marcador na Linha do tempo.** Na linha do item, uma linha vertical tracejada no fim do dia da data-limite, com um triângulo pequeno apontado para baixo no alto da linha, como a seta do MS Project; na cor das dependências (`--gantt-dependency`) e, estourado, em `--status-danger-text`, sem cor nova. A legenda ganha "Data-limite", e o nome acessível da barra diz a data-limite e o estouro. A página de impressão desenha o mesmo marcador.
- **Detalhe, Lista e Expandir.** O campo "Data-limite" (opcional) no detalhe, para quem edita; para quem vê, a data e o texto pelo `DueDate`: estourado em `overdue` ("Passou 1 dia útil da data-limite"), no dia ou antes em `onTrack` ("3 dias úteis antes da data-limite", "No dia da data-limite"), concluído antes em `met`. A coluna "Data-limite" na Lista e no Expandir, com o estouro pelo `DueDate`.
- **Resumo.** Ao lado de "4 tarefas atrasadas", "1 data-limite estourada", com ícone, cor e texto, só quando há.
- **MS Project.** A exportação grava o `Deadline` da tarefa (às 17h, como o fim). A importação lê o `Deadline` e o converte em distância em dias úteis ao primeiro dia do arquivo, como os inícios: o `OutlineItem` ganha `DeadlineOffset`, e o `Schedule.ApplyTemplate` a aplica a partir do início escolhido. O modelo não tem data-limite (`ToOutline` sem ela), e salvar como modelo não a leva.
- **Excel.** As colunas "Data-limite" e "Passou da data-limite (dias úteis)" (só o positivo) entram depois do "Atraso".

## Risks / Trade-offs

- [Item concluído depois da data-limite continua "passou 2 dias úteis" no detalhe e no Excel] → É o histórico; ele sai da contagem e da Visão geral, que são do que ainda dá para agir.
- [Mais uma coluna no Expandir] → A Linha do tempo já rola para o lado no Expandir; no celular, a tabela continua só com o nome.
- [Data-limite do MS Project em dia não útil vai para o dia útil seguinte na importação] → Igual aos inícios (ADR-0135), com o aviso de sempre quando as datas mudam.
