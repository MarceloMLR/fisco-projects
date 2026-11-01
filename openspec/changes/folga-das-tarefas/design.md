## Context

O `ScheduleCriticalPath.Of` volta do fim do projeto, em ordem topológica inversa, e calcula o dia mais tarde em que cada item pode terminar (pelo `ScheduleLinks.LatestEnd`, com o tipo e o intervalo, ADR-0137); o item que não pode terminar depois do fim dele está no caminho crítico. O `GET /schedule` traz `critical` em cada item, e a Linha do tempo pinta as barras críticas com o caminho crítico ligado em "Exibir".

## Goals / Non-Goals

**Goals:**

- A folga total de cada item, num lugar só, a partir do mesmo cálculo do caminho crítico.
- Mostrar a folga onde a gestão já olha o caminho crítico: o detalhe, a Linha do tempo e o Excel.

**Non-Goals:**

- A folga livre, filtros e avisos de folga, o PDF.

## Decisions

- **Domínio.** `ScheduleCriticalPath.Analyze` devolve o caminho crítico e a folga de cada item (`ScheduleSlack`: o id, a folga em dias úteis e o último fim); o `Of` passa a usá-lo. A folga é a conta dos dias úteis depois do fim do item até o último fim, zero quando o último fim não vem depois. A tarefa-resumo fica de fora, como no caminho crítico; um item concluído tem a folga das datas, como o caminho crítico (ADR-0021).
- **API.** `float` (dias úteis) e `latestEnd` em cada item, nulos para a tarefa-resumo e sem itens.
- **Tela.**
  - Detalhe: a linha "Folga" no `ItemDetails` e no editor, com "4 dias úteis de folga" e, sem folga, "Sem folga: no caminho crítico", pelo `slackText` de `schedule.ts`.
  - Linha do tempo: com `showCritical`, uma linha fina (`g-slack`) do fim da barra até o fim do último dia (`latestEnd`), na altura do meio da barra, na cor da linha de base (`--gantt-baseline`), sem token novo; a legenda ganha "Folga" com o caminho crítico.
  - Excel: "Folga (dias úteis)" depois de "Caminho crítico".

## Risks / Trade-offs

- [A folga de um item concluído] → Igual ao caminho crítico (ADR-0021): vale pelas datas; a tela mostra, e o Excel também.
- [A folga depende do fim do projeto, que muda a cada empurrão] → É o mesmo cálculo do caminho crítico, no servidor, a cada leitura.
