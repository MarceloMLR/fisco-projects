# Design: termos de aceite (Documentos-5)

## Context

- **ADR-0035:** pedido de aceite de um marco, resposta com o PDF assinado fora do sistema ou recusa com motivo, termo anexado pelo gestor, termo assinado na pasta de termos e o marco atingido.
- **ADR-0045:** os termos com o administrador e o gerente; a resposta com a pessoa do pedido, em qualquer papel; a pasta de termos para a gestão e para quem assinou, só com o dele.
- **ADR-0037:** o pedido de aceite por e-mail na hora; os aceites pendentes no resumo diário.
- **ADR-0043:** concluir o projeto com termos pendentes avisa, sem impedir.
- **ADR-0093:** a pasta de termos só tem os termos assinados, aprovados, vinculados ao marco, sem mudar de status nem ser excluídos.
- **ADR-0095 (novo):** o pedido cancelado sai da lista; a recusa encerra o pedido; o termo anexado sem pedido fica com uma pessoa do projeto.
- **O que já existe:**
  - o módulo `Documents`, com a pasta de termos (só leitura), os documentos e o `DocumentAccess`;
  - o `IScheduleItems` (fases, tarefas e marcos), o `IProjectClosing` e o `INoticeBoard`;
  - o `IFileStorage`, com `KeepBoth` para nomes repetidos;
  - o marco atingido (`ReachedOn`) no cronograma.
- **Canvas:** ProjetoTermos (a lista, o filtro Todos, Pendentes, Assinados e Recusados, a gaveta com o marco, para quem, o pedido, o termo, o histórico, "Lembrar" e "Cancelar pedido") e CelularTermo (a resposta no celular: o termo para assinar, o termo assinado, "Escolher PDF", "Recusar" e "Enviar termo").

## Goals / Non-Goals

**Goals:**
- o fluxo do pedido, da resposta e do termo anexado, com o histórico;
- o termo assinado na pasta de termos e o marco atingido;
- os avisos, o resumo diário e o aviso no encerramento;
- a tela, com a resposta também no celular.

**Non-Goals:**
- aceite por clique, validar a assinatura, vários aprovadores, termo de modelo (ADR-0035).

## Decisions

### 1. Termos (`acceptance_terms`)

- **Campos:** o projeto, o marco, o título (até 120), a mensagem (até 2.000, opcional), a pessoa do pedido (`RecipientId`), a situação (Pendente, Assinado, Recusado), quem pediu e quando, o termo a assinar (o arquivo no armazenamento, o nome e o tamanho; vazio no termo anexado sem pedido), o documento do termo assinado, quem enviou e quando, o motivo da recusa.
- **Termo a assinar:** pelo `IFileStorage`, na pasta `{código} - {nome}/Pedidos de aceite`, com `KeepBoth`; só PDF (o começo `%PDF`), até `Documents:MaxMegabytes`.
- **Auditoria:** o título é o rótulo, o projeto é o contexto, o marco e a pessoa aparecem pelos nomes atuais; os campos técnicos ficam de fora.
- **Histórico (`acceptance_events`):** pedido, lembrete, assinado, anexado (com quem anexou), recusado (com o motivo); fora da auditoria, como os eventos dos chamados.

### 2. Fluxo (`AcceptanceTerm`, Domain)

- **Pedir:** só o administrador e o gerente, com um marco do cronograma do projeto (`IScheduleItems`, tipo Milestone) e uma pessoa do projeto.
- **Responder:** só a pessoa do pedido, com o pedido Pendente:
  - o termo assinado: só PDF; vira o documento;
  - a recusa: o motivo, até 1.000 caracteres.
- **Anexar:** o administrador e o gerente, num pedido Pendente (em nome da pessoa do pedido) ou sem pedido (o marco, o título e a pessoa responsável); fica Assinado, com quem anexou.
- **Lembrar e cancelar:** só o pedido Pendente; o cancelamento remove o pedido, o histórico e o termo a assinar do armazenamento.
- **Respondido:** outra resposta, lembrete ou cancelamento dá 409 (`acceptance-answered`).
- **Projeto encerrado:** só leitura, com 409 (`project-closed`).

### 3. Termo assinado

- **Documento:** na pasta de termos, com o nome do arquivo (`KeepBoth` no armazenamento), Aprovado, vinculado ao marco, criado por quem enviou; a pasta continua só leitura na área de documentos (ADR-0093).
- **Marco:** o contrato novo `IScheduleMilestones.ReachAsync` do `Projects.Contracts` marca o marco como atingido no dia da instalação, se ainda não estava, na transação do cronograma e fora do desfazer de quem quer que seja; o marco removido do cronograma não impede o termo.
- **Quem vê na área de documentos:** o administrador e o gerente veem a pasta inteira; quem tem um termo assinado em seu nome vê a pasta só com os seus (`DocumentArea` com os documentos de termos da pessoa).

### 4. API (`/api/projects/{id}/acceptances`)

