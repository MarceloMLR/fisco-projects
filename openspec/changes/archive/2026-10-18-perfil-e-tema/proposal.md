# Proposta: perfil e tema (Acesso-5)

## Why

Hoje a aplicação sempre segue o tema do sistema operacional, e a escolha de um tema só dura até recarregar a página. O ADR-0014 pede claro, escuro e automático com a preferência salva no perfil, para valer em qualquer navegador e aparelho. O menu da conta, no cabeçalho, só tem o nome, o e-mail e Sair, mas o design aprovado (ADR-0066 e canvas) mostra também Meu perfil e o Tema. A tela Meu perfil ainda não existe.

A Acesso-5 original foi separada em duas a pedido de Marcelo: esta trata do perfil e do tema, e a Acesso-6 trata da configuração da instalação (nome, logo, cor principal e fuso).

## What Changes

- **Tema salvo no perfil** (ADR-0014): claro, escuro ou automático, com automático como padrão.
  - A escolha vale na hora e acompanha a pessoa em qualquer navegador depois da entrada.
  - A página de entrada continua seguindo o sistema operacional.
  - Fica no registro do usuário (módulo `Users`), fora da auditoria, por ser uma preferência pessoal e não um dado do negócio.
- **API:**
  - `GET /api/session` passa a trazer o tema;
  - `GET /api/profile` traz o perfil: nome, e-mail, papel, forma de entrada, último acesso e tema;
  - `PUT /api/profile/theme` troca o tema.
- **Menu da conta** como no design system: nome e e-mail, Meu perfil, Tema (controle segmentado só com ícones: claro, escuro e automático) e Sair.
- **Tela Meu perfil** (`/profile`), como no canvas, com o que já existe:
  - avatar, nome, e-mail e papel;
  - o cartão Preferências com o Tema em três opções com miniaturas;
  - ao lado, a Conta (papel no sistema, forma de entrada e último acesso) e o botão Sair.
- **Menu do celular** com o item Meu perfil, como no canvas.
- **Componente `SegmentedControl`** do design system em `web/src/components`.

## Non-goals

- O resto da tela Meu perfil no canvas, que chega com as changes donas de cada assunto:
  - Linhas das tabelas e Página inicial;
  - Avisos por e-mail (grupo Avisos, ADR-0037);
  - Meus projetos (grupo Projetos).
- Nome, logo, cor principal e fuso da instalação (Acesso-6).
- Idioma ou fuso por pessoa (ADR-0013, "depois").
- Evitar o instante em que a tela abre no tema do sistema antes de saber o tema do perfil. Isso pede guardar a escolha no navegador, o que o ADR-0054 e o ADR-0080 evitam.

## Capabilities

### New Capabilities

- `perfil`: o tema salvo no perfil e a tela Meu perfil.

### Modified Capabilities

- `estrutura-das-telas`:
  - o requisito "Tema automático" sai e passa para `perfil`, como "Tema no perfil";
  - o requisito "Cabeçalho com a trilha" passa a ter Meu perfil e Tema no menu da conta;
  - o requisito "Estrutura no celular" passa a ter Meu perfil no menu.

## Impact

- Backend (módulo `Users`):
  - `ThemePreference` e o tema no `User`, com a migração `ProfileTheme`;
  - os casos de uso `GetProfile` e `ChooseTheme`;
  - `ProfileEndpoints` e o tema na resposta da sessão;
  - testes do módulo e da API.
- Contrato OpenAPI e tipos do frontend (`npm run api:update`).
- Frontend:
  - `SessionProvider` aplica o tema e o troca;
  - `AccountMenu`, `Sidebar` (menu do celular), nova `ProfilePage` e rota `/profile`;
  - `SegmentedControl`;
  - traduções e testes.
- Documentação: `docs/roadmap.md` (Acesso-5 e Acesso-6), `docs/glossario.md` (tema, perfil).
- ADRs: 0014 (implementado), 0066 (menu da conta e Meu perfil), 0039 (o tema fica fora da auditoria, como o último acesso), 0054 (nada no navegador além do cookie).
