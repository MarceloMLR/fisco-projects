---
status: aceita
date: 2026-10-18
decision-makers: Marcelo Ribeiro
---

# ADR-0079: Arquivos num módulo próprio, com contrato único e implementação local em disco

## Contexto e problema

Os arquivos do FiscoProjects ficam no SharePoint, na pasta de cada projeto (ADR-0030), sempre atrás de uma interface, com uma implementação local para desenvolvimento e testes (ADR-0004). Vários módulos vão guardar arquivos: anexos dos chamados (ADR-0026), documentos com versões (ADR-0031), termos assinados (ADR-0035) e logos dos clientes (ADR-0015). Ainda não há tenant (ADR-0078), então o SharePoint só chega no grupo Microsoft 365 do roadmap. Onde ficam a interface e as implementações, e o que a interface oferece?

## Fatores de decisão

- Um contrato só para todos os módulos, sem cada um conhecer o SharePoint (ADR-0004, ADR-0047).
- Trocar a implementação local pela do SharePoint sem mexer em quem usa.
- O que o SharePoint oferece e os módulos precisam: pastas, versões de um mesmo arquivo e identificadores que não mudam quando alguém renomeia a pasta.
- Nunca apagar no SharePoint o que o FiscoProjects não criou (ADR-0041).

## Opções consideradas

- A. Módulo `Files`, com a interface no `Files.Contracts` e as implementações na `Infrastructure` do módulo
- B. Interface no `SharedKernel` e implementações no host
- C. Projeto compartilhado, como o `FiscoProjects.Persistence` (ADR-0075)

## Resultado da decisão

Opção escolhida: **A. Módulo `Files`**, porque segue a regra de que módulos só se conhecem pelos contratos (ADR-0047). O `SharedKernel` não ganha dependências de tecnologia, e a escolha da implementação fica num lugar só.

Regras:

- Os módulos guardam e abrem arquivos só pelo contrato `IFileStorage`, de `FiscoProjects.Files.Contracts`. O módulo `Files` não tem tabelas: o registro de cada arquivo (quem enviou, a que item pertence, status) fica no banco do módulo que o usa (ADR-0030).
- Pastas e arquivos são identificados por ids opacos, guardados por quem os usa, como no SharePoint, onde o id não muda quando a pasta é renomeada.
- O contrato oferece: garantir uma pasta (criando as que faltam), listar as subpastas, enviar um arquivo, abrir um arquivo (a versão atual ou uma anterior), excluir um arquivo com as versões e excluir uma pasta só se estiver vazia.
- Arquivo com o mesmo nome na mesma pasta: quem envia escolhe entre nova versão, com a anterior guardada (documentos, ADR-0031), e manter os dois, com outro nome (anexos).
- Nomes de arquivos e pastas são ajustados às regras do SharePoint num lugar só: caracteres não aceitos viram `_`, sem espaços nas pontas nem ponto no fim. Nenhum caminho sai da raiz configurada.
- A implementação é escolhida na configuração da instalação (`FileStorage:Provider`). Sem implementação configurada, a aplicação sobe, mas usar arquivos falha com uma mensagem que aponta a configuração. A implementação local guarda os arquivos e as versões numa pasta do disco e só vale fora da produção: a aplicação não sobe em produção com ela.
- Limites de tamanho e tipos de arquivo recusados são regras de cada uso (anexos, logos, documentos) e ficam nas changes desses módulos.

### Consequências

- Bom, porque cada módulo usa arquivos sem saber onde ficam, e os testes rodam sem Microsoft 365.
- Bom, porque a implementação do SharePoint entra depois como mais uma implementação do mesmo contrato, conferida pelos mesmos testes.
- Bom, porque excluir pasta só vazia protege o que a empresa guardou por fora na pasta do projeto (ADR-0041).
- Ruim, porque a implementação local imita versões e ids do SharePoint e pode divergir dele em detalhes. Os testes de contrato reduzem essa diferença.
- Neutro, porque acrescenta um módulo à lista do ADR-0047, como o próprio ADR-0074 prevê.

### Confirmação

Testes de contrato, que valem para qualquer implementação, cobrindo: criar pastas no caminho, enviar, abrir, nova versão com a anterior acessível, manter os dois com outro nome, excluir arquivo com as versões, excluir pasta só vazia, nomes ajustados e caminhos que tentam sair da raiz recusados. Um teste garante que a aplicação não sobe em produção com a implementação local.

## Prós e contras das opções

### A. Módulo `Files`

- Bom, porque segue a regra de contratos entre módulos e os testes de arquitetura já o verificam.
- Ruim, porque é um módulo sem tabelas, só com contrato e implementações.

### B. Interface no `SharedKernel`

- Bom, porque é o lugar mais visível para todos os módulos.
- Ruim, porque as implementações iriam para o host, e o host passaria a conhecer o Microsoft Graph.

### C. Projeto compartilhado

- Bom, porque repete o formato do `Persistence`.
- Ruim, porque o `Persistence` existe por ser técnico e sem regras de negócio; arquivos têm regras próprias (versões, nomes, exclusão segura) e configuração da instalação.
