# acessos Specification

## Purpose

Acessos aos portais dos clientes em cada projeto: o portal, o ambiente, o endereço, o usuário e a senha cifrada e mascarada, mostrada só quando pedida, a busca e o filtro por ambiente e a importação de uma planilha, com as permissões de cada papel (ADR-0033, ADR-0034, ADR-0045, ADR-0094).

## Requirements

### Requirement: Acessos do projeto

Quem cadastra acessos (ADR-0045) MUST incluir e editar acessos do projeto, com o portal (até 120 caracteres), o ambiente (até 60), o endereço opcional (http ou https, até 2.000), o usuário (até 200), a senha (até 500) e a observação opcional (até 500). Na edição, a senha em branco MUST ficar como estava. O acesso MUST guardar quem incluiu e quando (ADR-0034).

#### Scenario: Incluir acesso
- **WHEN** a colaboradora inclui o acesso "ERP" de "Homologação", com o usuário `consultoria.fiscal` e a senha
- **THEN** a aba Acessos mostra o acesso no grupo Homologação, com a senha mascarada

#### Scenario: Editar sem trocar a senha
- **WHEN** alguém muda a observação de um acesso e deixa a senha em branco
- **THEN** a observação muda, e a senha continua a mesma

#### Scenario: Acesso sem portal
- **WHEN** alguém inclui um acesso sem o portal ou sem o usuário
- **THEN** a API responde 400 e nada é gravado

### Requirement: Senha cifrada

A senha MUST ser cifrada com o Data Protection, num propósito só dos acessos, com o conjunto de chaves guardado no banco e nunca apagado, e MUST nunca aparecer em texto puro no banco. A senha cifrada com uma chave antiga ou revogada MUST continuar legível. Com o certificado configurado em `Credentials:KeyCertificate`, o conjunto de chaves MUST ficar cifrado; sem ele, o sistema MUST funcionar normalmente (ADR-0033).

#### Scenario: Senha nunca em texto puro no banco
- **WHEN** alguém inclui um acesso com a senha `Kx7#pa2V!q`
- **THEN** a tabela dos acessos não tem `Kx7#pa2V!q`, e a API devolve a senha certa

#### Scenario: Trocar a chave e ler a senha
- **WHEN** uma chave nova passa a valer depois que a senha foi cifrada
- **THEN** a senha antiga continua legível, e a senha nova sai com a chave nova

#### Scenario: Senha com chave revogada
- **WHEN** todas as chaves são revogadas depois que a senha foi cifrada
- **THEN** a senha continua legível

#### Scenario: Chaves cifradas pelo certificado
- **WHEN** a instalação configura o certificado das chaves
- **THEN** o conjunto de chaves guardado no banco fica cifrado, e as senhas continuam legíveis

### Requirement: Senha mascarada

A lista MUST trazer os acessos sem as senhas. A senha de um acesso MUST vir só quando a pessoa pede ("Mostrar senha" ou "Copiar senha"), numa resposta que o navegador não guarda, sem registro de quem mostrou ou copiou. A senha MUST nunca ir por e-mail nem para a auditoria, que registra só que ela mudou (ADR-0033, ADR-0034, ADR-0039).

#### Scenario: Senha mascarada por padrão
- **WHEN** alguém abre a aba Acessos
- **THEN** a lista não traz as senhas, e cada senha aparece mascarada

#### Scenario: Mostrar a senha
- **WHEN** alguém escolhe "Mostrar senha" num acesso
- **THEN** a senha aparece no lugar da máscara, e "Esconder senha" a mascara de novo

#### Scenario: Troca de senha na auditoria
- **WHEN** alguém troca a senha de um acesso
- **THEN** a auditoria registra que a senha mudou, sem o valor antes nem depois

#### Scenario: Senha nunca por e-mail
- **WHEN** acessos são incluídos e editados no projeto
- **THEN** nenhum e-mail do sistema traz a senha

### Requirement: Busca nos acessos

A lista MUST filtrar os acessos do projeto pelo portal e pelo usuário e pelo ambiente, e MUST trazer os ambientes usados no projeto, com a contagem de cada um (ADR-0034).

#### Scenario: Buscar por portal ou usuário
- **WHEN** alguém busca "erp" na aba Acessos
- **THEN** a lista mostra os acessos com "erp" no portal ou no usuário

#### Scenario: Filtrar por ambiente
- **WHEN** alguém escolhe "Produção"
- **THEN** a lista mostra só os acessos de Produção

### Requirement: Permissões nos acessos

Todas as pessoas do projeto e o administrador MUST ver os acessos e mostrar e copiar as senhas. Incluir e editar MUST ser de todos, menos o leitor. Excluir MUST ser do administrador, do gerente e do líder técnico. Quem não está no projeto recebe 404, e o projeto concluído ou cancelado MUST ser só leitura, com 409 (ADR-0016, ADR-0045).

#### Scenario: Permissões nos acessos por papel
- **WHEN** o administrador, o gerente, o líder técnico, o colaborador, o desenvolvedor e o leitor tentam ver a senha, incluir, editar e excluir um acesso
- **THEN** todos veem a senha; o administrador, o gerente e o líder técnico fazem tudo; o colaborador e o desenvolvedor incluem e editam e recebem 403 ao excluir; o leitor recebe 403 ao incluir, editar e excluir

#### Scenario: Acessos de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede os acessos ou a senha de um
- **THEN** a API responde 404

#### Scenario: Acessos de projeto encerrado
- **WHEN** alguém tenta incluir um acesso num projeto concluído
- **THEN** a API responde 409 e nada é gravado

### Requirement: Acessos pela tela

O projeto MUST ter a aba "Acessos", com a contagem, como no canvas ProjetoAcessos: os acessos agrupados por ambiente, com o portal, o endereço, que abre em nova aba, o usuário com "Copiar usuário", a senha mascarada com "Mostrar senha" e "Copiar senha", a observação e o "⋮" com editar e excluir; a busca; os ambientes num controle segmentado; e "Novo acesso". No celular, a linha mostra o portal, o usuário e a senha (ADR-0034, ADR-0065, ADR-0066).

#### Scenario: Copiar a senha pela tela
- **WHEN** alguém escolhe "Copiar senha"
- **THEN** a senha vai para a área de transferência sem aparecer na tela, e a tela diz que foi copiada

#### Scenario: Novo acesso pela tela
- **WHEN** a colaboradora escolhe "Novo acesso", preenche o portal, o ambiente, o usuário e a senha e confirma
- **THEN** o acesso aparece no grupo do ambiente, com a senha mascarada

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
