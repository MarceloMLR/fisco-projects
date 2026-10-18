# Proposta: entrada e sessão (Acesso-1)

## Why

Toda tela do FiscoProjects é de uma pessoa identificada: a fila de Meu trabalho, os papéis, a auditoria. Antes de qualquer módulo de negócio, a aplicação precisa da página de entrada (ADR-0009), do usuário criado no primeiro login (ADR-0005) e da sessão do BFF com cookie, expiração de 8 horas sem uso e Sair (ADR-0054). Sem tenant, Marcelo decidiu que desenvolvimento e demonstrações usam a entrada local por e-mail (ADR-0078); o login pelo Entra foi para a M365-1.

## What Changes

- Primeiro módulo de negócio, `Users` (Usuários): o usuário, com identificador próprio, e o vínculo com o provedor de identidade em registro separado (ADR-0009), no schema `users`. O usuário é criado na primeira entrada, e nome e e-mail são atualizados nas seguintes.
- Entrada local (ADR-0078): fora da produção, ligada na configuração, o e-mail de uma pessoa do diretório de demonstração entra sem senha. A aplicação não sobe em produção com ela ligada.
- Sessão do BFF: cookie `HttpOnly`, `Secure` e `SameSite=Lax`, sem token no navegador; expira após 8 horas sem uso (configurável); a cada requisição, confere se o usuário existe e está ativo; chamada à API sem sessão recebe 401 em Problem Details; toda alteração na API exige o cabeçalho `X-CSRF`.
- API da sessão: quem está na sessão, opções de entrada, entrada local e Sair.
- Frontend: página de entrada como no canvas (cartão dividido sobre o fundo marinho, e-mail lembrado no navegador); telas só com sessão, voltando à tela pedida depois de entrar; 401 leva à página de entrada; avatar com o menu da conta (nome, e-mail e Sair) no cabeçalho e o bloco da pessoa no menu do celular. Cliente HTTP com o openapi-fetch e os tipos gerados do contrato (ADR-0076); testes com MSW (ADR-0056).
- Diretório de demonstração no `appsettings.Development.json`, com as pessoas fictícias do canvas.
- Roadmap: Fundação-6 e Fundação-7 adiadas até a assinatura do Azure; login pelo Entra na nova M365-1. ADR-0078 novo, que substitui o item de login simulado do ADR-0009.

## Non-goals

- Login pelo Entra, recusa de convidados e de quem não tem atribuição no Entra (M365-1).
- Papéis, desativar e reativar e a tela Usuários (Acesso-2); aqui, a sessão só confere se o usuário está ativo.
- Meu perfil, a escolha do tema e Sair no celular, que fica em Meu perfil (Acesso-5).
- Auditoria das entradas (Acesso-3).
- Chaves do Data Protection guardadas fora da máquina (com a publicação, Fundação-7).

## Capabilities

### New Capabilities

- `entrada-e-sessao`: como a pessoa entra, quem ela é no sistema, quanto dura a sessão, como ela é protegida e como sair.

### Modified Capabilities

- `estrutura-das-telas`: o cabeçalho ganha o avatar com o menu da conta, e o menu do celular, o bloco da pessoa.

## Impact

- Código novo: `src/Modules/Users` (domínio, casos de uso, EF Core com migração, rotas da sessão, entrada local), `tests/FiscoProjects.Users.Tests`, página de entrada, sessão e conta no `web/`.
- Código alterado: host da API (autenticação por cookie, proteção contra requisição forjada, registro do módulo), contrato OpenAPI e tipos, estrutura das telas, traduções, `docs/glossario.md` e `docs/roadmap.md`.
- Dependências novas: `openapi-fetch` (MIT, no navegador), `msw` (MIT, só testes), `Microsoft.Extensions.TimeProvider.Testing` (MIT, só testes).
- ADRs implementados: 0005 (usuário no primeiro login), 0009 (página de entrada, sessão, Sair, vínculo separado), 0054 (BFF com cookie), 0078 (entrada local). ADR novo: 0078.
