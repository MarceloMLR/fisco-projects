## Context

A dependência (`ScheduleDependency`) liga um anterior a um seguinte, sempre "termina, então começa" sem intervalo: o `Schedule.EarliestAfter` dá o primeiro início do seguinte (o dia útil depois do fim, ou o dia do fim para um marco), o `Push` empurra em ordem topológica, o `Recalculate` (ADR-0126) recoloca tudo, e o `ScheduleCriticalPath` volta do fim do projeto pela regra inversa. O desfazer guarda as dependências como `DependencyState` (ADR-0091), o modelo como `ProjectTemplateDependency` e a importação como `OutlineDependency` (ADR-0135). O pedido de item leva `predecessorIds` e a resposta também.

## Goals / Non-Goals

**Goals:**

- Três tipos e o intervalo em dias úteis, com o mesmo empurrão só para frente (ADR-0137).
- Tudo o que hoje calcula ou guarda uma dependência passa a levar o tipo e o intervalo: o início, o empurrão, o recálculo, o caminho crítico, o desfazer, o modelo, a importação e a exportação.
- O pedido e a resposta continuam aceitando e trazendo `predecessorIds`, para nada que já usa a API quebrar.

**Non-Goals:**

- O quarto tipo, o intervalo em horas, dias corridos ou porcentagem, puxar ao antecipar.

## Decisions

- **Domínio.** `ScheduleLinkType` (`FinishToStart` = 1, `StartToStart` = 2, `FinishToFinish` = 3) e `Lag` (int, -99 a 999) em `ScheduleDependency`, com a regra num lugar só, `ScheduleLinks`:
  - `EarliestStart(link, anterior, seguinte, dias)`: "termina, então começa", o dia útil depois do fim do anterior (para um marco seguinte, o dia do fim), mais o intervalo; "começa junto", o início do anterior mais o intervalo; "termina junto", o fim do anterior mais o intervalo, menos a duração do seguinte em dias úteis;
  - `LatestEnd(link, anterior, seguinte, início mais tarde do seguinte, dias)`: a regra inversa, para o caminho crítico.
  - O intervalo conta em dias úteis a partir de um dia útil, para o negativo e o positivo serem simétricos.
- **Plano.** `ScheduleItemPlan` ganha `Links` (opcional): com ele, cada anterior tem o tipo e o intervalo dele; sem ele (o arrastar, as novas datas, o pedido antigo), a dependência que já existia mantém o tipo e o intervalo, e a nova é "termina, então começa" sem intervalo.
- **API.** `links` (lista de `{ predecessorId, type, lag }`) no pedido de incluir e editar item; quando vem, manda nas dependências e os `predecessorIds` são ignorados. Cada item da resposta ganha `links` na mesma ordem dos `predecessorIds`. Erro em `links` (tipo que não existe, intervalo fora da faixa, anterior repetido).
- **Desfazer e modelo.** `DependencyState` e `OutlineDependency` ganham o tipo e o intervalo, opcionais com o padrão de hoje, para as mudanças já guardadas continuarem valendo; `ProjectTemplateDependency` ganha as duas colunas; salvar como modelo e criar a partir dele os levam.
- **MS Project.** A exportação grava `Type` (1, 3 ou 0) e `LinkLag` em décimos de minuto do dia de 8 horas, com `LagFormat` 7. A importação lê os três tipos; `Type` 2 vira "termina, então começa" com o aviso `OtherDependencies`; o `LinkLag` vira dias úteis pelo `LagFormat`: 7 (dias) e 5 (horas) pelo dia de 8 horas, 8 (dias corridos) e 19 (porcentagem) arredondados com o aviso `DependencyLags`, que passa a dizer "convertido" em vez de "sem intervalo". Fora da faixa, o intervalo fica no limite, com o mesmo aviso.
- **Tela.** No detalhe, abaixo de "Depende de", uma linha por anterior escolhido: o nome, o tipo (`Select`) e o intervalo (`TextField` numérico, "dias úteis"); a coluna "Depende de" diz "Nome (começa junto, +1)" quando não é "termina, então começa" sem intervalo; a Linha do tempo desenha "começa junto" do início ao início (a seta entra pela esquerda) e "termina junto" do fim ao fim (a seta entra pela direita). O Excel usa as siglas do MS Project em português: TI, II e TT, com o intervalo ("5II+2"); "termina, então começa" sem intervalo fica só o número.

## Risks / Trade-offs

- [Intervalo negativo em "começa junto" pode pôr a seguinte antes da anterior] → É o que o MS Project faz; o empurrão continua respeitando o tipo, e o ciclo continua recusado pelo grafo.
- [Mudança do pedido do item] → `predecessorIds` continua valendo; `links` é opcional.
