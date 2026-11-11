---
status: aceita
date: 2026-11-10
decision-makers: Marcelo Ribeiro
---

# ADR-0157: Comentários da tarefa

Segue o ADR-0026 e o ADR-0069 (texto formatado e menções da conversa dos chamados), o ADR-0037 e o ADR-0038 (avisos na hora e canais), o ADR-0149 e o ADR-0151 (Teams e os canais de cada pessoa) e o ADR-0043 (permissões no cronograma).

## Contexto e problema

O que a equipe conversa sobre uma tarefa fica fora do sistema: a nota da tarefa (ADR-0139) é um texto só, de quem edita o cronograma, sem dizer quem escreveu nem quando, e o chamado é para pedir atendimento, não para combinar o trabalho de uma tarefa. Em 10/11/2026, com o quadro das tarefas (ADR-0155), Marcelo pediu os comentários em cada tarefa, com menções e o aviso pelo Teams, e aprovou o plano da Tarefas-3. Onde ficam os comentários, quem escreve, quem apaga e quem fica sabendo?

## Fatores de decisão

- A conversa da tarefa fica junto dela, para quem vê o projeto.
- O mesmo editor, o mesmo texto e as mesmas menções dos chamados, sem uma regra nova de texto.
- Quem é mencionado e o responsável ficam sabendo na hora, pelo canal que escolheram (ADR-0151).
- Nada que a auditoria ou a anonimização deixem de cobrir.

## Opções consideradas

- A. Comentários próprios em cada tarefa e marco, com o editor e as menções dos chamados
- B. A nota da tarefa como conversa
- C. Um chamado para cada assunto da tarefa

## Resultado da decisão

Opção escolhida: **A**, aprovada por Marcelo. A nota não diz quem escreveu nem quando e é só de quem edita (B); o chamado tem fluxo, prazos e times, que uma conversa sobre a tarefa não precisa (C).

Regras:

| Item | Definição |
| --- | --- |
| Onde | A seção "Comentários" no detalhe de cada tarefa (também a resumo e a subtarefa) e de cada marco, aberto pela Lista, pela Linha do tempo e pelo Quadro; do mais antigo para o mais novo |
| Quem vê | Todos que veem o projeto, inclusive o Leitor |
| Quem comenta | Todos do projeto menos o Leitor, e o administrador; só com o projeto em aberto |
| Texto | O texto formatado dos chamados (ADR-0026): parágrafos, listas, negrito, links e menções, até 20.000 caracteres; sem anexos e sem editar depois |
| Menções | As pessoas do projeto, em qualquer papel; outra pessoa dá 400 |
| Apagar | Quem escreveu apaga o seu, e o administrador e o gerente apagam qualquer um, com o projeto em aberto; o comentário apagado sai de vez |
| Contagem | O cartão do Quadro e a Lista mostram quantos comentários o item tem |
| Avisos | Na hora, a quem foi mencionado e ao responsável da tarefa (o marco não tem responsável), um aviso por pessoa, a menção no lugar do aviso ao responsável, nunca a quem escreveu: o e-mail e o Teams conforme a escolha da pessoa, e o sino sempre; o link abre o item no cronograma |
| Teams | Dois tipos novos de atividade no pacote do aplicativo, cuja versão sobe |
| Guarda | Fora da auditoria, como a conversa dos chamados (ADR-0039); o comentário de um item excluído fica guardado e volta com o desfazer (ADR-0091); sai com o projeto excluído (ADR-0041); na anonimização, as menções da pessoa passam a dizer o identificador, e o autor aparece pelo nome atual (ADR-0110) |

### Consequências

- Bom, porque a conversa da tarefa fica com ela, e quem precisa saber recebe o aviso.
- Bom, porque o texto, as menções e os canais são os mesmos dos chamados e dos avisos.
- Ruim, porque o pacote do Teams precisa ser subido de novo; até lá, os avisos no Teams ficam Falhou, e o e-mail e o sino seguem.
- Ruim, porque o comentário apagado não deixa rastro, como a conversa dos chamados não entra na auditoria.

### Confirmação

Testes da API: comentar, listar, apagar, as permissões por papel, o projeto encerrado, a menção fora do projeto, os avisos (menção, responsável, quem escreveu) também no Teams, a contagem no cronograma, a exclusão do projeto e a anonimização. Testes de tela da seção, da contagem e do texto do sino.

## Prós e contras das opções

### A. Comentários próprios

- Bom, porque cada comentário tem autor, data e menções.
- Ruim, porque é mais uma tabela no módulo dos projetos.

### B. Nota como conversa

- Bom, porque já existe.
- Ruim, porque não guarda quem disse nem quando, e só quem edita escreve.

### C. Chamado por assunto

- Bom, porque já tem conversa, anexos e avisos.
- Ruim, porque traz fluxo, prazos e times para uma conversa de trabalho.
