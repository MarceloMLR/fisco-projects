---
status: aceita
date: 2026-10-16
decision-makers: Marcelo Ribeiro
---

# ADR-0061: Convenções da API e do código: REST com OpenAPI, Problem Details, GUID v7 e sem dependências de licença comercial

## Contexto e problema

O backend ASP.NET Core expõe a API usada pelo React (ADR-0049, ADR-0054), num monolito modular (ADR-0047). Convenções puramente de estilo ficam no CLAUDE.md e no `.editorconfig`; este ADR registra as convenções que têm alternativas reais. O código pode ser adquirido por empresas (ADR-0002), o que torna licenças de dependências um risco.

## Fatores de decisão

- Contrato claro e verificável entre backend e frontend.
- Erros e identificadores consistentes.
- Nenhuma armadilha de licença para quem adquirir o código.
- Padrões conhecidos e fáceis de manter com IA.

## Opções consideradas

- A. API REST com contrato OpenAPI, erros Problem Details, identificadores GUID v7 e nenhuma dependência de licença comercial
- B. GraphQL
- C. REST sem contrato formal

## Resultado da decisão

Opção escolhida: **A**.

Convenções:

- API REST em JSON, sob `/api`, com contrato OpenAPI gerado pelo próprio ASP.NET Core; os tipos TypeScript do frontend são gerados a partir dele.
- Erros no padrão Problem Details (RFC 9457), com detalhe por campo nos erros de validação.
- Identificadores internos GUID versão 7; o número do chamado continua sequencial (ADR-0029).
- Sem versionamento público da API na v1; versionamento entra quando houver outros clientes (ADR-0054).
- Casos de uso como classes simples, sem MediatR; mapeamento escrito à mão ou com o Mapperly (Apache 2.0), sem AutoMapper (MediatR e AutoMapper passaram a licença comercial/RPL em julho de 2025).
- Nenhuma dependência com licença comercial ou copyleft forte entra sem ADR.
- C#: nullable habilitado, avisos tratados como erro, `.editorconfig` com `dotnet format` no CI.
- TypeScript em modo estrito, com ESLint e Prettier no CI.
- Código, nomes, comentários e commits em inglês (CLAUDE.md global).

### Consequências

- Bom, porque backend e frontend não se desencontram: o contrato gera os tipos.
- Bom, porque erros e identificadores seguem padrões conhecidos.
- Bom, porque o código não carrega dependências que exijam licença do comprador.
- Ruim, porque exige configurar a geração de tipos e as regras de análise no começo.

### Confirmação

O CI falha se: o contrato OpenAPI gerado divergir dos tipos do frontend; houver avisos de compilação; `dotnet format`, ESLint ou Prettier apontarem problemas; uma dependência com licença não permitida aparecer (verificação de licenças).

## Prós e contras das opções

### A. REST com OpenAPI e convenções

- Bom, porque é o padrão mais conhecido e gera os tipos automaticamente.
- Ruim, porque exige configuração inicial.

### B. GraphQL

- Bom, porque o frontend pede só os dados de que precisa.
- Ruim, porque complica autorização, cache e ferramentas sem ganho para esse porte.

### C. REST sem contrato formal

- Bom, porque é mais rápido no começo.
- Ruim, porque backend e frontend se desencontram sem aviso.
