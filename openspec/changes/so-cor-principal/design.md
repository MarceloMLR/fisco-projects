## Context

A Acesso-8.1 e a Acesso-8.2 acrescentaram à marca a cor secundária (com os tokens da barra lateral, dos destaques e do painel calculados no `brandColors.ts`) e uma cor por papel da conversa (com os tokens `msg-*`, `role-*` e `avatar-*` calculados), além do logo escuro e do seletor de cores. As duas estão na `main`.

## Goals / Non-Goals

**Goals:**
- Voltar às telas do design system, com só a cor principal derivando os tokens da marca.
- Manter o logo para o tema escuro e o seletor de cores.

**Non-Goals:**
- Mudar a cor principal ou as cores da conversa do design system.

## Decisions

- **Banco**: uma migração nova (`InstallationMainColorOnly`) apaga `SecondaryColor` e as seis colunas das mensagens; as migrações já aplicadas ficam como estão.
- **API**: `InstallationResponse` volta a `name`, `logoUrl`, `mainColor` e `darkLogoUrl`, e o formulário a `name`, `timeZone`, `mainColor`, `dailySummaryAt` e `notificationRetentionDays`.
- **Logo escuro**: `sidebarIsDark(tema)` olha o `sidebar-bg` do design system, escuro só no tema escuro; a barra lateral e a Prévia mostram o logo escuro nele.
- **Seletor**: o `ColorField` genérico volta a ser só o `MainColorField`, com a amostra que abre o seletor do navegador.
- **Auditoria**: os textos dos campos que saíram deixam de existir; registros antigos mostram o nome técnico do campo.

## Risks / Trade-offs

- [Instalações de desenvolvimento perdem as cores extras escolhidas] → não há instalação em produção; a cor principal e os logos ficam.
