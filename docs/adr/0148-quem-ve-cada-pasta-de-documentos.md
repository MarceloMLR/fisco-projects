---
status: aceita
date: 2026-11-02
decision-makers: Marcelo Ribeiro
---

# ADR-0148: Quem vê cada pasta de documentos, por papel, nas pastas padrão e em cada projeto

Complementa o ADR-0045. Substitui a linha "Documentos: ver e baixar (exceto a pasta de termos)" da matriz dele, que passa a valer por pasta. Mantém a pasta de termos só para o administrador e o gerente (ADR-0093, ADR-0129).

## Contexto e problema

Pelo ADR-0045, todas as pessoas do projeto veem todas as pastas de documentos, menos a de termos. Marcelo notou que um colaborador vê "01 Gestão do projeto", que em geral é só da gestão. Notou também que isso varia de empresa para empresa, e até de projeto para projeto: o administrador pode querer abrir essa pasta para o líder técnico num projeto só. Como configurar quem vê cada pasta, onde e por quem?

## Fatores de decisão

- Cada empresa decide quem vê cada pasta, sem nada fixo no código (ADR-0003).
- Um projeto pode ter uma exceção sem mudar os outros.
- O administrador e o gerente continuam vendo tudo, como na matriz.
- A regra fica num lugar só e vale em toda leitura de documentos: a lista, a busca, o detalhe, o arquivo e o envio.

## Opções consideradas

Onde (escolha de Marcelo):

- A. Nas pastas padrão, para os projetos novos, e em cada projeto
- B. Só nas pastas padrão
- C. Só em cada projeto

Quem ajusta no projeto (escolha de Marcelo):

- D. Só o administrador
- E. O administrador e o gerente

Por quem (escolha de Marcelo):

- F. Por papel no projeto
- G. Por papel e por pessoa

O que se configura (escolha de Marcelo):

- H. Só ver ou não ver
- I. Ver e enviar separados

## Resultado da decisão

Opções escolhidas: **A**, **D**, **F** e **H**, todas por escolha de Marcelo.

Regras:

- **Quem vê:** cada pasta tem os papéis que a veem, entre Líder técnico, Colaborador, Desenvolvedor e Leitor. O administrador e o gerente do projeto sempre veem todas as pastas.
- **Pastas padrão:** na Administração, cada pasta padrão tem "Quem vê". A pasta nova começa com os quatro papéis, como hoje.
- **Pasta de termos:** fica só com o administrador e o gerente, sem como mudar (ADR-0129).
- **Projeto novo:** cada pasta do projeto recebe o "Quem vê" da pasta padrão na criação. Mudar a pasta padrão depois vale só para os projetos novos, como o nome hoje.
- **Ajuste no projeto:** só o administrador muda quem vê cada pasta de um projeto, menos a de termos, com o projeto em aberto. O projeto encerrado continua só leitura (ADR-0016).
- **Só ver ou não ver:** quem vê a pasta envia, aprova e exclui nela conforme o papel, pela matriz do ADR-0045. Quem não vê:
  - não vê a pasta nem os documentos dela na lista, na busca ou em "Todos";
  - não abre, não baixa e não envia nada nela;
  - recebe 404 num documento dela, como num documento que não existe;
  - não conta os documentos dela, nem na pasta nem na aba.
- **Instalações que já existem:** toda pasta, padrão ou de projeto, começa com os quatro papéis, e a de termos com nenhum. Nada muda até o administrador configurar.
- **Auditoria:** a mudança de uma pasta padrão entra como as outras mudanças dela. A de uma pasta do projeto entra num registro próprio, com o projeto como contexto (ADR-0039). As pastas do projeto continuam fora da auditoria no resto, para a criação delas não encher a auditoria.

### Consequências

- Bom, porque cada empresa e cada projeto escondem as pastas da gestão de quem não deve vê-las.
- Bom, porque a regra fica no mesmo ponto que já escondia a pasta de termos, e toda leitura de documentos passa por ele.
- Ruim, porque quem entra direto no SharePoint vê o que as permissões dele dão, sem essas regras (ADR-0030), como já acontecia com a pasta de termos.
- Neutro, porque o gerente não ajusta a visibilidade no projeto: pede ao administrador.

### Confirmação

Testes cobrindo:

- cada papel com e sem a pasta visível, na lista, na busca, no detalhe, no arquivo e no envio;
- o administrador e o gerente vendo todas;
- a pasta de termos fixa;
- o projeto novo copiando o "Quem vê" da pasta padrão;
- só o administrador mudando no projeto, e o projeto encerrado recusando;
- a mudança na auditoria;
- as pastas que já existiam com os quatro papéis.

## Prós e contras das opções

### A. Nas pastas padrão e em cada projeto

- Bom, porque a regra da empresa vem pronta, e a exceção fica no projeto.
- Ruim, porque um projeto pode ficar diferente do padrão sem que se perceba de fora dele.

### B. Só nas pastas padrão

- Bom, porque é uma regra só.
- Ruim, porque não permite a exceção de um projeto.

### C. Só em cada projeto

- Bom, porque é flexível.
- Ruim, porque cada projeto novo precisaria ser configurado.

### D. Só o administrador

- Bom, porque a visibilidade fica com quem responde pela instalação.
- Ruim, porque o gerente depende do administrador para uma exceção.

### E. O administrador e o gerente

- Bom, porque o gerente resolve sozinho.
- Ruim, porque o gerente poderia abrir uma pasta que a empresa quer fechada.

### F. Por papel

- Bom, porque segue a matriz e os papéis que já existem.
- Ruim, porque uma pessoa específica só vê uma pasta trocando de papel.

### G. Por papel e por pessoa

- Bom, porque cobre qualquer exceção.
- Ruim, porque é mais uma lista para manter, e a pessoa que sai do projeto ficaria nela.

### H. Só ver ou não ver

- Bom, porque é simples e o resto continua a matriz.
- Ruim, porque não permite só leitura numa pasta para quem envia nas outras.

### I. Ver e enviar separados

- Bom, porque dá controle fino.
- Ruim, porque dobra as escolhas de cada pasta.
