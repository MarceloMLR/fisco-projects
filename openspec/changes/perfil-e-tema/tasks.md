# Tasks

## 1. Backend

- [x] 1.1 No módulo `Users`:
  - criar `ThemePreference` e `User.ChooseTheme`;
  - mapear a coluna `Theme` (texto, padrão `Automatic`, `ExcludeFromAudit`) e criar a migração `ProfileTheme`;
  - criar os casos de uso `GetProfile` e `ChooseTheme`, com `IUserStore.SignInProvidersAsync`;
  - testar os casos de uso e "Tema fora da auditoria".

  Verificar com `dotnet build -warnaserror` e `dotnet test`.
- [x] 1.2 Na API:
  - `Theme` na resposta da sessão;
  - `GET /api/profile` e `PUT /api/profile/theme`, com sessão e `X-CSRF`;
  - testar "Troca de tema sem sessão", tema inválido (400), tema salvo e devolvido pela sessão e pelo perfil;
  - atualizar o contrato com `npm run api:update`.

  Verificar com `dotnet test` e os testes de contrato.

## 2. Frontend

- [x] 2.1 No `SessionProvider`, aplicar o tema da sessão (e `system` sem sessão) e criar `chooseTheme`, que volta atrás se a API recusar. Testar:
  - "Tema segue o sistema";
  - "Tema em outro navegador";
  - "Página de entrada segue o sistema".

  Verificar com `npm test`.
- [x] 2.2 Criar o `SegmentedControl` (com a variante só de ícones e as setas do teclado) e passar o `AccountMenu` para Meu perfil, Tema e Sair. Testar "Conta no cabeçalho" e "Escolher o tema". Verificar com `npm test` e `npm run lint`.
- [x] 2.3 Criar a tela Meu perfil (`/profile`) com `profileQuery`, as opções de tema com miniaturas e a Conta. Também:
  - o item Meu perfil no menu do celular;
  - o `navigation` opcional no `PageHandle`;
  - testar "Dados da conta", "Tema na tela Meu perfil", "Meu perfil pelo menu da conta" e "Menu no celular".

  Verificar com `npm test`.

## 3. Verificação final

- [x] 3.1 Conferir no navegador, com o build de produção:
  - o menu da conta e a tela Meu perfil, nos temas claro e escuro, no computador e no celular;
  - a troca de tema;
  - o tema mantido depois de entrar em outro navegador.

  Mandar as capturas para Marcelo.
- [ ] 3.2 Documentação e verificações:
  - atualizar o glossário (tema, perfil), o `CLAUDE.md` se preciso e o roadmap;
  - rodar todos os scripts de `scripts/ci` sem erro;
  - abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions.

## Workflow follow-up

- Marcar a Acesso-5 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: `/opsx:archive` na mesma branch, roadmap como "Concluída" e merge.
