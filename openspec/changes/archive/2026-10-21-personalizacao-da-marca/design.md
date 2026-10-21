## Context

A cor principal (ADR-0082) gera os tokens da marca no `brandColors.ts`, aplicados pelo `BrandColors` numa folha de estilo que vale sobre a do tema, e a Prévia das Configurações gerais usa as mesmas variáveis no próprio estilo. O logo é um arquivo do `IFileStorage` na pasta do sistema, servido por `GET /api/installation/logo?v=`. O ADR-0100 decide onde a cor secundária entra e o logo escuro.

## Goals / Non-Goals

**Goals:**
- Cor secundária na barra lateral, nos destaques de apoio e no painel da entrada, com contraste garantido.
- Seletor de cores nas duas cores.
- Logo para o tema escuro, mostrado na barra lateral escura.

**Non-Goals:**
- Outras cores (as das mensagens dos chamados são a Acesso-8.2).
- Trocar o logo do selo claro da entrada.

## Decisions

- **Domínio**: `InstallationSettings` ganha `SecondaryColor` (qualquer `#RRGGBB`, `TryNormalizeSecondaryColor`) e os campos do logo escuro (`DarkLogo*`), com `LogoTheme` (`Light`, `Dark`) nos métodos do logo; os casos de uso do logo recebem o tema e guardam o escuro como `installation-logo-dark.{png|svg}`. A migração acrescenta as colunas; o id, o tipo e a data do logo escuro ficam fora da auditoria, como os do logo.
- **Tokens**: `secondaryTokens` (`brandColors.ts`) calcula, por tema, `sidebar-*`, `bg-selected` e `brand-panel-*` e a variável `--sidebar-title` (o nome na barra lateral). O fundo da barra lateral é a cor no tema claro e um tom escuro do matiz no escuro; os passos (hover, ativo, divisória) clareiam sobre fundo escuro e escurecem sobre fundo claro; os textos partem dos do design system do tema que combina com o fundo e são empurrados até 4,5:1 (`withContrast`); o filete do item ativo é o da cor principal quando tem 3:1, senão o texto ativo; o painel parte da derivação da cor principal aplicada à secundária e escurece até o texto branco ter 4,5:1. `brandVariables` e `brandStyleSheet` recebem a cor secundária, que vale sobre a principal nesses tokens.
- **Logo na barra lateral**: `sidebarIsDark(cor secundária, tema)` diz se o fundo é escuro (luminância abaixo de 0,18); a barra lateral e a Prévia mostram o logo escuro nesse caso, quando há. O selo do painel da entrada continua com o logo.
- **Tela**: o `ColorField` (`MainColorField.tsx`) generaliza o campo da cor: a amostra tem por cima um `<input type="color">` transparente, que abre o seletor do navegador; o `SecondaryColorField` não confere contraste e mostra a ajuda. O `LogoField` (`SettingsPage.tsx`) serve aos dois logos; o do tema escuro aparece num quadro com `data-theme="dark"`. Salvar envia os logos que mudaram e depois o formulário.

## Risks / Trade-offs

- [Uma variável fora do design system] → só existe com a cor secundária, e a barra lateral volta ao `--text` sem ela.
- [Cor saturada na barra lateral] → escolha da instalação, vista antes na Prévia.
