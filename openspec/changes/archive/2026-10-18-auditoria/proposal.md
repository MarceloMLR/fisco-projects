# Proposta: auditoria (Acesso-3)

## Why

Hoje nada registra quem mudou o quê: a troca de papel, a desativação de uma pessoa e as entradas recusadas não deixam rastro. O ADR-0039 pede uma auditoria automática de todas as alterações e dos eventos de segurança, feita num ponto único da camada de dados, e o ADR-0075 já reservou esse ponto no contexto base dos módulos. Ela precisa existir antes dos projetos (grupo 3), para que cada módulo novo já nasça auditado sem código próprio.

## What Changes

- Toda criação, alteração e exclusão salva por qualquer módulo gera um registro de auditoria com quem fez, quando, o tipo e a identificação do item e os campos alterados, com o valor antes e depois (ADR-0039). A captura fica no contexto base de persistência (ADR-0075), e o registro é gravado na mesma transação da alteração.
- Cada módulo pode marcar, na própria configuração de banco, o nome que identifica o item na auditoria, os campos técnicos que ficam de fora (como o último acesso) e os campos secretos, que entram só como "alterado", sem o valor (ADR-0033, ADR-0039).
- Eventos de segurança: entrada, entrada recusada (com a identidade usada e o motivo), troca de papel, desativação e reativação (ADR-0039). Os três últimos já são alterações do usuário e entram pelo registro automático.
- Registros somente leitura: nenhuma rota altera ou apaga, e a gravação da auditoria só acrescenta.
- Novo módulo `Audit`, dono das tabelas da auditoria (schema `audit`), com a rota de consulta só para o administrador (ADR-0046) e um contrato para os eventos de segurança.
- Contrato novo no módulo `Users` para a auditoria mostrar o nome de quem fez cada alteração.
- Frontend: seção Auditoria na Administração, como no canvas (`AdminAuditoria`): lista do mais novo para o mais antigo, filtros por pessoa, tipo de item e período, total no período e painel com os detalhes e a tabela de mudanças.

## Non-goals

- Filtro e coluna Projeto: chegam com os projetos (Projetos-3), quando os registros passarem a ter projeto. Até lá, nenhum registro tem projeto.
- Origem da alteração e itens afetados em cadeia, como as tarefas empurradas do canvas: chegam com o cronograma (grupo 4).
- Mudança de configuração da instalação: entra pelo registro automático quando a configuração existir (Acesso-5).
- Prazo de retenção, limpeza mensal e anonimização (ADR-0058): grupo de governança.
- Histórico do projeto para os gestores e exportação da auditoria (ADR-0039, depois); registro de quem visualizou cada item (ADR-0039, fora).
- Fuso da instalação nas datas (Acesso-5): até lá, as datas e o período usam o fuso do navegador.

## Capabilities

### New Capabilities

- `auditoria`: o que é registrado automaticamente e como eventos de segurança, o que nunca entra na auditoria, quem pode consultá-la e o que a tela de Auditoria mostra.

### Modified Capabilities

Nenhuma.

## Impact

- Código novo: módulo `Audit` (domínio, consulta, gravação, rotas e migração), captura das alterações no `FiscoProjects.Persistence`, tela de Auditoria no `web/`.
- Código alterado: contexto base dos módulos (gravação com auditoria), módulo `Users` (marcações de auditoria, eventos de entrada e contrato de nomes), host da API (registro do módulo), seção da Administração no frontend, tabela padrão (seleção de linha e texto do rodapé).
- Banco: schema `audit` com a tabela dos registros e os índices de data, pessoa e tipo.
- Dependências novas: nenhuma. O período usa campos de data do navegador, porque o seletor de intervalo do MUI X é pago.
- Guia de módulos (`src/Modules/README.md`) com as marcações de auditoria.
- ADRs implementados: 0039 (auditoria automática e eventos de segurança) e 0075 (ponto único no contexto base); apoio do 0033 (senhas fora da auditoria), 0046 (só o administrador), 0047 (módulo próprio) e 0058 (dados pessoais).
