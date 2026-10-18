# Tasks

## 1. Solução e configuração de build

- [x] 1.1 Criar `global.json` (SDK 10.0.100, `rollForward: latestFeature`), `Directory.Build.props`, `Directory.Packages.props` e `.editorconfig` na raiz; verificar que `dotnet --version` na raiz resolve um SDK 10.0
- [x] 1.2 Criar `FiscoProjects.slnx` com `src/FiscoProjects.SharedKernel` e `src/FiscoProjects.Api` (com `Program.cs` mínimo); verificar que `dotnet build -warnaserror` passa sem avisos e `dotnet format --verify-no-changes` não aponta nada
- [x] 1.3 Atualizar a seção 6 do `CLAUDE.md` com os comandos reais do backend (build, testes, formatação); verificar que cada comando documentado roda como escrito

## 2. Entidade com GUID versão 7

- [x] 2.1 Criar `Entity` no `SharedKernel` com `Id` em GUID versão 7, e o projeto `tests/FiscoProjects.SharedKernel.Tests` com os testes dos cenários "Entidade nova" e "Duas entidades"; verificar com `dotnet test`

## 3. Comportamento comum da API

- [x] 3.1 Criar `AddFiscoProjectsApi` e `UseFiscoProjectsApi` no host, com o endereço `/health`, e o projeto `tests/FiscoProjects.Api.Tests` com o teste do cenário "Aplicação no ar"; verificar com `dotnet test`
- [x] 3.2 Configurar Problem Details para rota inexistente e erro inesperado; testar os cenários "Rota inexistente" e "Erro inesperado numa rota" nos ambientes de desenvolvimento e produção; verificar com `dotnet test`
- [x] 3.3 Publicar o contrato OpenAPI 3.1 só no desenvolvimento; testar os cenários "Contrato no ambiente de desenvolvimento" e "Contrato fora do desenvolvimento"; verificar com `dotnet test`
- [x] 3.4 Criar o grupo de rotas `/api` que o host entrega aos módulos; testar o cenário "Rota de um módulo"; verificar com `dotnet test`

## 4. Regras de arquitetura

- [x] 4.1 Criar o projeto `tests/FiscoProjects.Architecture.Tests` com as regras de camadas, de independência tecnológica, de fronteira entre módulos, de contratos e do núcleo compartilhado, e os exemplos de módulos válidos e inválidos; testar cada cenário de `arquitetura-modular` (exceto os de identificador) nos dois sentidos; verificar com `dotnet test`
- [x] 4.2 Implementar a descoberta de módulos pelo nome do assembly e aplicar as regras aos módulos reais e ao `SharedKernel` real; testar os cenários "Novo módulo segue a convenção de nomes" e "Assemblies que não são módulos"; verificar com `dotnet test`
- [x] 4.3 Escrever `src/Modules/README.md` com a convenção de módulos (projetos, pastas das camadas, contratos e registro no host); verificar que os nomes e regras descritos batem com os testes de arquitetura

## 5. Verificação final

- [x] 5.1 Rodar `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes` e `openspec validate --all --strict` na raiz, todos sem erro

## Workflow follow-up

- Abrir o PR e marcar a Fundação-1 como "PR aberto" no `docs/roadmap.md`.
- Depois da aprovação de Marcelo: `/opsx:archive` na mesma branch, roadmap como "Concluída", merge e remoção da branch.
