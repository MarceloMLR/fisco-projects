---
status: aceita (quem troca o responsável substituído pelo ADR-0068)
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0027: Troca do responsável e do solicitante dos chamados, um a um ou em lote

## Contexto e problema

Quando uma pessoa sai de férias ou deixa a empresa, os chamados ligados a ela param. Se ela é a solicitante, ninguém responde quando o chamado fica Aguardando solicitante; se é a responsável, ninguém o atende. Exemplo levantado por Marcelo: Lucas, do time funcional, tem um chamado em andamento e sai de férias. Como os chamados continuam?

## Fatores de decisão

- Nenhum chamado parado por ausência de uma pessoa.
- Pouco trabalho para redistribuir muitos chamados de uma vez.
- Histórico claro de quem respondia por cada chamado.
- Complexidade proporcional à v1.

## Opções consideradas

- A. Troca manual, chamado a chamado
- B. Troca manual mais troca em lote na lista de chamados
- C. B mais ausência com substituto automático

## Resultado da decisão

Opção escolhida: **B. Troca manual mais troca em lote**, porque resolve férias e saídas em poucos cliques sem a complexidade de regras automáticas de ausência.

Regras:

- O responsável pode ser trocado por quem atende o destino e pelos gestores do projeto; o novo responsável precisa ser alguém que atende o destino ou um gestor do projeto (mesma lista do ADR-0023). Substituído pelo ADR-0068: só o administrador, o gerente e o líder técnico trocam o responsável; o novo responsável sai de quem atende o time de destino ou é o gerente do projeto.
- O solicitante pode ser trocado pelos gestores do projeto ou pelo próprio solicitante; o novo solicitante passa a responder no lugar do anterior, que continua acompanhando o chamado.
- A lista de chamados permite filtrar os chamados de uma pessoa, marcar vários e trocar o responsável ou o solicitante de todos de uma vez.
- Toda troca fica registrada na linha do tempo e as pessoas envolvidas são avisadas (forma decidida no bloco de e-mail).
- Os papéis exatos de quem pode trocar serão confirmados na matriz de permissões.

Escopo:

| Item | Classificação |
| --- | --- |
| Trocar o responsável de um chamado | v1 |
| Trocar o solicitante de um chamado | v1 |
| Troca em lote pela lista de chamados | v1 |
| Ausência com período e substituto automático | depois |

### Consequências

- Bom, porque nenhum chamado fica sem quem responda durante ausências.
- Bom, porque a troca em lote reduz o trabalho em férias e desligamentos.
- Bom, porque a linha do tempo mostra quem respondia por cada etapa.
- Ruim, porque redistribuir continua dependendo de alguém lembrar de fazê-lo.

### Confirmação

Testes cobrindo: só quem atende o destino e os gestores trocam o responsável (após o ADR-0068, só administrador e gestores); o novo responsável precisa atender o destino ou ser gestor; só gestores e o próprio solicitante trocam o solicitante; o novo solicitante consegue responder em Aguardando solicitante e o anterior continua acompanhando; a troca em lote altera todos os chamados marcados e registra cada troca na linha do tempo.

## Prós e contras das opções

### A. Troca manual, chamado a chamado

- Bom, porque é simples.
- Ruim, porque redistribuir muitos chamados é trabalhoso.

### B. Troca manual mais troca em lote

- Bom, porque resolve férias e saídas rapidamente.
- Ruim, porque exige um pouco mais de tela e regras.

### C. Ausência com substituto automático

- Bom, porque ninguém precisa lembrar de redistribuir.
- Ruim, porque é bem mais complexo (retorno da pessoa, ausências sobrepostas) e envolve férias e ausências, deixadas fora no ADR-0018.
