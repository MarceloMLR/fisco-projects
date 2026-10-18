---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0003: Uma instalação por empresa (single-tenant), sem nada da empresa fixo no código

## Contexto e problema

O FiscoProjects é um projeto pessoal (ADR-0002) que pode ter o código adquirido por uma empresa no futuro. É preciso decidir se cada instalação atende uma única empresa ou se uma instalação atende várias empresas (SaaS multiempresa). A escolha afeta o banco, o login, as consultas e o esforço antes do primeiro usuário.

## Fatores de decisão

- Simplicidade para um desenvolvedor sozinho, no tempo livre.
- Isolamento dos dados de cada empresa.
- Aderência ao cenário provável de venda: uma empresa adquire o código e o roda.
- Não fechar a porta para uma versão SaaS no futuro.

## Opções consideradas

- A. Uma instalação por empresa (single-tenant)
- B. SaaS multiempresa desde o início
- C. Uma instalação por empresa, com o campo "empresa" já em todas as tabelas

## Resultado da decisão

Opção escolhida: **A. Uma instalação por empresa (single-tenant)**, porque é a mais simples, isola os dados por natureza e corresponde ao cenário de uma empresa adquirir o código.

Regra: nada específico de uma empresa fica fixo no código. Nome e logo exibidos, fornecedores, pastas padrão, modelos de projeto e prazos por prioridade são configuração ou dados cadastrados.

### Consequências

- Bom, porque não há filtro por empresa em cada consulta nem risco de vazamento de dados entre clientes.
- Bom, porque o registro de aplicativo no Entra ID é de um único tenant por instalação, sem consentimento de vários administradores.
- Bom, porque a regra de configuração deixa o sistema reutilizável por qualquer empresa.
- Ruim, porque virar SaaS no futuro exige acrescentar a separação entre empresas, por meio de um novo ADR.
- Ruim, porque cada empresa cliente exige uma instalação própria para manter.

### Confirmação

Revisão de código e de cada spec: nenhum nome de empresa, cliente, fornecedor, pasta padrão ou prazo fixo no código; esses itens vêm de configuração ou de dados.

## Prós e contras das opções

### A. Uma instalação por empresa (single-tenant)

- Bom, porque é a opção mais simples de construir e testar.
- Bom, porque os dados de cada empresa ficam separados por instalação.
- Ruim, porque exige trabalho extra para virar SaaS depois.

### B. SaaS multiempresa desde o início

- Bom, porque uma instalação atende novas empresas sem novo deploy.
- Ruim, porque toda consulta precisa filtrar por empresa, e um erro nesse filtro vaza dados entre clientes.
- Ruim, porque exige cadastro de empresas, consentimento de cada tenant Microsoft e cobrança antes do primeiro usuário.

### C. Single-tenant com o campo "empresa" já em todas as tabelas

- Bom, porque facilita uma migração futura para SaaS.
- Ruim, porque traz o custo agora (filtro global, testes de isolamento) sem usar o benefício.
- Ruim, porque o benefício pode nunca ser necessário.
