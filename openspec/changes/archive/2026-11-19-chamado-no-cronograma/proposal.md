## Why

O que mais preocupa o gerente e o cliente num projeto de implantação é o que pode atrasar o go-live, e hoje o GerenWare guarda isso em lugares separados: o cronograma sabe as dependências e o caminho crítico, e o chamado sabe o prazo e o registro no fornecedor, mas nada diz "este chamado ameaça o marco Go-live". O gerente descobre olhando as setas e escreve à mão no status report. Status-1, do grupo 18 (Status do projeto), a pedido de Marcelo em 19/11/2026, com a decisão proposta no ADR-0164; é a base dos riscos do relatório de status (ADR-0165).

## What Changes

- Quem atende o chamado (o administrador, os gestores e quem atende o time, como o registro no fornecedor) liga o chamado a até 10 tarefas, também a resumo e a subtarefa, ou marcos do cronograma do projeto dele, e desliga, com o chamado não fechado.
- A partir dos itens ligados, o servidor segue as dependências, de qualquer tipo, até os marcos ainda não atingidos que vêm depois, e diz se algum item ligado está no caminho crítico. Nada muda nas datas nem nos prazos do chamado.
- Só o chamado em aberto (nem resolvido nem fechado) ameaça o cronograma; o resolvido e o fechado guardam a ligação.
- No chamado, a seção "No cronograma" do painel: os itens ligados, com as datas e o atraso, os marcos ameaçados e o aviso do caminho crítico, com "Alterar" para quem atende.
- No cronograma, a marca do chamado ao lado do nome, na Lista e no cartão do Quadro, com a quantidade de chamados em aberto ligados ao item; no detalhe do item, a seção "Chamados", e, no detalhe do marco, também os chamados que o ameaçam pelas dependências.
- Na Visão geral, em "Precisa de atenção", os chamados em aberto que ameaçam um marco não atingido, com o marco e o prazo do chamado.
- Ligar e desligar entram na conversa como evento e na auditoria, com os itens pelo nome atual; a ligação a um item excluído volta com o desfazer, e as ligações saem com o projeto excluído.
- `PUT /api/tickets/{number}/impacts` (`ChangeTicketImpacts`), e o chamado, o cronograma e a visão geral passam a trazer o que é preciso para mostrar a ligação.
- A demonstração liga um chamado do projeto principal, registrado no fornecedor, a uma tarefa que leva ao go-live.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `chamados`: ligar o chamado ao cronograma, o que depende dele, a seção "No cronograma" do painel, o evento na conversa e a auditoria.
- `cronograma`: os chamados ligados a cada item, a marca na Lista e no Quadro, a seção "Chamados" do detalhe e os chamados que ameaçam cada marco.
- `visao-geral-do-projeto`: os chamados que ameaçam marcos em "Precisa de atenção".
- `dados-de-demonstracao`: o chamado da demonstração ligado ao cronograma.

## Non-goals

- Mudar datas do cronograma ou prazos do chamado por causa da ligação (ADR-0051, ADR-0025).
- A projeção do atraso das tarefas, que é a Status-2.
- Ligar o chamado a uma fase, a um item de outro projeto ou já na abertura do chamado.
- Avisos por e-mail, Teams ou sino quando um chamado passa a ameaçar um marco.
- Mudar quem vê os chamados ou o cronograma (ADR-0043, ADR-0044).

## Impact

- `src/Modules/Tickets`: a ligação no domínio (`TicketImpact`, tabela `tickets.ticket_impacts` e migração), a regra de quem liga no `TicketAccess`, o caso de uso `ChangeTicketImpacts`, o evento `ImpactsChanged`, a rota, o chamado com a seção "No cronograma", a implementação nova do `IProjectTickets` com os chamados em aberto ligados e a exclusão do projeto (`TicketProjectDeletion`).
- `src/Modules/Projects`: o contrato `IScheduleImpacts` no `Projects.Contracts` (os itens ligados com as datas e os marcos que dependem deles), implementado pelo cronograma, o método novo no `IProjectTickets`, o `GET /schedule` com os chamados de cada item e as ameaças de cada marco, e a visão geral com as ameaças.
- `web/src`: a seção do painel e o diálogo de ligar (`web/src/tickets`), a marca e a seção no cronograma (`web/src/projects`), a linha em "Precisa de atenção", o evento na conversa e na página do Teams, a auditoria, os textos e o contrato da API (`npm run api:update`).
- `web/scripts/demo`: a ligação no plano.
- Testes da API, do domínio e de tela; o ADR-0164 aceito, `CLAUDE.md`, `docs/glossario.md`, `docs/discovery.md` e o roadmap.
