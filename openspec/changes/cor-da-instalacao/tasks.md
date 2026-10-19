# Tasks

## 1. Backend

- [x] 1.1 No módulo `Administration`:
  - `MainColor` em `InstallationSettings`, com `ChangeMainColor` (formato `#RRGGBB` e contraste de 4,5:1 com o branco) e a migração `InstallationMainColor`;
  - a cor no `UpdateInstallation`;
  - `mainColor` em `GET /api/installation` e no `PUT`.

  Testar a cor padrão, a cor aceita, a minúscula guardada em maiúsculas, o formato inválido, a cor clara recusada, a volta ao padrão e "Troca da cor na auditoria". Atualizar o contrato com `npm run api:update` e verificar com `dotnet test`.

## 2. Frontend

- [x] 2.1 Criar `web/src/theme/brandColors.ts` (conversão OKLCH, derivação, ajuste e conferência de contraste e folha de estilo) e o `BrandColors` na rota raiz. Testar:
  - "Cor padrão" (devolve o `tokens.json`);
  - "Contraste nos dois temas" para várias cores;
  - as cores de status iguais e os tokens sempre hexadecimais válidos;
  - "Trocar a cor principal" na estrutura e na entrada.

  Verificar com `npm test`.
- [x] 2.2 Na seção Marca das Configurações gerais, criar a cor principal (amostra, campo, "Voltar ao padrão" e selo de contraste), com a Prévia na cor digitada e "Cor principal" na Auditoria. Testar:
  - "Prévia antes de salvar" com a cor;
  - "Cor sem contraste";
  - "Voltar ao padrão";
  - "Descartar alterações" com a cor.

  Verificar com `npm test` e `npm run lint`.

## 3. Verificação final

- [x] 3.1 Conferir no navegador, com o build de produção, com uma cor verde e uma vinho:
  - a estrutura, a entrada e as Configurações gerais, nos temas claro e escuro;
  - a recusa de um amarelo;
  - a volta ao padrão.

  Mandar as capturas para Marcelo.
- [ ] 3.2 Documentação e verificações:
  - ADR-0082 na lista;
  - atualizar o glossário (cor principal), o `CLAUDE.md` e o roadmap;
  - rodar todos os scripts de `scripts/ci` sem erro;
  - abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions.

## Workflow follow-up

- Marcar a Acesso-8 como "PR aberto" no roadmap.
- Depois do merge da Acesso-7 e da aprovação de Marcelo: `/opsx:archive` na mesma branch, ADR-0082 como "aceita", roadmap como "Concluída" e merge.
