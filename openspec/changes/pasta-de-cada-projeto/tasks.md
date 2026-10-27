## 1. Decisão

- [x] 1.1 ADR-0121, índice dos ADRs e glossário (pasta dos projetos, pasta do projeto no armazenamento, `IProjectStorage`)

## 2. Armazenamento

- [x] 2.1 `RenameFolderAsync` no contrato e `StoragePaths` no `Files.Contracts`, com os testes do contrato "Renomear pasta", "Nome já usado ao lado" e "Pasta que não existe" nas duas implementações
- [x] 2.2 Ids estáveis na implementação local (índice e diário), com o teste "Ids antigos da implementação local" e os testes do contrato passando
- [x] 2.3 Renomear no SharePoint e no `FakeSharePoint`, com os testes do contrato
- [x] 2.4 `StoredFilesRemoval` com a pasta que fica, com o teste "Até a pasta indicada"

## 3. Pasta dos projetos

- [x] 3.1 `ProjectsFolder` nas configurações da instalação (migração, `InstallationInfo`, `PUT /api/installation`, validação), com os testes "Pasta dos projetos padrão", "Pasta dos projetos inválida" e "Pasta dos projetos na auditoria"
- [x] 3.2 Seção Arquivos das Configurações gerais, tipos da API e texto da auditoria, com os testes de tela ("Configurações gerais na Administração", "Descartar alterações")

## 4. Pasta de cada projeto

- [x] 4.1 Pasta do projeto no `Projects` (migração e `IProjectStorage`), com os testes "Pasta criada com o projeto", "Projeto com a pasta de antes", "Pasta apagada por fora" e "Escolher a pasta dos projetos"
- [x] 4.2 Documentos, pedidos de aceite e anexos dos chamados pela pasta do projeto, com o teste "Tudo na mesma pasta"
- [x] 4.3 Renomear a pasta com o projeto, com os testes "Renomear o projeto", "Nome da pasta já usado" e "Armazenamento fora do ar"
- [x] 4.4 Exclusão até a pasta do projeto (`IProjectDeletion`), com os testes "Arquivo de fora fica" e "Pasta dos projetos fica"

## 5. Documentação e conferência

- [x] 5.1 `CLAUDE.md`, `docs/instalacao.md` e roadmap (M365-3.1)
- [x] 5.2 Build sem avisos, `dotnet test`, `dotnet format`, lint, typecheck e testes do frontend, `openspec validate --all --strict`

## Workflow follow-up

- PR, aprovação de Marcelo, archive na mesma branch e merge por Marcelo.
- No `dev` do Azure, depois do merge: recarga limpa dos dados de demonstração, com a aprovação de Marcelo.
