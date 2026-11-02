# documentos Specification

## Purpose

Documentos dos projetos no SharePoint: as pastas padrão da instalação, copiadas para cada projeto, o envio com versões, o status manual, o vínculo com o cronograma, a busca, a visualização e a exclusão, com as permissões de cada papel e a pasta de termos de aceite (ADR-0030, ADR-0031, ADR-0045, ADR-0092, ADR-0093).

## Requirements

### Requirement: Pastas padrão

O administrador MUST incluir, renomear, desativar e reativar as pastas padrão, com nomes únicos, mostradas na ordem do nome. A migração MUST criar as pastas do MVP (01 Gestão do projeto, 02 Contratos e compras, 03 Requisitos e especificações, 04 Atas de reunião, 05 Entregas e manuais e 06 Termos de aceite), com a última marcada como a pasta de termos de aceite, que MUST não ser desativada. Nenhum nome de pasta fica fixo no código, e as mudanças entram na auditoria (ADR-0003, ADR-0031, ADR-0046).

#### Scenario: Incluir pasta padrão
- **WHEN** o administrador inclui a pasta padrão "07 Treinamentos"
- **THEN** a pasta aparece ativa na lista das pastas padrão, na ordem do nome

#### Scenario: Nome de pasta repetido
- **WHEN** o administrador inclui uma pasta padrão com o nome de outra
- **THEN** a API responde 409 e nada é gravado

#### Scenario: Pasta de termos sempre ativa
- **WHEN** o administrador tenta desativar a pasta de termos de aceite
- **THEN** a API responde 400 e a pasta continua ativa

#### Scenario: Pastas padrão só do administrador
- **WHEN** um gerente de projeto tenta incluir uma pasta padrão
- **THEN** a API responde 403

### Requirement: Pastas do projeto

Todo projeto novo MUST receber as pastas padrão ativas no momento da criação, com quem vê cada uma e a pasta de termos marcada. Uma mudança nas pastas padrão MUST valer só para os projetos novos. Um projeto criado antes do módulo MUST receber as pastas padrão ativas na primeira vez que a área de documentos é aberta (ADR-0030, ADR-0031, ADR-0092, ADR-0148).

#### Scenario: Projeto novo recebe as pastas padrão
- **WHEN** a gerente cria o projeto ERP-FIN
- **THEN** a área de documentos do projeto mostra as seis pastas padrão, na ordem do nome

#### Scenario: Pasta desativada fica fora dos projetos novos
- **WHEN** o administrador desativa "04 Atas de reunião" e depois a gerente cria um projeto
- **THEN** o projeto novo não tem a pasta "04 Atas de reunião", e os projetos que já existiam continuam com ela

#### Scenario: Projeto novo recebe quem vê as pastas
- **WHEN** o administrador deixou "01 Gestão do projeto" só para o líder técnico e depois a gerente cria um projeto
- **THEN** no projeto novo a pasta "01 Gestão do projeto" é vista pela gestão e pelo líder técnico, e os projetos que já existiam continuam como estavam

### Requirement: Envio de documentos

Quem envia documentos (ADR-0045) MUST enviar arquivos para uma pasta do projeto, até `Documents:MaxMegabytes` (25 MB) por arquivo, e o sistema MUST recusar o arquivo vazio, o maior que o limite e o programa ou script. O documento novo MUST ficar como Rascunho. A pasta de termos de aceite MUST recusar envios (ADR-0031, ADR-0093).

#### Scenario: Enviar documento
- **WHEN** a colaboradora envia "Mapa de processos fiscais.docx" para "03 Requisitos e especificações"
- **THEN** a pasta mostra o documento na versão 1, como Rascunho, com quem enviou e quando

#### Scenario: Arquivo acima do limite
- **WHEN** alguém envia um arquivo maior que o limite da instalação
- **THEN** a API responde 400, com o motivo, e nada é gravado

#### Scenario: Programa recusado
- **WHEN** alguém envia um arquivo ".exe"
- **THEN** a API responde 400, com o motivo, e nada é gravado

#### Scenario: Pasta de termos não recebe envios
- **WHEN** a gerente envia um arquivo para a pasta de termos de aceite
- **THEN** a API responde 409 e nada é gravado

