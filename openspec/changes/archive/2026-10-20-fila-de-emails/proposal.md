# Proposta: fila de e-mails (Avisos-1)

## Why

O e-mail é o canal de aviso da v1 (ADR-0036), e os avisos chegam nas próximas changes (Avisos-2 e 3). Antes deles, falta a base do envio:
- **Fila no banco:** a ação da pessoa não pode depender do envio, nem falhar quando o Microsoft 365 está fora do ar.
- **Envio em segundo plano:** com novas tentativas em caso de falha.
- **Implementação local:** no desenvolvimento e nos testes, nenhum e-mail sai; eles ficam gravados para conferência (ADR-0004).
- **Retenção:** o conteúdo do e-mail some da fila depois de 30 dias, ficando só a data e o tipo (ADR-0058).

O envio pelo Microsoft Graph, pela caixa compartilhada, fica para a M365-4.

## What Changes

- **Módulo `Notifications`** (schema `notifications`), dono da fila e, nas próximas changes, dos avisos e do sino.
- **Fila** (`outbox_emails`): cada e-mail com o destinatário, o assunto, o texto em HTML e em texto simples, o tipo, o status (Na fila, Enviado, Falhou), as tentativas e a próxima tentativa; fora da auditoria.
- **Envio em segundo plano:** a cada 30 segundos, os e-mails da vez são reservados, enviados pelo `IEmailSender` e marcados. Uma falha tenta de novo em 1 min, 5 min, 15 min, 1 h e 4 h; na sexta falha, o e-mail fica como Falhou.
- **Implementação local** (`Email:Provider` = `Local`): grava cada e-mail como um arquivo `.eml` em `App_Data/emails`, sem enviar. A aplicação não sobe em produção com ela.
- **Retenção:** uma vez por hora, os e-mails enviados ou que falharam há mais de `Email:KeepDays` dias (30) perdem o conteúdo e o destinatário.
- **E-mail com a marca:** o layout usa o nome da instalação e as cores do design system, e os textos ficam num arquivo de tradução (ADR-0013).
- **Teste de envio na Administração:** a seção "Microsoft 365" das Configurações gerais mostra a caixa de envio e o resultado do último teste, e "Enviar teste" põe na fila um e-mail para o administrador.

## Capabilities

### New Capabilities

- `avisos`: a fila de e-mails, o envio em segundo plano, a implementação local, a retenção e o teste de envio.

### Modified Capabilities

Nenhuma.

## Impact

- **Backend:** módulo novo `Notifications`, com a migração `EmailOutbox`, o processo em segundo plano e as rotas `GET /api/admin/email` e `POST /api/admin/email/test`.
- **Frontend:** a seção "Microsoft 365" das Configurações gerais.
- **Configuração:** `Email:Provider`, `Email:Mailbox`, `Email:SiteUrl`, `Email:KeepDays` e `Email:Local:Folder`.
- **Fora do escopo:** o envio pelo Graph (M365-4) e os avisos (Avisos-2 e 3).
