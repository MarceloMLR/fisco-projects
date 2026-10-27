## ADDED Requirements

### Requirement: Escolher a pasta do projeto

Ao criar o projeto, quem cria MUST poder escolher, no lugar da pasta criada pelo sistema, uma pasta que já existe dentro da raiz do armazenamento, que então recebe as pastas do projeto (documentos, chamados e pedidos de aceite). A raiz, a pasta dos projetos e as pastas acima dela, a pasta do sistema, a pasta de outro projeto e as pastas dentro ou acima dela MUST ser recusadas com 400 no campo da pasta (ADR-0030, ADR-0122).

#### Scenario: Criar projeto numa pasta que já existe
- **WHEN** o gerente de projeto cria um projeto escolhendo a pasta "Clientes/Alfa/ERP"
- **THEN** as pastas de documentos ficam dentro de "Clientes/Alfa/ERP", e o sistema não cria a pasta `{código} - {nome}`

#### Scenario: Pasta de outro projeto
- **WHEN** alguém escolhe a pasta de outro projeto, ou uma pasta dentro dela, para um projeto novo
- **THEN** a API recusa com 400 no campo da pasta, e o projeto não é criado

#### Scenario: Pasta que não pode ser escolhida
- **WHEN** alguém escolhe a raiz, a pasta dos projetos, uma pasta acima dela ou a pasta do sistema
- **THEN** a API recusa com 400 no campo da pasta

### Requirement: Lista de pastas para escolher

O administrador e o gerente de projeto MUST poder listar as pastas de dentro de uma pasta do armazenamento, ou da raiz, em ordem alfabética, com o projeto que usa cada uma e se ela pode ser escolhida, e saber o mesmo da pasta listada. As outras pessoas MUST receber 403 (ADR-0122).

#### Scenario: Listar as pastas
- **WHEN** o gerente de projeto lista as pastas da raiz, onde estão "Clientes", a pasta dos projetos e a pasta do sistema
- **THEN** a lista traz as três em ordem alfabética, com só "Clientes" para escolher

#### Scenario: Pastas de um projeto na lista
- **WHEN** o gerente de projeto lista a pasta dos projetos
- **THEN** cada pasta de projeto aparece com o código do projeto que a usa, sem poder ser escolhida

#### Scenario: Usuário não lista as pastas
- **WHEN** um usuário que não cria projetos lista as pastas
- **THEN** a API responde 403

### Requirement: Trocar a pasta do projeto

O administrador e o gerente do projeto MUST poder trocar a pasta do projeto por outra que já existe, com as mesmas regras da escolha, ou voltar à pasta criada pelo sistema, enquanto o projeto está aberto e não guardou nenhum arquivo. Com algum arquivo guardado, a troca MUST ser recusada com 409 (ADR-0043, ADR-0122).

#### Scenario: Trocar a pasta antes dos arquivos
- **WHEN** o gerente troca a pasta de um projeto sem arquivos por "Clientes/Alfa/ERP"
- **THEN** as pastas de documentos passam a ficar em "Clientes/Alfa/ERP", e a pasta que o sistema tinha criado sai

#### Scenario: Pasta com arquivos não muda
- **WHEN** o gerente troca a pasta de um projeto que já tem um documento
- **THEN** a API recusa com 409, e a pasta fica como estava

#### Scenario: Permissões da pasta do projeto
- **WHEN** cada papel do projeto pede para trocar a pasta
- **THEN** o administrador e o gerente conseguem, o líder técnico, o colaborador, o desenvolvedor e o leitor recebem 403, e quem não está no projeto recebe 404

### Requirement: Pasta anterior e auditoria da troca

Depois de uma troca, as pastas vazias que o sistema criou na pasta anterior MUST sair, e a pasta anterior também, se o sistema a criou e ela ficou vazia; a pasta escolhida por alguém MUST ficar. A pasta escolhida na criação e a troca MUST entrar na auditoria, pelo nome da pasta (ADR-0039, ADR-0122).

#### Scenario: Voltar à pasta do sistema
- **WHEN** o gerente volta à pasta criada pelo sistema um projeto sem arquivos que estava numa pasta escolhida
- **THEN** o sistema cria `{código} - {nome}` na pasta dos projetos, e a pasta escolhida fica, sem as pastas vazias que o sistema tinha criado nela

#### Scenario: Troca da pasta na auditoria
- **WHEN** o gerente troca a pasta do projeto
- **THEN** a auditoria tem "Alterou um projeto" com a pasta dos arquivos antes e depois

