---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0056: Pirâmide de testes ligada às specs, com matrizes de permissão 100% cobertas

## Contexto e problema

O CLAUDE.md global exige criar ou atualizar testes em toda mudança e rodá-los antes de concluir. O ideia.md previa xUnit e Testcontainers, e a regra de que um erro de permissão na spec vira um erro de segurança no sistema. A arquitetura é um monolito modular (ADR-0047), com React no frontend (ADR-0049) e integrações com o Microsoft 365 atrás de interfaces (ADR-0004). Como testar?

## Fatores de decisão

- Confiança para mudar o código sem quebrar regras.
- Rastreabilidade entre specs do OpenSpec e testes.
- Permissões, onde mora o risco de segurança, sempre testadas.
- Testes rápidos e estáveis no CI.
- Ferramentas sem custo de licença.

## Opções consideradas

- A. Pirâmide de testes ligada às specs
- B. Só testes unitários, com o resto testado à mão
- C. Foco em testes de ponta a ponta

## Resultado da decisão

Opção escolhida: **A. Pirâmide de testes ligada às specs**.

Camadas:

| Camada | O que cobre | Ferramentas |
| --- | --- | --- |
| Unitários | Regras de negócio e casos de uso (dias úteis, prazos, dependências, fluxo de chamados) | xUnit, AwesomeAssertions, NSubstitute |
| Integração | API completa com banco real e simulações do Microsoft 365 | WebApplicationFactory, Testcontainers com SQL Server |
| Arquitetura | Fronteiras entre módulos e camadas (ADR-0047) | NetArchTest ou ArchUnitNET |
| Permissões | Cada célula das matrizes dos ADR-0043 a ADR-0046, permitido e negado | Testes de integração gerados a partir de uma tabela |
| Frontend | Componentes e telas | Vitest, Testing Library, MSW |
| Ponta a ponta | Fluxos críticos: entrar, abrir e atender chamado, mover tarefa no Gantt, responder aceite com PDF | Playwright |

Regras:

- Todo cenário de uma spec do OpenSpec tem ao menos um teste, rastreável pelo nome.
- As matrizes de permissão são 100% cobertas, com casos permitidos e negados.
- Cobertura mínima de 80% nas camadas `Domain` e `Application`, exigida pelo CI como indicador, não como meta em si.
- Testes nunca acessam o Microsoft 365 real; usam as implementações locais ou simuladas (ADR-0004).
- Não usar FluentAssertions 8 ou superior (licença comercial desde 2025); usar AwesomeAssertions (fork com licença Apache 2.0).
- Testes de ponta a ponta rodam contra o ambiente de desenvolvimento e demonstração após o merge na `main`, antes de qualquer publicação em produção.

### Consequências

- Bom, porque regras, permissões e integrações ficam protegidas contra regressões.
- Bom, porque cada requisito especificado tem um teste correspondente.
- Ruim, porque exige disciplina e tempo de escrita desde a primeira mudança.
- Ruim, porque testes de integração com contêiner deixam o CI mais lento.

### Confirmação

O CI falha se: algum teste falhar; a cobertura de `Domain` e `Application` ficar abaixo de 80%; alguma célula das matrizes de permissão não tiver teste; o pacote FluentAssertions 8 ou superior aparecer nas dependências.

## Prós e contras das opções

### A. Pirâmide ligada às specs

- Bom, porque equilibra velocidade e confiança e liga specs a testes.
- Ruim, porque exige mais esforço inicial.

### B. Só unitários e testes manuais

- Bom, porque é mais rápido no começo.
- Ruim, porque permissões e integrações ficam sem cobertura automática.

### C. Foco em ponta a ponta

- Bom, porque testa como o usuário vê.
- Ruim, porque os testes são lentos, frágeis e caros de manter.
