## Why

Marcelo pediu para voltar a ter só a cor principal na marca da instalação: tirar a cor secundária (Acesso-8.1) e as cores das mensagens dos chamados (Acesso-8.2), mantendo o logo para o tema escuro e o seletor de cores (Acesso-8.3, a pedido de Marcelo).

## What Changes

- ADR-0103: só a cor principal na marca; substitui a cor secundária do ADR-0100 e o ADR-0101.
- Configurações gerais: saem o campo "Cor secundária" e a seção "Mensagens dos chamados"; ficam o nome, o logo, o logo para o tema escuro e a cor principal com o seletor de cores.
- **BREAKING** API: `secondaryColor` e `messageColors` saem da marca (`GET /api/installation`) e do formulário (`PUT /api/installation`). Uma migração apaga as sete colunas.
- A barra lateral, os destaques, o painel da entrada e a conversa dos chamados voltam às cores do design system; o logo escuro aparece na barra lateral do tema escuro.

## Capabilities

### New Capabilities

### Modified Capabilities
- `configuracao-da-instalacao`: sem a cor secundária e as cores das mensagens; o logo escuro e o seletor de cores só com a cor principal.

## Impact

- `Administration` (domínio, casos de uso, rotas e a migração `InstallationMainColorOnly`), `brandColors.ts`, `BrandColors`, `Sidebar`, Configurações gerais (`MainColorField`), textos, auditoria e testes; contrato e tipos da API.
