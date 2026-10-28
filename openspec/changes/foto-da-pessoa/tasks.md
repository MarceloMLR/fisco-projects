## 1. Servidor

- [x] 1.1 `IUserStore.IdentitiesAsync`, `IPersonPhotos`, `GetPersonPhoto` e o endpoint `GET /api/people/{id}/photo`
- [x] 1.2 `GraphPersonPhotos` e `PersonPhotoCache`, com `Photos` no `FakeGraph` e os testes da API ("Foto do Microsoft 365", "Foto só no tamanho original", "Pessoa sem foto", "Foto de quem foi anonimizado", "Microsoft 365 fora do ar na foto", "Foto sem sessão")

## 2. Telas

- [x] 2.1 `photo` no `Avatar` e `photoOf`, com os testes "Avatar com a foto" e "Avatar sem foto"
- [x] 2.2 A foto no `CardAvatar`, no cartão, no menu da conta, em Meu perfil, no detalhe em Membros e na menção

## 3. Documentação e conferência

- [x] 3.1 ADR-0124, `CLAUDE.md`, roadmap (M365-5) e glossário
- [x] 3.2 Build sem avisos, testes, `dotnet format`, lint, typecheck e `openspec validate --all --strict`

## Workflow follow-up

- PR, aprovação de Marcelo, archive na mesma branch e merge por Marcelo.
