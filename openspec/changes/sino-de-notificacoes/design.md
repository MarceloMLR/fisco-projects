# Design: sino de notificações (Avisos-3)

## Context

- **ADR-0038:** e-mail mais um sino dentro do FiscoProjects; os mesmos eventos dos e-mails (na hora e do resumo), link para o item, "marcar como lida" e "marcar todas como lidas"; atualização ao navegar e a cada minuto; guarda de 90 dias configurável; as regras de quem recebe não conhecem os canais.
- **ADR-0066:** o sino fica no canto superior direito, com o contador vermelho.
- **Avisos-2:** o `DeliverNotices` já aplica as regras de quem recebe e entrega a cada `INoticeChannel`; o e-mail é um canal.
- **Design system:** `NotificationBell` (fp-bell, contador `--notify`, rótulo "Notificações, N não lidas") e o painel (fp-notifications, 400 px, cabeça com "Não lidas N" e "Todas", "Marcar todas como lidas", grupos por dia, item não lido com o ponto e o texto em peso 500, "Ver todas" no pé). **Canvas:** Main com `notificacoes` ligado, e AdminConfiguracoes com "Guardar notificações por" (90 dias) nas Regras gerais.

## Goals / Non-Goals

**Goals:**
- canal do sino com os mesmos avisos;
- API das notificações da pessoa;
- sino e painel no topo e no celular;
- guarda configurável.

**Non-Goals:**
- tempo real e Teams (ADR-0038, depois);
- uma tela só de notificações (não há no canvas): "Ver todas" passa o painel para "Todas", e as mais antigas vêm por "Mostrar mais".

## Decisions

### 1. Canal do sino

- `BellChannel : INoticeChannel`, ao lado do `EmailChannel`, no mesmo `DeliverNotices` e na mesma gravação: um aviso, uma notificação para cada pessoa, na hora, de qualquer tipo (inclusive os que vão para o resumo por e-mail).
- **Dados:** o tipo, os valores do aviso (JSON), o caminho, o projeto e quem fez; o nome de quem fez é lido ao listar (nome atual, ADR-0084), e nenhum nome fica guardado (ADR-0058).
- **Textos:** no arquivo de tradução das telas (`notifications.kinds.*`), com os valores traduzidos pelas chaves que as telas já têm (time, status, papel, prazo).
- As tarefas que vencem nos próximos dias continuam só no resumo por e-mail: não são um evento.

### 2. API

- `GET /api/notifications?unread=true&before={id}`: 30 por página, as mais novas primeiro, com `unread` (o total de não lidas) e `hasMore`.
- `GET /api/notifications/unread-count`: só o número, para o contador, lido a cada minuto.
- `PUT /api/notifications/{id}/read` e `PUT /api/notifications/read-all`: 204; a notificação de outra pessoa é 404.

### 3. Guarda

- `NotificationRetentionDays` nas configurações da instalação (padrão 90, de 1 a 365), em "Guardar notificações por" das Regras gerais, ao lado do "Horário do resumo diário", como no canvas.
- O `NotificationCleanup`, em segundo plano, apaga a cada hora as notificações mais antigas que o prazo.

### 4. Tela

- **Sino:** `NotificationBell` em `web/src/notifications`, na `TopBar` à esquerda do menu da conta e na `MobileTopBar` à direita do título.
- **Contador:** `unreadCountQuery`, com `refetchInterval` de 60 segundos e lido de novo a cada navegação (mudança do endereço).
- **Painel:** um `Popover` do MUI com o visual do fp-notifications (400 px; no celular, a largura da tela menos as margens), a lista com rolagem por dentro e os grupos Hoje, Ontem e a data, no fuso da instalação (`useDates`). Cada item tem o ícone do tipo, o texto e "há 12 min · ERP-FIN" (hoje), "ontem, 17:40 · ERP-FIN" ou "16/10, 09:05 · ERP-FIN".
- **Escolher uma notificação:** marca como lida, fecha o painel e abre o caminho do item.

## Risks / Trade-offs

- **Muitas notificações de quem acompanha muitos chamados** → a guarda limita o volume, e o painel pagina.
- **Contador defasado até um minuto** → aceito pelo ADR-0038 (sem tempo real).
