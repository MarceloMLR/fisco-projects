## ADDED Requirements

### Requirement: Excluir projeto

Só o administrador MUST poder excluir um projeto, de qualquer status, digitando o código do projeto para confirmar. A exclusão MUST apagar o projeto e tudo o que é dele em todos os módulos, sem que nada do projeto fique em nenhuma tabela. O cliente, os modelos de projeto e os fornecedores MUST ficar, e os números dos chamados apagados MUST não ser reaproveitados (ADR-0029, ADR-0041, ADR-0111).

#### Scenario: Excluir projeto
- **WHEN** o administrador exclui um projeto com cronograma, chamados com anexos, documentos, links, acessos e termos de aceite, digitando o código
- **THEN** o projeto deixa de existir, nenhuma tabela guarda mais nada dele, os arquivos que o sistema criou saem do armazenamento, e o cliente e os modelos de projeto continuam

#### Scenario: Código errado
- **WHEN** o administrador pede a exclusão com um código diferente do código do projeto
- **THEN** a API recusa com 400 no campo do código, e nada é apagado

#### Scenario: Só o administrador exclui
- **WHEN** o gerente ou outra pessoa do projeto pede o resumo ou a exclusão
- **THEN** a API recusa com 403, e quem não está no projeto recebe 404

#### Scenario: Número de chamado não reaproveitado
- **WHEN** um chamado é aberto depois da exclusão de um projeto com chamados
- **THEN** ele recebe o número seguinte ao maior já usado, nunca um dos apagados

### Requirement: Arquivos do projeto excluído

Na exclusão, MUST sair do armazenamento só o que o sistema registrou para o projeto (anexos, documentos com as versões e termos) e as pastas que ficarem vazias; o resto MUST ficar (ADR-0041, ADR-0111).

#### Scenario: Arquivo de fora fica
- **WHEN** a pasta do projeto no armazenamento tem um arquivo que o sistema não criou
- **THEN** a exclusão apaga os arquivos do sistema e mantém esse arquivo e as pastas acima dele

### Requirement: Resumo da exclusão

Antes de excluir, o administrador MUST poder ver quantos chamados, documentos, links, acessos, termos de aceite, tarefas e marcos e pessoas o projeto tem (ADR-0041).

#### Scenario: Resumo da exclusão
- **WHEN** o administrador pede o resumo de um projeto com dois chamados, um documento e três tarefas
- **THEN** a API responde com o código e o nome do projeto e essas quantidades

### Requirement: Exclusão na auditoria

A exclusão MUST ficar na auditoria num registro só, de quem excluiu, com o código, o nome, o cliente e o status do projeto e quantos itens de cada tipo foram apagados, sem registros das linhas apagadas. Os registros anteriores do projeto MUST continuar (ADR-0039, ADR-0084, ADR-0111).

#### Scenario: Exclusão na auditoria
- **WHEN** o administrador exclui um projeto
- **THEN** a auditoria tem um registro "Excluiu um projeto" com o nome, o código e o resumo do que foi apagado, e os registros anteriores continuam lá

### Requirement: Exclusão pela tela

"Ações do projeto" MUST oferecer "Excluir projeto" só ao administrador. O diálogo MUST mostrar o resumo do que será apagado, avisar que não dá para desfazer e só deixar excluir com o código do projeto digitado; depois da exclusão, a tela MUST voltar para a lista de projetos com a confirmação (ADR-0041, ADR-0065).

#### Scenario: Excluir pela tela
- **WHEN** o administrador escolhe "Excluir projeto", digita o código e confirma
- **THEN** o diálogo mostrou o resumo, o botão só ficou disponível com o código certo, e a tela volta para Projetos com a confirmação

#### Scenario: Excluir só para o administrador
- **WHEN** o gerente abre "Ações do projeto"
- **THEN** "Excluir projeto" não aparece
