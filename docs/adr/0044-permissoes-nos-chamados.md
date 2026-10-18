---
status: aceita (filas, "Assumir" e troca do responsável por quem atende substituídos pelo ADR-0068)
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0044: Permissões nos chamados, com todos os membros do projeto vendo todos os chamados

## Contexto e problema

Segunda parte da matriz de permissões. Nos chamados, o que alguém pode fazer depende da relação da pessoa com o chamado: gestor do projeto, quem atende o destino, solicitante ou apenas membro. No ADR-0026, a indicação inicial era que só o solicitante, quem atende e os gestores veriam cada chamado. Na revisão, Marcelo decidiu que todos os membros do projeto veem todos os chamados do projeto, mas só atua quem tem relação com o chamado, e que o leitor apenas visualiza (não abre chamados).

## Fatores de decisão

- Transparência dentro do projeto.
- Só atua no chamado quem tem relação com ele.
- O leitor acompanha, sem participar.

## Opções consideradas

- A. Cada chamado visível só para solicitante, quem atende e gestores (indicação do ADR-0026)
- B. Todos os membros do projeto veem todos os chamados do projeto; atuar depende da relação com o chamado

## Resultado da decisão

Opção escolhida: **B. Todos os membros veem, só quem tem relação atua**, por decisão de Marcelo.

Relações:

- Gestores: gerente e líder técnico do projeto, que veem e atuam em todos os chamados do projeto (ADR-0040).
- Quem atende: atendentes do destino do chamado. Funcional: colaboradores. Técnico: líder técnico e desenvolvedores. Fila: atendentes da fila, mesmo sem serem membros do projeto. Substituído pelo ADR-0068: sem filas; os times são Funcional e Desenvolvimento.
- Solicitante: quem abriu o chamado, ou quem ficou no lugar dele (ADR-0027).
- Outros membros: demais pessoas do projeto, inclusive leitores.

Matriz:

| Ação | Administrador | Gestores | Quem atende | Solicitante | Outros membros (inclusive leitores) |
| --- | --- | --- | --- | --- | --- |
| Ver o chamado e acompanhar | ✓ | ✓ | ✓ | ✓ | ✓ |
| Mandar mensagens e anexos, mencionar | ✓ | ✓ | ✓ | ✓ | — |
| Assumir e mudar o status como quem atende (triagem, andamento, pedir informação, resolver) | ✓ | ✓ | ✓ | — | — |
| Responder a pedido de informação, confirmar e fechar, reabrir | ✓ | ✓ (fechar) | — | ✓ | — |
| Alterar a prioridade | ✓ | ✓ | ✓ | — | — |
| Transferir para outro destino | ✓ | ✓ | ✓ | — | — |
| Trocar o responsável | ✓ | ✓ | ✓, dentro do destino | — | — |
| Trocar o solicitante | ✓ | ✓ | — | ✓, o próprio | — |
| Troca em lote pela lista | ✓ | ✓ | — | — | — |
| Registrar e atualizar chamado no fornecedor | ✓ | ✓ | ✓ | — | — |
| Cancelar (quando está Aberto ou Em triagem) | ✓ | ✓ | — | ✓ | — |

Revisão de 17/10/2026 (ADR-0068):

- "Assumir" sai da matriz: a linha passa a ser "Mudar o status como quem atende (triagem, andamento, pedir informação, resolver)". O responsável é definido pela primeira resposta de alguém do time, só quando o chamado ainda não tem responsável.
- "Trocar o responsável": só administrador e gestores; quem atende não troca.
- "Transferir para outro destino" vale entre Funcional e Desenvolvimento e deixa o chamado sem responsável.

Regras:

- Abrir chamado: Administrador, Gerente, Líder técnico, Colaborador e Desenvolvedor do projeto. O Leitor não abre chamados. Atendentes de fila que não são membros do projeto não abrem chamados nele. (Sem filas na v1, ADR-0068.)
- Todos os membros do projeto veem todos os chamados do projeto; atendentes de fila não membros veem só os chamados da sua fila. (Sem filas na v1, ADR-0068.)
- Na tabela de status do ADR-0024, "gestor" significa o gerente e o líder técnico.
- A regra de menção do ADR-0026 continua: podem ser mencionados os gestores do projeto e os envolvidos no chamado (solicitante e quem atende).

### Consequências

- Bom, porque o projeto ganha transparência: todos acompanham o que está acontecendo.
- Bom, porque só quem tem relação com o chamado altera algo nele.
- Ruim, porque um chamado com assunto sensível fica visível a todo o projeto.
- Neutro, porque altera a indicação inicial de visibilidade registrada no ADR-0026.

### Confirmação

Testes cobrindo cada célula da matriz (permitido e negado); o leitor vê os chamados do projeto e não consegue abrir, responder nem alterar; outros membros veem e acompanham, mas não mandam mensagens; atendente de fila não membro vê só os chamados da fila (sem filas na v1, ADR-0068).

## Prós e contras das opções

### A. Visível só para os envolvidos

- Bom, porque restringe assuntos sensíveis.
- Ruim, porque o restante do projeto não acompanha os problemas.

### B. Todos veem, só envolvidos atuam

- Bom, porque dá transparência sem perder controle de quem atua.
- Ruim, porque não há como esconder um chamado de parte do projeto.
