# Tasks

## 1. Backend

- [x] 1.1 No módulo `Administration`:
  - `TimeZone` em `InstallationSettings`, com `ChangeTimeZone` (IANA conhecido pelo servidor) e a migração `InstallationTimeZone`;
  - `UpdateInstallation` no lugar de `RenameInstallation`;
  - `GetGeneralRules` e o fuso em `InstallationInfo`.

  Testar o fuso padrão, a troca, o fuso desconhecido, o id do Windows e o formulário recusado sem mudar nada. Verificar com `dotnet test`.
- [x] 1.2 Na API:
  - `PUT /api/installation` com `{ name, timeZone }`;
  - `GET /api/installation/rules` com sessão.

  Testar "Fuso inválido", "Fuso só com sessão", "Troca do fuso na auditoria" e o fuso na matriz de permissões. Atualizar o contrato com `npm run api:update` e verificar com `dotnet test`.

## 2. Frontend

- [x] 2.1 Reescrever `format/dates.ts` com `datesIn(timeZone)` e criar `generalRulesQuery` e `useDates`. Também:
  - a `AppShell` esperando o fuso;
  - Auditoria, detalhes, período, Usuários e Meu perfil no fuso da instalação.

  Testar as funções em America/Sao_Paulo e em outro fuso (meia-noite, dia anterior, "hoje" e "ontem"), "Horas no fuso da instalação" e "Período no fuso da instalação". Verificar com `npm test`.
- [x] 2.2 Criar a seção Regras gerais com o campo Fuso horário (busca, nome e identificador) nas Configurações gerais, e o campo "Fuso horário" na Auditoria. Testar "Fuso padrão", "Trocar o fuso", "Descartar alterações" (com o fuso) e o fuso recusado pela API. Verificar com `npm test` e `npm run lint`.

## 3. Verificação final

- [x] 3.1 Conferir no navegador, com o build de produção, num navegador em outro fuso:
  - as Configurações gerais com o fuso, nos temas claro e escuro;
  - as horas da Auditoria e dos Usuários no fuso da instalação;
  - a troca do fuso.

  Mandar as capturas para Marcelo.
- [x] 3.2 Documentação e verificações:
  - atualizar o glossário (fuso horário), o `CLAUDE.md` e o roadmap;
  - rodar todos os scripts de `scripts/ci` sem erro;
  - abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions.

## Workflow follow-up

- Marcar a Acesso-7 como "PR aberto" no roadmap.
- Depois do merge da Acesso-6 e da aprovação de Marcelo: `/opsx:archive` na mesma branch, roadmap como "Concluída" e merge, antes da Acesso-8.
