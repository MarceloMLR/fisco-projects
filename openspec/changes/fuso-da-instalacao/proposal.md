# Proposta: fuso da instalação (Acesso-7)

## Why

As datas e horas ficam em UTC no banco e devem aparecer no fuso da instalação, configurável e com o padrão America/Sao_Paulo (ADR-0013). Hoje as telas usam o fuso do navegador (`web/src/format/dates.ts` diz isso). Quem abre o sistema de outro fuso vê horas diferentes das da empresa, e o filtro de período da Auditoria corta os dias no fuso errado. O fuso também vai servir aos prazos e ao resumo diário que vêm depois.

É a segunda das três partes da antiga Acesso-6, a pedido de Marcelo. Ela parte da Acesso-6 (nome e logo), que cria a tela Configurações gerais.

## What Changes

- **Fuso nas configurações da instalação** (módulo `Administration`):
  - um identificador IANA (por exemplo, `America/Sao_Paulo`), com o padrão America/Sao_Paulo na migração;
  - um fuso que o servidor não conhece, ou que não é IANA, é recusado.
- **API:**
  - `PUT /api/installation` passa a receber `{ name, timeZone }`;
  - novo `GET /api/installation/settings`, para quem tem sessão, com `{ timeZone }`. O fuso não é da marca e não aparece sem sessão;
  - o contrato `IInstallationSettings` passa a dar o fuso aos outros módulos.
- **Tela Configurações gerais:** nova seção Regras gerais, com o campo Fuso horário, como no canvas. O campo tem busca e mostra o nome do fuso em português e o identificador, como "Horário de Brasília (America/Sao_Paulo)".
- **Datas no fuso da instalação, em todas as telas:**
  - o último acesso nos Usuários e em Meu perfil;
  - as datas e horas da Auditoria e dos detalhes;
  - o filtro de período, com os dias começando e terminando à meia-noite do fuso.
  
  A estrutura das telas com sessão só aparece depois de saber o fuso, para nunca mostrar uma hora em outro fuso.
- **Auditoria:** a troca do fuso entra como mudança das configurações, com o campo "Fuso horário".

## Non-goals

- Fuso por pessoa (ADR-0013, "depois").
- Os outros campos de Regras gerais do canvas (expiração da sessão, resumo diário, guarda de notificações e da auditoria, fornecedor sem retorno), que chegam com as changes donas de cada regra.
- Usar o fuso no servidor para prazos e avisos, o que fica com as changes de calendário e de avisos.
- Cor principal (Acesso-8).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `configuracao-da-instalacao`:
  - novo requisito "Fuso da instalação";
  - "Tela Configurações gerais" ganha a seção Regras gerais com o fuso;
  - "Mudança das configurações na auditoria" passa a citar o fuso.

## Impact

- Backend (módulo `Administration`):
  - `TimeZone` em `InstallationSettings`, com a migração `InstallationTimeZone`;
  - `UpdateInstallation` no lugar de `RenameInstallation`;
  - `GetInstallationSettings` e o fuso no contrato `InstallationInfo`;
  - testes do módulo e da API.
- Contrato OpenAPI e tipos do frontend (`npm run api:update`).
- Frontend:
  - `format/dates.ts` com as funções num fuso (`datesIn`) e o gancho `useDates`;
  - `installationSettingsQuery`, e a estrutura esperando o fuso;
  - Auditoria, detalhes, período, Usuários e Meu perfil;
  - o campo Fuso horário nas Configurações gerais;
  - traduções e testes.
- Documentação: `docs/roadmap.md` (Acesso-7), `docs/glossario.md` (fuso horário) e o `CLAUDE.md`, que passa a citar as datas no fuso da instalação.
- ADRs: 0013 (fuso da instalação, padrão America/Sao_Paulo), 0003 (configuração por instalação), 0046 (só o administrador muda), 0039 (mudança auditada).
