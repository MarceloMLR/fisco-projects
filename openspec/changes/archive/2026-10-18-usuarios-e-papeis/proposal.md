# Proposta: usuários e papéis (Acesso-2)

## Why

Desde a Acesso-1, quem entra vira usuário, mas todos são iguais: ninguém administra o sistema, ninguém pode criar projetos e não há como bloquear quem saiu da empresa. Antes dos projetos (grupo 3), a instalação precisa dos papéis no sistema (ADR-0040), de um primeiro administrador definido na configuração e protegido contra a própria remoção (ADR-0006) e da tela Usuários, em que o administrador troca papéis e desativa ou reativa pessoas (ADR-0008, ADR-0046).

## What Changes

- Papel no sistema em cada usuário: Administrador, Gerente de projeto ou Usuário. Quem entra pela primeira vez recebe Usuário, o de menor privilégio (ADR-0006, ADR-0040).
- Primeiro administrador definido na configuração da instalação pela identidade (provedor e identificador externo; no Entra, o `oid`). Ele vira administrador na primeira entrada, ou na entrada seguinte se ainda não houver nenhum administrador ativo (ADR-0006).
- O sistema impede trocar o papel ou desativar o último administrador ativo (ADR-0006).
- Desativar e reativar: o usuário desativado é bloqueado na requisição seguinte, mesmo com a sessão aberta, e o histórico é preservado; o reativado volta a entrar (ADR-0008).
- A troca de papel vale na requisição seguinte, sem novo login (ADR-0006).
- Administração só do administrador: as rotas de administração respondem 403 a quem não é administrador, e a tela esconde a área (ADR-0046).
- API: lista de usuários com busca, filtro por papel e situação e paginação; troca de papel; desativar e reativar. A rota da sessão passa a informar o papel de quem entrou.
- Frontend: item Administração na barra lateral, só para o administrador; área de Administração com a navegação das seções; tela Usuários como no canvas (`AdminUsuarios`), com a tabela padrão do MUI X Data Grid gratuita, em pt-BR, num componente próprio (ADR-0050).
- Diretório de demonstração: Paulo Nogueira como primeiro administrador no `appsettings.Development.json`, como no canvas.

## Non-goals

- Registro das trocas de papel e das desativações na auditoria: chega com a auditoria automática ao salvar (Acesso-3, ADR-0039), sem código extra aqui.
- Papéis por projeto, membros e criação de projetos (grupo 3); a coluna Projetos da tela Usuários chega com eles.
- Outras seções da Administração (auditoria, configurações e cadastros), cada uma com a própria change.
- Ver os projetos e as pendências de uma pessoa para redistribuir (ADR-0008, depois); criar, importar ou excluir usuários (ADR-0008, fora).
- Anonimização a pedido (ADR-0058), no grupo de governança.
- Fuso da instalação nas datas (Acesso-5); até lá, o último acesso aparece no fuso do navegador.

## Capabilities

### New Capabilities

- `usuarios-e-papeis`: papel de cada pessoa no sistema, primeiro e último administrador, desativação e reativação e a tela Usuários da Administração.

### Modified Capabilities

- `estrutura-das-telas`: a barra lateral ganha o item Administração para o administrador, e a trilha passa a mostrar os níveis acima da página.

## Impact

- Código alterado: módulo `Users` (papel no domínio, casos de uso, migração, rotas de administração, política de autorização), contrato da sessão, host da API (autorização por papel), estrutura das telas e rotas do frontend.
- Código novo: tela Usuários, área de Administração, componente de tabela padrão e componentes de seleção, menu e situação no `web/`.
- Dependência nova: `@mui/x-data-grid` (MIT, gratuita, prevista no ADR-0050).
- Banco: nova coluna de papel na tabela `users.users`, com migração.
- ADRs implementados: 0006, 0008, 0040 (papéis no sistema) e 0046 (área de usuários); apoio do 0005 e do 0078.
