# Design: entrada e sessão (Acesso-1)

## Context

Motivação em proposal.md. O host já tem Problem Details, `/health`, o grupo `/api`, a persistência por módulo (ADR-0075) e serve o frontend. O frontend tem o tema, a estrutura das telas e as rotas, sem chamar a API. Ainda não há módulo de negócio nem autenticação. Não há tenant: a única forma de entrar, por enquanto, é a entrada local (ADR-0078).

## Goals / Non-Goals

**Goals:**

- A M365-1 só acrescenta o provedor Entra: o caminho depois da identidade (usuário, vínculo, sessão, Sair) já existe.
- Nenhum token no navegador e nenhuma rota da API aberta por engano.

**Non-Goals:**

- Papéis e autorização por papel (Acesso-2): aqui, só "tem sessão ou não".

## Decisions

### 1. Módulo `Users`

Primeiro módulo de negócio, em `src/Modules/Users`, seguindo o guia de módulos; schema `users`, tabelas `users` e `identity_links`.

- `Domain`: `User` (nome, e-mail, ativo, criado em, última entrada) e `IdentityLink` (usuário, provedor, identificador externo, único por provedor). `User.RecordSignIn` atualiza nome, e-mail e a última entrada.
- `Application`: caso de uso `SignInWithIdentity` (acha o vínculo ou cria usuário e vínculo; recusa usuário inativo) e `GetSignedInUser`; a porta `IUserStore` é implementada na `Infrastructure` com o `UsersDbContext`.
- `Contracts`: `IUserAccess.IsActiveAsync(userId)`, usado pelo host a cada requisição.
- `Endpoints`: as rotas da sessão e o registro do módulo.
- Alternativa: deixar usuários no módulo `Administration`. A Administração é uma área de telas que junta usuários, auditoria e configurações; quase todos os módulos vão precisar do contrato de usuários, que fica melhor num módulo próprio. O nome entra no glossário.

### 2. Entrada local (ADR-0078)

- Opções `LocalSignIn`: `Enabled` e `People` (nome e e-mail de cada pessoa fictícia), validadas ao subir. Provedor `local`, identificador externo = e-mail em minúsculas.
- O registro do módulo lança erro se `Enabled` estiver ligado com o ambiente `Production`, e a aplicação não sobe.
- O `appsettings.Development.json` liga a entrada local e traz as seis pessoas do canvas (Marina Costa, Rafael Souza, Beatriz Lima, Carla Mendes, Diego Torres e Paulo Nogueira), com e-mails em `empresa.example`, domínio reservado para exemplos.

### 3. Sessão (ADR-0054)

- Cookie `__Host-fiscoprojects-session` (`HttpOnly`, `Secure`, `SameSite=Lax`, caminho `/`), com o identificador interno do usuário como única informação da pessoa; o prefixo `__Host-` impede o cookie de valer para subdomínios. `SameSite=Lax` em vez de `Strict` porque o retorno do Entra (M365-1) é uma navegação vinda de outro site.
- Expiração de 8 horas sem uso (`Session:IdleTimeout`). A cada requisição autenticada, o cookie é renovado (`ShouldRenew`), para as 8 horas contarem a partir do último uso; o padrão do ASP.NET Core só renova depois da metade do prazo.
- `OnValidatePrincipal` consulta `IUserAccess` e rejeita a sessão de usuário inexistente ou inativo. `OnRedirectToLogin` e `OnRedirectToAccessDenied` devolvem 401 e 403 sem redirecionar; o Problem Details vem do `UseStatusCodePages`.
- O grupo `/api` passa a exigir autenticação (`RequireAuthorization`); as rotas que precisam ser abertas (entrada, opções e quem está na sessão) usam `AllowAnonymous`. `/health`, o contrato e o frontend continuam abertos.
- Contra requisições forjadas, um middleware recusa com 403 as requisições `POST`, `PUT`, `PATCH` e `DELETE` sob `/api` sem o cabeçalho `X-CSRF: 1`. Um site de fora não envia cabeçalho próprio sem passar pelo CORS, que a API não libera. Alternativa: o antiforgery do ASP.NET Core, com um token lido pelo JavaScript num cookie, que o ADR-0054 proíbe.
- O tempo vem do `TimeProvider` registrado no host, para os testes avançarem o relógio.
- As chaves do Data Protection ficam no padrão da máquina; guardá-las fora dela vem com a publicação (Fundação-7).

