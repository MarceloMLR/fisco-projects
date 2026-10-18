---
status: aceita
date: 2026-10-18
decision-makers: Marcelo Ribeiro
---

# ADR-0076: Frontend com Vite e tipos da API gerados pelo openapi-typescript

## Contexto e problema

O frontend é React com TypeScript (ADR-0049), servido pelo host da API (ADR-0047), testado com Vitest e Testing Library (ADR-0056), e os tipos TypeScript da API devem ser gerados do contrato OpenAPI (ADR-0061). Faltam duas escolhas de ferramenta: com o que o frontend é desenvolvido e empacotado, e com o que os tipos são gerados do contrato.

## Fatores de decisão

- Uma aplicação de página única servida pelo ASP.NET Core, sem renderização no servidor.
- Funcionar com o Vitest, já escolhido para os testes.
- Tipos sempre iguais ao contrato, sem código gerado que esconda as chamadas.
- Pouco peso no navegador e licença permissiva.

## Opções consideradas

Build:

- A. Vite
- B. Rsbuild
- C. Next.js

Tipos da API:

- D. openapi-typescript (só tipos; cliente opcional com o openapi-fetch, que usa esses tipos)
- E. Orval (gera funções de chamada e hooks)
- F. NSwag ou Kiota (geradores de cliente completo)

## Resultado da decisão

Opções escolhidas: **A. Vite** e **D. openapi-typescript**.

Regras:

- O frontend fica em `web/`, com Vite; o build vai para a pasta de arquivos estáticos do host da API.
- No desenvolvimento, o servidor do Vite repassa `/api`, `/health` e `/openapi` para a API, para o navegador ver um endereço só, como na publicação (ADR-0054).
- O contrato OpenAPI fica versionado em `web/src/api/openapi.json`, gerado pelo build do backend quando pedido, e os tipos ficam em `web/src/api/schema.ts`, gerados pelo openapi-typescript. Nenhum dos dois arquivos é editado à mão.
- O cliente HTTP (openapi-fetch ou outro) é decidido com a primeira chamada à API.

### Consequências

- Bom, porque o Vite é o padrão das aplicações React de página única e é a base do Vitest.
- Bom, porque o openapi-typescript gera só tipos, sem código no navegador, e o contrato versionado mostra no PR qualquer mudança da API.
- Ruim, porque atualizar o contrato é um passo a mais quando a API muda; os testes reprovam se ele for esquecido.
- Neutro, porque o openapi-typescript ainda exige TypeScript 5, o que fixa o TypeScript do frontend nessa versão por enquanto.

### Confirmação

O `web/` builda com o Vite; os testes do backend reprovam quando o contrato versionado difere do publicado pela API; os testes do frontend reprovam quando os tipos diferem dos gerados a partir do contrato.

## Prós e contras das opções

### A. Vite

- Bom, porque é rápido, simples de configurar e usado pelo Vitest.
- Ruim, porque é mais uma ferramenta de build além do .NET.

### B. Rsbuild

- Bom, porque é rápido e compatível com o ecossistema do webpack.
- Ruim, porque o Vitest continuaria precisando do Vite, e seriam duas configurações.

### C. Next.js

- Bom, porque traz rotas e renderização no servidor.
- Ruim, porque exige um servidor Node, contra o host único do ADR-0047.

### D. openapi-typescript

- Bom, porque gera só tipos, leves e fiéis ao contrato.
- Ruim, porque o cliente HTTP fica por conta do projeto.

### E. Orval

- Bom, porque gera as funções de chamada e os hooks de dados.
- Ruim, porque amarra o projeto a uma biblioteca de dados antes da hora e gera mais código para revisar.

### F. NSwag ou Kiota

- Bom, porque geram clientes completos, conhecidos no mundo .NET.
- Ruim, porque os clientes gerados são pesados para o navegador e menos idiomáticos em React.
