---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0048: Azure SQL como banco, provisionado em produção e gratuito no desenvolvimento

## Contexto e problema

O ideia.md deixou em aberto entre Azure SQL (SQL Server) e PostgreSQL. O EF Core funciona bem com os dois. A empresa de referência já usa o ecossistema Microsoft, e o uso esperado é de 5 a 6 pessoas, não simultâneas, em horário comercial. Qual banco usar e em que modalidade?

## Fatores de decisão

- Custo no desenvolvimento (que dura meses) e na produção.
- Aderência às empresas Microsoft, público provável do produto.
- Testes de integração com banco real.
- Disponibilidade: o sistema não pode sair do ar por limite de uso.

## Opções consideradas

- A. PostgreSQL (Azure Database for PostgreSQL Flexible Server)
- B. Azure SQL / SQL Server
- C. Suportar os dois

Modalidade do Azure SQL em produção:

- B1. Serverless (cobrança por uso, com pausa automática)
- B2. Provisionado (preço fixo por mês)

## Resultado da decisão

Opção escolhida: **B. Azure SQL**, com **B2. provisionado em produção** e a oferta gratuita serverless só para desenvolvimento e demonstração.

Regras:

- Produção: plano provisionado. Padrão S0 (10 DTUs, até 250 GB); é possível começar no Basic (5 DTUs, até 2 GB) e subir para o S0 quando o banco se aproximar de 2 GB, sem migração.
- Desenvolvimento e demonstração: oferta gratuita serverless (100 mil segundos de vCore e 32 GB por mês, por banco), com a opção "pausar até o mês seguinte" ligada, para nunca gerar cobrança.
- Serverless pago não é usado em produção: os processos em segundo plano mantêm o banco acordado e impedem a pausa automática, tornando o custo maior que o do provisionado.
- Testes de integração usam SQL Server em contêiner (Testcontainers).
- O acesso a dados usa o provedor do EF Core para SQL Server.

### Consequências

- Bom, porque o desenvolvimento e as demonstrações não têm custo de banco.
- Bom, porque a produção tem custo fixo e previsível (cerca de R$ 93 por mês no S0 ou R$ 31 no Basic, em Brazil South, em outubro de 2026).
- Bom, porque é familiar às empresas Microsoft.
- Ruim, porque a imagem de contêiner do SQL Server é mais pesada para os testes.
- Ruim, porque, fora do Azure, a empresa compradora precisa de licença de SQL Server (a edição Express, gratuita, tem limite de 10 GB por banco).
- Neutro, porque a oferta gratuita pausa o banco quando não há uso, e o primeiro acesso depois da pausa é mais lento.

### Confirmação

O ambiente de desenvolvimento no Azure usa a oferta gratuita com pausa ao atingir o limite; o de produção usa plano provisionado; os testes de integração rodam contra SQL Server em contêiner no CI.

## Prós e contras das opções

### A. PostgreSQL

- Bom, porque é gratuito em qualquer lugar e leve em contêiner.
- Ruim, porque no Azure é gratuito só por 12 meses em conta nova e custa mais em Brazil South.

### B. Azure SQL

- Bom, porque tem oferta gratuita permanente para desenvolvimento e custo baixo provisionado.
- Ruim, porque o contêiner de testes é mais pesado e há licença fora do Azure.

### C. Os dois

- Bom, porque atende qualquer preferência.
- Ruim, porque dobra testes e cuidados com diferenças entre bancos.

### B1. Serverless em produção

- Bom, porque cobra só pelo uso.
- Ruim, porque, com uso em horário comercial e processos em segundo plano, custaria de R$ 375 a mais de R$ 1.700 por mês.

### B2. Provisionado em produção

- Bom, porque tem custo fixo e baixo para o uso esperado.
- Ruim, porque cobra mesmo sem uso.

## Mais informações

- Estimativa completa de custos: `claude/custos-estimados.md`.
- Oferta gratuita do Azure SQL: https://learn.microsoft.com/azure/azure-sql/database/free-offer