### 4. API da sessão

| Rota | Sessão | Resposta |
| --- | --- | --- |
| `GET /api/session` | não exige | 200 com id, nome e e-mail, ou 401 |
| `GET /api/session/sign-in-options` | não exige | 200 com `localSignIn` (ligada ou não) |
| `POST /api/session/local` | não exige | 204 e o cookie; 403 para e-mail fora do diretório ou usuário inativo; 404 com a entrada local desligada |
| `POST /api/session/sign-out` | não exige | 204 e o cookie apagado |

O contrato OpenAPI e os tipos do frontend são atualizados com `npm run api:update`.

### 5. Frontend

- Cliente HTTP com o openapi-fetch e os tipos do contrato (`src/api/client.ts`), na mesma origem, com o cabeçalho `X-CSRF: 1` em toda requisição. Uma resposta 401 de qualquer chamada avisa a sessão.
- `SessionProvider` consulta `GET /api/session` ao abrir e guarda a pessoa; a rota da estrutura fica dentro de `RequireSession`, que leva para `/sign-in` com a tela pedida e, enquanto consulta, mostra só o fundo da página.
- Página de entrada (`/sign-in`, fora da estrutura), com os valores das classes `fp-signin` do canvas: fundo `signin-bg` com os dois brilhos e a grade de pontos, cartão `radius-xl` com `shadow-signin`, painel da marca com o degradê e as formas, e o formulário com título, e-mail, Entrar e o rodapé. Com a entrada local ligada, a dica e a nota dizem que é a entrada de demonstração, sem senha (ícone `InfoOutlined` no lugar do cadeado da Microsoft). Erros num Alert `danger`. No celular, o cartão ocupa a tela, como no `fp-screens.css`. Para a máscara da grade de pontos, que só usa o canal alfa, a cor vem de um token, não de `#000`.
- O e-mail fica em `localStorage` (`fiscoProjects.signInEmail`), como o ADR-0009 prevê; nenhum dado da sessão vai para o navegador.
- Cabeçalho: avatar (32px, iniciais do nome, `avatar-bg` e `avatar-text`) que abre o menu da conta com 272px: nome, e-mail, divisória e Sair (`LogoutOutlined`). No celular, o menu de navegação mostra antes dos itens o avatar, o nome e o e-mail, como no canvas; Sair no celular fica em Meu perfil (Acesso-5).
- Testes de tela com MSW simulando a API.

### 6. Testes

- Backend: unitários do domínio e dos casos de uso; integração da `Infrastructure` com SQL Server em contêiner; testes da API com o host real (entrada, cookie, 401, 403, expiração com o `FakeTimeProvider`, usuário desativado, produção com a entrada local ligada). Os testes da API usam endereço `https`, para o cliente devolver o cookie `Secure`.
- Frontend: página de entrada, e-mail lembrado, volta à tela pedida, 401, conta no cabeçalho e no celular.

## Risks / Trade-offs

- [Um ambiente fora da produção exposto com a entrada local ligada deixa entrar quem souber um e-mail do diretório] → Ela é desligada por padrão, ligada só no `appsettings.Development.json` e proibida em produção; os ambientes no Azure (Fundação-6) não a ligam.
- [Renovar o cookie a cada requisição acrescenta um `Set-Cookie` em todas as respostas autenticadas] → Custo pequeno, em troca de as 8 horas contarem do último uso, como o ADR-0009 pede.
- [O login pelo Entra só é conferido de verdade quando houver tenant] → O caminho depois da identidade é o mesmo e já está testado.
- [Sem Sair no celular até Meu perfil] → A sessão expira em 8 horas sem uso; no celular, o canvas põe Sair em Meu perfil (Acesso-5).

## Migration Plan

Primeira migração do módulo `Users`, aplicada ao subir no desenvolvimento. Para desfazer, basta reverter o PR e apagar o schema `users` do banco local.

## Open Questions

Nenhuma.
