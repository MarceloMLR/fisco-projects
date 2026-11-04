# Tasks

## 1. Marca de Sair

- [ ] 1.1 `web/src/session/signedOut.ts` (gravar, ler e apagar a marca, com cada acesso protegido); o `signOut` do `SessionProvider` grava, e a sessão aberta apaga; conferido pelo teste "Sair lembrado até a próxima entrada".

## 2. Página de passagem

- [ ] 2.1 `teamsOpenPath` em `teamsPage.ts` e a `TeamsOpenPage` em `/teams/open`, fora da estrutura, com as decisões do design; conferido pelos testes "Link do Teams com sessão", "Link do Teams sem sessão", "Link do Teams sem e-mail lembrado", "Link do Teams depois de Sair", "Link do Teams sem a entrada pelo Entra", "Recusa da Microsoft pelo link do Teams" e "Link do Teams para outro site".
- [ ] 2.2 "Abrir no FiscoProjects" e os dois "Responder no FiscoProjects" da página do Teams pela passagem; conferido pelos testes da página do Teams que já existem, atualizados para o endereço novo.

## 3. Documentação e conferência

- [ ] 3.1 ADR-0153 aceito, `CLAUDE.md` (Teams na tela) e roadmap.
- [ ] 3.2 `npm test`, `npm run lint` e `npm run typecheck` em `web/` sem falhas, e `openspec validate --all --strict`.
