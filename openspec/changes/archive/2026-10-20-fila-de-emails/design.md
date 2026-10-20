# Design: fila de e-mails (Avisos-1)

## Context

- **ADR-0036:** e-mails pelo Graph a partir de uma caixa compartilhada, com fila no banco e envio em segundo plano; local no desenvolvimento e nos testes.
- **ADR-0004:** Microsoft 365 atrás de interfaces, com implementações locais.
- **ADR-0058:** o conteúdo dos e-mails enviados some da fila depois de 30 dias.
- **ADR-0047:** processos em segundo plano dentro da aplicação; o módulo `Notifications` é dono dos avisos.
- **O que já existe:** o `TicketAutoClose` (processo em segundo plano com o relógio do host) e o `Files` (implementação local escolhida na configuração e recusada em produção).
- **Canvas AdminConfiguracoes:** a seção "Microsoft 365" com "Caixa de envio", o selo "Envio testado" e "Enviar teste".

## Goals / Non-Goals

**Goals:**
- fila, envio com novas tentativas e implementação local;
- retenção do conteúdo;
- teste de envio pela Administração.

**Non-Goals:**
- envio pelo Graph (M365-4);
- avisos e sino (Avisos-2 e 3);
- a linha "Pasta dos projetos no SharePoint" da mesma seção (M365-3).

## Decisions

### 1. Módulo `Notifications`

- O ADR-0074 já prevê o nome. O módulo guarda a fila agora e os avisos depois.
- O `IEmailSender` fica na `Application` do módulo: só ele envia e-mails; os outros módulos vão avisar pelo contrato dos avisos (Avisos-2), nunca pela fila direto (ADR-0038).

### 2. Reserva antes do envio

- **Como:** o processo marca o e-mail como reservado (tentativa somada e próxima tentativa daqui a 10 minutos) e grava com o token de concorrência (`rowversion`) antes de enviar. Só quem gravou a reserva envia.
- **Por quê:** dois processos (duas instâncias ou duas rodadas) não enviam o mesmo e-mail. Se a aplicação parar no meio, a reserva vence e o e-mail é tentado de novo; o envio é "pelo menos uma vez".

### 3. Novas tentativas

- 1 min, 5 min, 15 min, 1 h e 4 h depois de cada falha; a sexta falha deixa o e-mail como Falhou.
- O erro fica guardado (até 500 caracteres) e no log, sem o conteúdo do e-mail.

### 4. Implementação local

- O `SmtpClient` com a entrega numa pasta (`SpecifiedPickupDirectory`) grava o e-mail completo como `.eml`, que qualquer cliente de e-mail abre, sem rede.
- **Configuração:** `Email:Provider` = `Local` e `Email:Local:Folder` (padrão `App_Data/emails`), como o `FileStorage`. Recusada em produção.

### 5. Layout e textos

- **Textos:** num arquivo de tradução do módulo (`Infrastructure/Emails/pt-BR.json`, recurso embutido), com chaves em inglês (ADR-0013).
- **Cores:** do `tokens.json` do design system, lido como recurso embutido (o mesmo arquivo de `web/src/theme`), no tema claro: nenhuma cor escrita no código.
- **Link:** `Email:SiteUrl` é o endereço das telas, para o botão de cada e-mail.

### 6. Retenção

- Uma vez por hora, os e-mails enviados ou que falharam há mais de `Email:KeepDays` (30) dias perdem o destinatário, o assunto, os textos e o erro; ficam a data, o tipo e o status (ADR-0058).

### 7. Teste de envio

- `POST /api/admin/email/test` põe na fila um e-mail do tipo `Test` para o administrador e responde 202.
- `GET /api/admin/email` traz a caixa de envio (`Email:Mailbox`) e o último teste: status, quando e para quem.
- **Tela:** a seção "Microsoft 365" das Configurações gerais, com "Caixa de envio" só para leitura, o selo do último teste (Envio testado, Na fila, Falhou ou Não testado) e "Enviar teste"; a tela lê de novo o resultado enquanto o teste está na fila.

## Risks / Trade-offs

- **[Gravação da ação e da fila em contextos diferentes]** Os avisos (Avisos-2) gravam na fila depois da ação; uma queda entre as duas gravações perde o aviso. Mitigação: a falha fica no log, e a ação nunca depende do aviso.
- **[Envio duplicado]** Uma queda depois do envio e antes da marcação envia de novo. Mitigação: é raro, e um e-mail a mais é melhor que um a menos.
