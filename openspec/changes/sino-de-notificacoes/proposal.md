# Proposta: sino de notificações (Avisos-3)

## Why

Os avisos chegam só por e-mail (Avisos-2). O ADR-0038 pede que quem está usando o sistema veja as novidades sem abrir o e-mail: um sino no canto superior direito (ADR-0066), com o número de não lidas em vermelho, e um painel com as notificações da pessoa, os mesmos eventos dos e-mails (na hora e do resumo), o link para o item, "marcar como lida" e "marcar todas como lidas". A atualização é ao navegar e a cada minuto, sem tempo real, e as notificações ficam guardadas por 90 dias (configurável).

## What Changes

- **Canal do sino** (`BellChannel`): cada aviso vira, na hora, uma notificação para cada pessoa que o recebe, pelas mesmas regras de quem recebe (ADR-0038). O e-mail continua decidindo entre na hora e o resumo; o sino recebe tudo na hora.
- **Notificações** (`notifications.notifications`): a pessoa, o tipo, quem fez, o projeto, o caminho do item, os dados, quando chegou e quando foi lida.
- **API:** `GET /api/notifications` (não lidas ou todas, as mais novas primeiro, em páginas), `GET /api/notifications/unread-count`, `PUT /api/notifications/{id}/read` e `PUT /api/notifications/read-all`, só as da própria pessoa.
- **Guarda:** a cada hora, as notificações mais antigas que "Guardar notificações por" (padrão 90 dias, nas Regras gerais das Configurações gerais) são apagadas.
- **Tela:** o sino na barra do topo e na barra do celular, com o número de não lidas; o painel do canvas, com "Não lidas" e "Todas", "Marcar todas como lidas", as notificações agrupadas por dia (Hoje, Ontem, a data), cada uma com ícone, texto, quando e o projeto; escolher uma marca como lida e abre o item.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `avisos`: o sino, as notificações e a guarda.
- `configuracao-da-instalacao`: "Guardar notificações por".

## Impact

- **Backend:** tabela `notifications.notifications` (migração no `Notifications`); `NotificationRetentionDays` na instalação (migração no `Administration`).
- **API:** as rotas de `/api/notifications`; `notificationRetentionDays` em `PUT /api/installation` e em `GET /api/installation/rules`.
- **Frontend:** o sino e o painel na `TopBar` e na `MobileTopBar`; "Guardar notificações por" nas Configurações gerais.
- **Depende da Avisos-2** (`avisos-por-email`), ainda sem merge: o PR desta change vem depois do dela.
- **Fora do escopo:** tempo real e o Teams (ADR-0038, depois).
