## 1. Decisão

- [ ] 1.1 ADR-0122, índice dos ADRs e glossário

## 2. Servidor

- [ ] 2.1 `StorageFolderChosen`, `ChooseFolder` e `UseSystemFolder` no projeto, a migração e o `StorageFolderName` na auditoria
- [ ] 2.2 `ProjectFolderChoices` e `GET /api/project-folders`, com os testes "Listar as pastas", "Pastas de um projeto na lista" e "Usuário não lista as pastas"
- [ ] 2.3 `folderId` na criação, com os testes "Criar projeto numa pasta que já existe", "Pasta de outro projeto" e "Pasta que não pode ser escolhida"
- [ ] 2.4 `IProjectFolderUse` no `Documents` e no `Tickets`, `GET` e `PUT /api/projects/{id}/folder` e `CanChooseFolder`, com os testes "Trocar a pasta antes dos arquivos", "Pasta com arquivos não muda", "Permissões da pasta do projeto", "Voltar à pasta do sistema" e "Troca da pasta na auditoria"
- [ ] 2.5 Pasta escolhida sem renome e sem exclusão, com os testes "Pasta escolhida não muda de nome" e "Pasta escolhida fica na exclusão"

## 3. Telas

- [ ] 3.1 Tipos da API, consultas, `ProjectFolderDialog` e `ProjectFolderField`, com os testes "Escolher a pasta no Novo projeto" e "Pasta que não pode ser escolhida no diálogo"
- [ ] 3.2 Pasta no Editar dados, com o teste "Pasta que não muda mais", e "Pasta dos arquivos" no texto da auditoria

## 4. Documentação e conferência

- [ ] 4.1 `CLAUDE.md` e roadmap (M365-3.2)
- [ ] 4.2 Build sem avisos, testes do servidor e das telas, `dotnet format`, lint, typecheck e `openspec validate --all --strict`

## Workflow follow-up

- PR, aprovação de Marcelo, archive na mesma branch e merge por Marcelo.
