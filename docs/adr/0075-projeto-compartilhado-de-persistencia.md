---
status: aceita
date: 2026-10-17
decision-makers: Marcelo Ribeiro
---

# ADR-0075: Projeto compartilhado de persistência para os módulos

## Contexto e problema

Cada módulo é dono das próprias tabelas, num schema próprio (ADR-0047), e todos usam EF Core com SQL Server (ADR-0048). Todo módulo precisa das mesmas coisas: schema e histórico de migrações próprios, datas em UTC (ADR-0013), chave gerada pela aplicação (ADR-0061), tentativas de novo em falhas passageiras do Azure SQL e, mais tarde, o ponto único da auditoria (ADR-0039). O `SharedKernel` não pode depender de EF Core, e um módulo não pode referenciar o host. Onde fica esse código comum?

## Fatores de decisão

- Não repetir em cada módulo a mesma configuração de banco ("sem duplicação", CLAUDE.md).
- Manter `SharedKernel`, `Domain` e `Application` sem tecnologia de banco (ADR-0047).
- Um lugar só para o ponto de auditoria ao salvar (ADR-0039).

## Opções consideradas

- A. Projeto compartilhado `FiscoProjects.Persistence`, usado só pela camada `Infrastructure` dos módulos e pelo host
- B. Configuração repetida no `Infrastructure` de cada módulo
- C. Código de banco dentro do `SharedKernel`

## Resultado da decisão

Opção escolhida: **A. Projeto compartilhado `FiscoProjects.Persistence`**.

Regras:

- Contém o contexto base dos módulos, o registro de cada contexto (schema, histórico de migrações, connection string, tentativas de novo), as convenções comuns, a aplicação das migrações de todos os módulos e a verificação do banco.
- Só a camada `Infrastructure` dos módulos e o host o usam; `Domain` e `Application` não. Os testes de arquitetura garantem.
- Não depende de nenhum módulo.
- Não é um módulo: não tem tabelas próprias nem regras de negócio.

### Consequências

- Bom, porque as convenções de banco ficam num lugar só e cada módulo só declara o próprio contexto.
- Bom, porque o `SharedKernel` continua sem dependência de banco.
- Ruim, porque acrescenta um projeto à estrutura do ADR-0047.
- Neutro, porque a auditoria automática (Acesso-3) entra no contexto base, valendo para todos os módulos.

### Confirmação

Testes de arquitetura falham se `Domain` ou `Application` usarem `FiscoProjects.Persistence`, ou se `FiscoProjects.Persistence` usar algum módulo.

## Prós e contras das opções

### A. Projeto compartilhado

- Bom, porque centraliza as convenções e o futuro ponto de auditoria.
- Ruim, porque é mais um projeto.

### B. Configuração repetida em cada módulo

- Bom, porque cada módulo fica independente.
- Ruim, porque as convenções divergem com o tempo e a auditoria teria de ser repetida.

### C. Dentro do SharedKernel

- Bom, porque não cria projeto novo.
- Ruim, porque levaria EF Core para o núcleo usado pelo `Domain`, contrariando o ADR-0047.
