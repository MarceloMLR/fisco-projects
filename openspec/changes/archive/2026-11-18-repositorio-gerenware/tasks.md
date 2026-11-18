## 1. Decisão

- [x] 1.1 ADR-0163, o índice dos ADRs e a nota no discovery

## 2. Código

- [x] 2.1 `github_repository` nos dois ambientes e o repositório padrão do `Set-GitHubEnvironment.ps1`, com os testes do pipeline

## 3. Documentação

- [x] 3.1 `docs/roadmap.md`; `openspec validate --all --strict` sem erros

## Workflow follow-up

- Archive na mesma branch depois da aprovação do PR.
- Depois do merge, Marcelo renomeia o repositório para `gerenware` (Settings → General → Repository name) e aplica o Terraform dos dois ambientes, no roteiro da Marca-6, antes de publicar.
- Em cada clone: `git remote set-url origin https://github.com/MarceloMLR/gerenware.git`.
