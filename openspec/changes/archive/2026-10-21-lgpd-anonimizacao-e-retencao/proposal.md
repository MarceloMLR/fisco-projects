## Why

O FiscoProjects guarda o nome, o e-mail e o vínculo com o Entra das pessoas da empresa, e a auditoria guarda tudo para sempre. O ADR-0058 decidiu atender pedidos de remoção (LGPD) anonimizando a pessoa sem quebrar o histórico, e limpar a auditoria por prazo. É a Operação-1 do roadmap.

## What Changes

- ADR-0110: como a anonimização e o prazo da auditoria funcionam, com as escolhas de Marcelo (só pessoa desativada, menções e e-mails também, prazo de 1 a 10 anos).
- O administrador anonimiza uma pessoa desativada em Usuários: o nome vira "Pessoa removida nº N", o e-mail fica vazio, os vínculos com o provedor são apagados, e não dá para desfazer.
- Na mesma transação, cada módulo apaga os dados pessoais dela: rótulos e valores na auditoria, menções nas mensagens dos chamados, e-mails, notificações do sino e itens do resumo.
- "Guardar auditoria por" (1 a 10 anos, padrão 5) nas Configurações gerais, e a limpeza mensal da auditoria, com o registro do que apagou.
- A demonstração ganha uma pessoa anonimizada.

## Capabilities

### New Capabilities
- `dados-pessoais`: anonimização a pedido e prazo da auditoria (LGPD).

### Modified Capabilities

## Impact

- Backend: `Users` (anonimização), `Audit` (apagar e limpeza mensal), `Tickets` (menções), `Notifications` (e-mails e sino), `Administration` (prazo), `FiscoProjects.Persistence` (contrato `IPersonalDataEraser`); migrações em `users`, `audit` e `administration`.
- Frontend: Usuários (Anonimizar), Configurações gerais (prazo), Auditoria (textos novos).
- Demonstração: `web/scripts/demo`.