- `GET /` — os termos que a pessoa vê, do mais novo para o mais antigo, com o marco (nome e dia), a pessoa, quem pediu e quando, a situação, a contagem de cada situação e `canManage`; 404 de quem não está no projeto.
- `GET /{termId}` — o termo com a mensagem, o termo a assinar, o termo assinado (o documento), o motivo, o histórico e o que a pessoa pode fazer (`canAnswer`, `canAttach`, `canRemind`, `canCancel`); 404 de quem não vê o termo.
- `GET /{termId}/file` — o termo a assinar, para a pessoa do pedido e a gestão.
- `GET /milestones` — os marcos do cronograma, com o dia, para os diálogos da gestão.
- `POST /` (multipart) — pedir aceite: `milestoneId`, `recipientId`, `title`, `message`, `file`.
- `POST /attached` (multipart) — anexar sem pedido: `milestoneId`, `recipientId`, `title`, `file`.
- `POST /{termId}/signed` (multipart) — a pessoa do pedido envia o termo assinado.
- `POST /{termId}/attached` (multipart) — a gestão anexa o termo assinado em nome da pessoa do pedido.
- `POST /{termId}/refusal` — a pessoa do pedido recusa, com `reason`.
- `POST /{termId}/reminder` — lembrar.
- `DELETE /{termId}` — cancelar.
- **Recusas:** 404 de quem não vê o projeto ou o termo; 403 de quem está no projeto e não pode a ação; 409 `project-closed` no projeto encerrado; 409 `acceptance-answered` no pedido já respondido; 400 com `acceptance-file-refused` (`Empty`, `TooLarge` ou `NotPdf`) no arquivo que não é PDF.
- **Concorrência:** o termo tem um token de concorrência; duas respostas ao mesmo tempo deixam só a primeira, e a outra recebe 409 `acceptance-answered`.

### 5. Avisos

- **Tipos novos (`NoticeKind`):** `AcceptanceRequested` e `AcceptanceReminded` (e-mail na hora, `EmailChannel.Immediate`), `AcceptanceSigned` e `AcceptanceRefused` (sino e resumo).
- **Quem recebe:** o pedido e o lembrete, a pessoa do pedido; o termo assinado e a recusa, quem pediu e o gerente do projeto; o `DeliverNotices` já tira quem fez a ação.
- **Caminho:** `/projects/{id}/acceptances?term={termId}`, que abre a gaveta do termo.
- **Resumo diário:** o contrato `IPendingAcceptances` do `Documents.Contracts` dá os pedidos pendentes da pessoa nos projetos abertos, e o resumo os lista numa seção própria, como as tarefas que vencem.

### 6. Encerramento

- O contrato novo `IProjectAcceptances.PendingAsync` do `Projects.Contracts`, que o `Documents` implementa, dá os pedidos pendentes; o `GET /closing` traz `pendingAcceptances`, e o diálogo "Concluir projeto" avisa, sem impedir.

### 7. Tela

- **Aba "Termos de aceite":** depois de "Acessos", com a contagem dos termos que a pessoa vê; aparece para a gestão e para quem tem um termo (o `GET /` traz `canManage` e o total).
- **Lista (`DataTable`):** Termo e marco, Para, Pedido em, Situação (`AcceptanceStatusBadge`: Pendente, Assinado e Recusado por ícone, cor e texto); o `SegmentedControl` Todos, Pendentes, Assinados e Recusados; "Pedir aceite" e "Anexar termo assinado" para a gestão.
- **Gaveta (`DetailsDrawer`):** o marco, para quem, o pedido, a mensagem, o termo enviado (abrir), o termo assinado (abrir), o motivo da recusa e o histórico; as ações conforme o termo e a pessoa: "Lembrar {nome}", "Anexar termo assinado" e "Cancelar pedido" para a gestão; para a pessoa do pedido, "Escolher PDF", "Enviar termo" e "Recusar" (com o motivo num diálogo).
- **Celular:** a gaveta ocupa a tela, como o canvas CelularTermo; o link do e-mail abre a aba com a gaveta do termo.
- **Diálogos:** "Pedir aceite" (marco, pessoa, título, mensagem, PDF), "Anexar termo assinado" (marco, pessoa responsável, título, PDF) e "Recusar" (motivo).
- **Aba Documentos:** o termo assinado da pasta de termos abre na gaveta sem as ações de status, vínculo e exclusão, que a API recusaria (ADR-0093).
- **Encerramento:** o diálogo "Concluir projeto" avisa os pedidos pendentes, com "Ver termos".

### 8. Demonstração

- No ERP-FIN: termos assinados (um pela pessoa, um anexado pela gerente), um recusado e um pendente, com PDFs fictícios gerados pelo comando.

## Risks / Trade-offs

- **[Assinatura não conferida]** O sistema não valida a assinatura do PDF (ADR-0035). Mitigação: o termo assinado fica com quem enviou e quando, e a gestão o vê.
- **[Pedido para quem sai do projeto]** A pessoa que sai do projeto deixa de ver o pedido. Mitigação: a gestão cancela e pede de novo, ou anexa o termo em nome dela.
