---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0099: Relatório da auditoria em Excel, montado na tela e registrado na auditoria

Complementa o ADR-0039 (auditoria automática), o ADR-0046 (Administração só do administrador) e o ADR-0058 (dados pessoais).

## Contexto e problema

A tela Auditoria mostra os registros 25 por página, com os filtros de pessoa, tipo e período. Marcelo pediu um botão para baixar um relatório da auditoria em Excel, com o período, o tipo e as pessoas, formatado e legível. O relatório tira dados pessoais do sistema (nomes, e-mails, identidades das entradas recusadas), então quem baixa e o que fica registrado é uma decisão de LGPD. E os textos que tornam um registro legível ("Trocou um papel", "Beatriz Lima · Usuário → Gerente de projeto") são montados na tela, a partir dos textos de tradução, pelo `describeAuditEntry` (`web/src/admin/auditText.ts`). Onde montar a planilha, e com quais regras?

## Fatores de decisão

- O relatório diz o mesmo que a tela, com os mesmos textos, sem duas versões das regras de texto (seção 5 do CLAUDE.md).
- Só o administrador tira os dados da auditoria, e a saída fica registrada (ADR-0046, ADR-0058).
- Nenhum valor secreto sai (ADR-0033, ADR-0039).
- Sem dependência nova com licença ou vulnerabilidades a acompanhar (ADR-0050, ADR-0061).

## Opções consideradas

- A. Planilha montada no servidor (Open XML SDK, já usado na importação dos acessos), com os textos da auditoria refeitos em C#
- B. Planilha montada na tela, com um gerador próprio de `.xlsx` (o formato é um `.zip` de arquivos XML), a partir dos registros que a API entrega numa exportação registrada
- C. Planilha montada na tela com uma biblioteca de planilhas (exceljs, SheetJS)

## Resultado da decisão

Opção escolhida: **B**, porque o relatório usa os mesmos textos da tela sem repeti-los no servidor, e o gerador próprio cobre o que o relatório precisa (cabeçalho fixo, filtro, larguras, quebra de linha, data e hora como data do Excel) sem uma dependência a mais no navegador.

Regras:

- Só o administrador exporta (`POST /api/audit/exports`, política `Administrator`), com o período (obrigatório, até 366 dias), o tipo e a pessoa opcionais, os mesmos filtros da tela.
- A API devolve todos os registros do filtro, do mais novo para o mais antigo, até 10.000; acima disso responde 400 e a tela pede um período menor.
- Toda exportação vira um registro da auditoria, na mesma requisição: "Exportou a auditoria", com o período, o tipo, a pessoa e quantos registros saíram. A exportação aparece na própria tela e em exportações seguintes.
- Valor secreto nunca sai: a auditoria já não guarda o valor das senhas, e a planilha mostra só "valor protegido", como a tela.
- A planilha tem a aba Auditoria (data e hora no fuso da instalação, quem, o que, o item, os detalhes e as alterações, com "antes → depois") e a aba Filtros (o período, o tipo, a pessoa, quantos registros, quando e por quem foi gerada). As cores vêm dos tokens do design system.
- O arquivo se chama `auditoria-{início}-a-{fim}.xlsx`, e nada fica guardado no servidor.

### Consequências

- Bom, porque a planilha diz exatamente o que a tela diz.
- Bom, porque cada saída de dados da auditoria fica registrada, com quem, quando e o quê.
- Ruim, porque o gerador de `.xlsx` é código nosso a manter, ainda que pequeno e testado.
- Ruim, porque um período muito movimentado precisa ser dividido em mais de uma exportação.

### Confirmação

Testes cobrindo: só o administrador exporta (403 para os outros); a exportação devolve os registros do filtro e grava o registro "Exportou a auditoria"; período acima do limite e excesso de registros dão 400; a planilha gerada abre como `.xlsx` válido, com as duas abas, o cabeçalho e as linhas.

## Prós e contras das opções

### A. Planilha no servidor

- Bom, porque o servidor garante que o arquivo é o que foi registrado.
- Ruim, porque os textos da auditoria (mais de mil linhas na tela) teriam de ser refeitos em C# e mantidos iguais.

### B. Planilha na tela, gerador próprio

- Bom, porque reusa os textos e não traz dependência.
- Ruim, porque o registro diz o filtro exportado, não guarda o arquivo.

### C. Planilha na tela, com biblioteca

- Bom, porque a biblioteca cobre mais recursos do formato.
- Ruim, porque traz centenas de KB e dependências com vulnerabilidades e licenças a acompanhar, para um uso pequeno.
