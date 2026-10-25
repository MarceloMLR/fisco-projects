## 1. Decisão

- [x] 1.1 ADR-0115 (publicação no ambiente de desenvolvimento, apply com quem opera)

## 2. Identidade do pipeline

- [x] 2.1 Módulo `ci` do Terraform: aplicativo, federação do environment do GitHub, papéis e leitura do diretório
- [x] 2.2 `github_repository` no `dev`, e o e-mail dos alertas sensível
- [x] 2.3 `infra/scripts/Set-GitHubEnvironment.ps1`, aplicado no GitHub de Marcelo

## 3. Publicação

- [x] 3.1 `scripts/cd/publish.sh`, `deploy.sh` e `plan.sh`, e o Terraform fixado em `scripts/ci/terraform.sh`
- [x] 3.2 Workflow `deploy-dev`
- [x] 3.3 Primeira publicação conferida no Azure: `/health`, entrada local e dados de demonstração

## 4. Licenças do NuGet

- [x] 4.1 `scripts/ci/licenses.sh` no job `backend`

## 5. Testes e documentação

- [x] 5.1 Testes do workflow, dos scripts de publicação, das licenças e da identidade do pipeline
- [x] 5.2 `docs/instalacao.md`, `CLAUDE.md`, glossário e roadmap
