## Why

Quem passa a ser responsável por uma tarefa do cronograma não fica sabendo: hoje só a mudança das datas da tarefa entra no resumo diário. No chamado, o atribuído recebe o aviso na hora, por e-mail, pelo Teams e no sino. Tarefas-2, do grupo 16 (Tarefas), a pedido de Marcelo em 10/11/2026, com o plano aprovado por ele; as decisões ficam no ADR-0156.

## What Changes

- Um aviso novo, "tarefa atribuída", na hora: a quem passa a ser responsável por uma tarefa, ao incluir a tarefa com responsável ou ao trocar o responsável, salvo quando é a própria pessoa quem faz.
- Pelos canais de sempre: o e-mail e o Teams conforme a escolha da pessoa em Meu perfil, e o sino sempre. O link abre a tarefa no cronograma.
- Sem aviso: na criação do projeto por modelo ou pelo MS Project, no desfazer e no refazer, quando o responsável é o cliente e para quem deixou de ser responsável.
- Um tipo novo de aviso no pacote do aplicativo do Teams, que ganha uma versão nova: o administrador baixa o pacote e o sobe de novo no Teams.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `avisos`: o aviso de tarefa atribuída, quem recebe e quando não sai.

## Non-goals

- Aviso a quem deixou de ser responsável e aviso das tarefas criadas por modelo ou pelo MS Project.
- A tarefa só para ler na página do Teams, como o chamado.

## Impact

- `src/Modules`: o tipo `TaskAssigned` no `Notifications.Contracts`, a lista dos avisos na hora, os textos do e-mail e do Teams, o `ProjectNotices` e a edição do cronograma no `Projects`.
- `web/src`: o texto do aviso no sino e o contrato da API (o tipo novo).
- Testes da API (e-mail, sino, Teams e os casos sem aviso) e do texto do sino.
- Documentação: o ADR-0156, `CLAUDE.md`, `docs/glossario.md` e o roadmap.
