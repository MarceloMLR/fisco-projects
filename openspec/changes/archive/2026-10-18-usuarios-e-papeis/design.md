# Design: usuários e papéis (Acesso-2)

## Context

Motivação em proposal.md. O módulo `Users` (Acesso-1) já tem o usuário com `IsActive`, o vínculo com o provedor, o caso de uso `SignInWithIdentity` e o contrato `IUserAccess.IsActiveAsync`, que o host consulta a cada requisição para validar a sessão. O grupo `/api` exige sessão, e toda alteração exige `X-CSRF`. No frontend, a sessão traz id, nome e e-mail; a barra lateral tem Início, Projetos e Chamados, e a trilha do cabeçalho mostra só a página atual. Não há autorização por papel nem tabela de dados.

## Goals / Non-Goals

**Goals:**

- Um só ponto para saber o papel de alguém, consultado a cada requisição, para a troca valer sem novo login.
- A instalação nunca fica sem administrador ativo, nem com duas trocas ao mesmo tempo.
- A tabela padrão da aplicação (ADR-0050) nasce aqui, pronta para as próximas listas.

**Non-Goals:**

- A matriz de permissões dos projetos (ADR-0043 a 0045): aqui só a regra "Administração só do administrador".

## Decisions

### 1. Papel no domínio e no contrato

- `SystemRole` (`User`, `ProjectManager`, `Administrator`) fica em `FiscoProjects.Users.Contracts`, porque os outros módulos vão precisar dele (o Gerente de projeto cria projetos). O domínio do próprio módulo usa o mesmo tipo, sem cópia nem mapeamento.
- `User` ganha `Role` (padrão `User`) e os métodos `ChangeRole`, `Deactivate` e `Reactivate`. No banco, coluna `Role` em texto (`nvarchar(32)`), com o padrão `User` para quem já existe; migração `SystemRoles`.
- `IUserAccess.IsActiveAsync` vira `FindAsync(userId)`, que devolve `UserAccess(UserId, IsActive, Role)` ou nada. A implementação guarda o resultado durante a requisição (serviço por escopo), para a validação da sessão e a autorização não consultarem o banco duas vezes.

### 2. Primeiro administrador

- Opções `Users:FirstAdministrator`: `Provider` e `ExternalId` (no Entra, `entra` e o `oid`; na entrada local, `local` e o e-mail em minúsculas). Validadas ao subir: as duas ou nenhuma.
- `SignInWithIdentity` dá o papel Administrador à identidade configurada quando cria o usuário, ou quando ela já existia e não há nenhum administrador ativo. Assim a configuração resolve a instalação nova e o caso de alguém configurado depois de já ter entrado, sem desfazer uma troca feita na tela.
- Alternativa: aplicar a configuração em toda entrada. Descartada porque o administrador não conseguiria rebaixar o primeiro administrador.
- O `appsettings.Development.json` indica Paulo Nogueira, como no canvas. O guia de instalação (`docs/instalacao.md`) passa a explicar a configuração.

### 3. Último administrador ativo, sem corrida

- Os casos de uso `ChangeSystemRole`, `DeactivateUser` e `ReactivateUser` recusam com `LastAdministrator` a troca ou a desativação que deixaria zero administradores ativos.
- A contagem e a gravação rodam numa transação `Serializable`, dentro da estratégia de novas tentativas do EF Core (guia de módulos). Duas trocas simultâneas entre os dois últimos administradores viram um impasse no SQL Server; a vítima é repetida pela estratégia, relê a contagem e é recusada. A porta `IUserStore` ganha a execução em transação, para a `Application` não depender do EF Core.
- Alternativa: um `UPDATE` condicional em SQL. Descartada por espalhar a regra de negócio no SQL.

### 4. Autorização por papel

- Política `Administrator` (nome em `SystemPolicies`, no contrato), registrada pelo módulo `Users`: um requisito com um handler que lê o papel por `IUserAccess`. As rotas de administração usam `RequireAuthorization(SystemPolicies.Administrator)`; sem o papel, a resposta é 403 em Problem Details, pelo evento que o host já tem.
- O papel não vai para o cookie, que continua só com o id (ADR-0054); por isso a troca vale na requisição seguinte.
- Alternativa: claims de papel com `IClaimsTransformation`. Descartada porque acrescenta uma consulta por requisição mesmo nas rotas que não precisam do papel.

### 5. API

| Rota | Quem | Resposta |
| --- | --- | --- |
| `GET /api/session` | sessão | passa a trazer `role` |
| `GET /api/users?search=&role=&status=&page=&pageSize=` | administrador | 200 com `items` (id, nome, e-mail, papel, ativo, último acesso) e `total`, em ordem alfabética |
| `PUT /api/users/{id}/role` | administrador | 204; 404; 409 `last-administrator` |
| `POST /api/users/{id}/deactivate` | administrador | 204; 404; 409 `last-administrator` |
| `POST /api/users/{id}/reactivate` | administrador | 204; 404 |

