## Context

Os módulos avisam pelo `INoticeBoard` depois de gravar (ADR-0038); o `DeliverNotices` tira quem fez, quem está desativado e quem não vê o projeto, e os canais entregam: o e-mail na hora para os tipos de `EmailChannel.Immediate` (os outros vão para o resumo), o sino sempre e o Teams para os tipos na hora, pelo canal que cada pessoa ligou (ADR-0151). O pacote do Teams declara um tipo de atividade por tipo na hora (`TeamsTexts.ActivityTypes`), e a versão dele sobe quando o conteúdo muda. No cronograma, toda mudança de quem edita passa pelo `ScheduleEditing.RunAsync`, que compara as fotos do agregado antes e depois e já avisa os donos das tarefas com datas mudadas (`ProjectNotices.DatesChangedAsync`); o desfazer e o refazer passam pelo `StepAsync`.

## Goals / Non-Goals

**Goals:** avisar na hora quem passa a ser responsável por uma tarefa, pelos canais e regras de sempre.

**Non-Goals:** avisar quem deixou de ser responsável, as tarefas criadas com o projeto e o desfazer.

## Decisions

- **O tipo.** `NoticeKind.TaskAssigned` (21) no `Notifications.Contracts`, em `EmailChannel.Immediate`; os valores são o projeto, o nome da tarefa (como `item` e como `title`, que o Teams usa na prévia) e o fim; o caminho é `/projects/{id}/schedule?item={itemId}`, que abre o detalhe da tarefa.
- **Onde se descobre.** Na foto antes e depois da mudança, no `RunAsync`: cada tarefa com uma pessoa como responsável que é nova ou tinha outro responsável (ou o cliente, ou ninguém) gera um aviso só para o novo responsável (`ProjectNotices.AssignedAsync`). Assim, incluir e editar a tarefa avisam, e qualquer caso de uso novo que mude o responsável também. O `AddScheduleItem` passa a receber o `ProjectNotices`.
- **Sem aviso.** O `StepAsync` (desfazer e refazer) só avisa as datas; a criação do projeto por modelo ou pelo MS Project não passa pelo `RunAsync`; o cliente não é pessoa, e quem sai do responsável não está na regra.
- **Textos.** O e-mail em `notices.taskAssigned` e o Teams em `teams.activities.taskAssigned` do `pt-BR.json` dos avisos; o sino em `notifications.kinds.taskAssigned` das telas.

## Risks / Trade-offs

- [O pacote do Teams muda] → a versão sobe sozinha, e o passo de baixar e subir o pacote já está no guia de instalação; sem ele, o Teams recusa o tipo novo e o aviso fica Falhou, sem afetar o e-mail e o sino.
- [Incluir várias tarefas para a mesma pessoa manda vários e-mails] → um por tarefa, como um chamado por chamado; quem prefere menos e-mails desliga o canal em Meu perfil e fica com o sino.
