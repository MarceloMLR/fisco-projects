## 1. Decisão

- [x] 1.1 ADR-0161 e o índice dos ADRs

## 2. Backend

- [x] 2.1 Solução, pastas, projetos, namespaces e tipos em `GerenWare`, com as migrações e os instantâneos; os dados gravados pelas migrações antigas continuam como eram
- [x] 2.2 Tipos de erro, cookie, medidores, chave da conexão, pasta do sistema, pasta interna e proteção das senhas com os nomes novos; build sem avisos, `dotnet format` e testes

## 3. Frontend e ferramentas

- [x] 3.1 Contrato e tipos gerados de novo, tipos de erro no frontend, pacotes, `vite.config.ts`, scripts de CI e de publicação, workflows, Codespace e `compose.yaml`
- [x] 3.2 Terraform e scripts de `infra/` no que nomeia código ou o produto em texto, com os testes do pipeline

## 4. Documentação

- [x] 4.1 Nomes de código nas specs, no `CLAUDE.md`, no `openspec/config.yaml`, nos guias e no `src/Modules/README.md`; `docs/roadmap.md`; lint, testes do frontend e `openspec validate --all --strict` sem erros

## Workflow follow-up

- Archive na mesma branch depois da aprovação do PR.
- Depois do merge: aplicar o Terraform no `dev` (ou seguir para a Marca-6, que recria o ambiente) e carregar a demonstração de novo.