### Requirement: Nova versão do documento

Um arquivo com o mesmo nome de um documento da mesma pasta MUST virar a nova versão dele, com a anterior guardada. A nova versão de um documento Aprovado MUST deixá-lo Em revisão, com quem enviou no registro do status; os outros status MUST não mudar (ADR-0031, ADR-0093).

#### Scenario: Mesmo nome vira nova versão
- **WHEN** alguém envia de novo "Mapa de processos fiscais.docx" para a mesma pasta
- **THEN** o documento passa para a versão 2, as duas versões aparecem nos detalhes, e a versão 1 continua abrindo

#### Scenario: Nova versão de documento aprovado volta para revisão
- **WHEN** a colaboradora envia a nova versão de um documento Aprovado
- **THEN** o documento fica Em revisão, com ela no registro do status

#### Scenario: Nova versão mantém o status
- **WHEN** alguém envia a nova versão de um documento em Rascunho
- **THEN** o documento continua em Rascunho

### Requirement: Status do documento

Quem envia documentos MUST mudar o status para Rascunho, Em revisão ou Obsoleto, e só o administrador, o gerente e o líder técnico MUST aprovar. Cada mudança MUST registrar quem mudou e quando. A lista padrão MUST mostrar todos os documentos menos os obsoletos, e o filtro "Todos" MUST mostrar também os obsoletos (ADR-0031, ADR-0045).

#### Scenario: Aprovar documento
- **WHEN** o líder técnico aprova um documento Em revisão
- **THEN** o documento fica Aprovado, com ele e a hora no registro do status

#### Scenario: Colaborador não aprova
- **WHEN** a colaboradora tenta aprovar um documento
- **THEN** a API responde 403 e o status não muda

#### Scenario: Obsoleto sai da lista padrão
- **WHEN** um documento fica Obsoleto
- **THEN** ele sai da lista padrão da pasta e da contagem, e aparece com o filtro "Todos"

### Requirement: Vínculo com o cronograma

Quem envia documentos MUST vincular um documento a uma fase, tarefa ou marco do cronograma do mesmo projeto, ou desfazer o vínculo. O vínculo a um item de outro projeto MUST ser recusado. A lista e os detalhes MUST mostrar o nome atual do item, e um item removido do cronograma MUST deixar o documento sem vínculo (ADR-0031).

#### Scenario: Vincular a uma tarefa
- **WHEN** a colaboradora vincula o documento à tarefa "Parametrizar regras de ICMS"
- **THEN** a lista mostra "Parametrizar regras de ICMS" na linha do documento

#### Scenario: Item de outro projeto
- **WHEN** alguém vincula o documento a uma tarefa de outro projeto
- **THEN** a API responde 400 e o vínculo não muda

#### Scenario: Item removido do cronograma
- **WHEN** a tarefa vinculada é removida do cronograma
- **THEN** o documento aparece sem vínculo

### Requirement: Busca e visualização

A busca MUST encontrar os documentos pelo nome em todas as pastas que a pessoa vê no projeto. PDF, imagens, CSV e texto MUST abrir no navegador, e qualquer arquivo MUST ser baixado, na versão atual ou numa anterior, sem que o navegador execute nada do arquivo (ADR-0031).

#### Scenario: Buscar nos documentos do projeto
- **WHEN** alguém busca "regras" na área de documentos
- **THEN** a lista mostra os documentos com "regras" no nome, de todas as pastas que a pessoa vê

#### Scenario: Abrir PDF no navegador
- **WHEN** alguém abre um PDF
- **THEN** o navegador mostra o arquivo numa aba, sem baixar

#### Scenario: Baixar versão anterior
- **WHEN** alguém baixa a versão 1 de um documento na versão 2
- **THEN** o arquivo baixado é o da versão 1

### Requirement: Exclusão de documento

Só o administrador, o gerente e o líder técnico MUST excluir documentos, com o arquivo e todas as versões; a exclusão entra na auditoria (ADR-0031, ADR-0045).

#### Scenario: Gestor exclui documento
- **WHEN** a gerente exclui um documento
- **THEN** o documento sai da pasta, e o arquivo e as versões são apagados do armazenamento

