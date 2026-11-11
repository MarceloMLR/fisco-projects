## Why

O que a equipe conversa sobre uma tarefa fica fora do sistema: a nota (ADR-0139) é um texto só, de quem edita, sem autor nem data, e o chamado é para pedir atendimento. Tarefas-3, do grupo 16 (Tarefas), a pedido de Marcelo em 10/11/2026, com o plano aprovado por ele; as decisões ficam no ADR-0157.

## What Changes

- A seção "Comentários" no detalhe de cada tarefa (também a resumo e a subtarefa) e de cada marco, do mais antigo para o mais novo, com o autor, quando e o texto, para todos que veem o projeto.
- Comentar com o editor dos chamados (negrito, listas, links e menções às pessoas do projeto), sem anexos nem edição: todos do projeto menos o Leitor, e o administrador, com o projeto em aberto.
- Apagar: quem escreveu apaga o seu, e o administrador e o gerente apagam qualquer um, com o projeto em aberto.
- A quantidade de comentários no cartão do Quadro e na Lista (`commentCount` em cada item do `GET /schedule`).
- Dois avisos novos, na hora: a quem foi mencionado e ao responsável da tarefa, um por pessoa, nunca a quem escreveu, pelo e-mail e pelo Teams conforme a escolha da pessoa e no sino; o pacote do Teams ganha uma versão nova.
- Fora da auditoria, como a conversa dos chamados; o comentário de um item excluído volta com o desfazer; sai com o projeto excluído; na anonimização, as menções dizem o identificador.
- O texto formatado (`RichText`) passa do módulo dos chamados para o `SharedKernel`, para os dois módulos usarem a mesma regra.
- A demonstração ganha comentários no projeto principal, com uma menção.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: os comentários no detalhe do item, quem vê, comenta e apaga, o projeto encerrado e a contagem no Quadro e na Lista.
- `avisos`: os avisos do comentário com menção e do comentário na tarefa do responsável.
- `dados-pessoais`: as menções dos comentários na anonimização.
- `dados-de-demonstracao`: os comentários da demonstração.

## Non-goals

- Editar um comentário, anexar arquivos e reagir a um comentário.
- O comentário na página do Teams, que continua mostrando só o chamado.
- Comentários no Meu trabalho e no resumo diário.

## Impact

- `src/FiscoProjects.SharedKernel`: o `RichText`, vindo do `Tickets`.
- `src/Modules/Projects`: a entidade, a tabela `projects.schedule_comments` e a migração, os casos de uso, as rotas `/api/projects/{id}/schedule/items/{itemId}/comments`, o `commentCount` do cronograma, o `CanComment` das permissões, os avisos no `ProjectNotices`, a exclusão do projeto e o apagador dos dados pessoais.
- `src/Modules/Notifications`: os tipos `TaskMentioned` e `TaskCommented`, a lista dos avisos na hora e os textos do e-mail e do Teams.
- `web/src`: a seção dos comentários, a contagem na Lista e no Quadro, o texto do sino e o contrato da API.
- `web/scripts/demo`: os comentários do plano.
- Testes da API, do domínio e de tela; o ADR-0157, `CLAUDE.md`, `docs/glossario.md` e o roadmap.
