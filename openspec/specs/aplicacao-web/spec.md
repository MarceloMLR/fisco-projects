# aplicacao-web Specification

## Purpose

Define como o frontend do FiscoProjects é entregue pela própria aplicação e as regras comuns a todas as telas: textos vindos dos arquivos de tradução e tipos da API gerados do contrato (ADR-0013, ADR-0047, ADR-0049, ADR-0061).

## Requirements

### Requirement: Frontend servido pela aplicação

A aplicação MUST servir o build do frontend no mesmo endereço da API. Todo caminho fora de `/api` que não seja um arquivo MUST receber a página principal do frontend, que decide a tela. Os arquivos do build com nome versionado MUST ser enviados com cache longo, e a página principal MUST NOT ficar em cache, para que uma nova versão chegue a quem já tinha a aplicação aberta.

#### Scenario: Página inicial
- **WHEN** alguém abre o endereço da aplicação
- **THEN** a resposta tem status 200, é a página principal do frontend e pede para não ser guardada em cache

#### Scenario: Rota do frontend
- **WHEN** alguém abre direto um caminho de tela, fora de `/api`, como `/projects/123`
- **THEN** a resposta tem status 200 e é a página principal do frontend

#### Scenario: Arquivo do build
- **WHEN** o navegador pede um arquivo do build com nome versionado
- **THEN** a resposta tem status 200, traz o arquivo e permite guardá-lo em cache por um ano, sem revalidar

### Requirement: Textos da interface em arquivos de tradução

Todo texto visível nas telas MUST vir dos arquivos de tradução, em português do Brasil, com chaves em inglês. Texto escrito direto numa tela MUST reprovar o lint, e uma chave que não existe nos arquivos de tradução MUST reprovar a checagem de tipos.

#### Scenario: Texto da tela vem da tradução
- **WHEN** uma tela é exibida
- **THEN** os textos mostrados são os do arquivo de tradução em português

#### Scenario: Texto escrito direto na tela
- **WHEN** um componente traz um texto visível escrito direto no código
- **THEN** o lint aponta o texto como erro

#### Scenario: Chave de tradução inexistente
- **WHEN** o código pede uma chave que não está nos arquivos de tradução
- **THEN** a checagem de tipos falha

### Requirement: Tipos da API gerados do contrato

O contrato OpenAPI da aplicação MUST ficar versionado no repositório, e os tipos que o frontend usa para falar com a API MUST ser gerados dele, nunca escritos à mão. Um contrato versionado diferente do que a API publica, ou tipos diferentes dos gerados a partir do contrato, MUST reprovar os testes.

#### Scenario: Contrato versionado desatualizado
- **WHEN** a API muda e o contrato versionado não é atualizado
- **THEN** os testes do backend falham e indicam como atualizar o contrato

#### Scenario: Tipos do frontend desatualizados
- **WHEN** o contrato versionado muda e os tipos do frontend não são gerados de novo
- **THEN** os testes do frontend falham e indicam como gerar os tipos
