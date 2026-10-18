---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0022: Chamados sempre ligados a um projeto, sem service desk geral

## Contexto e problema

No MVP, todo chamado nasce dentro de um projeto, e a TI atende numa fila única que reúne chamados de vários projetos (ADR-0011). É preciso decidir se também existe chamado avulso, sem projeto, como um pedido de suporte geral. A resposta define a fronteira entre o FiscoProjects e uma ferramenta de service desk (como GLPI ou Jira Service Management).

## Fatores de decisão

- Manter o foco do produto em gestão de projetos.
- Regras de acesso simples e coerentes.
- Evitar crescimento de escopo para funções de service desk.

## Opções consideradas

- A. Chamado sempre ligado a um projeto
- B. Projeto opcional, com chamados avulsos
- C. Projeto opcional só para chamados da TI

## Resultado da decisão

Opção escolhida: **A. Chamado sempre ligado a um projeto**, porque mantém o foco em projetos, faz as permissões dos chamados partirem do acesso ao projeto e evita que o sistema vire um service desk.

Regras:

- Todo chamado pertence a um projeto; não existe chamado avulso.
- Service desk geral (catálogo de serviços, base de conhecimento, suporte sem projeto) está fora do escopo do FiscoProjects.
- Uma empresa que queira receber pedidos contínuos pode criar um projeto próprio para isso (por exemplo, "Suporte interno").
- Projeto concluído ou cancelado é somente leitura (ADR-0016): não recebe chamados novos; é preciso reabri-lo.

### Consequências

- Bom, porque o produto mantém uma fronteira clara.
- Bom, porque as regras de acesso dos chamados derivam do acesso ao projeto.
- Bom, porque mensagens, anexos e histórico ficam no contexto do projeto.
- Ruim, porque pedidos que não são de projeto não cabem no sistema.
- Ruim, porque chamados depois do encerramento exigem reabrir o projeto; em implantações, o projeto tende a ficar em andamento durante a operação assistida.

### Confirmação

Testes cobrindo: não é possível abrir chamado sem projeto; não é possível abrir chamado em projeto concluído ou cancelado; reabrir o projeto volta a permitir a abertura.

## Prós e contras das opções

### A. Sempre ligado a um projeto

- Bom, porque mantém o foco e simplifica as permissões.
- Ruim, porque não atende suporte geral.

### B. Projeto opcional, com chamados avulsos

- Bom, porque serve também como canal de suporte geral.
- Ruim, porque abre caminho para funções de service desk e exige regras de acesso para chamados sem projeto.

### C. Projeto opcional só para a TI

- Bom, porque atende o suporte geral onde ele costuma aparecer.
- Ruim, porque o mesmo módulo passa a ter duas regras de acesso.
