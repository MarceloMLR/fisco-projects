## 1. Decisão e roadmap

- [ ] 1.1 ADR-0150, os ADR-0121 e 0146 apontando para ele, o índice dos ADRs e a Acesso-8.6 em andamento no roadmap; `openspec validate --all --strict` sem erros

## 2. API

- [ ] 2.1 O `UpdateInstallation` e o `PUT /api/installation` só com as Regras gerais; `RenameInstallation` com `PUT /api/installation/name` e `ChangeProjectsFolder` com `PUT /api/installation/projects-folder`, só do administrador; testes da API "Regras sem mudar a marca", "Nome sem mudar as regras", "Pasta dos projetos sem mudar o resto" e os de permissão e de validação das rotas novas

## 3. Telas

- [ ] 3.1 `npm run api:update`; as Configurações gerais só com as Regras gerais e o Microsoft 365, numa coluna; testes de tela "Configurações gerais na Administração" e "Descartar alterações"
- [ ] 3.2 A seção Visual (`VisualPage`, `/admin/visual`) com a Marca e as cores, a prévia com o nome e os logos digitados e o endereço de Cores levando a ela; testes de tela "Visual na Administração", "Endereço antigo de Cores", "Prévia da marca antes de salvar", "Salvar a marca e as cores", "Salvar as cores" e "Descartar as cores"
- [ ] 3.3 A pasta dos projetos em Pastas padrão, com o `ProjectsFolderDialog`; testes de tela "Pasta dos projetos padrão", "Alterar a pasta dos projetos pela tela" e "Pasta dos projetos inválida"
- [ ] 3.4 Conferência no navegador das Configurações gerais, de Visual e de Pastas padrão, no computador e no celular, nos temas claro e escuro

## 4. Documentação

- [ ] 4.1 `CLAUDE.md`, `docs/instalacao.md` e `docs/glossario.md`