### Requirement: Pasta do projeto na tela

O Novo projeto MUST ter o campo "Pasta dos arquivos", com a pasta que o sistema vai criar até alguém escolher outra, "Escolher pasta…" e, depois de escolher, "Usar a pasta criada pelo sistema". "Escolher pasta…" MUST abrir o diálogo "Escolher pasta", que navega pelas pastas a partir da raiz, com o caminho para voltar e "Usar esta pasta" para a pasta aberta, só quando ela pode ser escolhida (ADR-0122).

#### Scenario: Escolher a pasta no Novo projeto
- **WHEN** o gerente de projeto abre "Escolher pasta…", entra em "Clientes", depois em "Alfa", e escolhe "Usar esta pasta"
- **THEN** o campo mostra "Clientes / Alfa", e o projeto é criado com essa pasta

#### Scenario: Pasta que não pode ser escolhida no diálogo
- **WHEN** o diálogo mostra a pasta dos projetos com a pasta de outro projeto
- **THEN** a pasta do outro projeto aparece com o código dele, sem abrir, e "Usar esta pasta" fica desabilitado na pasta dos projetos, com o motivo

### Requirement: Pasta do projeto no Editar dados

O Editar dados MUST ter o campo "Pasta dos arquivos" para o administrador e o gerente, com a pasta atual e as mesmas ações do Novo projeto, e, depois que o projeto guardou arquivos, sem as ações e com o motivo (ADR-0122).

#### Scenario: Pasta que não muda mais
- **WHEN** o gerente abre o Editar dados de um projeto que já tem documentos
- **THEN** o campo mostra a pasta, sem "Escolher pasta…", e diz que ela não muda depois que o projeto guarda arquivos

## MODIFIED Requirements

### Requirement: Pasta renomeada com o projeto

Quando o código ou o nome do projeto muda, a pasta criada pelo sistema MUST ser renomeada para o `{código} - {nome}` novo depois de gravar o projeto, com os arquivos de dentro abrindo; a pasta escolhida MUST ficar com o nome que tem. Com o nome já usado ao lado ou o armazenamento com falha, o projeto MUST ficar gravado, a pasta MUST ficar como estava, com tudo nela, e a próxima alteração dos dados MUST tentar de novo (ADR-0121, ADR-0122).

#### Scenario: Renomear o projeto
- **WHEN** o gerente troca o nome do projeto "Implantação" para "Implantação do ERP", num projeto com documentos
- **THEN** a pasta passa a se chamar "{código} - Implantação do ERP", os documentos abrem, e os arquivos novos vão para ela

#### Scenario: Nome da pasta já usado
- **WHEN** o gerente troca o nome do projeto, e já existe ao lado uma pasta com o nome novo
- **THEN** o projeto fica com o nome novo, a pasta fica com o nome antigo, e os arquivos novos continuam indo para ela

#### Scenario: Armazenamento fora do ar
- **WHEN** o gerente troca o nome do projeto com o armazenamento fora do ar e, depois, salva os dados do projeto de novo com o armazenamento no ar
- **THEN** a primeira alteração é gravada sem renomear a pasta, e a segunda a renomeia

#### Scenario: Pasta escolhida não muda de nome
- **WHEN** o gerente troca o nome de um projeto que está numa pasta escolhida
- **THEN** a pasta fica com o nome que tinha

### Requirement: Arquivos do projeto excluído

Na exclusão, MUST sair do armazenamento só o que o sistema registrou para o projeto (anexos, documentos com as versões e termos), as pastas que ficarem vazias dentro da pasta do projeto e, por último, a pasta do projeto, se o sistema a criou e ela ficar vazia; o resto MUST ficar, e a pasta dos projetos e a pasta escolhida por alguém MUST nunca ser apagadas (ADR-0041, ADR-0111, ADR-0121, ADR-0122).

#### Scenario: Arquivo de fora fica
- **WHEN** a pasta do projeto no armazenamento tem um arquivo que o sistema não criou
- **THEN** a exclusão apaga os arquivos do sistema e mantém esse arquivo e as pastas acima dele

#### Scenario: Pasta dos projetos fica
- **WHEN** o administrador exclui o único projeto da pasta dos projetos "Projetos"
- **THEN** a pasta do projeto sai, e "Projetos" fica, mesmo vazia

#### Scenario: Pasta escolhida fica na exclusão
- **WHEN** o administrador exclui um projeto que estava numa pasta escolhida
- **THEN** os arquivos do sistema e as pastas vazias que ele criou saem, e a pasta escolhida fica
