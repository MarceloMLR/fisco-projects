# Spec Delta

## Purpose

Define como os módulos guardam, versionam, abrem e excluem arquivos e pastas atrás de um contrato único, com nomes e caminhos seguros, e onde a implementação local pode ser usada (ADR-0004, ADR-0030, ADR-0079).

## ADDED Requirements

### Requirement: Pastas

O armazenamento MUST garantir uma pasta a partir da raiz configurada ou de outra pasta, criando as pastas que faltam no caminho, e MUST listar as subpastas de uma pasta, para a escolha da pasta de um projeto. Cada pasta MUST ter um identificador que quem a usa guarda (ADR-0030, ADR-0079).

#### Scenario: Pastas criadas no caminho
- **WHEN** um módulo pede a pasta `Projetos/ERP-FIN/Chamados/#101` e nenhuma delas existe
- **THEN** as quatro pastas são criadas e a última é devolvida com o seu identificador

#### Scenario: Pasta que já existe
- **WHEN** um módulo pede uma pasta que já existe
- **THEN** a mesma pasta é devolvida, com o mesmo identificador, e nada é criado

#### Scenario: Listar subpastas
- **WHEN** um módulo pede as subpastas de uma pasta
- **THEN** recebe as pastas que estão diretamente nela, em ordem alfabética, sem os arquivos e sem as pastas internas do armazenamento

### Requirement: Enviar e abrir arquivos

O armazenamento MUST guardar um arquivo numa pasta e devolver o identificador, o nome final, o tamanho e a versão. Abrir um arquivo MUST devolver o conteúdo, o nome, o tipo e o tamanho. Um identificador desconhecido MUST ser informado como não encontrado (ADR-0030).

#### Scenario: Enviar um arquivo
- **WHEN** um módulo envia um arquivo para uma pasta
- **THEN** o arquivo fica na pasta, e o módulo recebe o identificador, o nome, o tamanho e a versão

#### Scenario: Abrir um arquivo
- **WHEN** um módulo abre um arquivo pelo identificador
- **THEN** recebe o mesmo conteúdo enviado, com o nome, o tipo (por exemplo, `application/pdf`) e o tamanho

#### Scenario: Arquivo inexistente
- **WHEN** um módulo abre ou exclui um arquivo com um identificador que não existe
- **THEN** o armazenamento informa que o arquivo não foi encontrado

### Requirement: Mesmo nome na mesma pasta

Ao enviar um arquivo com o nome de outro que já está na pasta, quem envia MUST escolher entre nova versão, que guarda a anterior e mantém o identificador, e manter os dois, que guarda o novo com outro nome (ADR-0031, ADR-0079).

#### Scenario: Nova versão de um arquivo
- **WHEN** um módulo envia, como nova versão, um arquivo com o nome de outro da pasta
- **THEN** o arquivo mantém o identificador, passa a ter o conteúdo novo e uma versão nova, e a versão anterior continua podendo ser aberta

#### Scenario: Manter os dois arquivos
- **WHEN** um módulo envia, mantendo os dois, um arquivo `print.png` numa pasta que já tem um `print.png`
- **THEN** o novo é guardado como `print (1).png`, com outro identificador, e o anterior não muda

### Requirement: Excluir arquivos e pastas

Excluir um arquivo MUST excluir também as versões anteriores. Uma pasta MUST ser excluída só quando estiver vazia, para nunca apagar o que não foi criado pelo FiscoProjects (ADR-0041).

#### Scenario: Excluir um arquivo
- **WHEN** um módulo exclui um arquivo que tem versões anteriores
- **THEN** nem o arquivo nem as versões anteriores podem mais ser abertos

#### Scenario: Excluir uma pasta vazia
- **WHEN** um módulo pede para excluir uma pasta vazia
- **THEN** a pasta é excluída

#### Scenario: Pasta com conteúdo não é excluída
- **WHEN** um módulo pede para excluir uma pasta que ainda tem arquivos ou subpastas
- **THEN** a pasta e o conteúdo continuam lá, e o módulo é informado de que ela não foi excluída

### Requirement: Nomes e caminhos seguros

Nomes de arquivos e pastas MUST ser ajustados às regras do SharePoint antes de guardar: caracteres não aceitos viram `_`, sem espaços nas pontas e sem ponto no fim. Nenhum caminho MUST sair da raiz configurada (ADR-0030, ADR-0079).

#### Scenario: Nome com caracteres não aceitos
- **WHEN** um módulo envia o arquivo `Relatório: março?.pdf `
- **THEN** ele é guardado como `Relatório_ março_.pdf`, e o nome final é devolvido ao módulo

#### Scenario: Caminho que sai da raiz
- **WHEN** um módulo pede uma pasta, envia ou abre um arquivo com um caminho ou identificador que sairia da raiz, como `../outra-pasta`
- **THEN** a operação é recusada e nada fora da raiz é lido ou escrito

### Requirement: Implementação escolhida na configuração

A implementação do armazenamento MUST ser escolhida na configuração da instalação. Sem implementação configurada, usar arquivos MUST falhar com uma mensagem que diz qual configuração falta. A implementação local, em disco, MUST valer só fora da produção: a aplicação MUST NOT subir em produção com ela (ADR-0004, ADR-0079).

#### Scenario: Implementação local no desenvolvimento
- **WHEN** a aplicação sobe no desenvolvimento com a implementação local
- **THEN** os arquivos ficam na pasta local configurada

#### Scenario: Armazenamento não configurado
- **WHEN** a aplicação sobe sem implementação de arquivos configurada e um módulo pede o armazenamento
- **THEN** a aplicação sobe, e o pedido falha com uma mensagem que aponta a configuração `FileStorage:Provider`

#### Scenario: Implementação local em produção
- **WHEN** a aplicação sobe em produção com a implementação local configurada
- **THEN** ela não sobe e explica que a implementação local não vale em produção
