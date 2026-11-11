## Context

O cronograma é um agregado `Schedule` do módulo `Projects`, com os itens (`ScheduleItem`) recriados com o mesmo id pelo desfazer (ADR-0091). O que cada pessoa faz num projeto sai de `ProjectPermissions` (ADR-0043). A conversa dos chamados já tem o texto formatado, validado no domínio pelo `RichText` do `Tickets` (só parágrafos, listas, negrito, links e menções), fora da auditoria e com as menções renomeadas na anonimização pelo `TicketsPersonalData`. Os avisos saem pelo `INoticeBoard` depois de gravar (ADR-0038), e os tipos de `EmailChannel.Immediate` vão por e-mail e pelo Teams conforme a escolha da pessoa e sempre para o sino (ADR-0151); o pacote do Teams declara um tipo de atividade por tipo na hora, e a versão sobe quando o conteúdo muda.

## Goals / Non-Goals

**Goals:** comentar tarefas e marcos com menções, apagar, mostrar a quantidade e avisar na hora quem foi mencionado e o responsável.

**Non-Goals:** editar comentários, anexos, reações, o comentário na página do Teams, no Meu trabalho ou no resumo diário.

## Decisions

- **O texto num lugar só.** O `RichText` vai para o `SharedKernel` (não depende de nada externo, só do `System.Text.Json`), usado pelo `Tickets` e pelo `Projects`; os testes vão para o `SharedKernel.Tests`.
- **A entidade.** `ScheduleComment` (`projects.schedule_comments`): projeto, item, autor, o JSON e o texto simples do `RichText`, e quando; o índice pelo projeto, o item e o quando. Sem chave para o item, porque a exclusão apaga a linha do item e o desfazer a recria com o mesmo id: o comentário fica guardado e volta com o item. Fora da auditoria (`ExcludeFromAudit`), como as mensagens dos chamados.
- **As permissões.** `ProjectPermissions` ganha `CanComment` (o administrador, o gerente, o líder técnico, o colaborador e o desenvolvedor) e `MayRemoveComment(isAuthor)`: quem edita o cronograma (o administrador e o gerente) apaga qualquer um, quem comenta apaga o seu. O projeto encerrado não recebe comentários nem os apaga.
- **As menções.** Só as pessoas do projeto, em qualquer papel (`project.Members`); outra dá 400 com o tipo `comment-mention-refused`, e o texto inválido dá 400 no campo `body`.
- **As rotas.** `GET /api/projects/{id}/schedule/items/{itemId}/comments` (`ListScheduleComments`: os comentários, cada um com o autor pelo nome atual e o `canRemove`, e o `canComment`), `POST` no mesmo endereço (`AddScheduleComment`, `{ body }`, 201 com o id) e `DELETE .../comments/{commentId}` (`RemoveScheduleComment`, 204). O item que não existe no cronograma dá 404, como o projeto que a pessoa não vê. O `GET /schedule` traz o `commentCount` de cada item, numa consulta agrupada.
- **Os avisos.** `NoticeKind.TaskMentioned` (22) e `TaskCommented` (23) em `EmailChannel.Immediate`, montados pelo `ProjectNotices.CommentedAsync` depois de gravar: primeiro a menção a quem foi mencionado, depois o comentário ao responsável que é uma pessoa e não foi avisado; quem escreveu nunca. O caminho é o do item no cronograma (`?item=`), e os valores, o projeto e o item (também como `title`, a prévia do Teams).
- **A guarda.** A exclusão do projeto apaga os comentários com o cronograma (`ProjectDeletionStore`), sem uma contagem própria, como o histórico do desfazer; o `ProjectsPersonalData` renomeia as menções da pessoa anonimizada, achando os comentários pelo id no JSON, como o `TicketsPersonalData`.
- **A tela.** A seção "Comentários" (`ScheduleComments.tsx`) fica no fim do detalhe, de quem edita e de quem só vê: a lista com o avatar (`CardAvatar`), o nome, quando e o texto (`RichText`), o "Apagar" com confirmação e o editor dos chamados (`RichTextEditor`, sob demanda), com as pessoas do projeto pelo `projectMembersQuery`, menos quem escreve. A quantidade fica na Lista, depois do nome e da nota, e no pé do cartão do Quadro (`CommentCount`, ícone e texto).
- **A demonstração.** `comments` no plano, cada um com o item, quem escreve, o texto e a menção opcional no começo, escritos depois do cronograma.

## Risks / Trade-offs

- [O pacote do Teams muda de novo] → a versão sobe sozinha e o guia já diz como subir o pacote; sem ele, o aviso no Teams fica Falhou, e o e-mail e o sino seguem.
- [O comentário de um item excluído fica guardado sem aparecer] → é o que deixa o desfazer trazê-lo de volta; ele sai com o projeto e tem as menções renomeadas na anonimização.
- [O comentário apagado não deixa rastro] → como a conversa dos chamados, fora da auditoria, decidido no ADR-0157.