- Paginação começando em 1, com `pageSize` de até 100 (padrão 25). `status` é `Active` ou `Inactive`, e os papéis vão pelo nome (`User`, `ProjectManager`, `Administrator`).
- A busca compara nome e e-mail com a collation `Latin1_General_100_CI_AI`, sem diferenciar maiúsculas e acentos: uma busca sem acento encontra o nome com acento.
- Enums em JSON como texto (`JsonStringEnumConverter` no host), para o contrato OpenAPI e os tipos do frontend terem os valores.
- O 409 traz o tipo `urn:fiscoprojects:problem:last-administrator`, como os tipos da recusa da entrada.
- Um corpo que a API não consegue ler (um papel desconhecido, por exemplo) recebe 400 também no desenvolvimento, onde o ASP.NET Core lança a exceção em vez de responder: o tratador de exceções do host usa o status da `BadHttpRequestException`.

### 6. Frontend

- A sessão traz o papel. O item Administração (ícone `AdminPanelSettingsOutlined`) entra na navegação principal marcado como só do administrador; a barra lateral e o menu do celular o escondem dos outros papéis.
- Rotas: `/admin` leva a `/admin/users`. `RequireAdministrator` envolve a área e leva ao Início quem não é administrador.
- A trilha: o `PageHandle` ganha os níveis acima da página (`trail`), e o cabeçalho mostra "Administração / Usuários", com Administração como link.
- Área de Administração: duas colunas como no canvas (`fp-split--left`, 240px), com a navegação das seções (`fp-nav2`) em `bg-subtle`. Nesta change, só a seção "Pessoas e segurança" com Usuários; as outras entram com as próprias changes. No celular, a navegação das seções some (há uma só seção) e a tabela rola de lado.
- Tela Usuários: título com o total (`fp-count`); a tabela com a busca (com ícone), os filtros de papel e de situação e o rodapé com a paginação. Colunas: Pessoa (avatar, nome e e-mail), Papel no sistema, Situação (ícone, cor e texto, como o `StatusBadge` do tipo `user`), Último acesso e o botão do menu. A coluna Projetos chega com os projetos.
- Menu da linha (`Papel e acesso`): seção "Papel no sistema" com os três papéis e o atual marcado, divisória e "Desativar acesso" (ícone `BlockOutlined`, em `status-danger-text`) ou "Reativar acesso". As confirmações e a recusa do último administrador aparecem num aviso (Snackbar do design system).
- Componentes próprios, para as próximas telas:
  - `DataTable`: o MUI X Data Grid gratuito (9.14.0, MIT, ADR-0050) com os textos em pt-BR, o visual do `fp-gt`, a paginação no servidor e a rolagem por dentro (ADR-0066);
  - `Select`, no visual do campo do design system;
  - `StatusBadge`;
  - o estilo dos menus e do Snackbar no tema central do MUI. O menu da conta passa a usar o mesmo estilo.
- Último acesso: "Agora", "Hoje, 09:12", "Ontem, 17:30" ou "14/10/2026", formatado num só lugar. Até a Acesso-5, no fuso do navegador.
- Busca com espera de 300 ms depois da última tecla.

### 7. Testes

- Backend:
  - unitários do domínio e dos casos de uso;
  - integração com SQL Server para o primeiro administrador, o último administrador (inclusive duas trocas simultâneas), a busca e os filtros;
  - testes da API para as rotas, a sessão com o papel e o bloqueio do desativado;
  - a matriz de permissões gerada de uma tabela, papel por ação (ADR-0056).
- Frontend, com MSW: o item Administração por papel, o endereço sem ser administrador, a trilha, a lista, a busca, os filtros, a troca de papel, desativar e reativar, e a recusa do último administrador.

## Risks / Trade-offs

- [A transação `Serializable` trava a leitura dos administradores durante a troca] → É uma operação rara e curta, feita só pelo administrador.
- [O Data Grid acrescenta peso ao frontend] → Ele já estava previsto no ADR-0050 e vai servir para todas as listas. O tamanho do build é conferido no PR.
- [Se a pessoa configurada como primeiro administrador estiver desativada e não houver outro, ninguém administra] → A proteção do último administrador impede chegar a esse estado pela tela.

## Migration Plan

Migração `SystemRoles` no módulo `Users`, com `User` para quem já entrou. Para desfazer, reverter o PR e a migração (`dotnet ef database update Initial`).