#### Scenario: Colaborador não exclui
- **WHEN** a colaboradora tenta excluir um documento
- **THEN** a API responde 403 e nada muda

### Requirement: Permissões nos documentos

Todos do projeto e o administrador MUST ver e baixar os documentos das pastas que veem. Enviar, vincular e mudar para Rascunho, Em revisão ou Obsoleto MUST ser de todos, menos o leitor; aprovar e excluir, do administrador, do gerente e do líder técnico. A pasta de termos e os termos assinados MUST aparecer só para o administrador e o gerente. Quem não está no projeto recebe 404, e o projeto encerrado MUST ser só leitura, com 409 (ADR-0016, ADR-0043, ADR-0045, ADR-0093, ADR-0129, ADR-0148).

#### Scenario: Permissões nos documentos por papel
- **WHEN** o administrador, o gerente, o líder técnico, o colaborador, o desenvolvedor e o leitor tentam enviar, vincular, mudar para Em revisão, aprovar e excluir
- **THEN** o administrador, o gerente e o líder técnico fazem tudo; o colaborador e o desenvolvedor enviam, vinculam e mudam para Em revisão, e recebem 403 ao aprovar e excluir; o leitor recebe 403 em tudo

#### Scenario: Documentos de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador pede os documentos ou tenta enviar um
- **THEN** a API responde 404

#### Scenario: Leitor vê e não envia
- **WHEN** o leitor abre a área de documentos e tenta enviar um arquivo
- **THEN** ele vê as pastas e os documentos, e o envio responde 403

#### Scenario: Pasta de termos só da gestão
- **WHEN** o líder técnico abre a área de documentos
- **THEN** a pasta de termos de aceite não aparece para ele

#### Scenario: Pasta de termos de quem assinou
- **WHEN** a colaboradora que assinou um termo do fluxo anterior abre a área de documentos
- **THEN** a pasta de termos não aparece para ela, e o termo dela responde 404

#### Scenario: Projeto encerrado só leitura
- **WHEN** alguém tenta enviar um documento num projeto concluído
- **THEN** a API responde 409 e nada é gravado

#### Scenario: Pasta escondida de um papel
- **WHEN** "01 Gestão do projeto" é vista só pelo líder técnico e a colaboradora abre a área de documentos
- **THEN** a pasta não aparece para ela, nem os documentos dela na busca e em "Todos", e a contagem da aba não os conta

#### Scenario: Documento de uma pasta escondida
- **WHEN** a colaboradora pede um documento, o arquivo dele ou o envio numa pasta que o papel dela não vê
- **THEN** a API responde 404 e nada é gravado

#### Scenario: Gestão vê todas as pastas
- **WHEN** nenhum papel está marcado numa pasta e o administrador e o gerente abrem a área de documentos
- **THEN** a pasta e os documentos dela aparecem para os dois

### Requirement: Documentos pela tela

O projeto MUST ter a aba "Documentos", com a contagem dos documentos que não estão obsoletos, como no canvas ProjetoDocumentos: as pastas numa coluna, com a contagem; a busca; o filtro de status; "Enviar arquivos" e arrastar e soltar na lista; e os detalhes numa gaveta (ADR-0081), com "Abrir", "Baixar", o "⋮" (status, vínculo e excluir), as propriedades e as versões. A Administração MUST ter "Pastas padrão". No celular, as pastas MUST virar um campo acima da lista (ADR-0031, ADR-0065, ADR-0066).

#### Scenario: Enviar arrastando
- **WHEN** a colaboradora solta dois arquivos sobre a lista de uma pasta
- **THEN** os dois são enviados para a pasta e aparecem na lista

#### Scenario: Detalhes do documento
- **WHEN** alguém escolhe um documento da lista
- **THEN** a gaveta mostra o nome, o status, quem mudou o status, o vínculo, a pasta e as versões, com "Abrir" e "Baixar"

#### Scenario: Pastas padrão pela tela
- **WHEN** o administrador abre "Pastas padrão" na Administração
- **THEN** a tela mostra as pastas com quem vê cada uma e a situação, a pasta de termos marcada, e "Nova pasta"

