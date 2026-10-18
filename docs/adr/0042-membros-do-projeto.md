---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0042: Membros do projeto geridos pelo gerente e pelo líder técnico, com ao menos um gerente

## Contexto e problema

Pelo ADR-0040, o gerente gerencia as pessoas do projeto e o líder técnico tem quase todos os poderes do gerente. Falta definir quem inclui e remove pessoas, como garantir que o projeto nunca fique sem gerente e o que acontece com os chamados e as tarefas de quem sai.

## Fatores de decisão

- Autonomia do líder técnico para montar o time técnico.
- Projeto nunca sem gerente.
- Nenhum chamado ou tarefa esquecido quando alguém sai.
- Controle de quem entra no projeto.

## Opções consideradas

- A. Gerente e líder técnico gerenciam as pessoas; o líder técnico não inclui, não remove e não promove gerentes
- B. Só o gerente gerencia as pessoas
- C. Qualquer membro inclui pessoas; só o gerente remove

## Resultado da decisão

Opção escolhida: **A. Gerente e líder técnico gerenciam as pessoas, com limite para o líder técnico**, porque segue o modelo do ADR-0040 e evita que o líder técnico dependa do gerente para montar o time técnico.

Escopo:

| Item | Classificação |
| --- | --- |
| Incluir pessoas buscando no diretório da empresa (ADR-0005) e escolhendo o papel no projeto | v1 |
| Um papel por pessoa em cada projeto | v1 |
| O projeto sempre tem ao menos um gerente: o sistema impede remover ou trocar o papel do último | v1 |
| Ao remover alguém, o sistema mostra os chamados (como responsável ou solicitante) e as tarefas da pessoa e oferece a troca em lote (ADR-0027); se seguir sem trocar, os itens ficam sem responsável e destacados | v1 |
| Quem é removido perde o acesso na hora; o que a pessoa já fez (mensagens, registros) continua com o nome dela | v1 |
| A pessoa incluída ou com o papel alterado é avisada (resumo diário, ADR-0037) | v1 |
| Incluir um grupo do Entra inteiro no projeto | depois |
| Período de participação (data de entrada e saída) | depois |

### Consequências

- Bom, porque o líder técnico monta o time técnico sem depender do gerente.
- Bom, porque o projeto nunca fica sem gerente e as pendências de quem sai são redistribuídas.
- Ruim, porque duas pessoas podem alterar a equipe (mitigado pela auditoria, ADR-0039).

### Confirmação

Testes cobrindo: gerente inclui, remove e troca o papel de qualquer pessoa; líder técnico não inclui, não remove nem promove gerentes; não é possível remover ou rebaixar o último gerente; a remoção lista chamados e tarefas da pessoa e permite a troca em lote; quem é removido perde o acesso na requisição seguinte; mensagens e registros antigos continuam com o nome da pessoa.

## Prós e contras das opções

### A. Gerente e líder técnico

- Bom, porque segue o modelo de papéis e dá autonomia ao líder técnico.
- Ruim, porque divide a gestão da equipe entre dois papéis.

### B. Só o gerente

- Bom, porque centraliza o controle.
- Ruim, porque o líder técnico depende do gerente para tudo.

### C. Qualquer membro inclui

- Bom, porque não cria gargalo.
- Ruim, porque perde o controle de quem entra no projeto.
