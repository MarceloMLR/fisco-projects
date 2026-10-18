---
status: aceita
date: 2026-10-16
decision-makers: Marcelo Ribeiro
---

# ADR-0064: Tipografia com IBM Plex Sans, servida pela própria aplicação

## Contexto e problema

Com a direção sóbria (ADR-0062) e as cores do azul corporativo (ADR-0063), falta a fonte. Marcelo quer evitar o visual genérico; a fonte padrão do MUI (Roboto) e a Inter estão entre as mais associadas a produtos genéricos. As telas têm muitas listas, prazos e valores.

## Fatores de decisão

- Identidade própria e coerente com o azul corporativo.
- Legibilidade em tabelas e números.
- Licença gratuita, inclusive para uso comercial.
- Privacidade e funcionamento sem serviços externos.

## Opções consideradas

- A. Roboto (padrão do MUI)
- B. Inter
- C. IBM Plex Sans
- D. Source Sans 3

## Resultado da decisão

Opção escolhida: **C. IBM Plex Sans**.

Regras:

- Fonte principal IBM Plex Sans; IBM Plex Mono para códigos e identificadores técnicos.
- Arquivos da fonte servidos pela própria aplicação (pacotes no projeto), sem buscar no Google Fonts a cada acesso: o IP das pessoas não vai para terceiros (LGPD) e o sistema funciona em redes que bloqueiam serviços externos.
- Números tabulares em listas, prazos e valores.
- Pesos usados: regular e médio (e semibold só se o design system pedir).
- Licença SIL Open Font License (gratuita, inclusive para uso comercial).

### Consequências

- Bom, porque dá personalidade corporativa e técnica ao produto, longe do visual genérico.
- Bom, porque é ótima para números e tabelas.
- Ruim, porque os caracteres são um pouco mais largos, cabendo menos texto por linha.
- Ruim, porque exige configurar a fonte no tema do MUI e no Syncfusion.

### Confirmação

O tema do MUI e o tema do Gantt usam IBM Plex Sans; nenhuma requisição a `fonts.googleapis.com` em produção; listas e prazos usam números tabulares.

## Prós e contras das opções

### A. Roboto

- Bom, porque já vem no MUI.
- Ruim, porque reforça a cara de Material e de produto genérico.

### B. Inter

- Bom, porque é muito legível.
- Ruim, porque é das fontes mais associadas a produtos genéricos.

### C. IBM Plex Sans

- Bom, porque tem personalidade e é ótima em números.
- Ruim, porque é um pouco mais larga.

### D. Source Sans 3

- Bom, porque é neutra e legível.
- Ruim, porque tem pouca personalidade.
