## Why

Marcelo pediu que a cor do cartão das respostas nos chamados seja configurável, como as cores da marca. Hoje cada papel da conversa tem as cores fixas do design system (ADR-0069) (Acesso-8.2, a pedido de Marcelo).

## What Changes

- ADR-0101: uma cor por papel da conversa, opcional, com os tons do cartão, do selo e do avatar calculados dela, mantendo a claridade do design system e o contraste de 4,5:1.
- Configurações gerais: a seção "Mensagens dos chamados", com a cor de cada papel (solicitante, responsável, gerente do projeto, líder técnico, desenvolvimento e funcional), "Restaurar padrão", o seletor de cores e a prévia de uma mensagem de cada papel.
- As cores valem em todas as telas, nos dois temas, pela marca da instalação; sem elas, nada muda.
- API: `messageColors` em `GET` e `PUT /api/installation`; cor fora do formato dá 400. Auditoria com o nome de cada cor.

## Capabilities

### New Capabilities

### Modified Capabilities
- `configuracao-da-instalacao`: as cores das mensagens dos chamados.

## Impact

- `Administration` (domínio, caso de uso, rota e a migração `InstallationMessageColors`), `brandColors.ts` (`messageTokens`), `BrandColors`, Configurações gerais (`MessageColors.tsx`), textos e testes.
