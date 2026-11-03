## Context

Os avisos que pedem ação (`EmailChannel.Immediate`, ADR-0037) saem pelos canais do `Notifications`: o `EmailChannel` põe o e-mail na fila, o `TeamsChannel` põe o aviso na fila do Teams e o `BellChannel` grava a notificação do sino. Por onde cada aviso vai sai do `NoticeRouting.RouteOf`, pela escolha da pessoa (`ImmediateNoticeChannel`: `Both`, `Email` ou `Teams`, em `notifications.notice_preferences`, `Both` para quem não tem linha) e pelo Teams disponível para ela. Quem escolheu só o Teams recebe o e-mail quando o aviso não chega pelo Teams (`TeamsNotice.AlsoByEmail`, `NeedsEmail`, `MarkEmailedInstead`, no `TeamsDispatcher`). A preferência fica fora da auditoria (`ExcludeFromAudit`), como o resumo diário (`User.ReceivesDailySummary`, no `Users`).

## Goals / Non-Goals

**Goals:**

- E-mail e Teams ligados e desligados por cada pessoa, independentes, com o e-mail ligado e o Teams desligado de início.
- Cada canal ligado recebe; nenhum canal recebe sem estar ligado.
- As três escolhas do cartão Avisos na auditoria.

**Non-Goals:**

- Escolha por tipo de aviso, desligar o sino ou mudar o que vai para o resumo diário.
- Conferir em Meu perfil se o aplicativo do Teams está instalado para a pessoa.

## Decisions

### 1. A preferência com dois campos

A `NoticePreference` troca o `Channel` por `ByEmail` e `ByTeams` (`bit`), com `Choose(byEmail, byTeams)`. Quem não tem linha fica com `ByEmail = true` e `ByTeams = false` (`NoticePreference.Default`). A migração cria as duas colunas, converte as linhas (`Email` → 1, 0; `Teams` → 0, 1; `Both` → 1, 1) e apaga a `Channel`. O enum `ImmediateNoticeChannel` sai.

*Alternativa:* manter o enum com mais valores (`None`, `EmailAndTeams`...). Recusada, porque cada canal novo multiplicaria as combinações e a auditoria mostraria um código, e não o que foi ligado.

### 2. A rota pelos dois campos

O `NoticeRouting.RouteOf(byEmail, byTeams, teamsAvailable)` devolve `Email = byEmail` e `Teams = byTeams && teamsAvailable`. Sem o Teams disponível, o e-mail não é mais forçado: quem desligou o e-mail não recebe. O `EmailChannel` e o `TeamsChannel` continuam lendo a rota; o `BellChannel` não a lê.

### 3. Sem a volta ao e-mail

O `TeamsNotice` perde o `AlsoByEmail` e o `EmailedInstead`, com as colunas, e o `TeamsDispatcher` deixa de pôr o e-mail na fila quando o Teams falha (`NeedsEmail`, `MarkEmailedInstead` e o contador `EmailedInstead` do `TeamsDispatchResult` saem). O aviso que falha fica como Falhou, como os e-mails.

### 4. A auditoria das escolhas

A `NoticePreference` entra na auditoria automática, com a pessoa como rótulo (`HasAuditLabelReference(p => p.UserId, UserAudit.Reference)`), e os campos `ByEmail` e `ByTeams`. O `ReceivesDailySummary` do `User` deixa de ser `ExcludeFromAudit`. A tela de Auditoria descreve cada um pelo `describeAuditEntry` ("Desligou os avisos por e-mail", "Ligou os avisos pelo Teams", "Desligou o resumo diário"), e a criação da primeira preferência diz só o que ficou diferente do padrão. Uma gravação igual à anterior não gera registro, pela regra da auditoria automática. Na anonimização, o registro mostra o nome anônimo pela referência, sem apagador novo.

### 5. A API e o cartão

`GET /api/profile/immediate-notices` devolve `{ email, teams, teamsAvailable }`, e o `PUT` recebe `{ email, teams }`. O cartão Avisos (`EmailNotices`) troca o `SegmentedControl` por duas `Checkbox` com gravação otimista, como o resumo diário: "Por e-mail" sempre e "Pelo Teams" só com `teamsAvailable`, com a ajuda sobre o aplicativo. Com as duas desmarcadas, uma linha diz que os avisos continuam no sino e no Meu trabalho. A falha volta a caixa e avisa pelo `useNotify`.

## Risks / Trade-offs

- **[Quem desliga os dois perde um prazo]** → O sino e o Meu trabalho mostram tudo o que pede ação, e o cartão diz isso quando os dois estão desmarcados.
- **[Quem liga só o Teams sem o aplicativo instalado]** → A ajuda da caixa diz que o aplicativo é preciso; o aviso fica no sino.
- **[Quem nunca escolheu deixa de receber pelo Teams]** → O Teams acabou de entrar (Avisos-4) e só o ambiente `dev` o usa; quem quiser liga em Meu perfil.
- **[A rota `immediate-notices` muda de forma]** → Só a tela a usa; o contrato e os tipos são gerados de novo, e os testes reprovam o contrato desatualizado.
