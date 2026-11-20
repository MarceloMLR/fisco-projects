## 1. Decisão e roadmap

- [x] 1.1 ADR-0164 aceito (depois da aprovação de Marcelo) e Status-1 "Em andamento" no roadmap, conferidos no `docs/adr/README.md` e no `docs/roadmap.md`

## 2. O que depende, no domínio do cronograma

- [x] 2.1 `ScheduleImpacts` no domínio do `Projects`: a troca da tarefa-resumo pelas subtarefas, os sucessores pelas dependências de qualquer tipo, os marcos não atingidos na ordem do dia e o caminho crítico, com os testes do domínio (os três tipos de dependência, a tarefa-resumo, o marco atingido fora, o item no caminho crítico e o item que não existe)
- [x] 2.2 Contrato `IScheduleImpacts` no `Projects.Contracts`, implementado pelo `Projects` com o cronograma lido sem rastreio, com o teste de integração dos itens, dos marcos e do atraso

## 3. A ligação no chamado

- [x] 3.1 `TicketImpact`, a tabela `tickets.ticket_impacts` na auditoria (o chamado como rótulo, o item como referência e o projeto como contexto) e a migração do `Tickets`, com `dotnet build -warnaserror` limpo
- [x] 3.2 `TicketAccess.MayLinkToSchedule`, com a matriz de quem atende por papel e por time coberta no teste de permissões
- [x] 3.3 `ChangeTicketImpacts`, a rota `PUT /api/tickets/{number}/impacts` e o evento `ImpactsChanged`, com os testes da API dos cenários de "Chamado no cronograma", "Quem liga o chamado ao cronograma" e "Itens que podem ser ligados"
- [x] 3.4 A seção `schedule` no `GET /api/tickets/{number}`, pelo `IScheduleImpacts`, com os testes dos cenários "O que depende do chamado" (marcos pelas dependências, marco atingido, caminho crítico, tarefa-resumo, item excluído e desfeito)
- [x] 3.5 As ligações na exclusão do projeto (`TicketProjectDeletion`) e na auditoria pelo nome atual, com os testes dos cenários "Ligação na conversa e na auditoria"

## 4. Chamados no cronograma e na Visão geral

- [x] 4.1 `IProjectTickets.ImpactsAsync` implementado pelo `Tickets` (só os chamados em aberto, com o prazo do `TicketDueNow`), com o teste do resolvido e do fechado fora
- [x] 4.2 `tickets` em cada item e `threats` em cada marco não atingido no `GET /schedule`, e o Minhas tarefas com o mapa vazio, com os testes da API dos cenários "Chamados do item"
- [x] 4.3 `threats` em "Precisa de atenção" da visão geral, com o chamado uma vez só, com os testes da API dos cenários de "Precisa de atenção"

## 5. Tela

- [x] 5.1 Seção "No cronograma" no painel do chamado e o diálogo de ligar, com o `MultiSelect` até 10 itens, nos temas claro e escuro, no computador e no celular, com os testes de tela dos cenários de "Seção No cronograma" e "Ligar pela tela"
- [x] 5.2 Marca `TicketCount` na Lista e no cartão do Quadro e a seção "Chamados" no detalhe da tarefa e do marco, com "Ameaçado por", com os testes de tela dos cenários "Chamados do item pela tela"
- [x] 5.3 Linha da ameaça em "Precisa de atenção", o evento na conversa e na página do Teams e a ligação na Auditoria (`describeAuditEntry`), com os testes de tela
- [x] 5.4 Contrato da API atualizado (`npm run api:update`) e `npm run lint`, `npm run typecheck` e `npm test` limpos

## 6. Demonstração e documentação

- [x] 6.1 Passo `impacts` no plano da demonstração, com o chamado da rejeição 539 ligado a Executar testes, e os testes de `web/scripts/demo`
- [x] 6.2 `CLAUDE.md`, `docs/glossario.md` e `docs/discovery.md` com a ligação do chamado ao cronograma, e `openspec validate --all --strict` limpo
- [x] 6.3 Verificação completa: `dotnet test GerenWare.slnx`, `dotnet format GerenWare.slnx --verify-no-changes` e a demonstração carregada mostrando o chamado no cronograma, na Lista e na Visão geral

## Workflow follow-up

- PR com a aprovação de Marcelo, archive na mesma branch, roadmap "Concluída" e o merge por Marcelo.
