# Tasks

## 1. Backend

- [x] 1.1 Criar o módulo `Administration` com Contracts, testes e registro no `ApiSetup`:
  - `InstallationSettings` (nome e logo), com `Rename`, `ReplaceLogo` e `RemoveLogo`;
  - a migração `Initial`, com a linha padrão "FiscoProjects";
  - a auditoria: `Name` e `LogoFileName` entram, os campos técnicos ficam de fora.

  Verificar com `dotnet build -warnaserror`, os testes de arquitetura (o módulo novo passa a ser verificado) e os testes do domínio.
- [x] 1.2 Criar os casos de uso:
  - `GetInstallation` e `RenameInstallation`;
  - `ReplaceInstallationLogo` e `RemoveInstallationLogo`, com a conferência do `LogoFile` (PNG, SVG seguro, até 1 MB) e o `IFileStorage`.

  Testar a conferência (PNG, SVG, JPG, arquivo grande, SVG com script, DTD, `on*`, `javascript:`), a troca de PNG por SVG e o arquivo excluído quando o banco falha. Verificar com `dotnet test`.
- [x] 1.3 Criar as rotas:
  - `GET /api/installation` e `GET /api/installation/logo`, sem sessão, com os cabeçalhos de segurança e de cache;
  - `PUT /api/installation`, `PUT` e `DELETE /api/installation/logo`, só do administrador.

  Testar os cenários de "Configurações só do administrador", "Nome da instalação" (API), "Logo recusado", "Logo sem scripts", "Marca visível sem sessão" e "Troca do nome na auditoria". Atualizar o contrato com `npm run api:update` e verificar com `dotnet test`.

## 2. Frontend

- [x] 2.1 Criar `installationQuery` e `useInstallation`, e mostrar o nome e o logo da instalação na `Sidebar`, na `SignInPage` e no título da aba. Testar "Nome padrão", "Trocar o nome", "Enviar o logo", "Remover o logo", "Marca na página de entrada" e "Navegação principal". Verificar com `npm test`.
- [x] 2.2 Criar a tela Configurações gerais (`/admin/settings`), com:
  - a seção Instalação no `AdminLayout`;
  - a seção Marca (nome e logo), a Prévia e a barra de ações;
  - o tipo Configurações na Auditoria.

  Testar "Configurações gerais na Administração", "Prévia antes de salvar", "Descartar alterações", "Nome inválido", "Logo recusado" e "Configurações no filtro da Auditoria". Verificar com `npm test` e `npm run lint`.

## 3. Verificação final

- [x] 3.1 Conferir no navegador, com o build de produção:
  - a tela Configurações gerais, nos temas claro e escuro;
  - a troca do nome e do logo, com a barra lateral, a página de entrada e a aba;
  - a Auditoria com o registro;
  - a barra lateral e a entrada no celular.

  Mandar as capturas para Marcelo.
- [ ] 3.2 Documentação e verificações:
  - atualizar o glossário (configurações gerais, nome exibido, logo), o guia dos módulos, o `CLAUDE.md` e o roadmap;
  - rodar todos os scripts de `scripts/ci` sem erro;
  - abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions.

## Workflow follow-up

- Marcar a Acesso-6 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: `/opsx:archive` na mesma branch, roadmap como "Concluída" e merge, antes dos PRs da Acesso-7 e da Acesso-8.
