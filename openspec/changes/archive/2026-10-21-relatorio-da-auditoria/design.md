## Context

A auditoria (`Audit`) só é lida pelo administrador, 25 a 100 registros por página (`ListAuditEntries`), e os textos legíveis de cada registro (título, resumo, detalhes e alterações) são montados na tela pelo `describeAuditEntry` (`web/src/admin/auditText.ts`), com os textos de tradução. O ADR-0099 decide montar a planilha na tela, com um gerador próprio, e registrar cada exportação.

## Goals / Non-Goals

**Goals:**
- Uma planilha legível, com os mesmos textos da tela, do filtro escolhido.
- Toda exportação registrada na auditoria, com quem, quando e o quê.

**Non-Goals:**
- Guardar o arquivo no servidor ou mandar por e-mail.
- Exportar para PDF ou CSV.
- Agendar relatórios.

## Decisions

- **Rota**: `POST /api/audit/exports` (política `Administrator`, com `X-CSRF`), corpo `{ from, to, itemType?, actorId? }`, com `from` e `to` como dias da instalação, inclusive. O servidor converte os dias em instantes pelo fuso da instalação (`InstallationInfo.StartOf`, novo no `Administration.Contracts`, para ninguém repetir a conta). POST porque a exportação grava o registro; a resposta sai com `no-store`.
- **Validação**: período obrigatório, início até o fim, no máximo 366 dias (400 com os campos). Até 10.000 registros (`ExportAuditEntries.MaxRows`); acima, 400 com o tipo `urn:fiscoprojects:problem:audit-export-too-large` e o total, e nada é gravado.
- **Registro**: depois de ler, o `ExportAuditEntries` grava pelo `IAuditStore.AddAsync` o registro com a ação nova `Exported`, o tipo `AuditExport`, o autor e os campos `From`, `To`, `ItemType` e `Person` (referência `User`, ADR-0084), quando há, e `Rows`. O registro não entra na própria exportação.
- **Os registros** saem com os nomes atuais dos autores e das referências, como na lista: o `ListAuditEntries` ganha `ViewsAsync`, usado pelas duas rotas.
- **Gerador** (`web/src/format/xlsx.ts`): monta o `.xlsx` (Office Open XML) com abas, textos, números e datas, estilos (cabeçalho em destaque, quebra de linha, bordas), larguras, cabeçalho fixo e o filtro do Excel; o `.zip` sai com `CompressionStream('deflate-raw')`, que os navegadores e o Node têm, e o CRC-32 calculado no próprio arquivo. As cores vêm do `tokens.json` (tema claro), sem cor escrita no código.
- **Planilha** (`web/src/admin/auditReport.ts`): aba "Auditoria" com Data e hora (data do Excel, no fuso da instalação, formato `dd/mm/aaaa hh:mm`), Quem, O que, Item, Detalhes (rótulo: valor, um por linha) e Alterações (campo: antes → depois, um por linha); aba "Filtros" com o período, o tipo, a pessoa, os registros, gerada em e gerada por. Nome do arquivo `auditoria-{início}-a-{fim}.xlsx`.
- **Tela**: "Baixar planilha" (ícone de download, texto) na barra de ferramentas da `DataTable` da Auditoria, com os filtros da tela; enquanto prepara, o botão fica desabilitado com "Preparando…"; erros no `Snackbar` padrão (registros demais pede um período menor, com o total).
- **Texto do registro**: "Exportou a auditoria", resumo "{n} registros · {início} a {fim}", detalhes com período, tipo, pessoa e registros.

## Risks / Trade-offs

- [O registro diz o filtro, não guarda o arquivo] → o filtro e a quantidade bastam para saber o que saiu; o arquivo é refeito com o mesmo filtro.
- [Gerador próprio] → coberto por testes que abrem o arquivo (as partes do `.zip` e o XML de cada aba).
- [10.000 registros por vez] → a tela pede um período menor, com o total.
