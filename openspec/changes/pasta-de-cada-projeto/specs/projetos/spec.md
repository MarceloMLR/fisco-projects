## ADDED Requirements

### Requirement: Pasta de cada projeto

Cada projeto MUST ter uma pasta só no armazenamento, onde ficam todos os arquivos dele: os documentos, os pedidos de aceite e os anexos dos chamados. A pasta MUST ser criada quando o projeto guarda o primeiro arquivo, com o nome `{código} - {nome}`, dentro da pasta dos projetos da instalação, e a partir daí MUST ser achada pelo projeto, nunca pelo nome (ADR-0030, ADR-0121).

#### Scenario: Pasta criada no primeiro arquivo
- **WHEN** o gerente envia o primeiro documento de um projeto com a pasta dos projetos "Projetos"
- **THEN** o documento fica em "Projetos/{código} - {nome}/Documentos/{pasta}"

#### Scenario: Tudo na mesma pasta
- **WHEN** o projeto recebe um documento, um anexo de chamado e um pedido de aceite
- **THEN** os três ficam dentro da mesma pasta do projeto, em Documentos, Chamados e Pedidos de aceite

### Requirement: Pasta de antes e pasta apagada

Um projeto que ainda não tem pasta e cuja pasta `{código} - {nome}` já existe na raiz, criada antes de o projeto guardar a pasta, MUST passar a usar essa pasta. Se a pasta do projeto for apagada fora do sistema, a próxima vez que o projeto guardar um arquivo MUST criá-la de novo (ADR-0121).

#### Scenario: Projeto com a pasta de antes
- **WHEN** um projeto criado antes desta mudança, com a pasta `{código} - {nome}` na raiz, recebe um anexo depois de a pasta dos projetos virar "Projetos"
- **THEN** o anexo fica na pasta que o projeto já tinha na raiz

#### Scenario: Pasta apagada por fora
- **WHEN** a pasta do projeto é apagada direto no SharePoint e o projeto recebe um documento
- **THEN** a pasta é criada de novo, e o documento fica nela

### Requirement: Pasta renomeada com o projeto

Quando o código ou o nome do projeto muda, a pasta do projeto MUST ser renomeada para o `{código} - {nome}` novo, depois de gravar o projeto, e os arquivos de dentro MUST continuar abrindo. Se já houver uma pasta com esse nome ao lado ou o armazenamento falhar, o projeto MUST ficar gravado, a pasta MUST ficar com o nome que tinha, com tudo nela, e a próxima alteração dos dados do projeto MUST tentar de novo (ADR-0121).

#### Scenario: Renomear o projeto
- **WHEN** o gerente troca o nome do projeto "Implantação" para "Implantação do ERP", num projeto com documentos
- **THEN** a pasta passa a se chamar "{código} - Implantação do ERP", os documentos abrem, e os arquivos novos vão para ela

#### Scenario: Nome da pasta já usado
- **WHEN** o gerente troca o nome do projeto, e já existe ao lado uma pasta com o nome novo
- **THEN** o projeto fica com o nome novo, a pasta fica com o nome antigo, e os arquivos novos continuam indo para ela

#### Scenario: Armazenamento fora do ar
- **WHEN** o gerente troca o nome do projeto com o armazenamento fora do ar e, depois, salva os dados do projeto de novo com o armazenamento no ar
- **THEN** a primeira alteração é gravada sem renomear a pasta, e a segunda a renomeia

## MODIFIED Requirements

### Requirement: Arquivos do projeto excluído

Na exclusão, MUST sair do armazenamento só o que o sistema registrou para o projeto (anexos, documentos com as versões e termos), as pastas que ficarem vazias dentro da pasta do projeto e, por último, a pasta do projeto, se ficar vazia; o resto MUST ficar, e a pasta dos projetos MUST nunca ser apagada (ADR-0041, ADR-0111, ADR-0121).

#### Scenario: Arquivo de fora fica
- **WHEN** a pasta do projeto no armazenamento tem um arquivo que o sistema não criou
- **THEN** a exclusão apaga os arquivos do sistema e mantém esse arquivo e as pastas acima dele

#### Scenario: Pasta dos projetos fica
- **WHEN** o administrador exclui o único projeto da pasta dos projetos "Projetos"
- **THEN** a pasta do projeto sai, e "Projetos" fica, mesmo vazia
