## 1. Scripts

- [ ] 1.1 `scripts/ci/changes.sh`: base pelo `CHANGES_BASE`, pelo último commit aprovado do PR (`gh api`) ou pela `main`; diferença, regra de documentação e saída `code=`; testes "Archive só com documentação", "Código depois de um commit aprovado" e "Commit aprovado anterior a uma falha" com repositórios de exemplo e um `gh` falso
- [ ] 1.2 `scripts/ci/verdict.sh` com o resultado de cada job; teste "Parte pulada sem motivo" e "Todas as verificações no pull request" para as combinações

## 2. Workflow

- [ ] 2.1 Jobs `changes`, `backend`, `format`, `frontend`, `specs` e `verify`, com as permissões de leitura e os caches; testes do pipeline "Todas as verificações no pull request", "Sem segredos", "Versões fixadas" e "Pacotes em cache"

## 3. Documentação e conferência

- [ ] 3.1 ADR-0090 e ADR-0055 marcado como substituído em parte; `CLAUDE.md` (verificação do PR) e roadmap com a Fundação-3.2
- [ ] 3.2 Build, testes, format e specs na máquina; no PR, conferir a verificação completa num commit com código e só as specs num commit só de documentação, com os tempos
