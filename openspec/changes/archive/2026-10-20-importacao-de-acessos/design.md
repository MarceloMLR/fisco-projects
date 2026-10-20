# Design: importação de acessos (Documentos-4)

## Context

- **ADR-0034:** importação de `.xlsx` ou `.csv`, mapeando as colunas, filtrando as linhas e importando só as marcadas.
- **ADR-0045:** só o administrador, o gerente e o líder técnico importam.
- **ADR-0094 (novo):** o `.xlsx` pelo Open XML SDK e o `.csv` por um leitor próprio.
- **O que já existe (Documentos-3):**
  - o módulo `Credentials`, com o cadastro, a senha cifrada pelo `IPasswordProtector` e as regras de cada campo;
  - o `CredentialAccess`, com quem exclui (os mesmos que importam);
  - a aba Acessos, com o botão "Importar planilha" previsto no canvas ProjetoAcessos.
- **Canvas:** não há tela da importação; o diálogo segue o design system (fp-dialog em tela quase inteira, como o Expandir do cronograma) e a tabela padrão.

## Goals / Non-Goals

**Goals:**
- ler `.xlsx` e `.csv` com segurança;
- mapear, filtrar, marcar e importar, com tudo ou nada;
- as mesmas regras do cadastro, num lugar só.

**Non-Goals:**
- `.xls`, planilha protegida por senha e atualização de acessos existentes.

## Decisions

### 1. Duas chamadas, sem guardar o arquivo

- **Prévia:** o servidor lê o arquivo e devolve as abas, o cabeçalho e as linhas como texto. Nada é gravado, nem o arquivo: a planilha tem senhas em texto puro e não fica no servidor.
- **Importação:** o navegador manda os acessos já mapeados (portal, ambiente, endereço, usuário, senha e observação, com o número da linha na planilha). O servidor valida todos com as regras do cadastro e grava todos numa transação, ou nenhum.
- **Por quê:** o servidor não guarda a planilha entre as duas chamadas, e o mapeamento, o filtro e as marcas ficam só na tela.
- **Respostas:** a prévia sai com `Cache-Control: no-store`, como a senha de um acesso.

### 2. Leitura (`SpreadsheetReader`, Infrastructure)

- **Tipo pelo conteúdo e pela extensão:** `.xlsx` precisa começar como um ZIP; `.csv`, como texto. Outro arquivo é recusado.
- **`.xlsx`:**
  - `SpreadsheetDocument.Open` só leitura, com `OpenSettings.MaxCharactersInPart` (proteção contra arquivo que se expande demais);
  - as abas na ordem do arquivo; a escolhida (a primeira, se nenhuma) é lida linha a linha pelas referências das células;
  - textos compartilhados, textos em linha, números (sem formatação regional; inteiros sem casas), verdadeiro e falso e o valor guardado das fórmulas;
  - arquivo corrompido, protegido por senha (não é ZIP) ou `.xls` antigo: recusado como ilegível.
- **`.csv`:**
  - codificação pelo BOM (UTF-8 ou UTF-16); sem BOM, UTF-8 se válido, senão Windows-1252;
  - separador pelo primeiro registro: o mais frequente entre ponto e vírgula, vírgula e tabulação, fora das aspas;
  - campos entre aspas com aspas duplicadas e quebras de linha dentro.
- **Linhas:** a primeira linha com algum valor é o cabeçalho; as vazias ficam de fora; os valores sem espaços nas pontas; o cabeçalho vazio vira "Coluna C" (a letra da coluna); o número de cada linha é o da planilha.
- **Limites:** 5 MB no arquivo, 2.000 linhas e 50 colunas. Acima disso, recusado.

### 3. API

- `POST /api/projects/{id}/credentials/import/preview` (multipart: `file` e `sheet` opcional):
  - responde `{ sheets, sheet, columns, rows: [{ number, cells }] }`;
  - 400 com `credential-import-refused` e o motivo (`type`, `unreadable`, `empty`, `too-large`, `too-many-rows`);
  - 403 de quem não importa, 404 de quem não vê o projeto e 409 no projeto encerrado.
- `POST /api/projects/{id}/credentials/import` (JSON `{ items: [{ row, portal, environment, url, username, password, notes }] }`):
  - de 1 a 2.000 itens; valida cada um com as regras do cadastro (as mesmas funções da Documentos-3);
  - algum inválido: 400 com `credential-import-invalid` e as linhas inválidas (`rows`), sem gravar nada;
  - responde `{ imported }`.
- **Permissão:** `CredentialAccess.CanImport`, igual ao excluir (ADR-0045).

### 4. Tela (`CredentialImportDialog`)

- **Abrir:** "Importar planilha" aparece para quem importa; abre um `Dialog` com `size="full"`.
- **Arquivo:** escolher ou arrastar um `.xlsx` ou `.csv`; a recusa vira a mensagem do motivo.
- **Aba:** quando há mais de uma, um seletor; trocar lê a outra aba.
- **Mapeamento:** seis seletores (Portal, Ambiente, Endereço, Usuário, Senha e Observação), cada um com as colunas da planilha e "Não importar" nos opcionais:
  - sugeridos pelo nome do cabeçalho (portal ou sistema; ambiente; endereço, URL ou link; usuário, login ou e-mail; senha; observação ou obs);
  - o Ambiente também aceita "Mesmo para todas", com um campo de texto.
- **Linhas:**
  - a tabela padrão (`DataTable`) mostra as linhas como ficariam (portal, ambiente, endereço, usuário, senha mascarada e observação), com o número da linha e o problema, se houver;
  - o filtro procura o texto em qualquer célula da linha (por exemplo, o cliente);
  - nenhuma linha começa marcada; a marca do cabeçalho marca as filtradas válidas;
  - a linha com problema ("Sem portal", "Endereço inválido", "Senha muito longa") não se marca;
  - as regras de cada campo são as do diálogo de acesso (o mesmo esquema Zod).
- **Importar:** "Importar N acessos"; ao terminar, a mensagem "N acessos importados", o diálogo fecha e a lista atualiza.
- **Celular:** o diálogo ocupa a tela; a tabela rola por dentro.

## Risks / Trade-offs

- **[Senhas no navegador durante a importação]** A prévia leva as senhas da planilha até a tela. Mitigação: a resposta não é guardada pelo navegador nem pelo cache das consultas, e a senha aparece mascarada.
- **[Acessos repetidos]** Importar a mesma planilha duas vezes repete os acessos. Mitigação: as linhas começam desmarcadas e a pessoa escolhe o que importa; quem importa também exclui.
