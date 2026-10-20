## ADDED Requirements

### Requirement: Leitura da planilha de acessos

Quem importa acessos (ADR-0045) MUST enviar uma planilha `.xlsx` ou `.csv` de até 5 MB e 2.000 linhas e receber as abas, o cabeçalho e as linhas como texto, sem nada gravado. A primeira linha com valor MUST ser o cabeçalho, e as linhas vazias MUST ficar de fora. O CSV MUST ser lido com o separador e a codificação do Excel em português. O arquivo que não é planilha, o `.xls` antigo e a planilha protegida por senha MUST ser recusados (ADR-0034, ADR-0094).

#### Scenario: Ler planilha xlsx
- **WHEN** o gerente envia um `.xlsx` com o cabeçalho na segunda linha, textos, números e células vazias
- **THEN** a resposta traz o cabeçalho e as linhas como texto, com o número de cada linha na planilha, e nada é gravado

#### Scenario: Ler planilha csv do Excel
- **WHEN** o gerente envia um `.csv` com ponto e vírgula, em Windows-1252, com um campo entre aspas que tem quebra de linha
- **THEN** a resposta traz os valores com os acentos certos e o campo inteiro numa célula

#### Scenario: Escolher a aba
- **WHEN** o gerente envia um `.xlsx` com duas abas e escolhe a segunda
- **THEN** a resposta traz as duas abas e as linhas da segunda

#### Scenario: Arquivo que não é planilha
- **WHEN** alguém envia um PDF ou um `.xlsx` corrompido
- **THEN** a API responde 400 com o motivo, e nada é gravado

### Requirement: Importação dos acessos

Quem importa acessos MUST incluir de uma vez os acessos mapeados da planilha, com as mesmas regras do cadastro, todos ou nenhum. A senha MUST ser cifrada como no cadastro, e cada acesso MUST entrar na auditoria como incluído. Os demais papéis MUST receber 403, quem não está no projeto 404 e o projeto encerrado 409 (ADR-0033, ADR-0034, ADR-0045).

#### Scenario: Importar acessos
- **WHEN** o líder técnico importa três acessos mapeados
- **THEN** os três aparecem na aba Acessos, as senhas não aparecem em texto puro na tabela e a auditoria registra as três inclusões

#### Scenario: Importação com linha inválida
- **WHEN** um dos acessos enviados não tem usuário
- **THEN** a API responde 400 com a linha inválida, e nenhum acesso é gravado

#### Scenario: Só gestores importam
- **WHEN** o colaborador, o desenvolvedor ou o leitor tenta ler ou importar uma planilha
- **THEN** a API responde 403

#### Scenario: Importação em projeto encerrado
- **WHEN** o gerente tenta importar num projeto concluído
- **THEN** a API responde 409, e nada é gravado

### Requirement: Importação pela tela

A aba Acessos MUST ter "Importar planilha" para quem importa, com um diálogo para escolher o arquivo e a aba, mapear as colunas (com a sugestão pelo cabeçalho e o ambiente por coluna ou digitado), filtrar as linhas, marcar as linhas e importar só as marcadas. A linha com problema MUST mostrar o motivo e não se marcar, e a senha MUST aparecer mascarada (ADR-0034, ADR-0065).

#### Scenario: Mapear e importar pela tela
- **WHEN** o gerente envia a planilha, confere o mapeamento sugerido, filtra pelo cliente, marca as linhas filtradas e escolhe "Importar 2 acessos"
- **THEN** só as duas linhas marcadas vão para a API, a tela diz que 2 acessos foram importados e eles aparecem na lista

#### Scenario: Ambiente digitado para todas
- **WHEN** a planilha não tem a coluna do ambiente e o gerente escolhe "Mesmo para todas" com "Produção"
- **THEN** os acessos importados ficam no ambiente Produção

#### Scenario: Linha incompleta não se marca
- **WHEN** uma linha da planilha não tem o usuário
- **THEN** a linha mostra "Sem usuário" e não pode ser marcada

#### Scenario: Importar só para quem pode
- **WHEN** o colaborador abre a aba Acessos
- **THEN** "Importar planilha" não aparece
