---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0108: Logo da instalação na aba do navegador

Complementa o ADR-0003 (configuração por instalação), o ADR-0082 (cor principal) e o ADR-0100 (logo para o tema escuro).

## Contexto e problema

A aba do navegador mostra o nome da instalação, mas nenhum ícone. Marcelo pediu o logo enviado nas Configurações gerais também na aba. Qual ícone usar com logo e sem logo?

## Fatores de decisão

- O mesmo logo da barra lateral, sem outro arquivo para o administrador enviar.
- Uma instalação sem logo também com ícone, igual ao que a barra lateral mostra.
- A cor do ícone padrão vinda dos tokens e da cor principal, sem cor escrita à mão.

## Opções consideradas

1. O logo da instalação; sem ele, o selo da barra lateral desenhado na hora.
2. Um campo próprio para o ícone da aba.
3. Um ícone fixo do produto, em arquivo.

## Resultado da decisão

Opção 1. A tela põe no `<link rel="icon">` o endereço do logo (o mesmo da barra lateral, que muda quando o logo muda) e, sem logo, o selo da barra lateral: o ícone da linha do tempo na cor `on-action`, sobre a cor principal da instalação (`action`), com os cantos do selo, num endereço `data:` em SVG. O ícone só entra depois de a marca chegar, para o selo não aparecer antes do logo. O logo para o tema escuro não vai para a aba, porque a cor da aba é a do sistema, não a do tema da aplicação.

### Consequências

- Boa, porque a aba mostra a marca da instalação sem configuração nova.
- Neutra, porque um logo largo fica pequeno na aba; o navegador o reduz ao quadrado.
