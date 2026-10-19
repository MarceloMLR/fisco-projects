---
status: proposta
date: 2026-10-19
decision-makers: Marcelo Ribeiro
---

# ADR-0084: Referências a outros itens na auditoria, guardadas pelo identificador e mostradas pelo nome atual

## Contexto e problema

A auditoria grava cada campo alterado como texto (ADR-0039, ADR-0075). Com os projetos, aparecem campos que apontam para outro item: o cliente do projeto e, nas pessoas do projeto, a pessoa e o projeto. Gravados como estão, esses campos mostram identificadores (GUID) na tela de Auditoria, e o ADR-0072 pede que a auditoria mostre quem foi definido como gerente. O mesmo vai acontecer com o responsável e o solicitante dos chamados, as tarefas e os documentos.

Como a auditoria mostra um campo que aponta para outro item, sem que o módulo Audit conheça as tabelas dos outros módulos (ADR-0047)?

## Fatores de decisão

- A tela de Auditoria mostra nomes, não identificadores.
- Nenhum módulo lê as tabelas de outro; tudo passa pelos contratos (ADR-0047).
- A anonimização (ADR-0058) troca o nome de uma pessoa em todo lugar, inclusive no histórico.
- A marcação fica na configuração de cada módulo, como os campos técnicos e secretos (ADR-0039).

## Opções consideradas

- A. Guardar o identificador e o tipo do item; a Auditoria pede os nomes aos módulos donos ao listar
- B. Copiar o nome do item no registro, no momento da alteração
- C. Guardar o identificador e o nome do momento

## Resultado da decisão

Opção escolhida: **A**, porque a auditoria continua sem dado pessoal copiado e mostra o nome atual, inclusive o anonimizado.

Regras:

- **Marcação:** cada módulo marca, na própria configuração, o campo que aponta para outro item e o tipo desse item (por exemplo, `User` ou `Client`). O rótulo do item também pode ser uma referência, como a pessoa incluída num projeto.
- **Registro:** o registro guarda o identificador e o tipo, sem o nome.
- **Nomes:** cada módulo dono de um tipo oferece os nomes por um contrato comum: o `Users` dá os nomes das pessoas e o `Projects`, os dos clientes e dos projetos. A Auditoria pede os nomes de uma página inteira de uma vez, por tipo.
- **Item que não existe mais:** a tela mostra "—", como um valor vazio. Só os itens que podem ser excluídos ficam assim, como um cliente sem projetos.

### Consequências

- Bom, porque a tela mostra nomes sem copiar dados pessoais na auditoria.
- Bom, porque a anonimização de uma pessoa vale também no histórico.
- Bom, porque os próximos módulos só marcam os campos e oferecem os nomes dos seus tipos.
- Ruim, porque um item renomeado aparece com o nome novo nos registros antigos.
- Ruim, porque um item excluído aparece como "—" nos registros antigos que apontam para ele.

### Confirmação

Testes cobrindo:

- o campo marcado é gravado com o identificador e o tipo;
- a lista da Auditoria mostra o nome atual no campo e no rótulo;
- um item excluído aparece sem nome;
- um tipo sem fonte de nomes não quebra a lista.

## Prós e contras das opções

### A. Identificador e tipo, com o nome pedido ao listar

- Bom, porque não copia dados e segue a anonimização.
- Ruim, porque o nome antigo de um item renomeado se perde nos registros.

### B. Nome copiado no momento

- Bom, porque o registro mostra o nome da época.
- Ruim, porque copia nomes de pessoas na auditoria, que a anonimização teria de percorrer.
- Ruim, porque o módulo precisaria dos nomes de outros módulos durante o salvamento.

### C. Identificador e nome do momento

- Bom, porque guarda as duas informações.
- Ruim, porque tem os dois problemas de B e um registro maior.
