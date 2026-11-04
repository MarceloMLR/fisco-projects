## Why

Quem escolhe "Responder no FiscoProjects" no Teams quase nunca tem sessão no navegador, porque o cookie acaba ao fechar o navegador e depois de 8 horas sem uso (ADR-0054). A pessoa cai na página de entrada, confirma o e-mail e só então vai à Microsoft, que muitas vezes nem pede senha. Marcelo pediu que o link do Teams abra direto o chamado (Avisos-6.1, a pedido de Marcelo, fora do roadmap, ADR-0153).

## What Changes

- Os links da página do Teams ("Responder no FiscoProjects" e "Abrir no FiscoProjects") passam por uma página de passagem (`/teams/open?item=`), fora da estrutura das telas e sem sessão, com o caminho do item conferido como hoje.
- Com sessão, a passagem abre o item. Sem sessão e com a entrada pelo Entra, vai direto à Microsoft, com o último e-mail usado no navegador como dica e o item como volta. Depois de "Sair" neste navegador, ou sem a entrada pelo Entra, mostra a página de entrada, que leva ao item depois de entrar.
- "Sair" fica guardado no navegador até a próxima entrada, ao lado do último e-mail; só a marca de que a pessoa saiu.
- Nenhuma mudança na API, na sessão nem no Entra ID.

## Capabilities

### New Capabilities

### Modified Capabilities
- `entrada-e-sessao`: a entrada direta pelo link do Teams e "Sair" lembrado no navegador.
- `avisos`: os links da página do Teams levam ao item pela entrada direta.

## Impact

- `web/src/teams` (página de passagem e links), `web/src/session` (marca de Sair), `web/src/routes.tsx` e os textos de `pt-BR.json`; testes de tela.
- ADR-0153 novo; `CLAUDE.md` e roadmap.