### Requirement: Quem vê as pastas padrão

Cada pasta padrão MUST dizer quem a vê, entre Líder técnico, Colaborador, Desenvolvedor e Leitor; o administrador e o gerente sempre veem todas. A pasta nova MUST começar com os quatro, e a de termos MUST ficar só com a gestão. Só o administrador MUST mudar, e a mudança MUST entrar na auditoria (ADR-0003, ADR-0046, ADR-0148).

#### Scenario: Quem vê a pasta padrão
- **WHEN** o administrador deixa "01 Gestão do projeto" só para o líder técnico
- **THEN** a lista das pastas padrão mostra que só o líder técnico, além da gestão, a vê, e a mudança entra na auditoria

#### Scenario: Pasta padrão nova vista por todos
- **WHEN** o administrador inclui a pasta padrão "07 Treinamentos" sem dizer quem a vê
- **THEN** a pasta é vista pelos quatro papéis

#### Scenario: Pasta de termos padrão fixa
- **WHEN** o administrador tenta abrir a pasta de termos padrão para o colaborador
- **THEN** a API responde 400 e a pasta continua só da gestão

### Requirement: Quem vê as pastas do projeto

Só o administrador MUST mudar quem vê cada pasta de um projeto, entre Líder técnico, Colaborador, Desenvolvedor e Leitor, com o projeto em aberto. A pasta de termos MUST ficar só com o administrador e o gerente. A mudança MUST entrar na auditoria, com a pasta, quem a via e quem passa a ver, e o projeto (ADR-0016, ADR-0039, ADR-0148).

#### Scenario: Administrador muda quem vê a pasta
- **WHEN** o administrador deixa "01 Gestão do projeto" do ERP-FIN só para o líder técnico
- **THEN** a pasta passa a ser vista pela gestão e pelo líder técnico, e a auditoria ganha o registro com quem via antes e depois

#### Scenario: Gerente não muda quem vê a pasta
- **WHEN** o gerente, o líder técnico, o colaborador, o desenvolvedor ou o leitor tentam mudar quem vê uma pasta do projeto
- **THEN** a API responde 403 e nada muda

#### Scenario: Quem vê de um projeto de fora
- **WHEN** quem não está no projeto e não é administrador tenta mudar quem vê uma pasta dele
- **THEN** a API responde 404

#### Scenario: Pasta de termos fixa
- **WHEN** o administrador tenta abrir a pasta de termos do projeto para o líder técnico
- **THEN** a API responde 400 e a pasta continua só da gestão

#### Scenario: Quem vê num projeto encerrado
- **WHEN** o administrador tenta mudar quem vê uma pasta de um projeto concluído
- **THEN** a API responde 409 e nada muda

#### Scenario: Pastas que já existiam
- **WHEN** a migração roda numa instalação com projetos
- **THEN** todas as pastas, padrão e dos projetos, ficam vistas pelos quatro papéis, e a de termos só pela gestão

### Requirement: Quem vê as pastas pela tela

"Nova pasta padrão" e "Editar pasta padrão" MUST ter "Quem vê", e a lista das pastas padrão MUST dizer quem vê cada uma. Na aba Documentos, o administrador MUST ter "Quem vê esta pasta" na pasta escolhida, menos na de termos, e a gestão MUST ver a marca de restrita nas pastas que algum papel não vê (ADR-0065, ADR-0148).

#### Scenario: Quem vê esta pasta pela tela
- **WHEN** o administrador escolhe "01 Gestão do projeto" na aba Documentos, abre "Quem vê esta pasta", desmarca Colaborador, Desenvolvedor e Leitor e salva
- **THEN** a pasta ganha a marca de restrita, com quem a vê, e o aviso confirma a mudança

#### Scenario: Quem vê esta pasta só do administrador
- **WHEN** o gerente ou o líder técnico escolhe uma pasta na aba Documentos
- **THEN** a tela não mostra "Quem vê esta pasta"

#### Scenario: Quem vê na pasta padrão pela tela
- **WHEN** o administrador edita uma pasta padrão e desmarca o Leitor
- **THEN** a lista das pastas padrão mostra quem vê a pasta sem o Leitor
