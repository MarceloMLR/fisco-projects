## 1. Nome padrão

- [x] 1.1 `Product` no `SharedKernel`, `InstallationSettings.DefaultName` e a migração `ProductName`, com os testes "Nome padrão", "Nome antigo do produto" e "Nome escolhido continua"

## 2. Telas

- [x] 2.1 `pt-BR.json` e `index.html` com o nome novo, e os testes das telas que esperavam o nome antigo
- [x] 2.2 Ícones do pacote do Teams dos arquivos da marca, com o teste "Ícones da marca no pacote"

## 3. E-mails

- [x] 3.1 Topo com a logo ou o nome e rodapé com Meu perfil no `EmailRenderer`, com a logo em `web/public/` e os testes "Logo no topo do e-mail", "Nome da instalação no topo do e-mail", "E-mail sem o endereço do site" e "Rodapé do e-mail"

## 4. Specs e documentação

- [x] 4.1 Nome do produto no texto das specs e nos títulos dos testes que citam os cenários renomeados
- [x] 4.2 `CLAUDE.md` e `docs/roadmap.md`; build sem avisos, testes do backend e do frontend, lint e `openspec validate --all --strict` sem erros

## Workflow follow-up

- Archive na mesma branch depois da aprovação do PR.
