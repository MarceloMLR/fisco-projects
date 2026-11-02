## 1. Decisão e roadmap

- [x] 1.1 ADR-0148, o ADR-0045 apontando para ele e a Documentos-1.1 em andamento no roadmap

## 2. Domínio e API

- [ ] 2.1 `FolderRole`, `FolderViewers`, `FolderVisibility`, as pastas com `VisibleTo`, o `DocumentAccess.SeesFolder` e a migração `FolderVisibility`; testes das regras
- [ ] 2.2 A área pelas pastas que a pessoa vê; as pastas padrão com quem vê; o `ChangeFolderVisibility` com o registro na auditoria; testes "Pasta escondida de um papel", "Documento de uma pasta escondida", "Gestão vê todas as pastas", "Projeto novo recebe quem vê as pastas", "Quem vê a pasta padrão", "Pasta padrão nova vista por todos" e "Pasta de termos padrão fixa"
- [ ] 2.3 As rotas; testes "Administrador muda quem vê a pasta", "Gerente não muda quem vê a pasta", "Quem vê de um projeto de fora", "Pasta de termos fixa", "Quem vê num projeto encerrado" e "Pastas que já existiam"

## 3. Tela

- [ ] 3.1 `npm run api:update`; "Quem vê" nas pastas padrão; testes "Quem vê na pasta padrão pela tela" e "Pastas padrão pela tela"
- [ ] 3.2 "Quem vê esta pasta" e a marca de restrita na aba Documentos; a Auditoria; testes "Quem vê esta pasta pela tela" e "Quem vê esta pasta só do administrador"
- [ ] 3.3 A demonstração com a pasta da gestão escondida no ERP-FIN; teste "Pasta da gestão escondida na demonstração"
- [ ] 3.4 Conferência no navegador, no computador e no celular, nos temas claro e escuro

## 4. Documentação

- [ ] 4.1 `CLAUDE.md` e `docs/glossario.md`
