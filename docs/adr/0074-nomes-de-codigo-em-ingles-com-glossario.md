---
status: aceita
date: 2026-10-17
decision-makers: Marcelo Ribeiro
---

# ADR-0074: Nomes de código em inglês, inclusive módulos e termos do negócio, com glossário português–inglês

Substitui, no ADR-0047, os nomes de módulos em português do exemplo de estrutura (`FiscoProjects.Chamados` e a lista Chamados, Projetos, Documentos, Acessos, Notificacoes, Auditoria, Administracao); e, no ADR-0051, o nome do componente `CronogramaGantt`, que passa a ser `ScheduleGantt`.

## Contexto e problema

O CLAUDE.md global e o do projeto pedem código, nomes e comentários em inglês. O ADR-0047 mostrou a estrutura dos módulos com nomes em português (`FiscoProjects.Chamados`), e o ADR-0051 chamou o componente do Gantt de `CronogramaGantt`. As specs, os ADRs e a interface ficam em português. Antes do primeiro módulo de negócio (Acesso-1), é preciso decidir em que idioma ficam os nomes de módulos, classes, métodos, tabelas e rotas.

## Fatores de decisão

- Um idioma só no código, sem misturas como `Chamado.AssignOwner()`.
- Coerência com a regra de código em inglês e com um possível comprador de fora do Brasil (ADR-0002, ADR-0013).
- Rastreabilidade entre os termos das specs, em português, e os nomes no código.
- Bibliotecas, frameworks e exemplos de mercado estão em inglês.

## Opções consideradas

- A. Tudo em inglês, com um glossário português–inglês dos termos do negócio
- B. Termos do negócio em português (módulos, entidades e regras), como no ADR-0047, e o restante em inglês
- C. Tudo em português

## Resultado da decisão

Opção escolhida: **A. Tudo em inglês, com glossário**, por decisão de Marcelo em 17/10/2026.

Regras:

- Módulos, projetos, namespaces, classes, métodos, propriedades, variáveis, testes, schemas e tabelas do banco, rotas da API e rotas das telas ficam em inglês.
- O glossário em `docs/glossario.md` liga cada termo do negócio, em português, ao nome usado no código. Um termo novo entra no glossário na mesma change que o cria.
- Specs, propostas, ADRs, documentação, commits, PRs, textos da interface e e-mails continuam em português, como já decidido (ADR-0013 e CLAUDE.md).
- O nome exibido do teste (`DisplayName`) continua sendo o nome do cenário da spec, em português (ADR-0056).
- Os módulos de exemplo do ADR-0047 passam a ser `Tickets`, `Projects`, `Documents`, `Credentials`, `Notifications`, `Audit` e `Administration`. A lista final de módulos e o que fica em cada um são decididos na change que cria cada módulo.
- O componente do Gantt chama-se `ScheduleGantt`; continua sendo o único lugar que importa `@syncfusion/*` (ADR-0051).

### Consequências

- Bom, porque o código tem um idioma só e segue a regra que já existia.
- Bom, porque o glossário dá um lugar único para conferir a tradução de cada termo.
- Ruim, porque quem lê a spec em português e o código em inglês depende do glossário para alguns termos (por exemplo, "termo de aceite" → `AcceptanceRequest`).
- Neutro, porque as rotas das telas em inglês aparecem na barra de endereço; os textos da tela continuam em português.

### Confirmação

Revisão de código: nomes de código em português são apontados no PR. Cada change que cria um termo do negócio atualiza `docs/glossario.md`.

## Prós e contras das opções

### A. Tudo em inglês, com glossário

- Bom, porque é coerente com a regra de código em inglês e com o ecossistema.
- Ruim, porque exige manter o glossário.

### B. Termos do negócio em português

- Bom, porque o código usa as mesmas palavras das specs.
- Ruim, porque mistura idiomas dentro da mesma linha de código e contraria a regra de código em inglês.

### C. Tudo em português

- Bom, porque todo o projeto fala a mesma língua.
- Ruim, porque contraria a regra de código em inglês e briga com os nomes das bibliotecas e frameworks.
