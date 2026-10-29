## 1. Pacote da versão

- [x] 1.1 `scripts/cd/release.sh` e o workflow `release`
- [x] 1.2 Testes: "Pacote anexado à versão", "Pacote só de versão" e "Pacote sem os ambientes"

## 2. Publicação à mão

- [x] 2.1 `infra/scripts/Publish-Version.ps1`
- [x] 2.2 Testes: "Publicação à mão", "Firewall aberto só nas migrações" e "Publicação sem a identidade da aplicação"

## 3. Guia

- [x] 3.1 `docs/instalacao.md` como guia de instalação, com os testes "Cada script no guia" e "Guia do começo ao fim"
- [x] 3.2 ADR-0127, `CLAUDE.md`, roadmap (Operação-6) e glossário
- [ ] 3.3 Build sem avisos, testes do pipeline, `dotnet format` e `openspec validate --all --strict`

## Workflow follow-up

- PR, aprovação de Marcelo, archive na mesma branch e merge por Marcelo.
