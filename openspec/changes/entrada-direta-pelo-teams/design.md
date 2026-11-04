## Context

A página do Teams (`TeamsPage`, `TeamsTicketView`) abre o item com um link comum para o caminho (`href={path}`, `target="_blank"`), que o Teams abre no navegador. Sem sessão, o `RequireSession` leva à página de entrada com o caminho no `state`, e a página de entrada, com o e-mail lembrado (`rememberedEmail`), pede o endereço da Microsoft ao `POST /api/session/entra` (`email` opcional, `returnUrl` conferido pelo `LocalPath`) e vai para lá pelo `microsoftSignIn.go`. "Sair" só marca o fim da sessão na memória (`ended: { by: 'signOut' }`), que some ao fechar o navegador.

## Goals / Non-Goals

**Goals:**
- O link do Teams leva ao item sem a página de entrada, quando a pessoa tem a entrada pelo Entra e não escolheu Sair neste navegador.
- Num computador compartilhado, depois de Sair, o link do Teams mostra a página de entrada.

**Non-Goals:**
- Mudar a sessão, as 8 horas ou o cookie (ADR-0054).
- Entrar direto por outros caminhos (favorito, link de e-mail).
- Mudar a API ou o Entra ID.

## Decisions

- **Página de passagem `/teams/open?item=`** (`TeamsOpenPage`, em `web/src/teams`), fora da estrutura e do `RequireSession`, dentro do `InstallationRoot`, como `/teams`. Ela confere o item pelo `safeItemPath` e decide pela sessão (`useSession`) e pelas formas de entrada (`/api/session/sign-in-options`, a `signInOptionsQuery`): com sessão, `Navigate` para o item (`replace`); sem sessão, com o Entra e sem a marca de Sair, `POST /api/session/entra` com `email: rememberedEmail() || null` e `returnUrl: item`, e `microsoftSignIn.go`; nos outros casos (inclusive a falha do pedido), `Navigate` para `/sign-in` com o item no `state.from`, como o `RequireSession`. Enquanto decide, só o fundo da página, como o `SessionPending`. Alternativa: um marcador no próprio caminho do item (`?from=teams`), descartada porque mistura o marcador com a busca de cada tela e passaria pelo `RequireSession` de todas.
- **Os links do Teams apontam para a passagem**, pelo `teamsOpenPath(item)` de `teamsPage.ts` (`/teams/open?item=` com o caminho codificado), em "Abrir no FiscoProjects" e nos dois "Responder no FiscoProjects" do chamado.
- **Marca de Sair no navegador**, em `web/src/session/signedOut.ts`, como o `rememberedEmail.ts` (chave `fiscoProjects.signedOut`, cada acesso protegido): o `signOut` do `SessionProvider` a grava, e a sessão aberta (o `refresh` e a leitura inicial com a pessoa) a apaga. Só a marca, nunca um token (ADR-0054).
- **A recusa volta à página de entrada**: a Microsoft devolve a recusa à página de entrada com `?refused=` (ADR-0117), e não à passagem, então não há volta em círculo.

## Risks / Trade-offs

- [Sem a conta da empresa aberta no navegador, a Microsoft pede a conta] → é o que ela já pediria depois da página de entrada; o último e-mail vai como dica.
- [Limpar os dados do site esquece a marca de Sair] → o link do Teams volta a ir direto à Microsoft, que pede a conta se o navegador não a tem; dito no ADR-0153.
- [O item passa na busca da passagem] → é o mesmo caminho que hoje vai no endereço da página do Teams, sem dado pessoal, conferido pelo `safeItemPath` e pelo `LocalPath` da API.
