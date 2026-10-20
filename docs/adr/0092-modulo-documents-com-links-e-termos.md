---
status: aceita
date: 2026-10-20
decision-makers: Marcelo Ribeiro
---

# ADR-0092: Documentos, links e termos de aceite no módulo `Documents`, e os acessos no `Credentials`

Define o módulo dos links e dos termos de aceite, que o discovery deixou para a change ("Módulo Documentos ou Projetos").

## Contexto e problema

O ADR-0047 e o ADR-0074 preveem os módulos `Documents` e `Credentials`. O discovery deixou em aberto onde ficam os links do projeto (ADR-0032) e os termos de aceite (ADR-0035): no módulo de documentos ou no de projetos.

O que se sabe de cada um:

- O termo assinado é um documento: fica na pasta de termos, vinculado ao marco, e não pode ser excluído (ADR-0031, ADR-0035).
- A pasta de termos tem a mesma regra de visibilidade dos termos (ADR-0045).
- Os links são dados do banco, sem arquivos, com categorias de uma lista da Administração, como as pastas padrão (ADR-0032).
- O `Projects` já reúne clientes, projetos, membros, cronograma, linha de base, desfazer, exportação e modelos.

Em que módulo ficam os links e os termos de aceite?

## Fatores de decisão

- A regra do termo assinado e da pasta de termos num lugar só, sem um contrato só para isso.
- Módulos com um assunto claro, sem um módulo para cada tabela.
- Não aumentar o `Projects`, que já é o maior módulo.
- Os módulos se falam só pelos contratos (ADR-0047).

## Opções consideradas

- A. Documentos, links e termos no `Documents`; acessos no `Credentials`
- B. Links e termos no `Projects`; documentos no `Documents`; acessos no `Credentials`
- C. Um módulo para cada assunto: `Documents`, `Links`, `Acceptances` e `Credentials`

## Resultado da decisão

Opção escolhida: **A**, porque o termo assinado e a pasta de termos ficam no mesmo módulo dos documentos, e o `Projects` não cresce.

Regras:

- **`Documents`** (schema `documents`):
  - as pastas padrão e as pastas de cada projeto, os documentos e as versões;
  - os links e as categorias de links;
  - os pedidos de aceite e os termos assinados;
  - as listas da Administração "Pastas padrão" e "Categorias de links".
- **`Credentials`** (schema `credentials`):
  - os acessos a portais;
  - o conjunto de chaves do Data Protection usado nas senhas (ADR-0033).
- **O que vem do `Projects`:** as pessoas e o papel delas pelo `IProjectAccess`, e o cronograma por contratos novos do `Projects.Contracts`:
  - as fases, tarefas e marcos, para o vínculo do documento e o marco do termo;
  - marcar o marco como atingido quando o termo é assinado.
- **Pastas de um projeto novo:** o `Projects` avisa a criação do projeto por um contrato. O `Documents` cria então as pastas a partir das pastas padrão. Um projeto criado antes do módulo recebe as pastas na primeira vez que a área de documentos é aberta.

### Consequências

- Bom, porque o termo assinado, a pasta de termos e as regras de exclusão e de visibilidade ficam juntos.
- Bom, porque o `Projects` não ganha mais assuntos.
- Bom, porque as senhas e as chaves ficam isoladas num módulo próprio.
- Ruim, porque o `Documents` depende de dois contratos novos do `Projects`.
- Neutro, porque os links não têm arquivos, mas são conteúdo do projeto como os documentos.

### Confirmação

- Os testes de arquitetura passam a verificar os módulos `Documents` e `Credentials`.
- Nenhum dos dois lê tabelas de outro módulo.
- Testes de que o projeto novo recebe as pastas padrão.

## Prós e contras das opções

### A. Documentos, links e termos no `Documents`

- Bom, porque a regra do termo assinado e da pasta de termos fica num lugar só.
- Ruim, porque o módulo junta três assuntos.

### B. Links e termos no `Projects`

- Bom, porque o marco e o termo ficam no mesmo módulo.
- Ruim, porque o termo assinado e a pasta de termos precisariam de um contrato com o `Documents` para a regra de exclusão e de visibilidade.
- Ruim, porque o `Projects` cresce ainda mais.

### C. Um módulo para cada assunto

- Bom, porque cada módulo fica pequeno.
- Ruim, porque o termo e o documento precisariam de contratos entre si.
- Ruim, porque o módulo de links teria duas tabelas.
