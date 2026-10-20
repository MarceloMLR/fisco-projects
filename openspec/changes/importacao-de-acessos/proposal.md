# Proposta: importação de acessos (Documentos-4)

## Why

Os acessos aos portais estão hoje numa planilha com os acessos de todos os clientes. Cadastrar um por um na aba Acessos é trabalho manual e sujeito a erro. O ADR-0034 decidiu a importação da planilha na v1, e o ADR-0045, quem importa:

- **Formatos:** `.xlsx` ou `.csv`.
- **Mapear colunas:** a pessoa diz qual coluna é o portal, o ambiente, o endereço, o usuário, a senha e a observação.
- **Filtrar e marcar:** filtra as linhas (por exemplo, pelo cliente) e importa só as marcadas.
- **Senhas cifradas:** os acessos importados ficam como os cadastrados na tela, com a senha cifrada (ADR-0033).
- **Quem importa:** o administrador, o gerente e o líder técnico (ADR-0045).

## What Changes

- **Leitura da planilha** (ADR-0094, novo):
  - o `.xlsx` pelo Open XML SDK (`DocumentFormat.OpenXml`, MIT) e o `.csv` por um leitor próprio, com o separador e a codificação detectados;
  - `POST /api/projects/{id}/credentials/import/preview` recebe o arquivo e devolve as abas, as colunas (o cabeçalho) e as linhas, sem gravar nada e sem o navegador guardar a resposta;
  - limites de 5 MB e de 2.000 linhas; o arquivo que não é planilha, o `.xls` antigo e a planilha protegida por senha são recusados.
- **Importação:**
  - `POST /api/projects/{id}/credentials/import` recebe os acessos já mapeados e grava todos ou nenhum, com as mesmas regras do cadastro;
  - cada acesso importado entra na auditoria como incluído, com a senha secreta.
- **Na tela:**
  - "Importar planilha" na aba Acessos, para quem pode importar, como no canvas ProjetoAcessos;
  - um diálogo em tela quase inteira: escolher o arquivo, a aba (quando há mais de uma), o mapeamento das colunas (com a sugestão pelo nome do cabeçalho e o ambiente por coluna ou digitado para todas), o filtro das linhas, as linhas marcadas e "Importar N acessos";
  - a linha sem um campo obrigatório ou com um valor inválido mostra o motivo e não se marca;
  - a senha aparece mascarada também na prévia.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `acessos`: importação da planilha.

## Impact

- **Backend:** a leitura de planilhas e a importação no módulo `Credentials`; o pacote `DocumentFormat.OpenXml` 3.5.1.
- **Frontend:** o diálogo de importação na aba Acessos.
- **Documentação:** ADR-0094, CLAUDE.md, glossário e roadmap.

## Non-goals

- O `.xls` antigo e a planilha protegida por senha (ADR-0094).
- Atualizar acessos já cadastrados pela planilha: a importação só inclui.
- Exportar os acessos (fora, ADR-0034).
