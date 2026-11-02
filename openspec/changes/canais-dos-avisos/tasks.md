## 1. Decisão e roadmap

- [x] 1.1 ADR-0151, os ADR-0037 e 0149 apontando para ele, o índice dos ADRs e a Avisos-5 no roadmap; `openspec validate --all --strict` sem erros

## 2. API

- [ ] 2.1 A `NoticePreference` com `ByEmail` e `ByTeams`, o padrão e a migração das escolhas gravadas; a rota pelos dois campos; testes "Só por e-mail", "Só pelo Teams", "Pelos dois", "Pessoa sem o Entra ID", "Sino com qualquer canal", "Canais de quem nunca escolheu", "Os dois canais desligados", "Teams ligado sem o Teams disponível" e "Teams desligado na instalação"
- [ ] 2.2 A fila do Teams sem a volta ao e-mail; teste "Teams sem volta ao e-mail" e os testes da volta ao e-mail retirados
- [ ] 2.3 `GET` e `PUT /api/profile/immediate-notices` com `email` e `teams`; a preferência e o resumo diário na auditoria; testes "E-mail desligado na auditoria", "Teams ligado na auditoria", "Resumo diário na auditoria" e "Escolha sem mudança fora da auditoria"

## 3. Telas

- [ ] 3.1 `npm run api:update`; o cartão Avisos com "Por e-mail", "Pelo Teams" e o resumo diário; testes de tela "Desligar o resumo", "Desligar o e-mail", "Escolher o Teams", "Canal de quem nunca escolheu", "Ajuda do aplicativo do Teams", "Teams fora do cartão" e "Falha ao gravar"
- [ ] 3.2 Os textos da Auditoria para as três escolhas (`describeAuditEntry`), com os testes
- [ ] 3.3 Conferência no navegador do cartão Avisos e da Auditoria, no computador e no celular, nos temas claro e escuro

## 4. Documentação

- [ ] 4.1 `CLAUDE.md` e `docs/instalacao.md`
