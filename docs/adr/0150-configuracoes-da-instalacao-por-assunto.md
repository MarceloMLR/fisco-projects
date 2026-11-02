---
status: aceita
date: 2026-11-02
decision-makers: Marcelo Ribeiro
---

# ADR-0150: Configurações da instalação por assunto, com a marca em Visual e a pasta dos projetos em Pastas padrão

Substitui a tela Cores do ADR-0146, que passa a se chamar Visual e a ter a marca, e o lugar da pasta dos projetos do ADR-0121, que sai das Configurações gerais para Pastas padrão. Mantém as regras, as validações, os valores padrão e a auditoria de cada configuração.

## Contexto e problema

As Configurações gerais juntaram, com o tempo, a marca da instalação (nome e logos), as Regras gerais, a pasta dos projetos e o Microsoft 365, com a prévia da marca ao lado e uma barra de Salvar que vale só para uma parte delas. As cores já tinham ido para uma seção própria (ADR-0146), e a marca ficou dividida: o nome e os logos numa tela, as cores em outra, cada uma com a sua prévia. Em 02/11/2026, ao testar os avisos pelo Teams, Marcelo achou a página confusa e pediu que cada configuração ficasse onde é controlada.

Onde fica cada configuração da instalação, e como cada tela as grava?

## Fatores de decisão

- Cada configuração na tela do assunto dela, onde quem procura a encontra.
- Uma tela salva não pode desfazer o que outra gravou.
- A marca é vista de uma vez, com o nome, os logos e as cores juntos.
- Nada muda nas regras, nas validações, nos valores padrão ou na auditoria.

## Opções consideradas

- A. Por assunto: Configurações gerais só com as Regras gerais e o Microsoft 365; a marca junto com as cores, numa seção Visual; a pasta dos projetos em Pastas padrão; uma gravação por tela
- B. Como hoje, só reorganizando as seções dentro das Configurações gerais
- C. Por assunto nas telas, mas com uma gravação só para todas as configurações

## Resultado da decisão

Opção escolhida: **A**, pedida por Marcelo em 02/11/2026.

- **Configurações gerais:** as Regras gerais (fuso, horário do resumo diário, guarda das notificações e da auditoria, horas por dia de trabalho), com Descartar e Salvar alterações, e o Microsoft 365 (caixa de envio e Teams), que grava na hora. Uma coluna, sem prévia.
- **Visual (antes Cores):** a Marca (nome exibido, logo e logo para o tema escuro) e as três cores, com a prévia grande mostrando tudo antes de salvar e uma barra só. O endereço antigo de Cores leva a Visual.
- **Pastas padrão:** ganha a pasta dos projetos, onde ficam as pastas dos projetos novos no armazenamento, numa linha acima da lista, com "Alterar" num diálogo. A configuração continua sendo da instalação (`Administration`); só a tela muda de lugar.
- **Gravação:** o `PUT /api/installation` fica só com as Regras gerais; o nome e a pasta dos projetos ganham rotas próprias, como as cores (`PUT /api/installation/colors`), todas só do administrador.

### Consequências

- Bom, porque cada configuração fica com o assunto dela, e as Configurações gerais ficam curtas.
- Bom, porque a marca inteira aparece numa prévia só, antes de salvar.
- Bom, porque uma tela não grava o que não mostra.
- Ruim, porque quem já conhecia o lugar antigo da marca e da pasta dos projetos precisa achar o novo; o guia de instalação passa a dizer onde está cada uma.
- Neutro, porque a auditoria registra as mudanças como antes.

### Confirmação

Testes cobrindo:

- as Configurações gerais só com as Regras gerais e o Microsoft 365;
- Visual com a marca e as cores, salvando juntos e descartando juntos, e o endereço antigo de Cores levando a Visual;
- a pasta dos projetos alterada em Pastas padrão, com a recusa no campo;
- cada rota gravando só o que é dela, só pelo administrador.

## Prós e contras das opções

### A. Por assunto, com uma gravação por tela

- Bom, porque cada configuração fica onde é controlada.
- Bom, porque cada tela grava só o que mostra.
- Ruim, porque pede rotas novas para o nome e a pasta dos projetos.

### B. Reorganizar as seções das Configurações gerais

- Bom, porque não muda a API.
- Ruim, porque a marca continua dividida entre duas telas, e a página continua longa e com gravações diferentes.

### C. Por assunto nas telas, com uma gravação só

- Bom, porque não muda a API.
- Ruim, porque cada tela teria de mandar os valores das outras, e uma tela salva poderia desfazer o que outra acabou de gravar.
