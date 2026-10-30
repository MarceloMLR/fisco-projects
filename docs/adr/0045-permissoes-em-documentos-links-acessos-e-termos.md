---
status: aceita (termos de aceite substituídos pelo ADR-0129)
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0045: Permissões em documentos, links, acessos e termos de aceite

## Contexto e problema

Terceira parte da matriz de permissões, cobrindo documentos (ADR-0031), links (ADR-0032), acessos (ADR-0034) e termos de aceite (ADR-0035). Na revisão, Marcelo definiu que o leitor vê tudo no projeto, mas não interage, e que os termos de aceite são vistos só pelo gerente do projeto e pelos administradores.

## Fatores de decisão

- Quem trabalha no projeto mantém documentos, links e acessos no dia a dia.
- Aprovação de documentos e termos de aceite com a gestão.
- Leitor com visão completa, sem interação.
- Termos de aceite restritos à gestão.

## Opções consideradas

- A. Matriz recomendada inicialmente (leitor sem acesso aos acessos; termos visíveis a todos os membros)
- B. Matriz com os ajustes de Marcelo (leitor vê tudo, inclusive acessos; termos só para gerente e administradores)

## Resultado da decisão

Opção escolhida: **B. Matriz com os ajustes de Marcelo**.

Matriz:

| Ação | Administrador | Gerente | Líder técnico | Colaborador e Desenvolvedor | Leitor |
| --- | --- | --- | --- | --- | --- |
| Documentos: ver e baixar (exceto a pasta de termos) | ✓ | ✓ | ✓ | ✓ | ✓ |
| Enviar documentos e novas versões, vincular ao cronograma | ✓ | ✓ | ✓ | ✓ | — |
| Mudar o status para Rascunho, Em revisão ou Obsoleto | ✓ | ✓ | ✓ | ✓ | — |
| Aprovar documento | ✓ | ✓ | ✓ | — | — |
| Excluir documento (nunca um termo assinado) | ✓ | ✓ | ✓ | — | — |
| Links: ver | ✓ | ✓ | ✓ | ✓ | ✓ |
| Links: cadastrar, editar e excluir | ✓ | ✓ | ✓ | ✓ | — |
| Acessos: ver, mostrar e copiar a senha | ✓ | ✓ | ✓ | ✓ | ✓ |
| Acessos: cadastrar e editar | ✓ | ✓ | ✓ | ✓ | — |
| Acessos: excluir e importar planilha | ✓ | ✓ | ✓ | — | — |
| Termos: ver os termos e o andamento | ✓ | ✓ | — | — | — |
| Termos: pedir aceite e anexar termo recebido por fora | ✓ | ✓ | — | — | — |
| Termos: responder (enviar o PDF assinado ou recusar) | só a pessoa a quem o pedido foi endereçado, em qualquer papel | | | | |

Regras:

- A pessoa a quem um termo foi endereçado vê o próprio pedido e a própria resposta, mesmo sem ver os demais termos.
- A pasta de termos de aceite no módulo de documentos e os termos assinados seguem a mesma regra dos termos: visíveis só para o gerente, os administradores e quem assinou (o próprio termo).
- O status do marco no Gantt (atingido ou não) continua visível a todos que veem o cronograma; o conteúdo do termo, não.
- O leitor vê tudo o que a matriz permite ver, mas não cria, altera nem exclui nada.

### Consequências

- Bom, porque quem trabalha no projeto mantém documentos, links e acessos sem depender da gestão.
- Bom, porque termos de aceite ficam restritos à gestão, sem expor negociações com clientes.
- Ruim, porque a pasta de termos exige uma regra de visibilidade própria dentro do módulo de documentos.
- Neutro, porque o acesso direto ao SharePoint não segue essas regras (ADR-0030).

### Confirmação

Testes cobrindo cada célula da matriz (permitido e negado); o leitor vê documentos, links e acessos e não consegue alterar nada; líder técnico, colaborador, desenvolvedor e leitor não veem termos nem a pasta de termos, exceto o próprio termo endereçado a eles; colaborador e desenvolvedor não aprovam documentos.

## Prós e contras das opções

### A. Matriz recomendada inicialmente

- Bom, porque restringe os acessos a quem trabalha no projeto.
- Ruim, porque expõe os termos de aceite a todo o projeto.

### B. Matriz com os ajustes de Marcelo

- Bom, porque dá visão completa ao leitor e restringe os termos à gestão.
- Ruim, porque o leitor vê as senhas dos acessos.
