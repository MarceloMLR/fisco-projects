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

### Requirement: Dados da API guardados durante a sessão

As respostas da API que as telas leem MUST ficar guardadas só na memória da página, durante a sessão:

- Ao voltar a uma tela, filtro ou página já vista, a lista MUST aparecer na hora com os dados guardados e MUST ser atualizada com a API em seguida.
- Uma alteração feita pela pessoa MUST pedir de novo à API as listas que mostram o item alterado.
- Os dados guardados MUST ser descartados quando a pessoa sai ou a sessão termina.
- Nada deles MUST ir para o armazenamento do navegador.

#### Scenario: Volta a uma lista já vista
- **WHEN** a pessoa volta a uma lista, ou a um filtro dela, que já abriu na sessão
- **THEN** as linhas da última vez aparecem na hora, sem indicador de carregamento, e a lista é pedida de novo à API; se a resposta passar de 300 ms, a barra de progresso aparece no topo até a lista se atualizar

#### Scenario: Alteração atualiza as listas
- **WHEN** o administrador troca o papel de uma pessoa, a desativa ou a reativa
- **THEN** a lista de usuários e a contagem da Administração são pedidas de novo à API, e a Auditoria, aberta em seguida, mostra o registro da alteração

#### Scenario: Saída descarta os dados guardados
- **WHEN** a pessoa sai, ou a sessão termina, e alguém entra em seguida no mesmo navegador
- **THEN** as listas são pedidas de novo à API, e nada da sessão anterior aparece

#### Scenario: Dados só na memória
- **WHEN** uma lista é aberta
- **THEN** nada dos dados é gravado no `localStorage` nem no `sessionStorage` do navegador

### Requirement: Espera nas listas

Ao trocar o filtro ou a página de uma lista, a tabela MUST continuar mostrando as linhas atuais até a resposta chegar. O indicador de carregamento MUST aparecer só se a espera passar de 300 ms, tanto numa lista nova quanto na atualização das linhas que já estão na tela. Na primeira abertura da lista, a tabela MUST mostrar o cabeçalho e, só depois de 300 ms de espera, o esqueleto das linhas. A mensagem de lista vazia MUST aparecer só depois da resposta.

#### Scenario: Resposta rápida sem indicador
- **WHEN** a pessoa troca o filtro de uma lista e a resposta chega em menos de 300 ms
- **THEN** as linhas anteriores ficam na tabela até a resposta, e a lista troca para as novas sem mostrar indicador de carregamento

#### Scenario: Resposta demorada com indicador
- **WHEN** a pessoa troca o filtro de uma lista e a resposta demora mais de 300 ms
- **THEN** a tabela mostra uma barra de progresso no topo, sem esconder as linhas anteriores, até a resposta chegar

#### Scenario: Primeira abertura da lista
- **WHEN** a pessoa abre uma lista pela primeira vez na sessão
- **THEN** a tabela mostra o cabeçalho sem a mensagem de lista vazia e, se a espera passar de 300 ms, o esqueleto das linhas

### Requirement: Menus fecham na hora

Ao escolher uma opção num menu ou num campo de seleção, o menu MUST sumir na hora, sem animação de saída, para que a tela atrás dele fique visível. A abertura MAY continuar animada.

#### Scenario: Escolha num campo de seleção
- **WHEN** a pessoa escolhe uma opção no filtro de uma lista
- **THEN** o menu de opções sai da tela no mesmo instante

#### Scenario: Escolha num menu de ações
- **WHEN** o administrador escolhe uma ação no menu de uma pessoa, na tela Usuários
- **THEN** o menu sai da tela no mesmo instante
