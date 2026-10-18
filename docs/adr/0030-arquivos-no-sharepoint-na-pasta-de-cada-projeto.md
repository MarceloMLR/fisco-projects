---
status: aceita
date: 2026-10-14
decision-makers: Marcelo Ribeiro
---

# ADR-0030: Arquivos no SharePoint da empresa, na pasta de cada projeto, com as permissões já existentes

## Contexto e problema

O ideia.md coloca os documentos no SharePoint, com uma pasta por projeto, porque o time já usa o SharePoint. Também é preciso guardar os anexos dos chamados (ADR-0026) e os logos dos clientes (ADR-0015). Os arquivos ficam atrás de uma interface (ADR-0004). Onde ficam os arquivos e quem pode abri-los fora do FiscoProjects?

A empresa de referência já tem um site do SharePoint com uma área de Projetos, com uma pasta por projeto, acessível só a quem tem as permissões corretas. Pelo ADR-0003, o local exato precisa ser configurável para qualquer empresa.

## Fatores de decisão

- Usar a estrutura e as permissões de SharePoint que a empresa já tem.
- Menor privilégio para a aplicação no SharePoint.
- Não estourar limites do SharePoint (permissões exclusivas por biblioteca).
- Um só lugar para os arquivos, coerente com o ADR-0004.

## Opções consideradas

Onde guardar:

- A. Tudo no SharePoint (documentos, anexos e logos)
- B. Documentos no SharePoint; anexos e logos em armazenamento da aplicação (por exemplo, Azure Blob Storage)
- C. Tudo em armazenamento da aplicação, sem SharePoint

Como controlar o acesso direto no SharePoint (dentro da opção A):

- A1. Site dedicado ao FiscoProjects, privado
- A2. Pasta privada do FiscoProjects em cada projeto, criada pela aplicação (exige que a aplicação gerencie permissões)
- A3. Pastas dentro da área de projetos já existente, herdando as permissões que o administrador do SharePoint já mantém

## Resultado da decisão

Opção escolhida: **A. Tudo no SharePoint**, com **A3. herança das permissões da área de projetos existente**, porque aproveita a estrutura e as permissões que a empresa já mantém, e a aplicação só precisa escrever.

Regras:

- O local dos arquivos é configurado na instalação: site, biblioteca e pasta raiz (pode ser uma área já existente).
- Cada projeto é ligado a uma pasta, escolhida entre as existentes ou criada pelo sistema com o código e o nome do projeto.
- Dentro da pasta do projeto, o sistema cria a estrutura padrão, com nomes configuráveis: Chamados (uma pasta por chamado, por exemplo #101), Documentos com as pastas padrão e as demais pastas que os módulos definirem.
- Os logos dos clientes ficam numa pasta do FiscoProjects dentro da pasta raiz configurada.
- As pastas criadas herdam as permissões da área de projetos; quem abre os arquivos direto no SharePoint é definido pelo administrador do SharePoint.
- A aplicação só lê e escreve arquivos (Sites.Selected com papel de escrita, limitado à biblioteca configurada quando possível); ela não gerencia permissões na v1.
- No FiscoProjects valem as regras do sistema: cada pessoa vê pelo sistema só o que pode ver.
- O FiscoProjects mostra apenas os arquivos enviados por ele (registrados no banco); arquivos que já existiam nas pastas não aparecem na v1.
- Status do documento e vínculo com o cronograma ficam no banco do FiscoProjects; o SharePoint guarda os arquivos e o histórico de versões.
- Em desenvolvimento e nos testes, os arquivos ficam numa implementação local em disco (ADR-0004); o desenvolvimento usa um tenant próprio, nunca o SharePoint da empresa (ADR-0002).

Escopo:

| Item | Classificação |
| --- | --- |
| Documentos, anexos e logos no SharePoint, no local configurado | v1 |
| Estrutura padrão criada na pasta de cada projeto, com uma pasta por chamado | v1 |
| Aplicação só com permissão de escrita | v1 |
| Gerenciar permissões pela aplicação (pastas privadas ou sincronizadas com o sistema) | depois |
| Importar ou exibir arquivos que já existiam nas pastas | depois |

### Consequências

- Bom, porque usa a estrutura e as permissões que a empresa já mantém.
- Bom, porque a aplicação tem o menor privilégio possível (só escrita) e não cria permissões exclusivas, sem risco de atingir os limites do SharePoint.
- Ruim, porque quem acessa a área de projetos direto no SharePoint vê os arquivos de todos os projetos e chamados ali guardados, sem as regras do FiscoProjects (por exemplo, anexos de um chamado que essa pessoa não veria no sistema). O guia de instalação deve orientar o administrador a restringir a área a quem realmente precisa.
- Ruim, porque tudo depende do Microsoft Graph e consome a cota de armazenamento do SharePoint da empresa.

### Confirmação

Testes cobrindo: criar um projeto cria a estrutura padrão na pasta escolhida ou criada; anexos de um chamado vão para a pasta desse chamado; arquivos não registrados pelo sistema não aparecem; quem não vê um chamado no sistema não consegue baixar seus anexos pelo sistema; os testes rodam com a implementação local, sem Microsoft 365. Revisão de código: a aplicação não chama APIs de gerenciamento de permissões do SharePoint.

## Prós e contras das opções

### A. Tudo no SharePoint

- Bom, porque é um só lugar, com versões e retenção do Microsoft 365.
- Ruim, porque depende do Graph e da cota do SharePoint.

### B. Documentos no SharePoint, anexos e logos em armazenamento da aplicação

- Bom, porque os arquivos pequenos ficam num armazenamento simples.
- Ruim, porque são dois armazenamentos para configurar, manter e pagar.

### C. Tudo em armazenamento da aplicação

- Bom, porque é o mais simples tecnicamente.
- Ruim, porque perde a integração com o SharePoint que o time já usa.

### A1. Site dedicado e privado

- Bom, porque isola os arquivos do FiscoProjects.
- Ruim, porque ignora a estrutura de projetos que a empresa já tem.

### A2. Pasta privada por projeto, criada pela aplicação

- Bom, porque protege os arquivos de quem acessa o SharePoint direto.
- Ruim, porque a aplicação precisaria gerenciar permissões (mais privilégio) e cada projeto consumiria uma permissão exclusiva.

### A3. Herdar as permissões da área de projetos

- Bom, porque usa o controle de acesso que a empresa já mantém e a aplicação só escreve.
- Ruim, porque o acesso direto no SharePoint não segue as regras do FiscoProjects.

## Mais informações

- Sites.Selected: https://devblogs.microsoft.com/microsoft365dev/controlling-app-access-on-specific-sharepoint-site-collections/
- Limites do SharePoint (permissões exclusivas por biblioteca): https://learn.microsoft.com/office365/servicedescriptions/sharepoint-online-service-description/sharepoint-online-limits
