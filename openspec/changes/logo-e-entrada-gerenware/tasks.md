## 1. Decisão

- [x] 1.1 ADR-0160, a nota no ADR-0108 e o índice dos ADRs

## 2. Marca nas telas

- [x] 2.1 `web/src/assets/brand/` e `web/public/` com as cópias de `docs/marca/`, `brand.ts` e `ProductMark`, com os testes "Arquivos iguais aos da marca", "Logo inteira com o nome do produto" e "Símbolo ao lado de outro nome"
- [x] 2.2 `Sidebar`, `SignInPage` e `ColorsPreview` com a marca do produto no lugar do selo, com os testes da barra lateral clara e escura e da entrada ("Logo para o fundo escuro")
- [x] 2.3 Frase e texto de apoio da página de entrada como no manual ("Frase da página de entrada")

## 3. Aba e celular

- [x] 3.1 `index.html` com o ícone da aba, o do celular e o manifesto; `favicon.ts` sem o selo desenhado; testes "Símbolo na aba sem logo", "Logo no lugar do ícone da página" e "Ícones do navegador e do celular"

## 4. Documentação

- [x] 4.1 Nota de uso de `brand-panel-accent` no `tokens.json` e no design system; `CLAUDE.md` e `docs/roadmap.md`; lint, tipos, testes do frontend, build e `openspec validate --all --strict` sem erros

## Workflow follow-up

- Archive na mesma branch depois da aprovação do PR.
