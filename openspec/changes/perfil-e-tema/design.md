# Design: perfil e tema (Acesso-5)

## Context

O tema vem do MUI com as variáveis CSS dos tokens: o `AppThemeProvider` usa `defaultMode="system"` e `storageManager={null}`, e o `main.tsx` marca o tema do sistema antes da primeira pintura. O `SessionProvider` sabe quem está na sessão por `GET /api/session`, e o cookie leva só o id do usuário. O módulo `Users` guarda o usuário (nome, e-mail, papel, situação, criação e último acesso) e os vínculos com os provedores de entrada (`IdentityLink`, provedor `local` hoje). O último acesso e a data de criação já estão fora da auditoria (`ExcludeFromAudit`).

O design aprovado define:

- **menu da conta:** nome e e-mail, Meu perfil, a linha Tema com um `SegmentedControl` só de ícones (`light_mode`, `dark_mode`, `brightness_auto`) e Sair;
- **tela Meu perfil:** `fp-split` com 400 px à direita; à esquerda, avatar grande, título e subtítulo, e o cartão Preferências com `fp-settings-row` e as opções `fp-theme-opt` com miniaturas; à direita, `fp-pane--side` com a seção Conta (`fp-props`) e o botão Sair.

## Goals / Non-Goals

**Goals:**

- Um lugar só para o tema no frontend: o `SessionProvider` aplica o tema da sessão e oferece a troca para o menu e a tela.
- O tema aplicado logo que a sessão é conhecida, sem pedido extra à API.

**Non-Goals:**

- Guardar o tema no navegador para evitar o instante no tema do sistema (ver a proposta).

## Decisions

### 1. Tema no usuário, fora da auditoria

- `ThemePreference { Automatic, Light, Dark }` no domínio do `Users`, gravado como texto (como o papel), com padrão `Automatic`.
- `User.ChooseTheme` valida o valor.
- No EF, a coluna `Theme` tem tamanho 16 e valor padrão `Automatic`; a migração `ProfileTheme` preenche quem já existe.
- A coluna fica marcada com `ExcludeFromAudit`, como o último acesso: é uma preferência da própria pessoa, não um dado do negócio, e encheria a auditoria a cada clique.

Alternativa: uma tabela de preferências por pessoa. Foi descartada por enquanto, porque só há uma preferência. Quando chegarem densidade, página inicial e avisos, a change dona avalia se elas cabem no usuário ou numa tabela própria.

### 2. API

- `SessionUserResponse` ganha `Theme`, para aplicar o tema já na resposta que diz quem está na sessão.
- `GET /api/profile` devolve `ProfileResponse(Id, Name, Email, Role, Theme, LastSignInAt, SignInMethods)`:
  - `SignInMethods` lista os provedores dos vínculos (`local`, e `entra` depois), e a tela traduz cada um ("Entrada local (demonstração)", "Conta Microsoft da empresa");
  - `LastSignInAt` vai em UTC e é exibido no fuso da tela (ADR-0013).
- `PUT /api/profile/theme` recebe `{ theme }` e responde 204. Exige sessão (é do grupo `/api`) e o `X-CSRF`, como toda alteração. Um valor fora do enum responde 400.
- Os casos de uso `GetProfile` e `ChooseTheme` são classes simples, com `IUserStore`. O `IUserStore` ganha `SignInProvidersAsync(userId)`.

### 3. Frontend: o tema vem da sessão

- No `SessionProvider`, um efeito chama `useColorScheme().setMode(...)`:
  - com a sessão aberta, `light`, `dark` ou `system` conforme o tema do perfil;
  - sem sessão (página de entrada, saída ou 401), `system`.
- O contexto da sessão ganha `chooseTheme(theme)`:
  1. aplica o modo na hora e atualiza o usuário da sessão em memória;
  2. chama o `PUT`;
  3. se a API recusar, volta ao tema anterior e lança o erro, para quem chamou mostrar o aviso.
- O menu da conta e a tela Meu perfil leem o tema de `session.user.theme` e chamam `chooseTheme`, então ficam sempre iguais.
- A tela Meu perfil usa o TanStack Query (ADR-0080) com `profileQuery`, chave `['profile']`, para os dados da Conta. Depois de trocar o tema, a chave é invalidada.

### 4. Componentes

- **`SegmentedControl`** em `web/src/components`, como o `fp-segmented` do design system:
  - um `role="radiogroup"` com botões `role="radio"` e `aria-checked`, setas para andar entre as opções;
  - variante só de ícones, com o rótulo no `aria-label` e no `title`.
- **Opções de tema** da tela Meu perfil (`ThemeOption`), em `web/src/profile`:
  - um `<label>` com `input type="radio"` escondido e a miniatura desenhada com os tokens do próprio tema;
  - a miniatura usa `data-mui-color-scheme` no elemento para pegar as variáveis do claro ou do escuro, independente do tema da tela;
  - o automático mostra as duas metades.
- **Menu da conta:** o `AccountMenu` ganha o item Meu perfil (link `/profile`, ícone `PersonOutlined`) e a linha Tema, entre divisores, como no `fp-appbar__menu-theme`.
- **Menu do celular:** a `Sidebar` aceita os itens extras do menu, e o celular passa Meu perfil depois da navegação principal, como no canvas.
- **Rota `/profile`:** com `titleKey: 'pages.profile.title'` e sem item da barra lateral. O `navigation` do `PageHandle` passa a ser opcional.

## Risks / Trade-offs

- **[Instante no tema do sistema]** Quem escolheu um tema diferente do sistema vê, por um instante, o tema do sistema até `GET /api/session` responder. → É uma resposta só, na abertura. Guardar no navegador ficou fora (proposta).
- **[Troca de tema sem rede]** O `PUT` pode falhar. → O tema volta ao anterior e aparece o aviso, então a tela nunca mostra um tema que o perfil não guardou.
- **[Miniaturas com variáveis do outro tema]** Depende de o `cssVariables()` gerar as variáveis também sob o seletor `[data-mui-color-scheme=dark]` e `light`. → O tema central já faz isso, e um teste confere que a miniatura escura usa o fundo escuro.

## Migration Plan

- A migração `ProfileTheme` acrescenta a coluna com o padrão `Automatic` e não mexe nos dados existentes.
- No desenvolvimento, a API a aplica ao subir; em produção, segue o processo de publicação (ADR-0055).
- Para voltar atrás, reverta o PR e a migração com `dotnet ef migrations remove` antes de publicar, ou uma migração nova depois.
