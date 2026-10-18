# Tasks

## 1. Frontend básico

- [x] 1.1 Criar `web/` com Vite, React, TypeScript estrito, ESLint, Prettier, Vitest e Testing Library (versões exatas, lockfile, `engines` com o Node 24) e a página provisória; verificar que `npm run lint`, `npm run typecheck`, `npm test` e `npm run build` passam
- [x] 1.2 Configurar o react-i18next com o `pt-BR.json`, as chaves tipadas e a regra de lint de textos; testar "Texto da tela vem da tradução", "Texto escrito direto na tela" e "Chave de tradução inexistente"; verificar com `npm test` e `npm run typecheck`

## 2. Host serve o frontend

- [x] 2.1 Servir o build no host (arquivos estáticos com cache, página principal para os caminhos de tela e 404 em Problem Details sob `/api`), com o build do Vite indo para o `wwwroot` e o proxy do Vite no desenvolvimento; testar "Página inicial", "Rota do frontend", "Arquivo do build" e "Rota inexistente"; verificar com `dotnet test` e abrindo a aplicação pelo host

## 3. Contrato e tipos

- [x] 3.1 Gerar o contrato no build do host sob demanda e os tipos com o openapi-typescript (`npm run api:update`); testar "Contrato versionado desatualizado" e "Tipos do frontend desatualizados"; verificar que os dois testes reprovam com os arquivos alterados, sem versionar a quebra

## 4. Verificação no PR

- [x] 4.1 Criar `scripts/ci/frontend.sh` e a etapa `frontend` no workflow; atualizar os testes do pipeline para "Todas as verificações no pull request", "Pacote do frontend com vulnerabilidade conhecida" e "Dependência do frontend com licença não permitida"; verificar que o script passa e falha com uma licença proibida e um erro de lint, sem versionar a quebra

## 5. Documentação e verificação final

- [x] 5.1 Atualizar a seção 6 do `CLAUDE.md`, o `.editorconfig`, o `cspell.json` e as extensões recomendadas do VS Code; verificar que os comandos documentados rodam como escritos
- [ ] 5.2 Rodar todos os scripts de `scripts/ci` sem erro, abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions

## Workflow follow-up

- Marcar a Fundação-4 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: ADR-0076 como aceito, `/opsx:archive`, roadmap como "Concluída" e merge.
