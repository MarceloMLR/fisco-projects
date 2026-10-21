## Why

A tela Auditoria mostra os registros 25 por página. Marcelo pediu um botão para baixar um relatório da auditoria em Excel, com o período, o tipo e as pessoas, formatado e legível, e decidiu que só o administrador baixa e que cada relatório fica registrado na própria auditoria (Acesso-3.2, a pedido de Marcelo, ADR-0039, ADR-0046, ADR-0058).

## What Changes

- ADR-0099: a planilha é montada na tela, com os mesmos textos da lista e dos detalhes, por um gerador próprio de `.xlsx`, a partir dos registros que a API entrega numa exportação registrada; só o administrador exporta; valor secreto nunca sai.
- API: `POST /api/audit/exports` com o período em dias da instalação (obrigatório, até 366 dias), o tipo e a pessoa opcionais; devolve todos os registros do filtro, do mais novo para o mais antigo, até 10.000 (acima, 400 com o total), e grava na mesma requisição o registro "Exportou a auditoria" com os filtros e a quantidade.
- Tela: "Baixar planilha" na barra da Auditoria, com os filtros da tela. A planilha tem a aba Auditoria (data e hora no fuso da instalação, quem, o que, o item, os detalhes e as alterações com antes e depois), com o cabeçalho fixo, o filtro do Excel e as cores do design system, e a aba Filtros (o período, o tipo, a pessoa, os registros, quando e por quem foi gerada).
- O registro da exportação aparece na lista como "Exportou a auditoria", com o tipo "Exportação da auditoria" no filtro.

## Capabilities

### New Capabilities

### Modified Capabilities
- `auditoria`: o relatório da auditoria em Excel, só do administrador e registrado.

## Impact

- `Audit` (domínio, caso de uso `ExportAuditEntries`, rota), `Administration.Contracts` (o início de um dia no fuso da instalação, `InstallationInfo.StartOf`), tela Auditoria (`web/src/admin`), gerador de planilha (`web/src/format/xlsx.ts`), textos e testes.
- Sem tabela nova nem migração: a ação nova é texto na coluna existente.
