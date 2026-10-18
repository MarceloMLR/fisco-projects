# Tasks

## 1. Módulo de usuários

- [x] 1.1 Criar o módulo `Users` (domínio, caso de uso `SignInWithIdentity`, `UsersDbContext` com a migração inicial, `IUserAccess` em `Contracts`) e registrá-lo no host, com o termo no glossário; testar "Primeira entrada" e "Entradas seguintes"; verificar com `dotnet build -warnaserror` e `dotnet test`

## 2. Sessão e entrada local na API

- [x] 2.1 Configurar a sessão por cookie no host (expiração de 8 horas sem uso, renovação a cada uso, conferência do usuário ativo, 401 e 403 sem redirecionar, `/api` exigindo sessão) e a proteção `X-CSRF`; testar "Cookie da sessão", "API sem sessão", "Sessão parada por 8 horas", "Uso renova a sessão", "Usuário desativado durante a sessão" e "Alteração sem proteção contra requisição forjada"; verificar com `dotnet test`
- [x] 2.2 Criar as rotas da sessão e a entrada local, com o diretório de demonstração no `appsettings.Development.json`; testar "Entrada com uma pessoa da demonstração", "E-mail fora da demonstração", "Entrada local em produção" e "Sair" (API); atualizar o contrato e os tipos com `npm run api:update`; verificar com `dotnet test` e `npm test`

## 3. Entrada e sessão nas telas

- [x] 3.1 Criar o cliente HTTP (openapi-fetch, `X-CSRF`), a sessão no frontend e a proteção das rotas, com MSW nos testes; testar "Tela aberta sem sessão", "Página de entrada com sessão" e "Sessão expirada na tela"; verificar com `npm test`, `npm run lint` e `npm run typecheck`
- [x] 3.2 Criar a página de entrada do canvas, com o e-mail lembrado e as mensagens de recusa; testar "E-mail lembrado" e a entrada e a recusa na tela; verificar com `npm test`
- [x] 3.3 Acrescentar o avatar com o menu da conta no cabeçalho e o bloco da pessoa no menu do celular; testar "Conta no cabeçalho", "Menu no celular" e "Sair" (tela); verificar com `npm test`

## 4. Verificação final

- [x] 4.1 Conferir no Chromium a entrada local de ponta a ponta (página de entrada, recusa, entrada, conta e Sair), no computador e no celular, nos temas claro e escuro, contra o canvas, com a API e o banco local
- [x] 4.2 Atualizar a seção 6 do `CLAUDE.md` (entrada local e diretório de demonstração), rodar todos os scripts de `scripts/ci` sem erro, abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions

## Workflow follow-up

- Marcar a Acesso-1 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: ADR-0078 como aceito, `/opsx:archive`, roadmap como "Concluída" e merge.
