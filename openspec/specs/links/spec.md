# links Specification

## Purpose

Links dos projetos: as categorias mantidas pelo administrador, os links de cada projeto com título, endereço, categoria e para que serve, a busca e o filtro por categoria, com as permissões de cada papel (ADR-0032, ADR-0045, ADR-0046).

## Requirements

### Requirement: Categorias de links

O administrador MUST incluir, renomear, desativar e reativar as categorias de links, com nomes únicos, mostradas na ordem do nome. A migração MUST criar as categorias do padrão (Sistemas, Ambientes, Repositórios, Painéis e Outros). A categoria inativa MUST não aparecer para links novos, e os links que já a usam MUST continuar com ela. Nenhuma categoria fica fixa no código, e as mudanças entram na auditoria (ADR-0003, ADR-0032, ADR-0046).

#### Scenario: Incluir categoria de link
- **WHEN** o administrador inclui a categoria "Documentação"
- **THEN** a categoria aparece ativa na lista e nas opções de novos links

#### Scenario: Nome de categoria repetido
- **WHEN** o administrador inclui uma categoria com o nome de outra
- **THEN** a API responde 409 e nada é gravado

#### Scenario: Categoria inativa não aparece para novos links
- **WHEN** o administrador desativa "Painéis"
- **THEN** "Painéis" sai das opções de novos links, e um link novo nela é recusado, enquanto os links que já a usam continuam nela

#### Scenario: Categorias só do administrador
- **WHEN** um gerente de projeto tenta incluir uma categoria
- **THEN** a API responde 403

### Requirement: Links do projeto

Quem cadastra links (ADR-0045) MUST incluir, editar e excluir links do projeto, com o título (obrigatório, até 120 caracteres), o endereço (http ou https, até 2.000 caracteres), uma categoria ativa e a descrição opcional (até 500 caracteres). O link MUST guardar quem incluiu e quando, e as mudanças entram na auditoria, com o projeto. Um link editado MUST poder manter a categoria que ficou inativa (ADR-0032).

#### Scenario: Incluir link
- **WHEN** o desenvolvedor inclui o link "ERP · Homologação" para `https://hml-erp.cliente.example` em "Ambientes"
- **THEN** a aba Links mostra o link no grupo Ambientes, com ele e o dia em que incluiu

#### Scenario: Link sem título ou com endereço inválido
- **WHEN** alguém inclui um link sem título, ou com o endereço `ftp://arquivos.example`
- **THEN** a API responde 400 e nada é gravado

#### Scenario: Editar link
- **WHEN** a colaboradora muda o título e a descrição de um link
- **THEN** o link mostra o título e a descrição novos

#### Scenario: Excluir link
- **WHEN** o líder técnico exclui um link
- **THEN** o link sai da aba Links

### Requirement: Busca nos links

A lista MUST filtrar os links do projeto pelo título e pelo endereço e pela categoria, e MUST trazer as categorias que os links do projeto usam, com a contagem de cada uma (ADR-0032).

#### Scenario: Buscar por título e endereço
- **WHEN** alguém busca "hml" na aba Links
- **THEN** a lista mostra os links com "hml" no título ou no endereço

#### Scenario: Filtrar por categoria
- **WHEN** alguém escolhe "Repositórios"
- **THEN** a lista mostra só os links de Repositórios

### Requirement: Permissões nos links

Todas as pessoas do projeto e o administrador MUST ver os links. Incluir, editar e excluir MUST ser de todos, menos o leitor, que recebe 403. Quem não está no projeto recebe 404, e o projeto concluído ou cancelado MUST ser só leitura, com 409 (ADR-0016, ADR-0045).

#### Scenario: Permissões nos links por papel
- **WHEN** o administrador, o gerente, o líder técnico, o colaborador, o desenvolvedor e o leitor tentam incluir, editar e excluir um link
- **THEN** todos conseguem, menos o leitor, que recebe 403 e vê os links

#### Scenario: Links de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede os links ou tenta incluir um
- **THEN** a API responde 404

#### Scenario: Links de projeto encerrado
- **WHEN** alguém tenta incluir um link num projeto cancelado
- **THEN** a API responde 409 e nada é gravado

### Requirement: Links pela tela

O projeto MUST ter a aba "Links", com a contagem, como no canvas ProjetoLinks: os links agrupados por categoria, com a inicial do título, o endereço sem o protocolo, que abre em nova aba, a descrição e quem incluiu; a busca; as categorias num controle segmentado; "Novo link"; e, em cada linha, "Copiar endereço", "Abrir em nova aba" e o "⋮" com editar e excluir. A Administração MUST ter "Categorias de links". No celular, a linha mostra o título e o endereço (ADR-0032, ADR-0065, ADR-0066).

#### Scenario: Copiar o endereço pela tela
- **WHEN** alguém escolhe "Copiar endereço" num link
- **THEN** o endereço vai para a área de transferência, e a tela diz que foi copiado

#### Scenario: Novo link pela tela
- **WHEN** a colaboradora escolhe "Novo link", preenche o título, o endereço e a categoria e confirma
- **THEN** o link aparece no grupo da categoria

#### Scenario: Categorias de links pela tela
- **WHEN** o administrador abre "Categorias de links" na Administração
- **THEN** a tela mostra as categorias com a situação e "Nova categoria"
