## 1. Decisão e roadmap

- [ ] 1.1 ADR-0152 aceito, os ADR-0054 e 0149 apontando para ele, o índice dos ADRs e a Avisos-6 no roadmap; `openspec validate --all --strict` sem erros

## 2. Entrada pelo Teams

- [ ] 2.1 A regra do convidado num lugar só (`EntraIdentity`), usada pela entrada pelo Entra e pelo `SignInFromTeams`; os testes da entrada pelo Entra continuam passando
- [ ] 2.2 O esquema `teams` (`TeamsSignIn`, JWT Bearer, com o escopo e os clientes do Teams), o manipulador que nunca autentica sem a entrada pelo Entra, a política `TeamsAccess.Policy` e o grupo `/api/teams` no host; o `FakeEntra` emite o token do Teams; testes "Token do Teams aceito", "Token de outro aplicativo recusado", "Convidado recusado pelo Teams", "Quem nunca entrou recusado pelo Teams", "Desativado recusado pelo Teams" e "Teams sem a entrada pelo Entra"
- [ ] 2.3 O token do Teams só sob `/api/teams` e o cookie fora delas; testes "Token do Teams fora das rotas do Teams", "Sessão nas rotas do Teams", "Token do Teams fora dos registros" (no `MonitoringTests`) e os da "Sessão protegida", que continuam passando; teste de arquitetura de que as rotas do grupo do Teams só leem

## 3. Chamado para o Teams

- [ ] 3.1 `GET /api/teams/tickets/{número}` pelo `GetTicket` e o `TeamsTicketView`, com `no-store`; testes "Resumo do chamado no Teams", "Últimas interações do chamado", "Descrição como a primeira interação", "Resumo do chamado sem e-mails", "Leitor lê o chamado no Teams", "Administrador lê o chamado no Teams", "Fora do projeto não lê o chamado no Teams" e "Chamado que não existe no Teams"
- [ ] 3.2 `npm run api:update`; o contrato e os tipos gerados de novo, com o esquema `Bearer` nas rotas do Teams; os testes do contrato passam

## 4. Página do Teams

- [ ] 4.1 `@microsoft/teams-js` com a versão fixada (de pelo menos duas semanas), o `teamsHost.ts` como o único import dela (regra do lint, com teste), o limite de 3 segundos e o tema do Teams; testes do `teamsHost` com a biblioteca simulada; o `frontend.sh` passa (licenças e vulnerabilidades)
- [ ] 4.2 O `teamsTicketQuery` com o token só no cabeçalho e um novo pedido no 401; a `TeamsPage` com os estados; testes "Abrir o item no navegador", "Página sem item", "Endereço de fora recusado", "Aviso de outro item no Teams", "Página do Teams fora do Teams", "Token só na memória da página", "Token recusado uma vez" e "Token recusado de novo"
- [ ] 4.3 O `TeamsTicket` como o canvas "Chamado no Teams", reaproveitando as peças da `TicketConversation` (exportadas, com os anexos só pelo nome), e o `TeamsNoAccess`; os textos em `teamsPage.ticket.*`; testes "Chamado do aviso no Teams", "Menção no chamado do Teams", "Interações anteriores no Teams", "Responder no FiscoProjects", "Chamado no Teams do celular", "Tema escuro do Teams no chamado", "Chamado de outro projeto no Teams" e "Falha ao ler o chamado no Teams"; a página do chamado continua passando nos testes dela
- [ ] 4.4 Conferência no navegador da página do Teams com o chamado, o chamado que a pessoa não vê e a volta à página de hoje, no computador e no celular, nos temas claro e escuro, contra o canvas

## 5. Aplicativo do Entra

- [ ] 5.1 O módulo `entra` com o `site_host`, o identificador, o escopo `access_as_user`, os tokens v2, os dois clientes do Teams pré-autorizados e o `acct` no token de acesso; teste "Aplicativo do Entra para o Teams" no `InfrastructureTests`; `bash scripts/ci/infra.sh` sem erros
- [ ] 5.2 `docs/instalacao.md` (o que muda na versão, a isenção dos identificadores quando o tenant a exige) e o `CLAUDE.md`

## Workflow follow-up

- `terraform apply` do módulo `entra` no `dev`, só com a aprovação de Marcelo, e o teste no Teams do computador com um aviso de chamado.
- Archive na mesma branch depois da aprovação do PR, com a Avisos-6 "Concluída" no roadmap.
